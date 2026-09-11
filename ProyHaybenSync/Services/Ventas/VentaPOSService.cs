using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Models.VentasPOS;
using ProySistemaVentas.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ProySistemaVentas.Services.VentasPOS
{
    public class VentaPOSService
    {
        private readonly string _cadenaConexion;

        public VentaPOSService()
        {
            _cadenaConexion =
                ConexionService.ObtenerCadenaConexion();
        }

        // =====================================================
        // BUSCAR PRODUCTOS
        // =====================================================

        public List<ProductoVentaPOS> BuscarProductos(
            string texto)
        {
            List<ProductoVentaPOS> productos =
                new List<ProductoVentaPOS>();

            string textoBusqueda =
                texto?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(textoBusqueda))
            {
                return productos;
            }

            using SqlConnection cn =
                new SqlConnection(_cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_POS_BuscarProductos",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@Texto",
                SqlDbType.VarChar,
                100).Value =
                textoBusqueda;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(
                    LeerProductoVenta(reader));
            }

            return productos;
        }

        // =====================================================
        // BUSCAR PRODUCTO POR CÓDIGO DE BARRAS
        // =====================================================

        public ProductoVentaPOS ObtenerProductoPorCodigo(
            string codigoBarra)
        {
            string codigo =
                codigoBarra?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            using SqlConnection cn =
                new SqlConnection(_cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_POS_ObtenerProductoPorCodigo",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@CodigoBarra",
                SqlDbType.VarChar,
                50).Value =
                codigo;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return LeerProductoVenta(reader);
        }

        // =====================================================
        // PRODUCTOS FRECUENTES DEL DÍA
        // =====================================================

        public List<ProductoVentaPOS>
            ObtenerProductosFrecuentes()
        {
            List<ProductoVentaPOS> productos =
                new List<ProductoVentaPOS>();

            using SqlConnection cn =
                new SqlConnection(_cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_POS_ProductosFrecuentesDia",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                ProductoVentaPOS producto =
                    LeerProductoVenta(reader);

                producto.VecesVendido =
                    ObtenerEntero(
                        reader,
                        "VecesVendido");

                productos.Add(producto);
            }

            return productos;
        }

        // =====================================================
        // LISTAR USUARIOS ACTIVOS
        // =====================================================

        /*
         * Reutilizamos UsuarioMovimiento porque contiene:
         *
         * IdUsuario
         * Nombre
         * Usuario
         * Rol
         * NombreMostrar
         */
        public List<UsuarioMovimiento>
            ListarUsuariosActivos()
        {
            List<UsuarioMovimiento> usuarios =
                new List<UsuarioMovimiento>();

            using SqlConnection cn =
                new SqlConnection(_cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_ListarUsuariosActivos",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                usuarios.Add(
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

            return usuarios;
        }

        // =====================================================
        // REGISTRAR VENTA
        // =====================================================

        public ResultadoVentaPOS RegistrarVenta(
            int idUsuario,
            string tipoDocumento,
            string numeroDocumento,
            string nombreCliente,
            string metodoPago,
            decimal subtotal,
            decimal igv,
            decimal total,
            IEnumerable<ItemCarritoVenta> detalle)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "Debe seleccionar al trabajador que realiza la venta.",
                    nameof(idUsuario));
            }

            List<ItemCarritoVenta> productos =
                detalle?
                    .Where(item => item != null)
                    .ToList()
                ?? new List<ItemCarritoVenta>();

            if (productos.Count == 0)
            {
                throw new InvalidOperationException(
                    "El carrito no contiene productos.");
            }

            if (productos.Any(
                    item =>
                        item.Cantidad <= 0))
            {
                throw new InvalidOperationException(
                    "Todos los productos deben tener una cantidad válida.");
            }

            if (productos.Any(
                    item =>
                        item.Cantidad > item.Stock))
            {
                throw new InvalidOperationException(
                    "Uno o más productos superan el stock disponible en tienda.");
            }

            if (productos.Any(
                    item =>
                        item.PrecioUnitario <= 0))
            {
                throw new InvalidOperationException(
                    "Uno o más productos tienen un precio inválido.");
            }

            string tipoDocumentoNormalizado =
                string.IsNullOrWhiteSpace(tipoDocumento)
                    ? "OTROS"
                    : tipoDocumento
                        .Trim()
                        .ToUpperInvariant();

            string metodoPagoNormalizado =
                string.IsNullOrWhiteSpace(metodoPago)
                    ? "EFECTIVO"
                    : metodoPago
                        .Trim()
                        .ToUpperInvariant();

            string clienteNormalizado =
                string.IsNullOrWhiteSpace(nombreCliente)
                    ? "Otros clientes"
                    : nombreCliente.Trim();

            string documentoNormalizado =
                numeroDocumento?.Trim()
                ?? string.Empty;

            if (tipoDocumentoNormalizado != "OTROS" &&
                string.IsNullOrWhiteSpace(
                    documentoNormalizado))
            {
                throw new InvalidOperationException(
                    "Debe ingresar el número de documento del cliente.");
            }

            DataTable tablaDetalle =
                CrearTablaDetalle(productos);

            int idVentaPOS;
            int idVentaSistema;
            string numeroVenta;

            using (
                SqlConnection cn =
                    new SqlConnection(
                        _cadenaConexion))
            using (
                SqlCommand cmd =
                    new SqlCommand(
                        "sp_POS_RegistrarVenta",
                        cn))
            {
                cmd.CommandType =
                    CommandType.StoredProcedure;

                cmd.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int).Value =
                    idUsuario;

                cmd.Parameters.Add(
                    "@TipoDocumento",
                    SqlDbType.VarChar,
                    20).Value =
                    tipoDocumentoNormalizado;

                cmd.Parameters.Add(
                    "@NumeroDocumento",
                    SqlDbType.VarChar,
                    20).Value =
                    tipoDocumentoNormalizado == "OTROS" ||
                    string.IsNullOrWhiteSpace(
                        documentoNormalizado)
                        ? DBNull.Value
                        : documentoNormalizado;

                cmd.Parameters.Add(
                    "@NombreCliente",
                    SqlDbType.VarChar,
                    150).Value =
                    clienteNormalizado;

                cmd.Parameters.Add(
                    "@MetodoPago",
                    SqlDbType.VarChar,
                    30).Value =
                    metodoPagoNormalizado;

                cmd.Parameters.Add(
                    "@SubTotal",
                    SqlDbType.Decimal).Value =
                    subtotal;

                cmd.Parameters[
                    "@SubTotal"].Precision = 10;

                cmd.Parameters[
                    "@SubTotal"].Scale = 2;

                cmd.Parameters.Add(
                    "@IGV",
                    SqlDbType.Decimal).Value =
                    igv;

                cmd.Parameters[
                    "@IGV"].Precision = 10;

                cmd.Parameters[
                    "@IGV"].Scale = 2;

                cmd.Parameters.Add(
                    "@Total",
                    SqlDbType.Decimal).Value =
                    total;

                cmd.Parameters[
                    "@Total"].Precision = 10;

                cmd.Parameters[
                    "@Total"].Scale = 2;

                SqlParameter parametroDetalle =
                    cmd.Parameters.Add(
                        "@Detalle",
                        SqlDbType.Structured);

                parametroDetalle.TypeName =
                    "dbo.TVP_DetalleVentaPOS";

                parametroDetalle.Value =
                    tablaDetalle;

                cn.Open();

                using SqlDataReader reader =
                    cmd.ExecuteReader();

                if (!reader.Read())
                {
                    throw new InvalidOperationException(
                        "La base de datos no devolvió el resultado de la venta.");
                }

                idVentaPOS =
                    ObtenerEntero(
                        reader,
                        "IdVentaPOS");

                idVentaSistema =
                    ObtenerEntero(
                        reader,
                        "IdVentaSistema");

                numeroVenta =
                    ObtenerTexto(
                        reader,
                        "NumeroVenta");
            }

            /*
             * Volvemos a consultar la venta para obtener
             * toda la información de la boleta.
             */
            ResultadoVentaPOS resultadoRespaldo =
    new ResultadoVentaPOS
    {
        IdVentaPOS =
            idVentaPOS,

        IdVentaSistema =
            idVentaSistema,

        NumeroVenta =
            numeroVenta,

        FechaVenta =
            DateTime.Now,

        IdUsuario =
            idUsuario,

        TipoDocumento =
            tipoDocumentoNormalizado,

        NumeroDocumento =
            documentoNormalizado,

        NombreCliente =
            clienteNormalizado,

        MetodoPago =
            metodoPagoNormalizado,

        Subtotal =
            subtotal,

        IGV =
            igv,

        Total =
            total,

        Estado =
            "COMPLETADA",

        Detalle =
            productos.ToList()
    };

            resultadoRespaldo.RecalcularResumen();

            /*
             * Intentamos obtener la información completa para la boleta.
             *
             * Si esta consulta falla, NO debemos indicar que la venta
             * no fue registrada, porque el procedimiento anterior ya
             * realizó el COMMIT y descontó el stock.
             */
            try
            {
                ResultadoVentaPOS resultadoCompleto =
                    ObtenerVentaCompleta(
                        idVentaPOS);

                if (resultadoCompleto != null)
                {
                    resultadoCompleto.IdVentaSistema =
                        idVentaSistema;

                    return resultadoCompleto;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "La venta fue registrada, pero no se pudo " +
                    "consultar la información completa: " +
                    ex.Message);
            }

            return resultadoRespaldo;
        }

        // =====================================================
        // OBTENER VENTA COMPLETA PARA BOLETA
        // =====================================================

        public ResultadoVentaPOS ObtenerVentaCompleta(
            int idVentaPOS)
        {
            if (idVentaPOS <= 0)
            {
                return null;
            }

            using SqlConnection cn =
                new SqlConnection(_cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_POS_ObtenerVentaCompleta",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@IdVentaPOS",
                SqlDbType.Int).Value =
                idVentaPOS;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            ResultadoVentaPOS venta =
                new ResultadoVentaPOS
                {
                    IdVentaPOS =
                        ObtenerEntero(
                            reader,
                            "IdVentaPOS"),

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

                    NombreTrabajador =
                        ObtenerTexto(
                            reader,
                            "NombreTrabajador"),

                    IdClientePOS =
                        ObtenerEntero(
                            reader,
                            "IdClientePOS"),

                    TipoDocumento =
                        ObtenerTexto(
                            reader,
                            "TipoDocumento"),

                    NumeroDocumento =
                        ObtenerTexto(
                            reader,
                            "NumeroDocumento"),

                    NombreCliente =
                        ObtenerTexto(
                            reader,
                            "NombreCliente"),

                    MetodoPago =
                        ObtenerTexto(
                            reader,
                            "MetodoPago"),

                    Subtotal =
                        ObtenerDecimal(
                            reader,
                            "Subtotal"),

                    IGV =
                        ObtenerDecimal(
                            reader,
                            "IGV"),

                    DescuentoTotal =
                        ObtenerDecimal(
                            reader,
                            "DescuentoTotal"),

                    Total =
                        ObtenerDecimal(
                            reader,
                            "Total"),

                    Estado =
                        ObtenerTexto(
                            reader,
                            "Estado"),

                    CantidadUnidades =
                        ObtenerEntero(
                            reader,
                            "CantidadUnidades"),

                    CantidadProductos =
                        ObtenerEntero(
                            reader,
                            "CantidadProductos"),

                    Detalle =
                        new List<ItemCarritoVenta>()
                };

            /*
             * El segundo resultado contiene los productos.
             */
            if (reader.NextResult())
            {
                while (reader.Read())
                {
                    ItemCarritoVenta item =
                        new ItemCarritoVenta
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

                            /*
                             * Estos precios originales no se
                             * guardan por separado en el detalle
                             * histórico. Para la boleta solamente
                             * se requiere PrecioUnitario.
                             */
                            PrecioUnidad =
                                ObtenerDecimal(
                                    reader,
                                    "PrecioUnitario"),

                            Stock =
                                ObtenerEntero(
                                    reader,
                                    "Cantidad")
                        };

                    venta.Detalle.Add(item);
                }
            }

            return venta;
        }

        // =====================================================
        // CREAR TABLA PARA EL TVP
        // =====================================================

        private static DataTable CrearTablaDetalle(
            IEnumerable<ItemCarritoVenta> productos)
        {
            DataTable tabla =
                new DataTable();

            tabla.Columns.Add(
                "IdProducto",
                typeof(int));

            tabla.Columns.Add(
                "IdVariante",
                typeof(int));

            tabla.Columns.Add(
                "NombreProducto",
                typeof(string));

            tabla.Columns.Add(
                "Categoria",
                typeof(string));

            tabla.Columns.Add(
                "CodigoBarra",
                typeof(string));

            tabla.Columns.Add(
                "TipoPrecio",
                typeof(string));

            tabla.Columns.Add(
                "PrecioUnitario",
                typeof(decimal));

            tabla.Columns.Add(
                "Cantidad",
                typeof(int));

            tabla.Columns.Add(
                "Descuento",
                typeof(decimal));

            tabla.Columns.Add(
                "TotalLinea",
                typeof(decimal));

            foreach (
                ItemCarritoVenta item
                in productos)
            {
                tabla.Rows.Add(
                    item.IdProducto,
                    item.IdVariante,
                    item.NombreProducto
                        ?? string.Empty,
                    item.Categoria
                        ?? string.Empty,
                    item.CodigoBarra
                        ?? string.Empty,
                    item.TipoPrecio
                        ?? "UNIDAD",
                    item.PrecioUnitario,
                    item.Cantidad,
                    item.Descuento,
                    item.TotalLinea);
            }

            return tabla;
        }

        // =====================================================
        // LEER PRODUCTO DESDE SQL SERVER
        // =====================================================

        private static ProductoVentaPOS
            LeerProductoVenta(
                SqlDataReader reader)
        {
            return new ProductoVentaPOS
            {
                IdProducto =
                    ObtenerEntero(
                        reader,
                        "id_producto",
                        "IdProducto"),

                IdVariante =
                    ObtenerEntero(
                        reader,
                        "IdVariante"),

                Nombre =
                    ObtenerTexto(
                        reader,
                        "nombre",
                        "Nombre"),

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

                PrecioUnidad =
                    ObtenerDecimal(
                        reader,
                        "PrecioUnidad"),

                PrecioMayor =
                    ObtenerDecimal(
                        reader,
                        "PrecioMayor"),

                Stock =
                    ObtenerEntero(
                        reader,
                        "Stock")
            };
        }

        // =====================================================
        // MÉTODOS AUXILIARES
        // =====================================================

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
                    .ToString(
                        reader[indice])
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