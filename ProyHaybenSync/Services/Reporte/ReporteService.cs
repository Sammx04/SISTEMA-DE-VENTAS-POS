using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.Almacen;
using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Models.Reporte;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProySistemaVentas.Services.Reporte
{
    /// <summary>
    /// Acceso a datos de los reportes.
    ///
    /// Todos los reportes se resuelven con procedimientos
    /// almacenados sp_Reporte_* creados en el script
    /// SQL/sp_reportes_SistemaVentas.sql.
    ///
    /// Base de datos: SistemaVentas
    /// Tablas de origen: VentaPOS, DetalleVentaPOS,
    /// usuarios, roles, ClientePOS, productos,
    /// ProductoInventario, Variantes, StockSucursal,
    /// Categorias y dbo.MovimientoStock.
    /// </summary>
    public class ReporteService
    {
        private readonly string _cadenaConexion;

        public ReporteService()
        {
            _cadenaConexion =
                ConexionService.ObtenerCadenaConexion();
        }

        // =====================================================
        // CATALOGOS DE LOS FILTROS
        // =====================================================

        /// <summary>
        /// Lista TODOS los usuarios, activos e inactivos,
        /// porque un reporte debe poder filtrar las ventas
        /// de un vendedor que fue dado de baja.
        /// sp_Reporte_Usuarios.
        /// </summary>
        public List<UsuarioMovimiento>
            ListarUsuarios()
        {
            List<UsuarioMovimiento> lista =
                new List<UsuarioMovimiento>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_Usuarios");

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new UsuarioMovimiento
                    {
                        IdUsuario =
                            ObtenerEntero(
                                reader,
                                "IdUsuario"),

                        Nombre =
                            ObtenerTexto(
                                reader,
                                "Nombre"),

                        Usuario =
                            ObtenerTexto(
                                reader,
                                "Usuario"),

                        Rol =
                            ObtenerTexto(
                                reader,
                                "Rol")
                    });
            }

            return lista;
        }

        /// <summary>
        /// Lista todas las categorias (activas e inactivas).
        /// sp_Reporte_Categorias.
        /// </summary>
        public List<Categoria>
            ListarCategorias()
        {
            List<Categoria> lista =
                new List<Categoria>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_Categorias");

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new Categoria
                    {
                        IdCategoria =
                            ObtenerEntero(
                                reader,
                                "IdCategoria"),

                        Nombre =
                            ObtenerTexto(
                                reader,
                                "Nombre"),

                        Estado =
                            ObtenerEntero(
                                reader,
                                "Estado") == 1
                    });
            }

            return lista;
        }

        /// <summary>
        /// Metodos de pago realmente registrados en
        /// VentaPOS. Se usa una consulta directa porque no
        /// es un reporte sino un dato de apoyo para el
        /// combo de filtro, igual que hace
        /// UsuarioService con la tabla roles.
        /// </summary>
        public List<string>
            ObtenerMetodosPago()
        {
            List<string> lista =
                new List<string>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "SELECT DISTINCT MetodoPago "
                    + "FROM dbo.VentaPOS "
                    + "WHERE MetodoPago IS NOT NULL "
                    + "ORDER BY MetodoPago",
                    cn);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                string metodo =
                    ObtenerTexto(
                        reader,
                        "MetodoPago");

                if (!string.IsNullOrWhiteSpace(metodo))
                {
                    lista.Add(metodo);
                }
            }

            return lista;
        }

        // =====================================================
        // REPORTES DE VENTAS
        // =====================================================

        /// <summary>
        /// Ventas agrupadas por dia con desglose por
        /// metodo de pago. sp_Reporte_VentasPorDia.
        /// </summary>
        public List<ReporteVenta>
            ObtenerVentasPorDia(
                FiltroReportePeriodo filtro)
        {
            List<ReporteVenta> lista =
                new List<ReporteVenta>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_VentasPorDia");

            AgregarParametroPeriodo(
                cmd,
                filtro);

            AgregarParametro(
                cmd,
                "@IdUsuario",
                filtro.IdUsuario,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@MetodoPago",
                filtro.MetodoPago,
                SqlDbType.VarChar,
                30);

            AgregarParametro(
                cmd,
                "@Estado",
                filtro.Estado,
                SqlDbType.VarChar,
                20);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new ReporteVenta
                    {
                        Fecha =
                            ObtenerFecha(
                                reader,
                                "Fecha"),

                        CantidadVentas =
                            ObtenerEntero(
                                reader,
                                "CantidadVentas"),

                        UnidadesVendidas =
                            ObtenerEntero(
                                reader,
                                "UnidadesVendidas"),

                        Subtotal =
                            ObtenerDecimal(
                                reader,
                                "Subtotal"),

                        RecargoTarjeta =
                            ObtenerDecimal(
                                reader,
                                "RecargoTarjeta"),

                        Total =
                            ObtenerDecimal(
                                reader,
                                "Total"),

                        TotalEfectivo =
                            ObtenerDecimal(
                                reader,
                                "TotalEfectivo"),

                        TotalYapePlin =
                            ObtenerDecimal(
                                reader,
                                "TotalYapePlin"),

                        TotalTarjeta =
                            ObtenerDecimal(
                                reader,
                                "TotalTarjeta"),

                        TotalOtros =
                            ObtenerDecimal(
                                reader,
                                "TotalOtros")
                    });
            }

            return lista;
        }

        /// <summary>
        /// Cabeceras de las ventas del periodo mas los
        /// totales generales por metodo de pago.
        /// sp_Reporte_VentasDetalle.
        /// </summary>
        public List<VentaReporteDetalle>
            ObtenerDetalleVentas(
                FiltroReportePeriodo filtro,
                out ReporteTotales totales)
        {
            List<VentaReporteDetalle> lista =
                new List<VentaReporteDetalle>();

            totales = new ReporteTotales();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_VentasDetalle");

            AgregarParametroPeriodo(
                cmd,
                filtro);

            AgregarParametro(
                cmd,
                "@IdUsuario",
                filtro.IdUsuario,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@MetodoPago",
                filtro.MetodoPago,
                SqlDbType.VarChar,
                30);

            AgregarParametro(
                cmd,
                "@Estado",
                filtro.Estado,
                SqlDbType.VarChar,
                20);

            AgregarParametro(
                cmd,
                "@Texto",
                filtro.Texto,
                SqlDbType.VarChar,
                100);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new VentaReporteDetalle
                    {
                        IdVenta =
                            ObtenerEntero(
                                reader,
                                "IdVenta"),

                        NumeroVenta =
                            ObtenerTexto(
                                reader,
                                "NumeroVenta"),

                        FechaVenta =
                            ObtenerFecha(
                                reader,
                                "FechaVenta"),

                        IdUsuario =
                            ObtenerEntero(
                                reader,
                                "IdUsuario"),

                        Usuario =
                            ObtenerTexto(
                                reader,
                                "Usuario"),

                        Rol =
                            ObtenerTexto(
                                reader,
                                "Rol"),

                        TipoDocumento =
                            ObtenerTexto(
                                reader,
                                "TipoDocumento"),

                        NumeroDocumento =
                            ObtenerTexto(
                                reader,
                                "NumeroDocumento"),

                        Cliente =
                            ObtenerTexto(
                                reader,
                                "Cliente"),

                        MetodoPago =
                            ObtenerTexto(
                                reader,
                                "MetodoPago"),

                        Subtotal =
                            ObtenerDecimal(
                                reader,
                                "Subtotal"),

                        RecargoTarjeta =
                            ObtenerDecimal(
                                reader,
                                "RecargoTarjeta"),

                        Total =
                            ObtenerDecimal(
                                reader,
                                "Total"),

                        Descuento =
                            ObtenerDecimal(
                                reader,
                                "Descuento"),

                        Estado =
                            ObtenerTexto(
                                reader,
                                "Estado"),

                        Unidades =
                            ObtenerEntero(
                                reader,
                                "Unidades"),

                        Items =
                            ObtenerEntero(
                                reader,
                                "Items")
                    });
            }

            if (reader.NextResult() && reader.Read())
            {
                totales = new ReporteTotales
                {
                    CantidadVentas =
                        ObtenerEntero(
                            reader,
                            "CantidadVentas"),

                    UnidadesVendidas =
                        ObtenerEntero(
                            reader,
                            "UnidadesVendidas"),

                    Subtotal =
                        ObtenerDecimal(
                            reader,
                            "Subtotal"),

                    RecargoTarjeta =
                        ObtenerDecimal(
                            reader,
                            "RecargoTarjeta"),

                    Total =
                        ObtenerDecimal(
                            reader,
                            "Total")
                };
            }

            if (reader.NextResult() && reader.Read())
            {
                totales.TotalEfectivo =
                    ObtenerDecimal(
                        reader,
                        "TotalEfectivo");

                totales.TotalYapePlin =
                    ObtenerDecimal(
                        reader,
                        "TotalYapePlin");

                totales.TotalTarjeta =
                    ObtenerDecimal(
                        reader,
                        "TotalTarjeta");

                totales.TotalOtros =
                    ObtenerDecimal(
                        reader,
                        "TotalOtros");
            }

            return lista;
        }

        /// <summary>
        /// Lineas de venta del periodo con sus totales.
        /// sp_Reporte_LineasVenta.
        /// </summary>
        public List<LineaVentaReporte>
            ObtenerLineasVenta(
                FiltroReportePeriodo filtro,
                out ReporteTotalesLineas totales)
        {
            List<LineaVentaReporte> lista =
                new List<LineaVentaReporte>();

            totales = new ReporteTotalesLineas();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_LineasVenta");

            AgregarParametroPeriodo(
                cmd,
                filtro);

            AgregarParametro(
                cmd,
                "@IdUsuario",
                filtro.IdUsuario,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@IdCategoria",
                (filtro as FiltroReporteProductos)
                    ?.IdCategoria,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@MetodoPago",
                filtro.MetodoPago,
                SqlDbType.VarChar,
                30);

            AgregarParametro(
                cmd,
                "@Texto",
                filtro.Texto,
                SqlDbType.VarChar,
                100);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new LineaVentaReporte
                    {
                        IdVenta =
                            ObtenerEntero(
                                reader,
                                "IdVenta"),

                        NumeroVenta =
                            ObtenerTexto(
                                reader,
                                "NumeroVenta"),

                        FechaVenta =
                            ObtenerFecha(
                                reader,
                                "FechaVenta"),

                        Usuario =
                            ObtenerTexto(
                                reader,
                                "Usuario"),

                        MetodoPago =
                            ObtenerTexto(
                                reader,
                                "MetodoPago"),

                        IdDetalle =
                            ObtenerEntero(
                                reader,
                                "IdDetalle"),

                        IdProducto =
                            ObtenerEntero(
                                reader,
                                "IdProducto"),

                        IdVariante =
                            ObtenerEntero(
                                reader,
                                "IdVariante"),

                        NombreProducto =
                            ObtenerTexto(
                                reader,
                                "NombreProducto"),

                        Variante =
                            ObtenerTexto(
                                reader,
                                "Variante"),

                        Categoria =
                            ObtenerTexto(
                                reader,
                                "Categoria"),

                        CodigoBarra =
                            ObtenerTexto(
                                reader,
                                "CodigoBarra"),

                        TipoPrecio =
                            ObtenerTexto(
                                reader,
                                "TipoPrecio"),

                        PrecioUnitario =
                            ObtenerDecimal(
                                reader,
                                "PrecioUnitario"),

                        Cantidad =
                            ObtenerEntero(
                                reader,
                                "Cantidad"),

                        Descuento =
                            ObtenerDecimal(
                                reader,
                                "Descuento"),

                        TotalLinea =
                            ObtenerDecimal(
                                reader,
                                "TotalLinea")
                    });
            }

            if (reader.NextResult() && reader.Read())
            {
                totales = new ReporteTotalesLineas
                {
                    CantidadLineas =
                        ObtenerEntero(
                            reader,
                            "CantidadLineas"),

                    UnidadesVendidas =
                        ObtenerEntero(
                            reader,
                            "UnidadesVendidas"),

                    Descuentos =
                        ObtenerDecimal(
                            reader,
                            "Descuentos"),

                    TotalGenerado =
                        ObtenerDecimal(
                            reader,
                            "TotalGenerado")
                };
            }

            return lista;
        }

        /// <summary>
        /// Ranking de productos mas vendidos en el
        /// periodo. sp_Reporte_ProductosVendidos.
        /// </summary>
        public List<ProductoMasVendido>
            ObtenerProductosMasVendidos(
                FiltroReporteProductos filtro)
        {
            List<ProductoMasVendido> lista =
                new List<ProductoMasVendido>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_ProductosVendidos");

            AgregarParametroPeriodo(
                cmd,
                filtro);

            AgregarParametro(
                cmd,
                "@IdCategoria",
                filtro.IdCategoria,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@TopN",
                filtro.TopN,
                SqlDbType.Int);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new ProductoMasVendido
                    {
                        IdProducto =
                            ObtenerEntero(
                                reader,
                                "IdProducto"),

                        IdVariante =
                            ObtenerEntero(
                                reader,
                                "IdVariante"),

                        NombreProducto =
                            ObtenerTexto(
                                reader,
                                "NombreProducto"),

                        Variante =
                            ObtenerTexto(
                                reader,
                                "Variante"),

                        Categoria =
                            ObtenerTexto(
                                reader,
                                "Categoria"),

                        CodigoBarra =
                            ObtenerTexto(
                                reader,
                                "CodigoBarra"),

                        CantidadVendida =
                            ObtenerEntero(
                                reader,
                                "CantidadVendida"),

                        TotalGenerado =
                            ObtenerDecimal(
                                reader,
                                "TotalGenerado"),

                        Descuentos =
                            ObtenerDecimal(
                                reader,
                                "Descuentos"),

                        NumeroVentas =
                            ObtenerEntero(
                                reader,
                                "NumeroVentas"),

                        PrecioPromedio =
                            ObtenerDecimal(
                                reader,
                                "PrecioPromedio"),

                        UltimaVenta =
                            ObtenerFecha(
                                reader,
                                "UltimaVenta")
                    });
            }

            return lista;
        }

        /// <summary>
        /// Rendimiento por vendedor.
        /// sp_Reporte_VentasPorUsuario.
        /// </summary>
        public List<VentaPorUsuarioReporte>
            ObtenerVentasPorUsuario(
                FiltroReportePeriodo filtro)
        {
            List<VentaPorUsuarioReporte> lista =
                new List<VentaPorUsuarioReporte>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_VentasPorUsuario");

            AgregarParametroPeriodo(
                cmd,
                filtro);

            AgregarParametro(
                cmd,
                "@IdUsuario",
                filtro.IdUsuario,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@MetodoPago",
                filtro.MetodoPago,
                SqlDbType.VarChar,
                30);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new VentaPorUsuarioReporte
                    {
                        IdUsuario =
                            ObtenerEntero(
                                reader,
                                "IdUsuario"),

                        Usuario =
                            ObtenerTexto(
                                reader,
                                "Usuario"),

                        Rol =
                            ObtenerTexto(
                                reader,
                                "Rol"),

                        CantidadVentas =
                            ObtenerEntero(
                                reader,
                                "CantidadVentas"),

                        UnidadesVendidas =
                            ObtenerEntero(
                                reader,
                                "UnidadesVendidas"),

                        Subtotal =
                            ObtenerDecimal(
                                reader,
                                "Subtotal"),

                        RecargoTarjeta =
                            ObtenerDecimal(
                                reader,
                                "RecargoTarjeta"),

                        Total =
                            ObtenerDecimal(
                                reader,
                                "Total"),

                        TicketPromedio =
                            ObtenerDecimal(
                                reader,
                                "TicketPromedio"),

                        TotalEfectivo =
                            ObtenerDecimal(
                                reader,
                                "TotalEfectivo"),

                        TotalYapePlin =
                            ObtenerDecimal(
                                reader,
                                "TotalYapePlin"),

                        TotalTarjeta =
                            ObtenerDecimal(
                                reader,
                                "TotalTarjeta")
                    });
            }

            return lista;
        }

        /// <summary>
        /// Participacion de cada categoria en las ventas.
        /// sp_Reporte_VentasPorCategoria.
        /// </summary>
        public List<VentaPorCategoriaReporte>
            ObtenerVentasPorCategoria(
                FiltroReportePeriodo filtro)
        {
            List<VentaPorCategoriaReporte> lista =
                new List<VentaPorCategoriaReporte>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_VentasPorCategoria");

            AgregarParametroPeriodo(
                cmd,
                filtro);

            AgregarParametro(
                cmd,
                "@IdCategoria",
                (filtro as FiltroReporteProductos)
                    ?.IdCategoria,
                SqlDbType.Int);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new VentaPorCategoriaReporte
                    {
                        IdCategoria =
                            ObtenerEntero(
                                reader,
                                "IdCategoria"),

                        Categoria =
                            ObtenerTexto(
                                reader,
                                "Categoria"),

                        NumeroVentas =
                            ObtenerEntero(
                                reader,
                                "NumeroVentas"),

                        Productos =
                            ObtenerEntero(
                                reader,
                                "Productos"),

                        CantidadVendida =
                            ObtenerEntero(
                                reader,
                                "CantidadVendida"),

                        TotalGenerado =
                            ObtenerDecimal(
                                reader,
                                "TotalGenerado"),

                        Descuentos =
                            ObtenerDecimal(
                                reader,
                                "Descuentos")
                    });
            }

            return lista;
        }

        /// <summary>
        /// Reparto entre precio unidad y precio por mayor.
        /// sp_Reporte_VentasPorTipoPrecio.
        /// </summary>
        public List<VentaPorTipoPrecioReporte>
            ObtenerVentasPorTipoPrecio(
                FiltroReportePeriodo filtro)
        {
            List<VentaPorTipoPrecioReporte> lista =
                new List<VentaPorTipoPrecioReporte>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_VentasPorTipoPrecio");

            AgregarParametroPeriodo(
                cmd,
                filtro);

            AgregarParametro(
                cmd,
                "@IdCategoria",
                (filtro as FiltroReporteProductos)
                    ?.IdCategoria,
                SqlDbType.Int);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new VentaPorTipoPrecioReporte
                    {
                        TipoPrecio =
                            ObtenerTexto(
                                reader,
                                "TipoPrecio"),

                        Cantidad =
                            ObtenerEntero(
                                reader,
                                "Cantidad"),

                        NumeroVentas =
                            ObtenerEntero(
                                reader,
                                "NumeroVentas"),

                        TotalGenerado =
                            ObtenerDecimal(
                                reader,
                                "TotalGenerado")
                    });
            }

            return lista;
        }

        // =====================================================
        // REPORTES DE INVENTARIO
        // =====================================================

        /// <summary>
        /// Stock actual por variante con los filtros de
        /// ubicacion y estado. sp_Reporte_StockActual.
        /// </summary>
        public List<InventarioReporte>
            ObtenerStockActual(
                FiltroReporteInventario filtro)
        {
            List<InventarioReporte> lista =
                new List<InventarioReporte>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_StockActual");

            AgregarParametro(
                cmd,
                "@IdCategoria",
                filtro.IdCategoria,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@TipoUbicacion",
                filtro.TipoUbicacion,
                SqlDbType.VarChar,
                20);

            AgregarParametro(
                cmd,
                "@EstadoStock",
                filtro.EstadoStock,
                SqlDbType.VarChar,
                20);

            AgregarParametro(
                cmd,
                "@Texto",
                filtro.Texto,
                SqlDbType.VarChar,
                100);

            AgregarParametro(
                cmd,
                "@IncluirInactivos",
                filtro.IncluirInactivos,
                SqlDbType.Bit);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new InventarioReporte
                    {
                        IdProducto =
                            ObtenerEntero(
                                reader,
                                "IdProducto"),

                        CodigoBase =
                            ObtenerTexto(
                                reader,
                                "CodigoBase"),

                        NombreProducto =
                            ObtenerTexto(
                                reader,
                                "NombreProducto"),

                        DescripcionProducto =
                            ObtenerTexto(
                                reader,
                                "DescripcionProducto"),

                        IdCategoria =
                            ObtenerEntero(
                                reader,
                                "IdCategoria"),

                        Categoria =
                            ObtenerTexto(
                                reader,
                                "Categoria"),

                        TieneVariantes =
                            ObtenerEntero(
                                reader,
                                "TieneVariantes") == 1,

                        IdVariante =
                            ObtenerEntero(
                                reader,
                                "IdVariante"),

                        Variante =
                            ObtenerTexto(
                                reader,
                                "Variante"),

                        CodigoBarras =
                            ObtenerTexto(
                                reader,
                                "CodigoBarras"),

                        PrecioUnidad =
                            ObtenerDecimal(
                                reader,
                                "PrecioUnidad"),

                        PrecioMayor =
                            ObtenerDecimal(
                                reader,
                                "PrecioMayor"),

                        StockAlmacen =
                            ObtenerEntero(
                                reader,
                                "StockAlmacen"),

                        StockTienda =
                            ObtenerEntero(
                                reader,
                                "StockTienda"),

                        StockTotal =
                            ObtenerEntero(
                                reader,
                                "StockTotal"),

                        ValorVenta =
                            ObtenerDecimal(
                                reader,
                                "ValorVenta")
                    });
            }

            return lista;
        }

        /// <summary>
        /// Valorizacion del inventario por categoria.
        /// sp_Reporte_ValorizacionInventario.
        /// </summary>
        public List<ValorizacionReporte>
            ObtenerValorizacionInventario(
                FiltroReporteInventario filtro)
        {
            List<ValorizacionReporte> lista =
                new List<ValorizacionReporte>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_ValorizacionInventario");

            AgregarParametro(
                cmd,
                "@IdCategoria",
                filtro.IdCategoria,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@TipoUbicacion",
                filtro.TipoUbicacion,
                SqlDbType.VarChar,
                20);

            AgregarParametro(
                cmd,
                "@IncluirInactivos",
                filtro.IncluirInactivos,
                SqlDbType.Bit);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new ValorizacionReporte
                    {
                        IdCategoria =
                            ObtenerEntero(
                                reader,
                                "IdCategoria"),

                        Categoria =
                            ObtenerTexto(
                                reader,
                                "Categoria"),

                        Productos =
                            ObtenerEntero(
                                reader,
                                "Productos"),

                        Variantes =
                            ObtenerEntero(
                                reader,
                                "Variantes"),

                        Unidades =
                            ObtenerEntero(
                                reader,
                                "Unidades"),

                        ValorAlmacen =
                            ObtenerDecimal(
                                reader,
                                "ValorAlmacen"),

                        ValorTienda =
                            ObtenerDecimal(
                                reader,
                                "ValorTienda"),

                        ValorTotal =
                            ObtenerDecimal(
                                reader,
                                "ValorTotal")
                    });
            }

            return lista;
        }

        /// <summary>
        /// Kardex de movimientos de stock.
        /// sp_Reporte_MovimientosStock.
        ///
        /// La tabla dbo.MovimientoStock todavia no es
        /// alimentada por ningun procedimiento, por lo que
        /// el reporte devuelve una lista vacia hasta que se
        /// instrumente el registro de movimientos.
        /// </summary>
        public List<MovimientoStockReporte>
            ObtenerMovimientosStock(
                FiltroReportePeriodo filtro)
        {
            List<MovimientoStockReporte> lista =
                new List<MovimientoStockReporte>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                CrearComando(
                    cn,
                    "sp_Reporte_MovimientosStock");

            AgregarParametroPeriodo(
                cmd,
                filtro);

            AgregarParametro(
                cmd,
                "@IdUsuario",
                filtro.IdUsuario,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@IdVariante",
                null,
                SqlDbType.Int);

            AgregarParametro(
                cmd,
                "@TipoMovimiento",
                null,
                SqlDbType.VarChar,
                30);

            AgregarParametro(
                cmd,
                "@Texto",
                filtro.Texto,
                SqlDbType.VarChar,
                100);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new MovimientoStockReporte
                    {
                        IdMovimiento =
                            ObtenerEntero(
                                reader,
                                "IdMovimiento"),

                        IdProducto =
                            ObtenerEntero(
                                reader,
                                "IdProducto"),

                        IdVariante =
                            ObtenerEntero(
                                reader,
                                "IdVariante"),

                        NombreProducto =
                            ObtenerTexto(
                                reader,
                                "NombreProducto"),

                        Variante =
                            ObtenerTexto(
                                reader,
                                "Variante"),

                        Categoria =
                            ObtenerTexto(
                                reader,
                                "Categoria"),

                        TipoMovimiento =
                            ObtenerTexto(
                                reader,
                                "TipoMovimiento"),

                        Cantidad =
                            ObtenerEntero(
                                reader,
                                "Cantidad"),

                        StockAlmacenAntes =
                            ObtenerEntero(
                                reader,
                                "StockAlmacenAntes"),

                        StockAlmacenDespues =
                            ObtenerEntero(
                                reader,
                                "StockAlmacenDespues"),

                        StockTiendaAntes =
                            ObtenerEntero(
                                reader,
                                "StockTiendaAntes"),

                        StockTiendaDespues =
                            ObtenerEntero(
                                reader,
                                "StockTiendaDespues"),

                        Usuario =
                            ObtenerTexto(
                                reader,
                                "Usuario"),

                        Observacion =
                            ObtenerTexto(
                                reader,
                                "Observacion"),

                        FechaMovimiento =
                            ObtenerFecha(
                                reader,
                                "FechaMovimiento")
                    });
            }

            return lista;
        }

        // =====================================================
        // MÉTODOS AUXILIARES
        // =====================================================

        private static SqlCommand CrearComando(
            SqlConnection cn,
            string nombreProcedimiento)
        {
            SqlCommand cmd =
                new SqlCommand(
                    nombreProcedimiento,
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            return cmd;
        }

        private static void AgregarParametro(
            SqlCommand cmd,
            string nombre,
            object? valor,
            SqlDbType tipo,
            int tamano = 0)
        {
            SqlParameter parametro =
                new SqlParameter(
                    nombre,
                    tipo);

            if (tamano > 0)
            {
                parametro.Size = tamano;
            }

            parametro.Value =
                valor ?? DBNull.Value;

            cmd.Parameters.Add(parametro);
        }

        private static void AgregarParametroPeriodo(
            SqlCommand cmd,
            FiltroReportePeriodo filtro)
        {
            DateTime desde =
                filtro.FechaDesde.Date;

            DateTime hasta =
                filtro.FechaHasta.Date;

            if (desde > hasta)
            {
                (desde, hasta) = (hasta, desde);
            }

            AgregarParametro(
                cmd,
                "@FechaDesde",
                desde,
                SqlDbType.Date);

            AgregarParametro(
                cmd,
                "@FechaHasta",
                hasta,
                SqlDbType.Date);
        }

        private static bool ExisteColumna(
            SqlDataReader reader,
            string nombreColumna)
        {
            for (int indice = 0;
                 indice < reader.FieldCount;
                 indice++)
            {
                if (string.Equals(
                    reader.GetName(indice),
                    nombreColumna,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ObtenerTexto(
            SqlDataReader reader,
            params string[] nombresColumnas)
        {
            foreach (
                string nombreColumna
                in nombresColumnas)
            {
                if (!ExisteColumna(
                        reader,
                        nombreColumna))
                {
                    continue;
                }

                int indice =
                    reader.GetOrdinal(
                        nombreColumna);

                if (reader.IsDBNull(indice))
                {
                    return string.Empty;
                }

                return Convert
                    .ToString(reader[indice])
                    ?.Trim()
                    ?? string.Empty;
            }

            return string.Empty;
        }

        private static int ObtenerEntero(
            SqlDataReader reader,
            params string[] nombresColumnas)
        {
            foreach (
                string nombreColumna
                in nombresColumnas)
            {
                if (!ExisteColumna(
                        reader,
                        nombreColumna))
                {
                    continue;
                }

                int indice =
                    reader.GetOrdinal(
                        nombreColumna);

                if (reader.IsDBNull(indice))
                {
                    return 0;
                }

                return Convert.ToInt32(
                    reader[indice]);
            }

            return 0;
        }

        private static decimal ObtenerDecimal(
            SqlDataReader reader,
            params string[] nombresColumnas)
        {
            foreach (
                string nombreColumna
                in nombresColumnas)
            {
                if (!ExisteColumna(
                        reader,
                        nombreColumna))
                {
                    continue;
                }

                int indice =
                    reader.GetOrdinal(
                        nombreColumna);

                if (reader.IsDBNull(indice))
                {
                    return 0m;
                }

                return Convert.ToDecimal(
                    reader[indice]);
            }

            return 0m;
        }

        private static DateTime ObtenerFecha(
            SqlDataReader reader,
            params string[] nombresColumnas)
        {
            foreach (
                string nombreColumna
                in nombresColumnas)
            {
                if (!ExisteColumna(
                        reader,
                        nombreColumna))
                {
                    continue;
                }

                int indice =
                    reader.GetOrdinal(
                        nombreColumna);

                if (reader.IsDBNull(indice))
                {
                    return DateTime.MinValue;
                }

                return Convert.ToDateTime(
                    reader[indice]);
            }

            return DateTime.MinValue;
        }
    }
}
