/*========================================================
  PROCEDIMIENTOS ALMACENADOS DE REPORTES
  Base de datos : SistemaVentas
  Script        : sp_reportes_SistemaVentas.sql
  Compatible    : SQL Server 2016 o superior
                  (CREATE OR ALTER requiere 2016)
  Re-ejecutable : si (usa CREATE OR ALTER)

  NOTA IMPORTANTE SOBRE VentaPOS.IGV
  -----------------------------------
  En el modulo POS la columna VentaPOS.IGV NO almacena
  el IGV tributario peruano (18%). Almacena el RECARGO
  POR TARJETA, que VentaPOSControl.xaml.cs calcula como
  Subtotal * 0.05 y solo aplica cuando el metodo de pago
  es 'TARJETA'. Por lo tanto:

      Total = Subtotal + IGV(recargo 5%)

  En los reportes y en la interfaz este valor se expone
  con el nombre "RecargoTarjeta" para no confundirlo con
  el IGV tributario.
========================================================*/
USE SistemaVentas;
GO

/*========================================================
  INDICES PARA REPORTES
  VentaPOS y DetalleVentaPOS solo tenian PRIMARY KEY
  sobre IdVenta / IdDetalle, por lo que cualquier filtro
  por FechaVenta(o por IdVenta en el detalle) terminaba
  en un SCAN completo de tabla.
========================================================*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_VentaPOS_FechaVenta'
      AND object_id = OBJECT_ID('dbo.VentaPOS'))
BEGIN
    CREATE INDEX IX_VentaPOS_FechaVenta
        ON dbo.VentaPOS(FechaVenta);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_VentaPOS_Usuario_Fecha'
      AND object_id = OBJECT_ID('dbo.VentaPOS'))
BEGIN
    CREATE INDEX IX_VentaPOS_Usuario_Fecha
        ON dbo.VentaPOS(id_usuario, FechaVenta);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DetalleVentaPOS_IdVenta'
      AND object_id = OBJECT_ID('dbo.DetalleVentaPOS'))
BEGIN
    CREATE INDEX IX_DetalleVentaPOS_IdVenta
        ON dbo.DetalleVentaPOS(IdVenta);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DetalleVentaPOS_IdProducto'
      AND object_id = OBJECT_ID('dbo.DetalleVentaPOS'))
BEGIN
    CREATE INDEX IX_DetalleVentaPOS_IdProducto
        ON dbo.DetalleVentaPOS(id_producto);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_StockSucursal_IdVariante'
      AND object_id = OBJECT_ID('dbo.StockSucursal'))
BEGIN
    CREATE INDEX IX_StockSucursal_IdVariante
        ON dbo.StockSucursal(IdVariante, TipoUbicacion);
END;
GO

IF OBJECT_ID('dbo.MovimientoStock', 'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = 'IX_MovimientoStock_Fecha'
         AND object_id = OBJECT_ID('dbo.MovimientoStock'))
BEGIN
    CREATE INDEX IX_MovimientoStock_Fecha
        ON dbo.MovimientoStock(FechaMovimiento);
END;
GO

/*========================================================
  CATALOGOS PARA LOS FILTROS DE LA PANTALLA DE REPORTES
  Son distintas de sp_ListarCategorias y
  sp_ListarUsuariosActivos porque un reporte debe poder
  filtrar por categorias y usuarios INACTIVOS: si un
  vendedor fue dado de baja el mes pasado, sus ventas
  siguen existiendo y deben poder consultarse.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_Usuarios
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario AS IdUsuario,
        u.nombre AS Nombre,
        u.usuario AS Usuario,
        r.nombre AS Rol,
        CAST(u.estado AS INT) AS Estado,
        (SELECT COUNT(*)
         FROM dbo.VentaPOS v
         WHERE v.id_usuario = u.id_usuario) AS CantidadVentas
    FROM dbo.usuarios u
    INNER JOIN dbo.roles r
        ON r.id_rol = u.id_rol
    ORDER BY u.nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_Categorias
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.IdCategoria,
        c.Nombre,
        CAST(c.Estado AS INT) AS Estado
    FROM dbo.Categorias c
    ORDER BY c.Nombre;
END
GO

/*========================================================
  1) VENTAS POR DIA
  Resultado: una fila por dia dentro del rango, con el
  desglose por metodo de pago.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_VentasPorDia
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdUsuario INT = NULL,
    @MetodoPago VARCHAR(30) = NULL,
    @Estado VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(v.FechaVenta AS DATE) AS Fecha,
        COUNT(*) AS CantidadVentas,
        SUM(CAST(v.Subtotal AS DECIMAL(18, 2))) AS Subtotal,
        SUM(CAST(v.IGV AS DECIMAL(18, 2))) AS RecargoTarjeta,
        SUM(CAST(v.Total AS DECIMAL(18, 2))) AS Total,
        SUM(CAST(ISNULL(det.unidades, 0) AS INT)) AS UnidadesVendidas,
        SUM(CASE WHEN v.MetodoPago = 'EFECTIVO'  THEN v.Total ELSE 0 END) AS TotalEfectivo,
        SUM(CASE WHEN v.MetodoPago = 'YAPE_PLIN' THEN v.Total ELSE 0 END) AS TotalYapePlin,
        SUM(CASE WHEN v.MetodoPago = 'TARJETA'   THEN v.Total ELSE 0 END) AS TotalTarjeta,
        SUM(CASE WHEN v.MetodoPago NOT IN ('EFECTIVO', 'YAPE_PLIN', 'TARJETA')
                 THEN v.Total ELSE 0 END) AS TotalOtros
    FROM dbo.VentaPOS v
    OUTER APPLY (
        SELECT SUM(d.Cantidad) AS unidades
        FROM dbo.DetalleVentaPOS d
        WHERE d.IdVenta = v.IdVenta) det
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdUsuario IS NULL OR v.id_usuario = @IdUsuario)
      AND (@MetodoPago IS NULL OR v.MetodoPago = @MetodoPago)
      AND (@Estado IS NULL OR v.Estado = @Estado)
    GROUP BY CAST(v.FechaVenta AS DATE)
    ORDER BY CAST(v.FechaVenta AS DATE);
END
GO

/*========================================================
  2) DETALLE DE VENTAS (CABECERAS)
  Resultado 1: listado de ventas del periodo.
  Resultado 2: totales generales del mismo filtro.
  Resultado 3: totales por metodo de pago.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_VentasDetalle
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdUsuario INT = NULL,
    @MetodoPago VARCHAR(30) = NULL,
    @Estado VARCHAR(20) = NULL,
    @Texto VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @busqueda VARCHAR(100) =
        LTRIM(RTRIM(ISNULL(@Texto, '')));

    SELECT
        v.IdVenta,
        v.NumeroVenta,
        v.FechaVenta,
        u.id_usuario AS IdUsuario,
        u.nombre AS Usuario,
        r.nombre AS Rol,
        ISNULL(c.TipoDocumento, 'OTROS') AS TipoDocumento,
        ISNULL(c.NumeroDocumento, '') AS NumeroDocumento,
        ISNULL(c.NombreCompleto, 'Otros clientes') AS Cliente,
        v.MetodoPago,
        v.Subtotal,
        v.IGV AS RecargoTarjeta,
        v.Total,
        v.Estado,
        (SELECT ISNULL(SUM(d.Cantidad), 0)
         FROM dbo.DetalleVentaPOS d
         WHERE d.IdVenta = v.IdVenta) AS Unidades,
        (SELECT COUNT(*)
         FROM dbo.DetalleVentaPOS d
         WHERE d.IdVenta = v.IdVenta) AS Items,
        (SELECT CAST(ISNULL(SUM(d.Descuento), 0) AS DECIMAL(18, 2))
         FROM dbo.DetalleVentaPOS d
         WHERE d.IdVenta = v.IdVenta) AS Descuento
    FROM dbo.VentaPOS v
    INNER JOIN dbo.usuarios u
        ON u.id_usuario = v.id_usuario
    INNER JOIN dbo.roles r
        ON r.id_rol = u.id_rol
    LEFT JOIN dbo.ClientePOS c
        ON c.IdClientePOS = v.IdClientePOS
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdUsuario IS NULL OR v.id_usuario = @IdUsuario)
      AND (@MetodoPago IS NULL OR v.MetodoPago = @MetodoPago)
      AND (@Estado IS NULL OR v.Estado = @Estado)
      AND (
            @busqueda = ''
         OR v.NumeroVenta LIKE '%' + @busqueda + '%'
         OR u.nombre LIKE '%' + @busqueda + '%'
         OR u.usuario LIKE '%' + @busqueda + '%'
         OR ISNULL(c.NombreCompleto, '') LIKE '%' + @busqueda + '%'
         OR ISNULL(c.NumeroDocumento, '') LIKE '%' + @busqueda + '%')
    ORDER BY v.FechaVenta DESC, v.IdVenta DESC;

    SELECT
        COUNT(*) AS CantidadVentas,
        SUM(CAST(v.Subtotal AS DECIMAL(18, 2))) AS Subtotal,
        SUM(CAST(v.IGV AS DECIMAL(18, 2))) AS RecargoTarjeta,
        SUM(CAST(v.Total AS DECIMAL(18, 2))) AS Total,
        SUM(CAST(ISNULL(det.unidades, 0) AS INT)) AS UnidadesVendidas
    FROM dbo.VentaPOS v
    INNER JOIN dbo.usuarios u
        ON u.id_usuario = v.id_usuario
    LEFT JOIN dbo.ClientePOS c
        ON c.IdClientePOS = v.IdClientePOS
    OUTER APPLY (
        SELECT SUM(d.Cantidad) AS unidades
        FROM dbo.DetalleVentaPOS d
        WHERE d.IdVenta = v.IdVenta) det
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdUsuario IS NULL OR v.id_usuario = @IdUsuario)
      AND (@MetodoPago IS NULL OR v.MetodoPago = @MetodoPago)
      AND (@Estado IS NULL OR v.Estado = @Estado)
      AND (
            @busqueda = ''
         OR v.NumeroVenta LIKE '%' + @busqueda + '%'
         OR u.nombre LIKE '%' + @busqueda + '%'
         OR u.usuario LIKE '%' + @busqueda + '%'
         OR ISNULL(c.NombreCompleto, '') LIKE '%' + @busqueda + '%'
         OR ISNULL(c.NumeroDocumento, '') LIKE '%' + @busqueda + '%');

    SELECT
        SUM(CAST(v.Total AS DECIMAL(18, 2))) AS TotalEfectivo,
        SUM(CASE WHEN v.MetodoPago = 'YAPE_PLIN'
                 THEN v.Total ELSE 0 END) AS TotalYapePlin,
        SUM(CASE WHEN v.MetodoPago = 'TARJETA'
                 THEN v.Total ELSE 0 END) AS TotalTarjeta,
        SUM(CASE WHEN v.MetodoPago NOT IN ('EFECTIVO', 'YAPE_PLIN', 'TARJETA')
                 THEN v.Total ELSE 0 END) AS TotalOtros
    FROM dbo.VentaPOS v
    INNER JOIN dbo.usuarios u
        ON u.id_usuario = v.id_usuario
    LEFT JOIN dbo.ClientePOS c
        ON c.IdClientePOS = v.IdClientePOS
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdUsuario IS NULL OR v.id_usuario = @IdUsuario)
      AND (@MetodoPago IS NULL OR v.MetodoPago = @MetodoPago)
      AND (@Estado IS NULL OR v.Estado = @Estado)
      AND (
            @busqueda = ''
         OR v.NumeroVenta LIKE '%' + @busqueda + '%'
         OR u.nombre LIKE '%' + @busqueda + '%'
         OR u.usuario LIKE '%' + @busqueda + '%'
         OR ISNULL(c.NombreCompleto, '') LIKE '%' + @busqueda + '%'
         OR ISNULL(c.NumeroDocumento, '') LIKE '%' + @busqueda + '%');
END
GO

/*========================================================
  3) LINEAS DE VENTA (para drill-down y export detallado)
  Resultado 1: una fila por linea de venta.
  Resultado 2: totales del mismo filtro.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_LineasVenta
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdUsuario INT = NULL,
    @IdCategoria INT = NULL,
    @MetodoPago VARCHAR(30) = NULL,
    @Texto VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @busqueda VARCHAR(100) =
        LTRIM(RTRIM(ISNULL(@Texto, '')));

    SELECT
        v.IdVenta,
        v.NumeroVenta,
        v.FechaVenta,
        u.nombre AS Usuario,
        v.MetodoPago,
        d.IdDetalle,
        d.id_producto AS IdProducto,
        d.IdVariante,
        d.NombreProducto,
        va.Descripcion AS Variante,
        d.Categoria,
        d.CodigoBarra,
        d.TipoPrecio,
        d.PrecioUnitario,
        d.Cantidad,
        d.Descuento,
        d.TotalLinea
    FROM dbo.DetalleVentaPOS d
    INNER JOIN dbo.VentaPOS v
        ON v.IdVenta = d.IdVenta
    INNER JOIN dbo.usuarios u
        ON u.id_usuario = v.id_usuario
    LEFT JOIN dbo.Variantes va
        ON va.IdVariante = d.IdVariante
    LEFT JOIN dbo.ProductoInventario pi
        ON pi.id_producto = d.id_producto
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdUsuario IS NULL OR v.id_usuario = @IdUsuario)
      AND (@IdCategoria IS NULL OR pi.IdCategoria = @IdCategoria)
      AND (@MetodoPago IS NULL OR v.MetodoPago = @MetodoPago)
      AND (
            @busqueda = ''
         OR d.NombreProducto LIKE '%' + @busqueda + '%'
         OR ISNULL(va.Descripcion, '') LIKE '%' + @busqueda + '%'
         OR ISNULL(d.CodigoBarra, '') LIKE '%' + @busqueda + '%'
         OR ISNULL(d.Categoria, '') LIKE '%' + @busqueda + '%'
         OR v.NumeroVenta LIKE '%' + @busqueda + '%')
    ORDER BY v.FechaVenta DESC, d.IdDetalle;

    SELECT
        COUNT(*) AS CantidadLineas,
        SUM(d.Cantidad) AS UnidadesVendidas,
        SUM(CAST(d.Descuento AS DECIMAL(18, 2))) AS Descuentos,
        SUM(CAST(d.TotalLinea AS DECIMAL(18, 2))) AS TotalGenerado
    FROM dbo.DetalleVentaPOS d
    INNER JOIN dbo.VentaPOS v
        ON v.IdVenta = d.IdVenta
    INNER JOIN dbo.usuarios u
        ON u.id_usuario = v.id_usuario
    LEFT JOIN dbo.Variantes va
        ON va.IdVariante = d.IdVariante
    LEFT JOIN dbo.ProductoInventario pi
        ON pi.id_producto = d.id_producto
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdUsuario IS NULL OR v.id_usuario = @IdUsuario)
      AND (@IdCategoria IS NULL OR pi.IdCategoria = @IdCategoria)
      AND (@MetodoPago IS NULL OR v.MetodoPago = @MetodoPago)
      AND (
            @busqueda = ''
         OR d.NombreProducto LIKE '%' + @busqueda + '%'
         OR ISNULL(va.Descripcion, '') LIKE '%' + @busqueda + '%'
         OR ISNULL(d.CodigoBarra, '') LIKE '%' + @busqueda + '%'
         OR ISNULL(d.Categoria, '') LIKE '%' + @busqueda + '%'
         OR v.NumeroVenta LIKE '%' + @busqueda + '%');
END
GO

/*========================================================
  4) PRODUCTOS MAS VENDIDOS
  Ranking por cantidad y por importe generado.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_ProductosVendidos
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdCategoria INT = NULL,
    @TopN INT = 200
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (CASE WHEN ISNULL(@TopN, 0) <= 0 THEN 200 ELSE @TopN END)
        d.id_producto AS IdProducto,
        d.IdVariante,
        d.NombreProducto,
        va.Descripcion AS Variante,
        d.Categoria,
        d.CodigoBarra,
        SUM(d.Cantidad) AS CantidadVendida,
        SUM(CAST(d.TotalLinea AS DECIMAL(18, 2))) AS TotalGenerado,
        SUM(CAST(d.Descuento AS DECIMAL(18, 2))) AS Descuentos,
        COUNT(DISTINCT d.IdVenta) AS NumeroVentas,
        CAST(AVG(CAST(d.PrecioUnitario AS DECIMAL(18, 4))) AS DECIMAL(18, 2)) AS PrecioPromedio,
        MAX(v.FechaVenta) AS UltimaVenta
    FROM dbo.DetalleVentaPOS d
    INNER JOIN dbo.VentaPOS v
        ON v.IdVenta = d.IdVenta
    LEFT JOIN dbo.Variantes va
        ON va.IdVariante = d.IdVariante
    LEFT JOIN dbo.ProductoInventario pi
        ON pi.id_producto = d.id_producto
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdCategoria IS NULL OR pi.IdCategoria = @IdCategoria)
    GROUP BY
        d.id_producto,
        d.IdVariante,
        d.NombreProducto,
        va.Descripcion,
        d.Categoria,
        d.CodigoBarra
    ORDER BY CantidadVendida DESC, TotalGenerado DESC;
END
GO

/*========================================================
  5) VENTAS POR USUARIO (rendimiento de vendedores)
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_VentasPorUsuario
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdUsuario INT = NULL,
    @MetodoPago VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario AS IdUsuario,
        u.nombre AS Usuario,
        r.nombre AS Rol,
        COUNT(*) AS CantidadVentas,
        SUM(CAST(v.Subtotal AS DECIMAL(18, 2))) AS Subtotal,
        SUM(CAST(v.IGV AS DECIMAL(18, 2))) AS RecargoTarjeta,
        SUM(CAST(v.Total AS DECIMAL(18, 2))) AS Total,
        CAST(AVG(CAST(v.Total AS DECIMAL(18, 2))) AS DECIMAL(18, 2)) AS TicketPromedio,
        SUM(CAST(ISNULL(det.unidades, 0) AS INT)) AS UnidadesVendidas,
        SUM(CASE WHEN v.MetodoPago = 'EFECTIVO'  THEN v.Total ELSE 0 END) AS TotalEfectivo,
        SUM(CASE WHEN v.MetodoPago = 'YAPE_PLIN' THEN v.Total ELSE 0 END) AS TotalYapePlin,
        SUM(CASE WHEN v.MetodoPago = 'TARJETA'   THEN v.Total ELSE 0 END) AS TotalTarjeta
    FROM dbo.VentaPOS v
    INNER JOIN dbo.usuarios u
        ON u.id_usuario = v.id_usuario
    INNER JOIN dbo.roles r
        ON r.id_rol = u.id_rol
    OUTER APPLY (
        SELECT SUM(d.Cantidad) AS unidades
        FROM dbo.DetalleVentaPOS d
        WHERE d.IdVenta = v.IdVenta) det
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdUsuario IS NULL OR v.id_usuario = @IdUsuario)
      AND (@MetodoPago IS NULL OR v.MetodoPago = @MetodoPago)
    GROUP BY u.id_usuario, u.nombre, r.nombre
    ORDER BY Total DESC;
END
GO

/*========================================================
  6) VENTAS POR CATEGORIA
  El filtro de categoria se apoya en ProductoInventario
  (categoria actual del producto), no en el texto
  categorico guardado en DetalleVentaPOS.Categoria,
  que es una fotografia del momento de la venta.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_VentasPorCategoria
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdCategoria INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL(c.IdCategoria, 0) AS IdCategoria,
        ISNULL(c.Nombre, 'Sin categoria registrada') AS Categoria,
        COUNT(DISTINCT d.IdVenta) AS NumeroVentas,
        COUNT(DISTINCT d.id_producto) AS Productos,
        SUM(d.Cantidad) AS CantidadVendida,
        SUM(CAST(d.TotalLinea AS DECIMAL(18, 2))) AS TotalGenerado,
        SUM(CAST(d.Descuento AS DECIMAL(18, 2))) AS Descuentos
    FROM dbo.DetalleVentaPOS d
    INNER JOIN dbo.VentaPOS v
        ON v.IdVenta = d.IdVenta
    LEFT JOIN dbo.ProductoInventario pi
        ON pi.id_producto = d.id_producto
    LEFT JOIN dbo.Categorias c
        ON c.IdCategoria = pi.IdCategoria
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdCategoria IS NULL OR c.IdCategoria = @IdCategoria)
    GROUP BY c.IdCategoria, c.Nombre
    ORDER BY TotalGenerado DESC;
END
GO

/*========================================================
  7) VENTAS POR TIPO DE PRECIO (UNIDAD vs MAYOR)
  Compara cuanto se movio cada canal de precio.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_VentasPorTipoPrecio
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdCategoria INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL(d.TipoPrecio, 'SIN DATO') AS TipoPrecio,
        SUM(d.Cantidad) AS Cantidad,
        COUNT(DISTINCT d.IdVenta) AS NumeroVentas,
        SUM(CAST(d.TotalLinea AS DECIMAL(18, 2))) AS TotalGenerado
    FROM dbo.DetalleVentaPOS d
    INNER JOIN dbo.VentaPOS v
        ON v.IdVenta = d.IdVenta
    LEFT JOIN dbo.ProductoInventario pi
        ON pi.id_producto = d.id_producto
    WHERE v.FechaVenta >= @FechaDesde
      AND v.FechaVenta < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdCategoria IS NULL OR pi.IdCategoria = @IdCategoria)
    GROUP BY d.TipoPrecio
    ORDER BY TotalGenerado DESC;
END
GO

/*========================================================
  8) STOCK ACTUAL
  Filtros:
    @TipoUbicacion : 'ALMACEN' | 'TIENDA' | NULL (ambas)
    @EstadoStock   : 'AGOTADO' | 'BAJO' | 'NORMAL' | NULL (todos)
  El corte de stock bajo (<= 5) replica la regla que
  usa InventarioTiendaProducto.EstadoTienda.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_StockActual
    @IdCategoria INT = NULL,
    @TipoUbicacion VARCHAR(20) = NULL,
    @EstadoStock VARCHAR(20) = NULL,
    @Texto VARCHAR(100) = NULL,
    @IncluirInactivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @busqueda VARCHAR(100) =
        LTRIM(RTRIM(ISNULL(@Texto, '')));

    SELECT
        p.id_producto AS IdProducto,
        RTRIM(pi.CodigoBase) AS CodigoBase,
        p.nombre AS NombreProducto,
        pi.Descripcion AS DescripcionProducto,
        c.IdCategoria,
        c.Nombre AS Categoria,
        pi.TieneVariantes,
        v.IdVariante,
        v.Descripcion AS Variante,
        RTRIM(v.CodigoBarras) AS CodigoBarras,
        v.PrecioUnidad,
        v.PrecioMayor,
        ISNULL(sa.Stock, 0) AS StockAlmacen,
        ISNULL(st.Stock, 0) AS StockTienda,
        CAST(ISNULL(sa.Stock, 0) + ISNULL(st.Stock, 0) AS INT) AS StockTotal,
        CAST(ISNULL(st.Stock, 0) * v.PrecioUnidad AS DECIMAL(18, 2)) AS ValorVenta
    FROM dbo.ProductoInventario pi
    INNER JOIN dbo.productos p
        ON p.id_producto = pi.id_producto
    INNER JOIN dbo.Categorias c
        ON c.IdCategoria = pi.IdCategoria
    INNER JOIN dbo.Variantes v
        ON v.id_producto = p.id_producto
    OUTER APPLY (
        SELECT ISNULL(SUM(ss.Stock), 0) AS Stock
        FROM dbo.StockSucursal ss
        WHERE ss.IdVariante = v.IdVariante
          AND ss.TipoUbicacion = 'ALMACEN') sa
    OUTER APPLY (
        SELECT ISNULL(SUM(ss.Stock), 0) AS Stock
        FROM dbo.StockSucursal ss
        WHERE ss.IdVariante = v.IdVariante
          AND ss.TipoUbicacion = 'TIENDA') st
    WHERE (
            @IncluirInactivos = 1
         OR (pi.Estado = 1 AND p.estado = 1 AND v.Estado = 1))
      AND (@IdCategoria IS NULL OR c.IdCategoria = @IdCategoria)
      AND (
            @busqueda = ''
         OR p.nombre LIKE '%' + @busqueda + '%'
         OR v.Descripcion LIKE '%' + @busqueda + '%'
         OR RTRIM(v.CodigoBarras) LIKE '%' + @busqueda + '%'
         OR RTRIM(pi.CodigoBase) LIKE '%' + @busqueda + '%'
         OR c.Nombre LIKE '%' + @busqueda + '%')
      AND (
            @TipoUbicacion IS NULL
         OR (@TipoUbicacion = 'ALMACEN' AND ISNULL(sa.Stock, 0) > 0)
         OR (@TipoUbicacion = 'TIENDA'  AND ISNULL(st.Stock, 0) > 0))
      AND (
            @EstadoStock IS NULL
         OR (@EstadoStock = 'AGOTADO' AND ISNULL(st.Stock, 0) <= 0)
         OR (@EstadoStock = 'BAJO'    AND ISNULL(st.Stock, 0) > 0
                                       AND ISNULL(st.Stock, 0) <= 5)
         OR (@EstadoStock = 'NORMAL'  AND ISNULL(st.Stock, 0) > 5))
    ORDER BY c.Nombre, p.nombre, v.Descripcion;
END
GO

/*========================================================
  9) VALORIZACION DE INVENTARIO
  @TipoUbicacion : 'ALMACEN' | 'TIENDA' | NULL (ambas)
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_ValorizacionInventario
    @IdCategoria INT = NULL,
    @TipoUbicacion VARCHAR(20) = NULL,
    @IncluirInactivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.IdCategoria,
        c.Nombre AS Categoria,
        COUNT(DISTINCT p.id_producto) AS Productos,
        COUNT(DISTINCT v.IdVariante) AS Variantes,
        SUM(stockTotal) AS Unidades,
        SUM(valorAlmacen) AS ValorAlmacen,
        SUM(valorTienda) AS ValorTienda,
        SUM(valorTotal) AS ValorTotal
    FROM dbo.ProductoInventario pi
    INNER JOIN dbo.productos p
        ON p.id_producto = pi.id_producto
    INNER JOIN dbo.Categorias c
        ON c.IdCategoria = pi.IdCategoria
    INNER JOIN dbo.Variantes v
        ON v.id_producto = p.id_producto
    /*
     * IMPORTANTE: el stock de cada ubicacion se agrega por
     * separado. Si se usara un unico SUM con filtro
     * opcional, cuando @TipoUbicacion IS NULL el mismo
     * total se devolveria dos veces (una por columna) y la
     * valorizacion saldria duplicada.
     */
    OUTER APPLY (
        SELECT ISNULL(SUM(
                   CASE WHEN ss.TipoUbicacion = 'ALMACEN'
                        THEN ss.Stock ELSE 0 END), 0) AS stockAlmacen
        FROM dbo.StockSucursal ss
        WHERE ss.IdVariante = v.IdVariante
          AND (
                @TipoUbicacion IS NULL
             OR ss.TipoUbicacion = 'ALMACEN')) sa
    OUTER APPLY (
        SELECT ISNULL(SUM(
                   CASE WHEN ss.TipoUbicacion = 'TIENDA'
                        THEN ss.Stock ELSE 0 END), 0) AS stockTienda
        FROM dbo.StockSucursal ss
        WHERE ss.IdVariante = v.IdVariante
          AND (
                @TipoUbicacion IS NULL
             OR ss.TipoUbicacion = 'TIENDA')) st
    CROSS APPLY (
        SELECT
            ISNULL(sa.stockAlmacen, 0)
          + ISNULL(st.stockTienda, 0) AS stockTotal,
            CAST(ISNULL(sa.stockAlmacen, 0)
                 * v.PrecioUnidad AS DECIMAL(18, 2)) AS valorAlmacen,
            CAST(ISNULL(st.stockTienda, 0)
                 * v.PrecioUnidad AS DECIMAL(18, 2)) AS valorTienda,
            CAST((ISNULL(sa.stockAlmacen, 0)
                + ISNULL(st.stockTienda, 0))
                 * v.PrecioUnidad AS DECIMAL(18, 2)) AS valorTotal) val
    WHERE (
            @IncluirInactivos = 1
         OR (pi.Estado = 1 AND p.estado = 1 AND v.Estado = 1))
      AND (@IdCategoria IS NULL OR c.IdCategoria = @IdCategoria)
    GROUP BY c.IdCategoria, c.Nombre
    ORDER BY ValorTotal DESC;
END
GO

/*========================================================
  10) MOVIMIENTOS DE STOCK (KARDEX)
  ADVERTENCIA: hoy la tabla dbo.MovimientoStock esta
  definida en bd_svhbn.sql pero NINGUN procedimiento la
  escribe: sp_ActualizarStockAlmacen,
  sp_TransferirStockATienda y sp_POS_RegistrarVenta
  modifican StockSucursal sin registrar el movimiento.
  Este SP devuelve la estructura vacia y no falla, para
  que en cuanto se instrumente el registro de kardex el
  reporte funcione sin cambios.
========================================================*/

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_MovimientosStock
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdUsuario INT = NULL,
    @IdVariante INT = NULL,
    @TipoMovimiento VARCHAR(30) = NULL,
    @Texto VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID('dbo.MovimientoStock', 'U') IS NULL
    BEGIN
        SELECT TOP 0
            CAST(NULL AS INT) AS IdMovimiento,
            CAST(NULL AS INT) AS IdProducto,
            CAST(NULL AS INT) AS IdVariante,
            CAST(NULL AS NVARCHAR(150)) AS NombreProducto,
            CAST(NULL AS NVARCHAR(100)) AS Variante,
            CAST(NULL AS NVARCHAR(100)) AS Categoria,
            CAST(NULL AS NVARCHAR(30)) AS TipoMovimiento,
            CAST(NULL AS INT) AS Cantidad,
            CAST(NULL AS INT) AS StockAlmacenAntes,
            CAST(NULL AS INT) AS StockAlmacenDespues,
            CAST(NULL AS INT) AS StockTiendaAntes,
            CAST(NULL AS INT) AS StockTiendaDespues,
            CAST(NULL AS NVARCHAR(100)) AS Usuario,
            CAST(NULL AS NVARCHAR(250)) AS Observacion,
            CAST(NULL AS DATETIME) AS FechaMovimiento;
        RETURN;
    END

    DECLARE @busqueda VARCHAR(100) =
        LTRIM(RTRIM(ISNULL(@Texto, '')));

    SELECT
        m.IdMovimiento,
        p.id_producto AS IdProducto,
        m.IdVariante,
        p.nombre AS NombreProducto,
        v.Descripcion AS Variante,
        c.Nombre AS Categoria,
        m.TipoMovimiento,
        m.Cantidad,
        m.StockAlmacenAntes,
        m.StockAlmacenDespues,
        m.StockTiendaAntes,
        m.StockTiendaDespues,
        u.nombre AS Usuario,
        m.Observacion,
        m.FechaMovimiento
    FROM dbo.MovimientoStock m
    INNER JOIN dbo.Variantes v
        ON v.IdVariante = m.IdVariante
    INNER JOIN dbo.productos p
        ON p.id_producto = v.id_producto
    LEFT JOIN dbo.ProductoInventario pi
        ON pi.id_producto = p.id_producto
    LEFT JOIN dbo.Categorias c
        ON c.IdCategoria = pi.IdCategoria
    INNER JOIN dbo.usuarios u
        ON u.id_usuario = m.id_usuario
    WHERE m.FechaMovimiento >= @FechaDesde
      AND m.FechaMovimiento < DATEADD(DAY, 1, CAST(@FechaHasta AS DATE))
      AND (@IdUsuario IS NULL OR m.id_usuario = @IdUsuario)
      AND (@IdVariante IS NULL OR m.IdVariante = @IdVariante)
      AND (@TipoMovimiento IS NULL OR m.TipoMovimiento = @TipoMovimiento)
      AND (
            @busqueda = ''
         OR p.nombre LIKE '%' + @busqueda + '%'
         OR v.Descripcion LIKE '%' + @busqueda + '%'
         OR RTRIM(v.CodigoBarras) LIKE '%' + @busqueda + '%'
         OR ISNULL(m.Observacion, '') LIKE '%' + @busqueda + '%')
    ORDER BY m.FechaMovimiento DESC, m.IdMovimiento DESC;
END
GO
