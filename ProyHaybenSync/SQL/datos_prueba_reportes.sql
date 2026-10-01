/*========================================================
  DATOS DE PRUEBA PARA LOS REPORTES
  Base de datos : SistemaVentas
  Script        : datos_prueba_reportes.sql

  Que hace
  --------
  Genera ventas historicas repartidas en los ultimos 30
  dias para que los 10 reportes tengan informacion que
  mostrar: varios dias, varios usuarios, los tres metodos
  de pago, los dos tipos de precio, descuentos y ventas
  anuladas.

  Como reconocerlas
  -----------------
  Todas las ventas falsas tienen el NumeroVenta con
  prefijo 'PRUEBA-'. Para borrarlas por completo:

      DELETE d
      FROM DetalleVentaPOS d
      INNER JOIN VentaPOS v ON v.IdVenta = d.IdVenta
      WHERE v.NumeroVenta LIKE 'PRUEBA-%';

      DELETE FROM VentaPOS
      WHERE NumeroVenta LIKE 'PRUEBA-%';

  Que NO hace a proposito
  -----------------------
  NO descuenta stock. StockSucursal es informacion real de
  inventario y los reportes de stock deben seguir
  reflejando la cantidad verdadera. Por eso los datos de
  prueba se insertan directo en VentaPOS y
  DetalleVentaPOS, y no con sp_POS_RegistrarVenta (que si
  descuenta y ademas fecha todo con GETDATE(), lo que
  impediria probar los filtros de periodo).

  NO crea dbo.MovimientoStock. Esa tabla no existe todavia
  en la base, asi que el reporte de Kardex seguira
  devolviendo cero filas (ver seccion 6.2 del documento de
  cambios).

  Re-ejecutable: si, borra lo PRUEBA- antes de insertar.
  Todo va en un solo lote porque la tabla temporal con las
  lineas tiene que sobrevivir entre los INSERT.
===========================================================*/

USE SistemaVentas;
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

/*========================================================
  PASO 1 - BORRAR CUALQUIER CARGA ANTERIOR
===========================================================*/

DELETE d
FROM dbo.DetalleVentaPOS d
INNER JOIN dbo.VentaPOS v
    ON v.IdVenta = d.IdVenta
WHERE v.NumeroVenta LIKE 'PRUEBA-%';

DELETE FROM dbo.VentaPOS
WHERE NumeroVenta LIKE 'PRUEBA-%';

/*========================================================
  PASO 2 - CLIENTES
  ClientePOS esta vacia, asi que el reporte de detalle de
  ventas mostraria siempre 'Otros clientes'.
===========================================================*/

IF NOT EXISTS (
    SELECT 1
    FROM dbo.ClientePOS
    WHERE TipoDocumento = 'OTROS')
BEGIN
    INSERT INTO dbo.ClientePOS(
        TipoDocumento,
        NumeroDocumento,
        NombreCompleto)
    VALUES(
        'OTROS',
        NULL,
        'Otros clientes');
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.ClientePOS
    WHERE NumeroDocumento = '70123456')
BEGIN
    INSERT INTO dbo.ClientePOS(
        TipoDocumento,
        NumeroDocumento,
        NombreCompleto)
    VALUES
        ('DNI', '70123456', 'Cliente Prueba Uno'),
        ('DNI', '45987654', 'Cliente Prueba Dos'),
        ('DNI', '10203040', 'Cliente Prueba Tres');
END;

/*========================================================
  PASO 3 - CATALOGOS PARA ROTAR
===========================================================*/

DECLARE @variantes TABLE
(
    rn INT,
    IdVariante INT,
    IdProducto INT,
    NombreProducto VARCHAR(150),
    Categoria VARCHAR(100),
    CodigoBarra VARCHAR(50),
    PrecioUnidad DECIMAL(10, 2),
    PrecioMayor DECIMAL(10, 2)
);

INSERT INTO @variantes
SELECT
    ROW_NUMBER() OVER (ORDER BY v.IdVariante),
    v.IdVariante,
    v.id_producto,
    p.nombre,
    ISNULL(c.Nombre, 'Sin categoria'),
    RTRIM(v.CodigoBarras),
    v.PrecioUnidad,
    v.PrecioMayor
FROM dbo.Variantes v
INNER JOIN dbo.productos p
    ON p.id_producto = v.id_producto
LEFT JOIN dbo.ProductoInventario pi
    ON pi.id_producto = p.id_producto
LEFT JOIN dbo.Categorias c
    ON c.IdCategoria = pi.IdCategoria
WHERE v.Estado = 1
  AND p.estado = 1;

IF NOT EXISTS (SELECT 1 FROM @variantes)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(
        'No hay variantes activas: no se pueden generar datos de prueba.',
        16, 1);
    RETURN;
END

DECLARE @usuarios TABLE (rn INT, IdUsuario INT);

INSERT INTO @usuarios
SELECT
    ROW_NUMBER() OVER (ORDER BY u.id_usuario),
    u.id_usuario
FROM dbo.usuarios u
WHERE u.estado = 1;

IF NOT EXISTS (SELECT 1 FROM @usuarios)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(
        'No hay usuarios activos: no se pueden generar datos de prueba.',
        16, 1);
    RETURN;
END

DECLARE @clientes TABLE (rn INT, IdClientePOS INT);

/* El cliente OTROS queda al final de la rotacion. */
INSERT INTO @clientes
SELECT
    ROW_NUMBER() OVER (
        ORDER BY CASE WHEN c.TipoDocumento = 'OTROS'
                      THEN 1 ELSE 0 END, c.IdClientePOS),
    c.IdClientePOS
FROM dbo.ClientePOS c;

/*========================================================
  PASO 4 - ARMAR LAS LINEAS
  48 ventas, cada una con entre 1 y 3 lineas.

  El reparto usa MOD y no RAND a proposito: al borrar y
  volver a correr el script sale exactamente el mismo
  conjunto de datos.
===========================================================*/

DECLARE @lineas TABLE
(
    NumeroVenta VARCHAR(30),
    IdUsuario INT,
    MetodoPago VARCHAR(30),
    FechaVenta DATETIME,
    Estado VARCHAR(20),
    IdClientePOS INT,
    IdProducto INT,
    IdVariante INT,
    NombreProducto VARCHAR(150),
    Categoria VARCHAR(100),
    CodigoBarra VARCHAR(50),
    TipoPrecio VARCHAR(20),
    PrecioUnitario DECIMAL(10, 2),
    Cantidad INT,
    Descuento DECIMAL(10, 2),
    TotalLinea DECIMAL(10, 2)
);

WITH numeros AS
(
    SELECT TOP (48)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a
    CROSS JOIN sys.all_objects b
)
INSERT INTO @lineas
SELECT
    'PRUEBA-' + FORMAT(n.n, '000'),
    u.IdUsuario,
    CASE (n.n - 1) % 3
        WHEN 0 THEN 'EFECTIVO'
        WHEN 1 THEN 'YAPE_PLIN'
        ELSE 'TARJETA'
    END,
    /*
     * Se parte de la medianoche del dia y se suman minutos
     * en el rango 08:00 a 17:59 para que las ventas caigan
     * en horario de tienda.
     */
    DATEADD(
        MINUTE,
        480 + ((n.n * 37) % 600),
        DATEADD(
            DAY,
            -((n.n * 7) % 30),
            CAST(CAST(GETDATE() AS DATE) AS DATETIME))),
    CASE WHEN n.n % 17 = 0
         THEN 'ANULADA' ELSE 'COMPLETADA' END,
    cl.IdClientePOS,
    v.IdProducto,
    v.IdVariante,
    v.NombreProducto,
    v.Categoria,
    v.CodigoBarra,
    CASE
        WHEN (n.n + k.k) % 4 = 0
         AND v.PrecioMayor IS NOT NULL
        THEN 'MAYOR'
        ELSE 'UNIDAD'
    END,
    CASE
        WHEN (n.n + k.k) % 4 = 0
         AND v.PrecioMayor IS NOT NULL
        THEN v.PrecioMayor
        ELSE v.PrecioUnidad
    END,
    1 + ((n.n + k.k) % 3),
    CASE WHEN (n.n * k.k) % 5 = 0
         THEN 5.00 ELSE 0.00 END,
    CAST(0 AS DECIMAL(10, 2))
FROM numeros n
CROSS APPLY (SELECT 1 AS k
             UNION ALL SELECT 2
             UNION ALL SELECT 3) k
CROSS APPLY (
    SELECT TOP 1 *
    FROM @usuarios
    WHERE rn = ((n.n - 1) % (SELECT COUNT(*) FROM @usuarios)) + 1
    ORDER BY rn) u
CROSS APPLY (
    SELECT TOP 1 *
    FROM @variantes
    WHERE rn = ((n.n + k.k - 2) % (SELECT COUNT(*) FROM @variantes)) + 1
    ORDER BY rn) v
CROSS APPLY (
    SELECT TOP 1 *
    FROM @clientes
    WHERE rn = ((n.n - 1) % (SELECT COUNT(*) FROM @clientes)) + 1
    ORDER BY rn) cl
WHERE k.k <= 1 + (n.n % 3);

/* Total de linea = precio por cantidad menos descuento. */
UPDATE @lineas
SET TotalLinea =
    CAST(
        PrecioUnitario * Cantidad - Descuento
        AS DECIMAL(10, 2));

/*========================================================
  PASO 5 - CABECERAS
  IGV = Subtotal * 0.05 SOLO cuando el metodo de pago es
  TARJETA. Es el recargo por uso de tarjeta, no el IGV
  tributario: asi lo calcula VentaPOSControl.
===========================================================*/

INSERT INTO dbo.VentaPOS(
    NumeroVenta,
    IdClientePOS,
    id_usuario,
    MetodoPago,
    Subtotal,
    IGV,
    Total,
    Estado,
    FechaVenta)
SELECT
    l.NumeroVenta,
    l.IdClientePOS,
    l.IdUsuario,
    l.MetodoPago,
    l.Subtotal,
    l.Recargo,
    l.Subtotal + l.Recargo,
    l.Estado,
    l.FechaVenta
FROM (
    SELECT
        l2.NumeroVenta,
        MAX(l2.IdClientePOS) AS IdClientePOS,
        MAX(l2.IdUsuario) AS IdUsuario,
        MAX(l2.MetodoPago) AS MetodoPago,
        MAX(l2.Estado) AS Estado,
        MAX(l2.FechaVenta) AS FechaVenta,
        SUM(l2.TotalLinea) AS Subtotal,
        CASE WHEN MAX(l2.MetodoPago) = 'TARJETA'
             THEN CAST(ROUND(SUM(l2.TotalLinea) * 0.05, 2)
                       AS DECIMAL(10, 2))
             ELSE CAST(0 AS DECIMAL(10, 2))
        END AS Recargo
    FROM @lineas l2
    GROUP BY l2.NumeroVenta) l;

/*========================================================
  PASO 6 - DETALLE
  El IdVenta se resuelve por NumeroVenta, que es unico.
===========================================================*/

INSERT INTO dbo.DetalleVentaPOS(
    IdVenta,
    id_producto,
    IdVariante,
    NombreProducto,
    Categoria,
    CodigoBarra,
    TipoPrecio,
    PrecioUnitario,
    Cantidad,
    Descuento,
    TotalLinea)
SELECT
    v.IdVenta,
    l.IdProducto,
    l.IdVariante,
    l.NombreProducto,
    l.Categoria,
    l.CodigoBarra,
    l.TipoPrecio,
    l.PrecioUnitario,
    l.Cantidad,
    l.Descuento,
    l.TotalLinea
FROM @lineas l
INNER JOIN dbo.VentaPOS v
    ON v.NumeroVenta = l.NumeroVenta;

COMMIT TRANSACTION;
GO

/*========================================================
  PASO 7 - RESUMEN
===========================================================*/

SELECT
    COUNT(*) AS VentasInsertadas,
    COUNT(DISTINCT CAST(FechaVenta AS DATE)) AS DiasCubiertos,
    SUM(Total) AS TotalGenerado,
    MIN(FechaVenta) AS VentaMasAntigua,
    MAX(FechaVenta) AS VentaMasReciente
FROM dbo.VentaPOS
WHERE NumeroVenta LIKE 'PRUEBA-%';

SELECT
    COUNT(*) AS LineasInsertadas
FROM dbo.DetalleVentaPOS d
INNER JOIN dbo.VentaPOS v
    ON v.IdVenta = d.IdVenta
WHERE v.NumeroVenta LIKE 'PRUEBA-%';

SELECT MetodoPago, Estado, COUNT(*) AS Ventas, SUM(Total) AS Total
FROM dbo.VentaPOS
WHERE NumeroVenta LIKE 'PRUEBA-%'
GROUP BY MetodoPago, Estado
ORDER BY MetodoPago, Estado;
GO