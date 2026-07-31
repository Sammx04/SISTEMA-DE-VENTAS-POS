using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Models.PrestamosCreditos;
using ProySistemaVentas.Services;

namespace ProySistemaVentas.Services.PrestamosCreditos
{
    public class PrestamoCreditoService
    {
        /*====================================================
          PRODUCTOS
        ====================================================*/

        public List<ProductoPrestamoCredito> BuscarProductos(
            string texto)
        {
            List<ProductoPrestamoCredito> productos =
                new List<ProductoPrestamoCredito>();

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_BuscarProductos",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@Texto",
                SqlDbType.NVarChar,
                150
            ).Value = texto?.Trim() ?? string.Empty;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(
                    MapearProducto(reader));
            }

            return productos;
        }

        public ProductoPrestamoCredito
            ObtenerProductoPorCodigo(
                string codigoBarra)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_ObtenerProductoPorCodigo",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@CodigoBarra",
                SqlDbType.VarChar,
                50
            ).Value = codigoBarra?.Trim() ??
                      string.Empty;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return MapearProducto(reader);
        }

        private static ProductoPrestamoCredito
            MapearProducto(
                SqlDataReader reader)
        {
            return new ProductoPrestamoCredito
            {
                IdProducto =
                    LeerInt(
                        reader,
                        "IdProducto"),

                IdVariante =
                    LeerInt(
                        reader,
                        "IdVariante"),

                NombreProducto =
                    LeerString(
                        reader,
                        "NombreProducto"),

                Categoria =
                    LeerString(
                        reader,
                        "Categoria"),

                Variante =
                    LeerString(
                        reader,
                        "Variante"),

                CodigoBarra =
                    LeerString(
                        reader,
                        "CodigoBarra"),

                PrecioUnidad =
                    LeerDecimal(
                        reader,
                        "PrecioUnidad"),

                PrecioMayor =
                    LeerDecimal(
                        reader,
                        "PrecioMayor"),

                StockTienda =
                    LeerInt(
                        reader,
                        "StockTienda")
            };
        }

        /*====================================================
          RESUMEN
        ====================================================*/

        public ResumenPrestamoCredito ObtenerResumen()
        {
            ResumenPrestamoCredito resumen =
                new ResumenPrestamoCredito();

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_ObtenerResumen",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (reader.Read())
            {
                resumen.PrestamosPendientes =
                    LeerInt(
                        reader,
                        "PrestamosPendientes");

                resumen.StockPrestadoTotal =
                    LeerInt(
                        reader,
                        "StockPrestadoTotal");

                resumen.CreditosPendientes =
                    LeerInt(
                        reader,
                        "CreditosPendientes");

                resumen.DeudaTotalAcumulada =
                    LeerDecimal(
                        reader,
                        "DeudaTotalAcumulada");
            }

            return resumen;
        }

        /*====================================================
          TRABAJADORES
        ====================================================*/

        public List<UsuarioMovimiento>
            ListarUsuariosActivos()
        {
            List<UsuarioMovimiento> usuarios =
                new List<UsuarioMovimiento>();

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_ListarUsuariosActivos",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                usuarios.Add(
                    new UsuarioMovimiento
                    {
                        IdUsuario =
                            LeerIntAlternativo(
                                reader,
                                "IdUsuario",
                                "id_usuario"),

                        Nombre =
                            LeerStringAlternativo(
                                reader,
                                "Nombre",
                                "nombre"),

                        Usuario =
                            LeerStringAlternativo(
                                reader,
                                "Usuario",
                                "usuario"),

                        Rol =
                            LeerStringAlternativo(
                                reader,
                                "Rol",
                                "rol")
                    });
            }

            return usuarios;
        }

        /*====================================================
          REGISTRAR PRÉSTAMO
        ====================================================*/

        public ResultadoOperacionPrestamoCredito
            RegistrarPrestamo(
                int idProducto,
                int idVariante,
                string clienteTienda,
                int cantidad,
                string tipoPrecio,
                decimal precioUnitario,
                int idUsuario,
                string observacion = null)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_RegistrarPrestamo",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@IdProducto",
                SqlDbType.Int
            ).Value = idProducto;

            cmd.Parameters.Add(
                "@IdVariante",
                SqlDbType.Int
            ).Value = idVariante;

            cmd.Parameters.Add(
                "@ClienteTienda",
                SqlDbType.NVarChar,
                150
            ).Value = clienteTienda?.Trim() ??
                      string.Empty;

            cmd.Parameters.Add(
                "@Cantidad",
                SqlDbType.Int
            ).Value = cantidad;

            cmd.Parameters.Add(
                "@TipoPrecio",
                SqlDbType.VarChar,
                20
            ).Value = tipoPrecio?.Trim().ToUpper() ??
                      string.Empty;

            AgregarParametroDecimal(
                cmd,
                "@PrecioUnitario",
                precioUnitario);

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int
            ).Value = idUsuario;

            AgregarParametroNullable(
                cmd,
                "@Observacion",
                SqlDbType.NVarChar,
                300,
                observacion);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "No se recibió el resultado del préstamo.");
            }

            return MapearResultadoOperacion(
                reader);
        }

        /*====================================================
          REGISTRAR CRÉDITO
        ====================================================*/

        public ResultadoOperacionPrestamoCredito
            RegistrarCredito(
                int idProducto,
                int idVariante,
                string clienteTienda,
                int cantidad,
                string tipoPrecio,
                decimal precioUnitario,
                DateTime fechaPagoEsperada,
                int idUsuario,
                string observacion = null)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_RegistrarCredito",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@IdProducto",
                SqlDbType.Int
            ).Value = idProducto;

            cmd.Parameters.Add(
                "@IdVariante",
                SqlDbType.Int
            ).Value = idVariante;

            cmd.Parameters.Add(
                "@ClienteTienda",
                SqlDbType.NVarChar,
                150
            ).Value = clienteTienda?.Trim() ??
                      string.Empty;

            cmd.Parameters.Add(
                "@Cantidad",
                SqlDbType.Int
            ).Value = cantidad;

            cmd.Parameters.Add(
                "@TipoPrecio",
                SqlDbType.VarChar,
                20
            ).Value = tipoPrecio?.Trim().ToUpper() ??
                      string.Empty;

            AgregarParametroDecimal(
                cmd,
                "@PrecioUnitario",
                precioUnitario);

            cmd.Parameters.Add(
                "@FechaPagoEsperada",
                SqlDbType.Date
            ).Value = fechaPagoEsperada.Date;

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int
            ).Value = idUsuario;

            AgregarParametroNullable(
                cmd,
                "@Observacion",
                SqlDbType.NVarChar,
                300,
                observacion);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "No se recibió el resultado del crédito.");
            }

            return MapearResultadoOperacion(
                reader);
        }

        /*====================================================
          LISTAR PRÉSTAMOS
        ====================================================*/

        public List<PrestamoCreditoItem>
            ListarPrestamos(
                string texto = null,
                string estado = null,
                DateTime? fechaInicio = null,
                DateTime? fechaFin = null,
                int pagina = 1,
                int tamanoPagina = 5)
        {
            List<PrestamoCreditoItem> prestamos =
                new List<PrestamoCreditoItem>();

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_ListarPrestamos",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            AgregarFiltrosListado(
                cmd,
                texto,
                estado,
                fechaInicio,
                fechaFin,
                pagina,
                tamanoPagina);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                prestamos.Add(
                    MapearPrestamo(reader));
            }

            return prestamos;
        }

        private static PrestamoCreditoItem
            MapearPrestamo(
                SqlDataReader reader)
        {
            return new PrestamoCreditoItem
            {
                IdPrestamoCredito =
                    LeerInt(
                        reader,
                        "IdPrestamoCredito"),

                NumeroOperacion =
                    LeerString(
                        reader,
                        "NumeroOperacion"),

                TipoOperacion =
                    "PRESTAMO",

                IdProducto =
                    LeerInt(
                        reader,
                        "IdProducto"),

                IdVariante =
                    LeerInt(
                        reader,
                        "IdVariante"),

                NombreProducto =
                    LeerString(
                        reader,
                        "NombreProducto"),

                Variante =
                    LeerString(
                        reader,
                        "Variante"),

                CodigoBarra =
                    LeerString(
                        reader,
                        "CodigoBarra"),

                ClienteTienda =
                    LeerString(
                        reader,
                        "ClienteTienda"),

                Cantidad =
                    LeerInt(
                        reader,
                        "Cantidad"),

                TipoPrecio =
                    LeerString(
                        reader,
                        "TipoPrecio"),

                PrecioUnitario =
                    LeerDecimal(
                        reader,
                        "PrecioUnitario"),

                PrecioEditado =
                    LeerBool(
                        reader,
                        "PrecioEditado"),

                Total =
                    LeerDecimal(
                        reader,
                        "Total"),

                FechaRegistro =
                    LeerDateTime(
                        reader,
                        "FechaPrestamo"),

                Estado =
                    LeerString(
                        reader,
                        "Estado"),

                AccionFinal =
                    LeerString(
                        reader,
                        "AccionFinal"),

                FechaFinalizacion =
                    LeerDateTimeNullable(
                        reader,
                        "FechaDevolucion"),

                StockDescontado =
                    LeerBool(
                        reader,
                        "StockDescontado"),

                Observacion =
                    LeerString(
                        reader,
                        "Observacion"),

                IdUsuarioRegistro =
                    LeerInt(
                        reader,
                        "IdUsuarioRegistro"),

                NombreTrabajadorRegistro =
                    LeerString(
                        reader,
                        "NombreTrabajadorRegistro"),

                IdUsuarioUltimaAccion =
                    LeerIntNullable(
                        reader,
                        "IdUsuarioUltimaAccion"),

                NombreTrabajadorUltimaAccion =
                    LeerString(
                        reader,
                        "NombreTrabajadorUltimaAccion"),

                AccionActual =
                    LeerString(
                        reader,
                        "AccionActual"),

                TotalRegistros =
                    LeerInt(
                        reader,
                        "TotalRegistros"),

                TotalPaginas =
                    LeerInt(
                        reader,
                        "TotalPaginas"),

                PaginaActual =
                    LeerInt(
                        reader,
                        "PaginaActual")
            };
        }

        /*====================================================
          LISTAR CRÉDITOS
        ====================================================*/

        public List<PrestamoCreditoItem>
            ListarCreditos(
                string texto = null,
                string estado = null,
                DateTime? fechaInicio = null,
                DateTime? fechaFin = null,
                int pagina = 1,
                int tamanoPagina = 5)
        {
            List<PrestamoCreditoItem> creditos =
                new List<PrestamoCreditoItem>();

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_ListarCreditos",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            AgregarFiltrosListado(
                cmd,
                texto,
                estado,
                fechaInicio,
                fechaFin,
                pagina,
                tamanoPagina);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                creditos.Add(
                    MapearCredito(reader));
            }

            return creditos;
        }

        private static PrestamoCreditoItem
            MapearCredito(
                SqlDataReader reader)
        {
            return new PrestamoCreditoItem
            {
                IdPrestamoCredito =
                    LeerInt(
                        reader,
                        "IdPrestamoCredito"),

                NumeroOperacion =
                    LeerString(
                        reader,
                        "NumeroOperacion"),

                TipoOperacion =
                    "CREDITO",

                IdProducto =
                    LeerInt(
                        reader,
                        "IdProducto"),

                IdVariante =
                    LeerInt(
                        reader,
                        "IdVariante"),

                NombreProducto =
                    LeerString(
                        reader,
                        "NombreProducto"),

                Variante =
                    LeerString(
                        reader,
                        "Variante"),

                CodigoBarra =
                    LeerString(
                        reader,
                        "CodigoBarra"),

                ClienteTienda =
                    LeerString(
                        reader,
                        "ClienteTienda"),

                Cantidad =
                    LeerInt(
                        reader,
                        "Cantidad"),

                TipoPrecio =
                    LeerString(
                        reader,
                        "TipoPrecio"),

                PrecioUnitario =
                    LeerDecimal(
                        reader,
                        "PrecioUnitario"),

                PrecioEditado =
                    LeerBool(
                        reader,
                        "PrecioEditado"),

                Total =
                    LeerDecimal(
                        reader,
                        "Deuda"),

                FechaRegistro =
                    LeerDateTime(
                        reader,
                        "FechaRegistro"),

                FechaPagoEsperada =
                    LeerDateTimeNullable(
                        reader,
                        "FechaPagoEsperada"),

                Estado =
                    LeerString(
                        reader,
                        "Estado"),

                AccionFinal =
                    LeerString(
                        reader,
                        "AccionFinal"),

                FechaFinalizacion =
                    LeerDateTimeNullable(
                        reader,
                        "FechaPagoReal"),

                MetodoPago =
                    LeerString(
                        reader,
                        "MetodoPago"),

                StockDescontado =
                    LeerBool(
                        reader,
                        "StockDescontado"),

                Observacion =
                    LeerString(
                        reader,
                        "Observacion"),

                IdUsuarioRegistro =
                    LeerInt(
                        reader,
                        "IdUsuarioRegistro"),

                NombreTrabajadorRegistro =
                    LeerString(
                        reader,
                        "NombreTrabajadorRegistro"),

                IdUsuarioUltimaAccion =
                    LeerIntNullable(
                        reader,
                        "IdUsuarioUltimaAccion"),

                NombreTrabajadorUltimaAccion =
                    LeerString(
                        reader,
                        "NombreTrabajadorUltimaAccion"),

                IdPagoCredito =
                    LeerIntNullable(
                        reader,
                        "IdPagoCredito"),

                NumeroPago =
                    LeerString(
                        reader,
                        "NumeroPago"),

                IdCaja =
                    LeerIntNullable(
                        reader,
                        "IdCaja"),

                FechaPago =
                    LeerDateTimeNullable(
                        reader,
                        "FechaPago"),

                AccionActual =
                    LeerString(
                        reader,
                        "AccionActual"),

                TotalRegistros =
                    LeerInt(
                        reader,
                        "TotalRegistros"),

                TotalPaginas =
                    LeerInt(
                        reader,
                        "TotalPaginas"),

                PaginaActual =
                    LeerInt(
                        reader,
                        "PaginaActual")
            };
        }

        /*====================================================
          DETALLE
        ====================================================*/

        public PrestamoCreditoDetalle ObtenerDetalle(
            int idPrestamoCredito)
        {
            PrestamoCreditoDetalle detalle = null;

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_ObtenerDetalle",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@IdPrestamoCredito",
                SqlDbType.Int
            ).Value = idPrestamoCredito;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (reader.Read())
            {
                detalle =
                    MapearDetalle(reader);
            }

            if (detalle == null)
            {
                return null;
            }

            if (reader.NextResult())
            {
                while (reader.Read())
                {
                    detalle.Historial.Add(
                        MapearMovimiento(reader));
                }
            }

            return detalle;
        }

        private static PrestamoCreditoDetalle
            MapearDetalle(
                SqlDataReader reader)
        {
            return new PrestamoCreditoDetalle
            {
                IdPrestamoCredito =
                    LeerInt(
                        reader,
                        "IdPrestamoCredito"),

                NumeroOperacion =
                    LeerString(
                        reader,
                        "NumeroOperacion"),

                TipoOperacion =
                    LeerString(
                        reader,
                        "TipoOperacion"),

                IdProducto =
                    LeerInt(
                        reader,
                        "IdProducto"),

                IdVariante =
                    LeerInt(
                        reader,
                        "IdVariante"),

                NombreProducto =
                    LeerString(
                        reader,
                        "NombreProducto"),

                Variante =
                    LeerString(
                        reader,
                        "Variante"),

                CodigoBarra =
                    LeerString(
                        reader,
                        "CodigoBarra"),

                ClienteTienda =
                    LeerString(
                        reader,
                        "ClienteTienda"),

                Cantidad =
                    LeerInt(
                        reader,
                        "Cantidad"),

                TipoPrecio =
                    LeerString(
                        reader,
                        "TipoPrecio"),

                PrecioUnitario =
                    LeerDecimal(
                        reader,
                        "PrecioUnitario"),

                PrecioEditado =
                    LeerBool(
                        reader,
                        "PrecioEditado"),

                Total =
                    LeerDecimal(
                        reader,
                        "Total"),

                FechaRegistro =
                    LeerDateTime(
                        reader,
                        "FechaRegistro"),

                FechaPagoEsperada =
                    LeerDateTimeNullable(
                        reader,
                        "FechaPagoEsperada"),

                Estado =
                    LeerString(
                        reader,
                        "Estado"),

                AccionFinal =
                    LeerString(
                        reader,
                        "AccionFinal"),

                FechaFinalizacion =
                    LeerDateTimeNullable(
                        reader,
                        "FechaFinalizacion"),

                MetodoPago =
                    LeerString(
                        reader,
                        "MetodoPago"),

                StockDescontado =
                    LeerBool(
                        reader,
                        "StockDescontado"),

                Observacion =
                    LeerString(
                        reader,
                        "Observacion"),

                IdUsuarioRegistro =
                    LeerInt(
                        reader,
                        "IdUsuarioRegistro"),

                NombreTrabajadorRegistro =
                    LeerString(
                        reader,
                        "NombreTrabajadorRegistro"),

                IdUsuarioUltimaAccion =
                    LeerIntNullable(
                        reader,
                        "IdUsuarioUltimaAccion"),

                NombreTrabajadorUltimaAccion =
                    LeerString(
                        reader,
                        "NombreTrabajadorUltimaAccion"),

                IdPagoCredito =
                    LeerIntNullable(
                        reader,
                        "IdPagoCredito"),

                NumeroPago =
                    LeerString(
                        reader,
                        "NumeroPago"),

                IdCaja =
                    LeerIntNullable(
                        reader,
                        "IdCaja"),

                FechaPago =
                    LeerDateTimeNullable(
                        reader,
                        "FechaPago"),

                MontoPagado =
                    LeerDecimalNullable(
                        reader,
                        "MontoPagado"),

                MetodoPagoRegistrado =
                    LeerString(
                        reader,
                        "MetodoPagoRegistrado"),

                ObservacionPago =
                    LeerString(
                        reader,
                        "ObservacionPago"),

                AccionActual =
                    LeerString(
                        reader,
                        "AccionActual")
            };
        }

        private static MovimientoPrestamoCreditoItem
            MapearMovimiento(
                SqlDataReader reader)
        {
            return new MovimientoPrestamoCreditoItem
            {
                IdMovimientoPC =
                    LeerInt(
                        reader,
                        "IdMovimientoPC"),

                IdPrestamoCredito =
                    LeerInt(
                        reader,
                        "IdPrestamoCredito"),

                TipoMovimiento =
                    LeerString(
                        reader,
                        "TipoMovimiento"),

                TipoOperacionAnterior =
                    LeerString(
                        reader,
                        "TipoOperacionAnterior"),

                TipoOperacionNuevo =
                    LeerString(
                        reader,
                        "TipoOperacionNuevo"),

                EstadoAnterior =
                    LeerString(
                        reader,
                        "EstadoAnterior"),

                EstadoNuevo =
                    LeerString(
                        reader,
                        "EstadoNuevo"),

                IdUsuario =
                    LeerInt(
                        reader,
                        "IdUsuario"),

                NombreTrabajador =
                    LeerString(
                        reader,
                        "NombreTrabajador"),

                FechaMovimiento =
                    LeerDateTime(
                        reader,
                        "FechaMovimiento"),

                Observacion =
                    LeerString(
                        reader,
                        "Observacion")
            };
        }

        /*====================================================
          DEVOLUCIÓN
        ====================================================*/

        public ResultadoOperacionPrestamoCredito
            RegistrarDevolucion(
                int idPrestamoCredito,
                int idUsuario,
                string observacion = null)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_RegistrarDevolucion",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@IdPrestamoCredito",
                SqlDbType.Int
            ).Value = idPrestamoCredito;

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int
            ).Value = idUsuario;

            AgregarParametroNullable(
                cmd,
                "@Observacion",
                SqlDbType.NVarChar,
                300,
                observacion);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "No se recibió el resultado de la devolución.");
            }

            return MapearResultadoOperacion(
                reader);
        }

        /*====================================================
          PASAR PRÉSTAMO A COBRANZA
        ====================================================*/

        public ResultadoOperacionPrestamoCredito
            ConvertirPrestamoACredito(
                int idPrestamoCredito,
                int idUsuario,
                DateTime fechaPagoEsperada,
                string observacion = null)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_ConvertirPrestamoACredito",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@IdPrestamoCredito",
                SqlDbType.Int
            ).Value = idPrestamoCredito;

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int
            ).Value = idUsuario;

            cmd.Parameters.Add(
                "@FechaPagoEsperada",
                SqlDbType.Date
            ).Value = fechaPagoEsperada.Date;

            AgregarParametroNullable(
                cmd,
                "@Observacion",
                SqlDbType.NVarChar,
                300,
                observacion);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "No se recibió el resultado de la conversión.");
            }

            return MapearResultadoOperacion(
                reader);
        }

        /*====================================================
          PASAR COBRANZA A PRÉSTAMO
        ====================================================*/

        public ResultadoOperacionPrestamoCredito
            ConvertirCreditoAPrestamo(
                int idPrestamoCredito,
                int idUsuario,
                string observacion = null)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_ConvertirCreditoAPrestamo",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@IdPrestamoCredito",
                SqlDbType.Int
            ).Value = idPrestamoCredito;

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int
            ).Value = idUsuario;

            AgregarParametroNullable(
                cmd,
                "@Observacion",
                SqlDbType.NVarChar,
                300,
                observacion);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "No se recibió el resultado de la conversión.");
            }

            return MapearResultadoOperacion(
                reader);
        }

        /*====================================================
          REGISTRAR PAGO
        ====================================================*/

        public ResultadoPagoCredito RegistrarPago(
            int idPrestamoCredito,
            int idUsuario,
            string metodoPago,
            string observacion = null)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_RegistrarPago",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@IdPrestamoCredito",
                SqlDbType.Int
            ).Value = idPrestamoCredito;

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int
            ).Value = idUsuario;

            cmd.Parameters.Add(
                "@MetodoPago",
                SqlDbType.VarChar,
                20
            ).Value = metodoPago?.Trim().ToUpper() ??
                      string.Empty;

            AgregarParametroNullable(
                cmd,
                "@Observacion",
                SqlDbType.NVarChar,
                300,
                observacion);

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "No se recibió el resultado del pago.");
            }

            return new ResultadoPagoCredito
            {
                IdPrestamoCredito =
                    LeerInt(
                        reader,
                        "IdPrestamoCredito"),

                IdPagoCredito =
                    LeerInt(
                        reader,
                        "IdPagoCredito"),

                NumeroOperacion =
                    LeerString(
                        reader,
                        "NumeroOperacion"),

                NumeroPago =
                    LeerString(
                        reader,
                        "NumeroPago"),

                ClienteTienda =
                    LeerString(
                        reader,
                        "ClienteTienda"),

                NombreProducto =
                    LeerString(
                        reader,
                        "NombreProducto"),

                Variante =
                    LeerString(
                        reader,
                        "Variante"),

                Cantidad =
                    LeerInt(
                        reader,
                        "Cantidad"),

                PrecioUnitario =
                    LeerDecimal(
                        reader,
                        "PrecioUnitario"),

                TotalPagado =
                    LeerDecimal(
                        reader,
                        "TotalPagado"),

                MetodoPago =
                    LeerString(
                        reader,
                        "MetodoPago"),

                IdCaja =
                    LeerIntNullable(
                        reader,
                        "IdCaja"),

                FechaPago =
                    LeerDateTime(
                        reader,
                        "FechaPago"),

                Estado =
                    LeerString(
                        reader,
                        "Estado")
            };
        }

        /*====================================================
          BOLETA DE COBRANZA
        ====================================================*/

        public BoletaPagoCredito ObtenerBoletaPago(
            int idPrestamoCredito)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "dbo.sp_PC_ObtenerBoletaPago",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.CommandTimeout = 60;

            cmd.Parameters.Add(
                "@IdPrestamoCredito",
                SqlDbType.Int
            ).Value = idPrestamoCredito;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new BoletaPagoCredito
            {
                IdPagoCredito =
                    LeerInt(
                        reader,
                        "IdPagoCredito"),

                NumeroPago =
                    LeerString(
                        reader,
                        "NumeroPago"),

                FechaPago =
                    LeerDateTime(
                        reader,
                        "FechaPago"),

                MetodoPago =
                    LeerString(
                        reader,
                        "MetodoPago"),

                TotalPagado =
                    LeerDecimal(
                        reader,
                        "TotalPagado"),

                IdCaja =
                    LeerIntNullable(
                        reader,
                        "IdCaja"),

                IdPrestamoCredito =
                    LeerInt(
                        reader,
                        "IdPrestamoCredito"),

                NumeroOperacion =
                    LeerString(
                        reader,
                        "NumeroOperacion"),

                ClienteTienda =
                    LeerString(
                        reader,
                        "ClienteTienda"),

                NombreProducto =
                    LeerString(
                        reader,
                        "NombreProducto"),

                Variante =
                    LeerString(
                        reader,
                        "Variante"),

                CodigoBarra =
                    LeerString(
                        reader,
                        "CodigoBarra"),

                Cantidad =
                    LeerInt(
                        reader,
                        "Cantidad"),

                TipoPrecio =
                    LeerString(
                        reader,
                        "TipoPrecio"),

                PrecioUnitario =
                    LeerDecimal(
                        reader,
                        "PrecioUnitario"),

                Total =
                    LeerDecimal(
                        reader,
                        "Total"),

                Estado =
                    LeerString(
                        reader,
                        "Estado"),

                FechaRegistro =
                    LeerDateTime(
                        reader,
                        "FechaRegistro"),

                FechaPagoEsperada =
                    LeerDateTimeNullable(
                        reader,
                        "FechaPagoEsperada"),

                IdUsuario =
                    LeerInt(
                        reader,
                        "IdUsuario"),

                NombreTrabajador =
                    LeerString(
                        reader,
                        "NombreTrabajador"),

                UsuarioTrabajador =
                    LeerString(
                        reader,
                        "UsuarioTrabajador"),

                RolTrabajador =
                    LeerString(
                        reader,
                        "RolTrabajador"),

                Observacion =
                    LeerString(
                        reader,
                        "Observacion")
            };
        }

        /*====================================================
          MAPEO DE RESULTADOS GENERALES
        ====================================================*/

        private static ResultadoOperacionPrestamoCredito
            MapearResultadoOperacion(
                SqlDataReader reader)
        {
            ResultadoOperacionPrestamoCredito resultado =
                new ResultadoOperacionPrestamoCredito();

            if (ExisteColumna(
                    reader,
                    "IdPrestamoCredito"))
            {
                resultado.IdPrestamoCredito =
                    LeerInt(
                        reader,
                        "IdPrestamoCredito");
            }

            if (ExisteColumna(
                    reader,
                    "NumeroOperacion"))
            {
                resultado.NumeroOperacion =
                    LeerString(
                        reader,
                        "NumeroOperacion");
            }

            if (ExisteColumna(
                    reader,
                    "TipoOperacion"))
            {
                resultado.TipoOperacion =
                    LeerString(
                        reader,
                        "TipoOperacion");
            }

            if (ExisteColumna(
                    reader,
                    "Estado"))
            {
                resultado.Estado =
                    LeerString(
                        reader,
                        "Estado");
            }

            if (ExisteColumna(
                    reader,
                    "AccionFinal"))
            {
                resultado.AccionFinal =
                    LeerString(
                        reader,
                        "AccionFinal");
            }

            if (ExisteColumna(
                    reader,
                    "NombreProducto"))
            {
                resultado.NombreProducto =
                    LeerString(
                        reader,
                        "NombreProducto");
            }

            if (ExisteColumna(
                    reader,
                    "Variante"))
            {
                resultado.Variante =
                    LeerString(
                        reader,
                        "Variante");
            }

            if (ExisteColumna(
                    reader,
                    "ClienteTienda"))
            {
                resultado.ClienteTienda =
                    LeerString(
                        reader,
                        "ClienteTienda");
            }

            if (ExisteColumna(
                    reader,
                    "Cantidad"))
            {
                resultado.Cantidad =
                    LeerInt(
                        reader,
                        "Cantidad");
            }

            if (ExisteColumna(
                    reader,
                    "PrecioUnitario"))
            {
                resultado.PrecioUnitario =
                    LeerDecimal(
                        reader,
                        "PrecioUnitario");
            }

            if (ExisteColumna(
                    reader,
                    "Total"))
            {
                resultado.Total =
                    LeerDecimal(
                        reader,
                        "Total");
            }

            if (ExisteColumna(
                    reader,
                    "StockTienda"))
            {
                resultado.StockTienda =
                    LeerInt(
                        reader,
                        "StockTienda");
            }

            if (ExisteColumna(
                    reader,
                    "CantidadDevuelta"))
            {
                resultado.CantidadDevuelta =
                    LeerInt(
                        reader,
                        "CantidadDevuelta");
            }

            if (ExisteColumna(
                    reader,
                    "FechaDevolucion"))
            {
                resultado.FechaDevolucion =
                    LeerDateTimeNullable(
                        reader,
                        "FechaDevolucion");
            }

            if (ExisteColumna(
                    reader,
                    "FechaPagoEsperada"))
            {
                resultado.FechaPagoEsperada =
                    LeerDateTimeNullable(
                        reader,
                        "FechaPagoEsperada");
            }

            return resultado;
        }

        /*====================================================
          PARÁMETROS
        ====================================================*/

        private static void AgregarFiltrosListado(
            SqlCommand cmd,
            string texto,
            string estado,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            int pagina,
            int tamanoPagina)
        {
            cmd.Parameters.Add(
                "@Texto",
                SqlDbType.NVarChar,
                150
            ).Value =
                string.IsNullOrWhiteSpace(texto)
                    ? (object)DBNull.Value
                    : texto.Trim();

            cmd.Parameters.Add(
                "@Estado",
                SqlDbType.VarChar,
                20
            ).Value =
                string.IsNullOrWhiteSpace(estado)
                    ? (object)DBNull.Value
                    : estado.Trim().ToUpper();

            cmd.Parameters.Add(
                "@FechaInicio",
                SqlDbType.Date
            ).Value =
                fechaInicio.HasValue
                    ? (object)fechaInicio.Value.Date
                    : DBNull.Value;

            cmd.Parameters.Add(
                "@FechaFin",
                SqlDbType.Date
            ).Value =
                fechaFin.HasValue
                    ? (object)fechaFin.Value.Date
                    : DBNull.Value;

            cmd.Parameters.Add(
                "@Pagina",
                SqlDbType.Int
            ).Value = pagina < 1
                ? 1
                : pagina;

            cmd.Parameters.Add(
                "@TamanoPagina",
                SqlDbType.Int
            ).Value = tamanoPagina < 1
                ? 5
                : tamanoPagina;
        }

        private static void AgregarParametroDecimal(
            SqlCommand cmd,
            string nombre,
            decimal valor)
        {
            SqlParameter parametro =
                cmd.Parameters.Add(
                    nombre,
                    SqlDbType.Decimal);

            parametro.Precision = 10;
            parametro.Scale = 2;
            parametro.Value = valor;
        }

        private static void AgregarParametroNullable(
            SqlCommand cmd,
            string nombre,
            SqlDbType tipo,
            int tamano,
            string valor)
        {
            cmd.Parameters.Add(
                nombre,
                tipo,
                tamano
            ).Value =
                string.IsNullOrWhiteSpace(valor)
                    ? (object)DBNull.Value
                    : valor.Trim();
        }

        /*====================================================
          LECTURA SEGURA
        ====================================================*/

        private static string LeerString(
            SqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return string.Empty;
            }

            return Convert.ToString(
                       reader.GetValue(posicion))
                   ?? string.Empty;
        }

        private static int LeerInt(
            SqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return 0;
            }

            return Convert.ToInt32(
                reader.GetValue(posicion));
        }

        private static int? LeerIntNullable(
            SqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return null;
            }

            return Convert.ToInt32(
                reader.GetValue(posicion));
        }

        private static decimal LeerDecimal(
            SqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return 0;
            }

            return Convert.ToDecimal(
                reader.GetValue(posicion));
        }

        private static decimal? LeerDecimalNullable(
            SqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return null;
            }

            return Convert.ToDecimal(
                reader.GetValue(posicion));
        }

        private static DateTime LeerDateTime(
            SqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return DateTime.MinValue;
            }

            return Convert.ToDateTime(
                reader.GetValue(posicion));
        }

        private static DateTime?
            LeerDateTimeNullable(
                SqlDataReader reader,
                string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return null;
            }

            return Convert.ToDateTime(
                reader.GetValue(posicion));
        }

        private static bool LeerBool(
            SqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return false;
            }

            return Convert.ToBoolean(
                reader.GetValue(posicion));
        }

        private static bool ExisteColumna(
            SqlDataReader reader,
            string columna)
        {
            for (int i = 0;
                 i < reader.FieldCount;
                 i++)
            {
                if (string.Equals(
                        reader.GetName(i),
                        columna,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int LeerIntAlternativo(
            SqlDataReader reader,
            params string[] columnas)
        {
            foreach (string columna in columnas)
            {
                if (ExisteColumna(
                        reader,
                        columna))
                {
                    return LeerInt(
                        reader,
                        columna);
                }
            }

            return 0;
        }

        private static string LeerStringAlternativo(
            SqlDataReader reader,
            params string[] columnas)
        {
            foreach (string columna in columnas)
            {
                if (ExisteColumna(
                        reader,
                        columna))
                {
                    return LeerString(
                        reader,
                        columna);
                }
            }

            return string.Empty;
        }
    }
}