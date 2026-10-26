/*=========================================
  CREAR BASE DE DATOS
=========================================*/
CREATE DATABASE SistemaVentas;
GO

USE SistemaVentas;
GO

/*=========================================
  TABLA ROLES
=========================================*/
CREATE TABLE roles(
    id_rol INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL UNIQUE
);
GO

INSERT INTO roles(nombre)
VALUES
('Administrador'),
('Vendedor');
GO

/*=========================================
  TABLA USUARIOS
=========================================*/
CREATE TABLE usuarios(
    id_usuario INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    usuario VARCHAR(50) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    id_rol INT NOT NULL,
    estado BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_usuarios_roles
    FOREIGN KEY(id_rol)
    REFERENCES roles(id_rol)
);
GO

/*====================================================
  CATEGORÍAS
====================================================*/
CREATE TABLE Categorias (
    IdCategoria INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL UNIQUE,
    Estado BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/*=========================================
  TABLA PRODUCTOS
=========================================*/
CREATE TABLE productos(
    id_producto INT IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL UNIQUE,
    nombre VARCHAR(150) NOT NULL,
    precio DECIMAL(10,2) NOT NULL,
    stock INT NOT NULL DEFAULT 0,
    estado BIT NOT NULL DEFAULT 1,
    fecha_registro DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT CK_productos_precio
    CHECK(precio > 0),

    CONSTRAINT CK_productos_stock
    CHECK(stock >= 0)
);
GO

/*====================================================
  DATOS EXTRA DE INVENTARIO PARA TU TABLA productos
====================================================*/
CREATE TABLE ProductoInventario (
    IdProductoInventario INT IDENTITY(1,1) PRIMARY KEY,
    id_producto INT NOT NULL,
    IdCategoria INT NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    CodigoBase CHAR(7) NOT NULL UNIQUE,
    TieneVariantes BIT NOT NULL,
    Estado BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_ProductoInventario_productos
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),

    CONSTRAINT FK_ProductoInventario_Categorias
        FOREIGN KEY (IdCategoria) REFERENCES Categorias(IdCategoria)
);
GO

/*====================================================
  SECUENCIA PARA GENERAR CÓDIGO BASE
====================================================*/
CREATE TABLE SecuenciaProducto (
    Id INT PRIMARY KEY CHECK (Id = 1),
    UltimoNumero INT NOT NULL
);
GO

INSERT INTO SecuenciaProducto(Id, UltimoNumero)
VALUES(1, 0);
GO

/*====================================================
  VARIANTES DEL PRODUCTO
====================================================*/
CREATE TABLE Variantes (
    IdVariante INT IDENTITY(1,1) PRIMARY KEY,
    id_producto INT NOT NULL,
    Descripcion NVARCHAR(100) NOT NULL,
    CodigoBarras CHAR(9) NOT NULL UNIQUE,
    PrecioUnidad DECIMAL(10,2) NOT NULL,
    PrecioMayor DECIMAL(10,2) NULL,
    Estado BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Variantes_productos
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),

    CONSTRAINT CK_Variantes_PrecioUnidad
        CHECK (PrecioUnidad > 0)
);
GO

/*====================================================
  STOCK EN ALMACÉN Y TIENDA
====================================================*/
CREATE TABLE StockSucursal (
    IdStock INT IDENTITY(1,1) PRIMARY KEY,
    IdVariante INT NOT NULL,
    TipoUbicacion VARCHAR(20) NOT NULL,
    Stock INT NOT NULL DEFAULT 0,

    CONSTRAINT FK_StockSucursal_Variantes
        FOREIGN KEY (IdVariante) REFERENCES Variantes(IdVariante),

    CONSTRAINT CK_StockSucursal_TipoUbicacion
        CHECK (TipoUbicacion IN ('ALMACEN','TIENDA')),

    CONSTRAINT CK_StockSucursal_Stock
        CHECK (Stock >= 0)
);
GO

/*====================================================
  CLIENTE POS
====================================================*/
CREATE TABLE ClientePOS (
    IdClientePOS INT IDENTITY(1,1) PRIMARY KEY,
    TipoDocumento VARCHAR(20) NOT NULL DEFAULT 'OTROS',
    NumeroDocumento VARCHAR(20) NULL,
    NombreCompleto VARCHAR(150) NOT NULL DEFAULT 'Otros clientes',
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/*====================================================
  VENTA POS
====================================================*/
CREATE TABLE VentaPOS (
    IdVenta INT IDENTITY(1,1) PRIMARY KEY,
    NumeroVenta VARCHAR(30) NOT NULL UNIQUE,
    IdClientePOS INT NULL,
    id_usuario INT NOT NULL,
    MetodoPago VARCHAR(30) NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,
    IGV DECIMAL(10,2) NOT NULL DEFAULT 0,
    Total DECIMAL(10,2) NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'COMPLETADA',
    FechaVenta DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_VentaPOS_ClientePOS
        FOREIGN KEY (IdClientePOS) REFERENCES ClientePOS(IdClientePOS),

    CONSTRAINT FK_VentaPOS_usuarios
        FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario)
);
GO

/*====================================================
  DETALLE VENTA POS
====================================================*/
CREATE TABLE DetalleVentaPOS (
    IdDetalle INT IDENTITY(1,1) PRIMARY KEY,
    IdVenta INT NOT NULL,
    id_producto INT NOT NULL,
    IdVariante INT NOT NULL,
    NombreProducto VARCHAR(150) NOT NULL,
    Categoria VARCHAR(100) NULL,
    CodigoBarra VARCHAR(50) NULL,
    TipoPrecio VARCHAR(20) NOT NULL,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    Cantidad INT NOT NULL,
    Descuento DECIMAL(10,2) NOT NULL DEFAULT 0,
    TotalLinea DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_DetalleVentaPOS_VentaPOS
        FOREIGN KEY (IdVenta) REFERENCES VentaPOS(IdVenta),

    CONSTRAINT FK_DetalleVentaPOS_productos
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),

    CONSTRAINT FK_DetalleVentaPOS_Variantes
        FOREIGN KEY (IdVariante) REFERENCES Variantes(IdVariante)
);
GO

/*====================================================
  SECUENCIA PARA NÚMERO DE VENTA POS
====================================================*/
CREATE SEQUENCE SeqVentaPOS
AS INT
START WITH 1
INCREMENT BY 1;
GO

/*=========================================
  TABLA CAJA
=========================================*/
CREATE TABLE caja(
    id_caja INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    fecha_apertura DATETIME NOT NULL DEFAULT GETDATE(),
    monto_inicial DECIMAL(10,2) NOT NULL,
    fecha_cierre DATETIME NULL,
    monto_final DECIMAL(10,2) NULL,
    estado VARCHAR(20) NOT NULL DEFAULT 'ABIERTA',

    CONSTRAINT FK_caja_usuario
    FOREIGN KEY(id_usuario)
    REFERENCES usuarios(id_usuario),

    CONSTRAINT CK_caja_estado
    CHECK(estado IN ('ABIERTA','CERRADA'))
);
GO

/*=========================================
  TABLA VENTAS
=========================================*/
CREATE TABLE ventas(
    id_venta INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    fecha DATETIME NOT NULL DEFAULT GETDATE(),
    total DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_ventas_usuarios
    FOREIGN KEY(id_usuario)
    REFERENCES usuarios(id_usuario),

    CONSTRAINT CK_ventas_total
    CHECK(total >= 0)
);
GO

/*=========================================
  TABLA DETALLE VENTA
=========================================*/
CREATE TABLE detalle_venta(
    id_detalle INT IDENTITY(1,1) PRIMARY KEY,
    id_venta INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL,
    precio DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_detalleventa_venta
    FOREIGN KEY(id_venta)
    REFERENCES ventas(id_venta),

    CONSTRAINT FK_detalleventa_producto
    FOREIGN KEY(id_producto)
    REFERENCES productos(id_producto),

    CONSTRAINT CK_detalle_cantidad
    CHECK(cantidad > 0),

    CONSTRAINT CK_detalle_precio
    CHECK(precio > 0)
);
GO

/*=========================================
  ÍNDICES
=========================================*/

CREATE INDEX IX_productos_nombre
ON productos(nombre);
GO

CREATE INDEX IX_productos_codigo
ON productos(codigo);
GO

CREATE INDEX IX_ventas_fecha
ON ventas(fecha);
GO

CREATE INDEX IX_detalle_venta
ON detalle_venta(id_venta);
GO
SELECT usuario, password
FROM usuarios;
INSERT INTO usuarios
(
    nombre,
    usuario,
    password,
    id_rol
)
VALUES
(
    'Administrador',
    'admin',
    '123456',
    1
);

