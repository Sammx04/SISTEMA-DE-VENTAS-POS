use SistemaVentas
go

/*=========================================
  LOGIN
=========================================*/
CREATE PROCEDURE sp_login
    @usuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario,
        u.nombre,
        u.usuario,
        u.password,
        r.nombre AS rol
    FROM usuarios u
    INNER JOIN roles r
        ON u.id_rol = r.id_rol
    WHERE u.usuario = @usuario;
END;
GO
UPDATE usuarios
SET password = '$2a$11$jmS4CXrV0ds5nXUjscO6eetadTmRf0WxlxrXCt31kt/BjgidWJx7i'
WHERE usuario = 'admin';

/*=========================================
  CREAR USUARIO
=========================================*/
CREATE PROCEDURE sp_crear_usuario
    @nombre VARCHAR(100),
    @usuario VARCHAR(50),
    @password VARCHAR(255),
    @id_rol INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO usuarios(
        nombre,
        usuario,
        password,
        id_rol
    )
    VALUES(
        @nombre,
        @usuario,
        @password,
        @id_rol
    );
END;
GO

/*=========================================
  LISTAR USUARIOS
=========================================*/
CREATE PROCEDURE sp_listar_usuarios
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario,
        u.nombre,
        u.usuario,
        r.nombre AS rol,
        u.estado
    FROM usuarios u
    INNER JOIN roles r
        ON u.id_rol = r.id_rol
    ORDER BY u.nombre;
END
GO

CREATE PROCEDURE sp_buscar_usuario
    @texto VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario,
        u.nombre,
        u.usuario,
        r.nombre AS rol,
        u.estado
    FROM usuarios u
    INNER JOIN roles r
        ON u.id_rol = r.id_rol
    WHERE
        u.nombre LIKE '%' + @texto + '%'
        OR
        u.usuario LIKE '%' + @texto + '%'
    ORDER BY u.nombre;
END
GO

CREATE PROCEDURE sp_actualizar_usuario
(
    @id_usuario INT,
    @nombre VARCHAR(100),
    @usuario VARCHAR(50),
    @id_rol INT,
    @estado BIT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE usuarios
    SET
        nombre = @nombre,
        usuario = @usuario,
        id_rol = @id_rol,
        estado = @estado
    WHERE id_usuario = @id_usuario;
END
GO

CREATE PROCEDURE sp_cambiar_estado_usuario
(
    @id_usuario INT,
    @estado BIT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE usuarios
    SET estado = @estado
    WHERE id_usuario = @id_usuario;
END;
GO

/*====================================================
  TIPOS PARA ENVIAR LISTAS DESDE C#
====================================================*/
IF TYPE_ID('TipoVariante') IS NULL
BEGIN
    EXEC('
    CREATE TYPE TipoVariante AS TABLE(
        Descripcion NVARCHAR(100),
        PrecioUnidad DECIMAL(10,2),
        PrecioMayor DECIMAL(10,2),
        Stock INT
    );
    ');
END
GO

IF TYPE_ID('TVP_DetalleVentaPOS') IS NULL
BEGIN
    EXEC('
    CREATE TYPE TVP_DetalleVentaPOS AS TABLE(
        IdProducto INT,
        IdVariante INT,
        NombreProducto VARCHAR(150),
        Categoria VARCHAR(100),
        CodigoBarra VARCHAR(50),
        TipoPrecio VARCHAR(20),
        PrecioUnitario DECIMAL(10,2),
        Cantidad INT,
        Descuento DECIMAL(10,2),
        TotalLinea DECIMAL(10,2)
    );
    ');
END
GO

/*====================================================
  CATEGORÍAS
====================================================*/
CREATE OR ALTER PROCEDURE sp_CrearCategoria
    @Nombre NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Categorias(Nombre)
    VALUES(@Nombre);

    SELECT SCOPE_IDENTITY() AS IdCategoria;
END
GO

CREATE OR ALTER PROCEDURE sp_ListarCategorias
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        IdCategoria,
        Nombre,
        Estado
    FROM Categorias
    WHERE Estado = 1
    ORDER BY Nombre;
END
GO

/*====================================================
  CÓDIGO BASE
====================================================*/
CREATE OR ALTER PROCEDURE sp_GenerarCodigoBase
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    UPDATE SecuenciaProducto
    SET UltimoNumero = UltimoNumero + 1
    WHERE Id = 1;

    DECLARE @NuevoNumero INT;

    SELECT @NuevoNumero = UltimoNumero
    FROM SecuenciaProducto
    WHERE Id = 1;

    COMMIT TRANSACTION;

    SELECT RIGHT('0000000' + CAST(@NuevoNumero AS VARCHAR(7)), 7) AS CodigoBase;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerProximoCodigoBase
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProximoNumero INT;

    SELECT @ProximoNumero = UltimoNumero + 1
    FROM SecuenciaProducto
    WHERE Id = 1;

    SELECT RIGHT('0000000' + CAST(@ProximoNumero AS VARCHAR(7)), 7) AS CodigoBase;
END
GO

/*====================================================
  CREAR PRODUCTO CON INVENTARIO
====================================================*/
CREATE OR ALTER PROCEDURE sp_CrearProductoInventario
    @Nombre NVARCHAR(150),
    @IdCategoria INT,
    @Descripcion NVARCHAR(300),
    @TieneVariantes BIT,
    @ListaVariantes TipoVariante READONLY
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CodigoBase CHAR(7);
        DECLARE @IdProducto INT;
        DECLARE @IdVariante INT;
        DECLARE @Contador INT = 1;

        DECLARE @Desc NVARCHAR(100);
        DECLARE @PrecioUnidad DECIMAL(10,2);
        DECLARE @PrecioMayor DECIMAL(10,2);
        DECLARE @Stock INT;

        IF NOT EXISTS (SELECT 1 FROM @ListaVariantes)
        BEGIN
            RAISERROR('Debe enviar al menos una variante.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE SecuenciaProducto
        SET UltimoNumero = UltimoNumero + 1
        WHERE Id = 1;

        SELECT @CodigoBase = RIGHT('0000000' + CAST(UltimoNumero AS VARCHAR(7)), 7)
        FROM SecuenciaProducto
        WHERE Id = 1;

        INSERT INTO productos(
            codigo,
            nombre,
            precio,
            stock,
            estado
        )
        SELECT
            @CodigoBase,
            @Nombre,
            ISNULL((SELECT TOP 1 PrecioUnidad FROM @ListaVariantes), 1),
            ISNULL((SELECT SUM(Stock) FROM @ListaVariantes), 0),
            1;

        SET @IdProducto = SCOPE_IDENTITY();

        INSERT INTO ProductoInventario(
            id_producto,
            IdCategoria,
            Descripcion,
            CodigoBase,
            TieneVariantes,
            Estado
        )
        VALUES(
            @IdProducto,
            @IdCategoria,
            @Descripcion,
            @CodigoBase,
            @TieneVariantes,
            1
        );

        IF @TieneVariantes = 1
        BEGIN
            DECLARE cur CURSOR LOCAL FOR
            SELECT Descripcion, PrecioUnidad, PrecioMayor, Stock
            FROM @ListaVariantes;

            OPEN cur;
            FETCH NEXT FROM cur INTO @Desc, @PrecioUnidad, @PrecioMayor, @Stock;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                DECLARE @CodigoBarras CHAR(9);

                SET @CodigoBarras = @CodigoBase + RIGHT('00' + CAST(@Contador AS VARCHAR(2)), 2);

                INSERT INTO Variantes(
                    id_producto,
                    Descripcion,
                    CodigoBarras,
                    PrecioUnidad,
                    PrecioMayor
                )
                VALUES(
                    @IdProducto,
                    @Desc,
                    @CodigoBarras,
                    @PrecioUnidad,
                    @PrecioMayor
                );

                SET @IdVariante = SCOPE_IDENTITY();

                INSERT INTO StockSucursal(IdVariante, TipoUbicacion, Stock)
                VALUES(@IdVariante, 'ALMACEN', @Stock);

                INSERT INTO StockSucursal(IdVariante, TipoUbicacion, Stock)
                VALUES(@IdVariante, 'TIENDA', 0);

                SET @Contador = @Contador + 1;

                FETCH NEXT FROM cur INTO @Desc, @PrecioUnidad, @PrecioMayor, @Stock;
            END

            CLOSE cur;
            DEALLOCATE cur;
        END
        ELSE
        BEGIN
            SELECT TOP 1
                @PrecioUnidad = PrecioUnidad,
                @PrecioMayor = PrecioMayor,
                @Stock = Stock
            FROM @ListaVariantes;

            INSERT INTO Variantes(
                id_producto,
                Descripcion,
                CodigoBarras,
                PrecioUnidad,
                PrecioMayor
            )
            VALUES(
                @IdProducto,
                'Única',
                @CodigoBase + '01',
                @PrecioUnidad,
                @PrecioMayor
            );

            SET @IdVariante = SCOPE_IDENTITY();

            INSERT INTO StockSucursal(IdVariante, TipoUbicacion, Stock)
            VALUES(@IdVariante, 'ALMACEN', @Stock);

            INSERT INTO StockSucursal(IdVariante, TipoUbicacion, Stock)
            VALUES(@IdVariante, 'TIENDA', 0);
        END

        COMMIT TRANSACTION;

        SELECT 
            @IdProducto AS id_producto,
            @CodigoBase AS CodigoBase;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO

/*====================================================
  LISTAR PRODUCTOS CON VARIANTES
====================================================*/
CREATE OR ALTER PROCEDURE sp_ListarProductosConVariantes
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.id_producto,
        p.codigo,
        p.nombre,
        pi.Descripcion,
        pi.IdCategoria,
        c.Nombre AS NombreCategoria,
        pi.CodigoBase,
        pi.TieneVariantes,
        p.estado,
        v.IdVariante,
        v.Descripcion AS VarianteDescripcion,
        v.CodigoBarras,
        v.PrecioUnidad,
        v.PrecioMayor,
        ISNULL(sa.Stock, 0) AS StockAlmacen,
        ISNULL(st.Stock, 0) AS StockTienda,
        v.Estado AS VarianteEstado
    FROM productos p
    INNER JOIN ProductoInventario pi
        ON pi.id_producto = p.id_producto
    INNER JOIN Categorias c
        ON c.IdCategoria = pi.IdCategoria
    INNER JOIN Variantes v
        ON v.id_producto = p.id_producto
    LEFT JOIN StockSucursal sa
        ON sa.IdVariante = v.IdVariante
       AND sa.TipoUbicacion = 'ALMACEN'
    LEFT JOIN StockSucursal st
        ON st.IdVariante = v.IdVariante
       AND st.TipoUbicacion = 'TIENDA'
    WHERE p.estado = 1
      AND pi.Estado = 1
      AND v.Estado = 1
    ORDER BY p.nombre, v.Descripcion;
END
GO

/*====================================================
  OBTENER PRODUCTO COMPLETO
====================================================*/
CREATE OR ALTER PROCEDURE sp_ObtenerProductoCompleto
    @IdProducto INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.id_producto,
        p.codigo,
        p.nombre,
        pi.IdCategoria,
        c.Nombre AS NombreCategoria,
        pi.Descripcion,
        pi.CodigoBase,
        pi.TieneVariantes,
        p.estado
    FROM productos p
    INNER JOIN ProductoInventario pi
        ON pi.id_producto = p.id_producto
    INNER JOIN Categorias c
        ON c.IdCategoria = pi.IdCategoria
    WHERE p.id_producto = @IdProducto;

    SELECT
        v.IdVariante,
        v.Descripcion,
        v.CodigoBarras,
        v.PrecioUnidad,
        v.PrecioMayor,
        ISNULL(sa.Stock, 0) AS StockAlmacen,
        ISNULL(st.Stock, 0) AS StockTienda,
        v.Estado
    FROM Variantes v
    LEFT JOIN StockSucursal sa
        ON sa.IdVariante = v.IdVariante
       AND sa.TipoUbicacion = 'ALMACEN'
    LEFT JOIN StockSucursal st
        ON st.IdVariante = v.IdVariante
       AND st.TipoUbicacion = 'TIENDA'
    WHERE v.id_producto = @IdProducto
      AND v.Estado = 1;
END
GO

/*====================================================
  EDITAR PRODUCTO Y VARIANTE
====================================================*/
CREATE OR ALTER PROCEDURE sp_EditarProductoInventario
    @IdProducto INT,
    @Nombre NVARCHAR(150),
    @IdCategoria INT,
    @Descripcion NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE productos
    SET nombre = @Nombre
    WHERE id_producto = @IdProducto;

    UPDATE ProductoInventario
    SET
        IdCategoria = @IdCategoria,
        Descripcion = @Descripcion
    WHERE id_producto = @IdProducto;
END
GO

CREATE OR ALTER PROCEDURE sp_EditarVariante
    @IdVariante INT,
    @Descripcion NVARCHAR(100),
    @PrecioUnidad DECIMAL(10,2),
    @PrecioMayor DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Variantes
    SET
        Descripcion = @Descripcion,
        PrecioUnidad = @PrecioUnidad,
        PrecioMayor = @PrecioMayor
    WHERE IdVariante = @IdVariante;
END
GO

CREATE OR ALTER PROCEDURE sp_AgregarVarianteAProducto
    @IdProducto INT,
    @Descripcion NVARCHAR(100),
    @PrecioUnidad DECIMAL(10,2),
    @PrecioMayor DECIMAL(10,2),
    @Stock INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CodigoBase CHAR(7);
        DECLARE @Correlativo INT;
        DECLARE @CodigoBarras CHAR(9);
        DECLARE @IdVariante INT;

        SELECT @CodigoBase = CodigoBase
        FROM ProductoInventario
        WHERE id_producto = @IdProducto;

        SELECT @Correlativo = COUNT(*) + 1
        FROM Variantes
        WHERE id_producto = @IdProducto;

        SET @CodigoBarras = @CodigoBase + RIGHT('00' + CAST(@Correlativo AS VARCHAR(2)), 2);

        INSERT INTO Variantes(
            id_producto,
            Descripcion,
            CodigoBarras,
            PrecioUnidad,
            PrecioMayor
        )
        VALUES(
            @IdProducto,
            @Descripcion,
            @CodigoBarras,
            @PrecioUnidad,
            @PrecioMayor
        );

        SET @IdVariante = SCOPE_IDENTITY();

        INSERT INTO StockSucursal(IdVariante, TipoUbicacion, Stock)
        VALUES(@IdVariante, 'ALMACEN', @Stock);

        INSERT INTO StockSucursal(IdVariante, TipoUbicacion, Stock)
        VALUES(@IdVariante, 'TIENDA', 0);

        UPDATE productos
        SET stock = stock + @Stock
        WHERE id_producto = @IdProducto;

        COMMIT TRANSACTION;

        SELECT @IdVariante AS IdVariante, @CodigoBarras AS CodigoBarras;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO

/*====================================================
  STOCK
====================================================*/
CREATE OR ALTER PROCEDURE sp_ActualizarStockAlmacen
    @IdVariante INT,
    @CantidadIngreso INT,
    @CantidadSalida INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @StockActual INT;
        DECLARE @IdProducto INT;

        SELECT @StockActual = Stock
        FROM StockSucursal
        WHERE IdVariante = @IdVariante
          AND TipoUbicacion = 'ALMACEN';

        IF @StockActual IS NULL
        BEGIN
            RAISERROR('No existe stock de almacén para esta variante.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @CantidadSalida > @StockActual
        BEGIN
            RAISERROR('No puede retirar más del stock disponible en almacén.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE StockSucursal
        SET Stock = Stock + @CantidadIngreso - @CantidadSalida
        WHERE IdVariante = @IdVariante
          AND TipoUbicacion = 'ALMACEN';

        SELECT @IdProducto = id_producto
        FROM Variantes
        WHERE IdVariante = @IdVariante;

        UPDATE productos
        SET stock = (
            SELECT ISNULL(SUM(Stock), 0)
            FROM StockSucursal ss
            INNER JOIN Variantes v ON v.IdVariante = ss.IdVariante
            WHERE v.id_producto = @IdProducto
        )
        WHERE id_producto = @IdProducto;

        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE sp_TransferirStockATienda
    @IdVariante INT,
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @StockAlmacen INT;

        SELECT @StockAlmacen = Stock
        FROM StockSucursal
        WHERE IdVariante = @IdVariante
          AND TipoUbicacion = 'ALMACEN';

        IF @StockAlmacen IS NULL
        BEGIN
            RAISERROR('No existe stock en almacén para esta variante.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Cantidad <= 0
        BEGIN
            RAISERROR('La cantidad debe ser mayor a cero.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Cantidad > @StockAlmacen
        BEGIN
            RAISERROR('Stock insuficiente en almacén.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE StockSucursal
        SET Stock = Stock - @Cantidad
        WHERE IdVariante = @IdVariante
          AND TipoUbicacion = 'ALMACEN';

        UPDATE StockSucursal
        SET Stock = Stock + @Cantidad
        WHERE IdVariante = @IdVariante
          AND TipoUbicacion = 'TIENDA';

        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO

/*====================================================
  BUSCAR POR CÓDIGO DE BARRAS
====================================================*/
CREATE OR ALTER PROCEDURE sp_BuscarPorCodigoBarras
    @CodigoBarras VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        p.id_producto,
        p.nombre AS NombreProducto,
        c.Nombre AS Categoria,
        v.IdVariante,
        v.Descripcion AS Variante,
        v.CodigoBarras,
        v.PrecioUnidad,
        v.PrecioMayor,
        ISNULL(sa.Stock, 0) AS StockAlmacen,
        ISNULL(st.Stock, 0) AS StockTienda
    FROM Variantes v
    INNER JOIN productos p
        ON p.id_producto = v.id_producto
    INNER JOIN ProductoInventario pi
        ON pi.id_producto = p.id_producto
    INNER JOIN Categorias c
        ON c.IdCategoria = pi.IdCategoria
    LEFT JOIN StockSucursal sa
        ON sa.IdVariante = v.IdVariante
       AND sa.TipoUbicacion = 'ALMACEN'
    LEFT JOIN StockSucursal st
        ON st.IdVariante = v.IdVariante
       AND st.TipoUbicacion = 'TIENDA'
    WHERE v.CodigoBarras = @CodigoBarras
      AND p.estado = 1
      AND v.Estado = 1;
END
GO

/*====================================================
  POS - BUSCAR PRODUCTOS
====================================================*/
CREATE OR ALTER PROCEDURE sp_POS_BuscarProductos
    @Texto VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 20
        p.id_producto,
        v.IdVariante,
        p.nombre,
        v.Descripcion AS Variante,
        c.Nombre AS Categoria,
        v.CodigoBarras AS CodigoBarra,
        v.PrecioUnidad,
        v.PrecioMayor,
        ISNULL(st.Stock, 0) AS Stock
    FROM productos p
    INNER JOIN ProductoInventario pi
        ON pi.id_producto = p.id_producto
    INNER JOIN Categorias c
        ON c.IdCategoria = pi.IdCategoria
    INNER JOIN Variantes v
        ON v.id_producto = p.id_producto
    LEFT JOIN StockSucursal st
        ON st.IdVariante = v.IdVariante
       AND st.TipoUbicacion = 'TIENDA'
    WHERE p.estado = 1
      AND pi.Estado = 1
      AND v.Estado = 1
      AND ISNULL(st.Stock, 0) > 0
      AND (
            p.nombre LIKE '%' + @Texto + '%'
         OR v.Descripcion LIKE '%' + @Texto + '%'
         OR v.CodigoBarras LIKE '%' + @Texto + '%'
         OR c.Nombre LIKE '%' + @Texto + '%'
      )
    ORDER BY p.nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_POS_ObtenerProductoPorCodigo
    @CodigoBarra VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        p.id_producto,
        v.IdVariante,
        p.nombre,
        v.Descripcion AS Variante,
        c.Nombre AS Categoria,
        v.CodigoBarras AS CodigoBarra,
        v.PrecioUnidad,
        v.PrecioMayor,
        ISNULL(st.Stock, 0) AS Stock
    FROM productos p
    INNER JOIN ProductoInventario pi
        ON pi.id_producto = p.id_producto
    INNER JOIN Categorias c
        ON c.IdCategoria = pi.IdCategoria
    INNER JOIN Variantes v
        ON v.id_producto = p.id_producto
    LEFT JOIN StockSucursal st
        ON st.IdVariante = v.IdVariante
       AND st.TipoUbicacion = 'TIENDA'
    WHERE p.estado = 1
      AND pi.Estado = 1
      AND v.Estado = 1
      AND ISNULL(st.Stock, 0) > 0
      AND v.CodigoBarras = @CodigoBarra;
END
GO

/*====================================================
  POS - REGISTRAR VENTA
====================================================*/
CREATE OR ALTER PROCEDURE sp_POS_RegistrarVenta
    @id_usuario INT,
    @TipoDocumento VARCHAR(20),
    @NumeroDocumento VARCHAR(20) = NULL,
    @NombreCliente VARCHAR(150),
    @MetodoPago VARCHAR(30),
    @SubTotal DECIMAL(10,2),
    @IGV DECIMAL(10,2),
    @Total DECIMAL(10,2),
    @Detalle TVP_DetalleVentaPOS READONLY
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IdClientePOS INT;
        DECLARE @IdVentaPOS INT;
        DECLARE @NumeroVenta VARCHAR(30);
        DECLARE @IdVentaSistema INT;

        IF @NombreCliente IS NULL OR LTRIM(RTRIM(@NombreCliente)) = ''
            SET @NombreCliente = 'Otros clientes';

        IF @TipoDocumento IS NULL OR LTRIM(RTRIM(@TipoDocumento)) = ''
            SET @TipoDocumento = 'OTROS';

        IF NOT EXISTS (SELECT 1 FROM @Detalle)
        BEGIN
            RAISERROR('La venta no tiene productos.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (
            SELECT 1
            FROM @Detalle d
            LEFT JOIN StockSucursal st
                ON st.IdVariante = d.IdVariante
               AND st.TipoUbicacion = 'TIENDA'
            WHERE ISNULL(st.Stock, 0) < d.Cantidad
        )
        BEGIN
            RAISERROR('Stock insuficiente en tienda.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        INSERT INTO ClientePOS(
            TipoDocumento,
            NumeroDocumento,
            NombreCompleto
        )
        VALUES(
            @TipoDocumento,
            @NumeroDocumento,
            @NombreCliente
        );

        SET @IdClientePOS = SCOPE_IDENTITY();

        SET @NumeroVenta =
            'NV-' +
            CONVERT(VARCHAR(8), GETDATE(), 112) +
            '-' +
            RIGHT('00000' + CAST(NEXT VALUE FOR SeqVentaPOS AS VARCHAR(10)), 5);

        INSERT INTO VentaPOS(
            NumeroVenta,
            IdClientePOS,
            id_usuario,
            MetodoPago,
            Subtotal,
            IGV,
            Total,
            Estado,
            FechaVenta
        )
        VALUES(
            @NumeroVenta,
            @IdClientePOS,
            @id_usuario,
            @MetodoPago,
            @SubTotal,
            @IGV,
            @Total,
            'COMPLETADA',
            GETDATE()
        );

        SET @IdVentaPOS = SCOPE_IDENTITY();

        INSERT INTO DetalleVentaPOS(
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
            TotalLinea
        )
        SELECT
            @IdVentaPOS,
            IdProducto,
            IdVariante,
            NombreProducto,
            Categoria,
            CodigoBarra,
            TipoPrecio,
            PrecioUnitario,
            Cantidad,
            Descuento,
            TotalLinea
        FROM @Detalle;

        UPDATE st
        SET st.Stock = st.Stock - d.Cantidad
        FROM StockSucursal st
        INNER JOIN @Detalle d
            ON d.IdVariante = st.IdVariante
        WHERE st.TipoUbicacion = 'TIENDA';

        UPDATE p
        SET p.stock = (
            SELECT ISNULL(SUM(ss.Stock), 0)
            FROM StockSucursal ss
            INNER JOIN Variantes v
                ON v.IdVariante = ss.IdVariante
            WHERE v.id_producto = p.id_producto
        )
        FROM productos p
        WHERE p.id_producto IN (
            SELECT DISTINCT IdProducto FROM @Detalle
        );

        INSERT INTO ventas(
            id_usuario,
            fecha,
            total
        )
        VALUES(
            @id_usuario,
            GETDATE(),
            @Total
        );

        SET @IdVentaSistema = SCOPE_IDENTITY();

        INSERT INTO detalle_venta(
            id_venta,
            id_producto,
            cantidad,
            precio
        )
        SELECT
            @IdVentaSistema,
            IdProducto,
            SUM(Cantidad),
            MAX(PrecioUnitario)
        FROM @Detalle
        GROUP BY IdProducto;

        COMMIT TRANSACTION;

        SELECT
            @IdVentaPOS AS IdVentaPOS,
            @NumeroVenta AS NumeroVenta,
            @IdVentaSistema AS IdVentaSistema;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO

/*====================================================
  POS - PRODUCTOS FRECUENTES DEL DÍA
====================================================*/
CREATE OR ALTER PROCEDURE sp_POS_ProductosFrecuentesDia
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 5
        d.id_producto,
        d.IdVariante,
        d.NombreProducto AS Nombre,
        d.Categoria,
        d.CodigoBarra,
        d.PrecioUnitario AS PrecioUnidad,
        SUM(d.Cantidad) AS VecesVendido
    FROM DetalleVentaPOS d
    INNER JOIN VentaPOS v
        ON v.IdVenta = d.IdVenta
    WHERE CAST(v.FechaVenta AS DATE) = CAST(GETDATE() AS DATE)
      AND v.Estado = 'COMPLETADA'
    GROUP BY
        d.id_producto,
        d.IdVariante,
        d.NombreProducto,
        d.Categoria,
        d.CodigoBarra,
        d.PrecioUnitario
    ORDER BY SUM(d.Cantidad) DESC;
END
GO

/*====================================================
  POS - RESUMEN ADMIN DEL DÍA
====================================================*/
CREATE OR ALTER PROCEDURE sp_POS_ResumenAdminDia
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL(SUM(Total), 0) AS VentasDelDia,
        COUNT(*) AS NumeroTransacciones
    FROM VentaPOS
    WHERE CAST(FechaVenta AS DATE) = CAST(GETDATE() AS DATE)
      AND Estado = 'COMPLETADA';
END
GO

/*====================================================
  REPORTES
====================================================*/
CREATE OR ALTER PROCEDURE sp_Reporte_Dashboard
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL(SUM(CASE 
            WHEN CAST(FechaVenta AS DATE) = CAST(GETDATE() AS DATE)
            THEN Total ELSE 0 END), 0) AS VentasHoy,

        ISNULL(COUNT(CASE
            WHEN CAST(FechaVenta AS DATE) = CAST(GETDATE() AS DATE)
            THEN 1 END), 0) AS CantidadVentasHoy,

        ISNULL((
            SELECT SUM(d.Cantidad)
            FROM DetalleVentaPOS d
            INNER JOIN VentaPOS v ON v.IdVenta = d.IdVenta
            WHERE CAST(v.FechaVenta AS DATE) = CAST(GETDATE() AS DATE)
              AND v.Estado = 'COMPLETADA'
        ), 0) AS ProductosVendidosHoy,

        ISNULL((
            SELECT COUNT(*)
            FROM StockSucursal
            WHERE TipoUbicacion = 'TIENDA'
              AND Stock <= 5
        ), 0) AS StockBajo
    FROM VentaPOS
    WHERE Estado = 'COMPLETADA';
END
GO

CREATE OR ALTER PROCEDURE sp_Reporte_Ventas
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(FechaVenta AS DATE) AS Fecha,
        COUNT(*) AS CantidadVentas,
        SUM(Subtotal) AS Subtotal,
        SUM(IGV) AS IGV,
        SUM(Total) AS Total
    FROM VentaPOS
    WHERE Estado = 'COMPLETADA'
      AND (@FechaInicio IS NULL OR CAST(FechaVenta AS DATE) >= @FechaInicio)
      AND (@FechaFin IS NULL OR CAST(FechaVenta AS DATE) <= @FechaFin)
    GROUP BY CAST(FechaVenta AS DATE)
    ORDER BY Fecha;
END
GO

CREATE OR ALTER PROCEDURE sp_Reporte_ProductosMasVendidos
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL,
    @Texto VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.id_producto,
        d.IdVariante,
        d.NombreProducto,
        d.Categoria,
        d.CodigoBarra,
        SUM(d.Cantidad) AS CantidadVendida,
        SUM(d.TotalLinea) AS TotalGenerado
    FROM DetalleVentaPOS d
    INNER JOIN VentaPOS v
        ON v.IdVenta = d.IdVenta
    WHERE v.Estado = 'COMPLETADA'
      AND (@FechaInicio IS NULL OR CAST(v.FechaVenta AS DATE) >= @FechaInicio)
      AND (@FechaFin IS NULL OR CAST(v.FechaVenta AS DATE) <= @FechaFin)
      AND (
            @Texto IS NULL
         OR d.NombreProducto LIKE '%' + @Texto + '%'
         OR d.Categoria LIKE '%' + @Texto + '%'
         OR d.CodigoBarra LIKE '%' + @Texto + '%'
      )
    GROUP BY
        d.id_producto,
        d.IdVariante,
        d.NombreProducto,
        d.Categoria,
        d.CodigoBarra
    ORDER BY CantidadVendida DESC;
END
GO

/*=========================================
  REGISTRAR PRODUCTO
=========================================*/
CREATE PROCEDURE sp_registrar_producto
    @codigo VARCHAR(50),
    @nombre VARCHAR(150),
    @precio DECIMAL(10,2),
    @stock INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO productos(
        codigo,
        nombre,
        precio,
        stock
    )
    VALUES(
        @codigo,
        @nombre,
        @precio,
        @stock
    );
END;
GO

/*=========================================
  ACTUALIZAR PRODUCTO
=========================================*/
CREATE PROCEDURE sp_actualizar_producto
    @id_producto INT,
    @codigo VARCHAR(50),
    @nombre VARCHAR(150),
    @precio DECIMAL(10,2),
    @stock INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE productos
    SET
        codigo = @codigo,
        nombre = @nombre,
        precio = @precio,
        stock = @stock
    WHERE id_producto = @id_producto;
END;
GO

/*=========================================
  ELIMINAR PRODUCTO
=========================================*/
CREATE PROCEDURE sp_eliminar_producto
    @id_producto INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM productos
    WHERE id_producto = @id_producto;
END;
GO

/*=========================================
  LISTAR PRODUCTOS
=========================================*/
CREATE PROCEDURE sp_listar_productos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM productos
    ORDER BY nombre;
END;
GO

/*=========================================
  BUSCAR PRODUCTO
=========================================*/
CREATE PROCEDURE sp_buscar_producto
    @texto VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM productos
    WHERE nombre LIKE '%' + @texto + '%'
       OR codigo LIKE '%' + @texto + '%';
END;
GO

/*=========================================
  ABRIR CAJA
=========================================*/
CREATE PROCEDURE sp_abrir_caja
    @monto_inicial DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO caja(
        fecha_apertura,
        monto_inicial,
        estado
    )
    VALUES(
        GETDATE(),
        @monto_inicial,
        'ABIERTA'
    );
END;
GO

/*=========================================
  CERRAR CAJA
=========================================*/
CREATE PROCEDURE sp_cerrar_caja
    @id_caja INT,
    @monto_final DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE caja
    SET
        fecha_cierre = GETDATE(),
        monto_final = @monto_final,
        estado = 'CERRADA'
    WHERE id_caja = @id_caja;
END;
GO

/*=========================================
  LISTAR CAJAS
=========================================*/
CREATE PROCEDURE sp_listar_cajas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM caja
    ORDER BY id_caja DESC;
END;
GO

/*=========================================
  REGISTRAR VENTA
=========================================*/
CREATE PROCEDURE sp_registrar_venta
    @id_usuario INT,
    @total DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ventas(
        id_usuario,
        fecha,
        total
    )
    VALUES(
        @id_usuario,
        GETDATE(),
        @total
    );

    SELECT SCOPE_IDENTITY() AS id_venta;
END;
GO

/*=========================================
  REGISTRAR DETALLE VENTA
=========================================*/
CREATE PROCEDURE sp_registrar_detalle_venta
    @id_venta INT,
    @id_producto INT,
    @cantidad INT,
    @precio DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO detalle_venta(
        id_venta,
        id_producto,
        cantidad,
        precio
    )
    VALUES(
        @id_venta,
        @id_producto,
        @cantidad,
        @precio
    );

    UPDATE productos
    SET stock = stock - @cantidad
    WHERE id_producto = @id_producto;
END;
GO

/*=========================================
  LISTAR VENTAS
=========================================*/
CREATE PROCEDURE sp_listar_ventas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        v.id_venta,
        v.fecha,
        v.total,
        u.nombre AS vendedor
    FROM ventas v
    INNER JOIN usuarios u
        ON v.id_usuario = u.id_usuario
    ORDER BY v.id_venta DESC;
END;
GO

/*=========================================
  DETALLE DE UNA VENTA
=========================================*/
CREATE PROCEDURE sp_detalle_venta
    @id_venta INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.nombre,
        dv.cantidad,
        dv.precio,
        (dv.cantidad * dv.precio) AS subtotal
    FROM detalle_venta dv
    INNER JOIN productos p
        ON dv.id_producto = p.id_producto
    WHERE dv.id_venta = @id_venta;
END;
GO

/*=========================================
  DASHBOARD - VENTAS HOY
=========================================*/
CREATE PROCEDURE sp_ventas_hoy
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS cantidad_ventas,
        ISNULL(SUM(total),0) AS total vendido
    FROM ventas
    WHERE CAST(fecha AS DATE) = CAST(GETDATE() AS DATE);
END;
GO

/*=========================================
  STOCK BAJO
=========================================*/
CREATE PROCEDURE sp_stock_bajo
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM productos
    WHERE stock <= 5;
END;
GO
