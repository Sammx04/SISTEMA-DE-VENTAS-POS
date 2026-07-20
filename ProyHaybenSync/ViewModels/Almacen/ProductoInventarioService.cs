using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.Almacen;
using System.Data;

namespace ProySistemaVentas.Services.Almacen
{
    public class ProductoInventarioService
    {
        public string ObtenerProximoCodigoBase()
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_ObtenerProximoCodigoBase", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cn.Open();

            object? resultado = cmd.ExecuteScalar();

            return resultado?.ToString() ?? "";
        }

        public bool CrearProductoInventario(
            string nombre,
            int idCategoria,
            string descripcion,
            bool tieneVariantes,
            List<TipoVariante> variantes)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_CrearProductoInventario", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@IdCategoria", idCategoria);
            cmd.Parameters.AddWithValue("@Descripcion", descripcion);
            cmd.Parameters.AddWithValue("@TieneVariantes", tieneVariantes);

            SqlParameter parametroVariantes = cmd.Parameters.AddWithValue(
                "@ListaVariantes",
                ConvertirVariantesADataTable(variantes));

            parametroVariantes.SqlDbType = SqlDbType.Structured;
            parametroVariantes.TypeName = "TipoVariante";

            cn.Open();

            return cmd.ExecuteNonQuery() >= 0;
        }

        public List<ProductoInventarioListado> ListarProductosConVariantes()
        {
            List<ProductoInventarioListado> lista = new();

            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_ListarProductosConVariantes", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new ProductoInventarioListado
                {
                    IdProducto = Convert.ToInt32(dr["id_producto"]),
                    Codigo = dr["codigo"].ToString()!,
                    Nombre = dr["nombre"].ToString()!,
                    Descripcion = dr["Descripcion"].ToString()!,
                    IdCategoria = Convert.ToInt32(dr["IdCategoria"]),
                    NombreCategoria = dr["NombreCategoria"].ToString()!,
                    CodigoBase = dr["CodigoBase"].ToString()!,
                    TieneVariantes = Convert.ToBoolean(dr["TieneVariantes"]),
                    IdVariante = Convert.ToInt32(dr["IdVariante"]),
                    VarianteDescripcion = dr["VarianteDescripcion"].ToString()!,
                    CodigoBarras = dr["CodigoBarras"].ToString()!,
                    PrecioUnidad = Convert.ToDecimal(dr["PrecioUnidad"]),
                    PrecioMayor = dr["PrecioMayor"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["PrecioMayor"]),
                    StockAlmacen = Convert.ToInt32(dr["StockAlmacen"]),
                    StockTienda = Convert.ToInt32(dr["StockTienda"])
                });
            }

            return lista;
        }

        public bool EditarProductoInventario(
    int idProducto,
    string nombre,
    int idCategoria,
    string descripcion,
    bool tieneVariantes,
    List<TipoVariante> variantes)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_EditarProductoInventario",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@IdProducto",
                SqlDbType.Int).Value =
                idProducto;

            cmd.Parameters.Add(
                "@Nombre",
                SqlDbType.NVarChar,
                150).Value =
                nombre;

            cmd.Parameters.Add(
                "@IdCategoria",
                SqlDbType.Int).Value =
                idCategoria;

            cmd.Parameters.Add(
                "@Descripcion",
                SqlDbType.NVarChar,
                300).Value =
                string.IsNullOrWhiteSpace(descripcion)
                    ? DBNull.Value
                    : descripcion;

            /*
             * El procedimiento almacenado actual no recibe:
             *
             * @TieneVariantes
             * @Variantes
             *
             * Por eso no se agregan al SqlCommand.
             */

            cn.Open();

            object resultado =
                cmd.ExecuteScalar();

            return resultado != null &&
                   Convert.ToInt32(resultado) == 1;
        }

        private DataTable ConvertirVariantesADataTable(List<TipoVariante> variantes)
        {
            DataTable tabla = new();

            tabla.Columns.Add("Descripcion", typeof(string));
            tabla.Columns.Add("PrecioUnidad", typeof(decimal));
            tabla.Columns.Add("PrecioMayor", typeof(decimal));
            tabla.Columns.Add("Stock", typeof(int));

            foreach (TipoVariante item in variantes)
            {
                tabla.Rows.Add(
                    item.Descripcion,
                    item.PrecioUnidad,
                    item.PrecioMayor,
                    item.Stock);
            }

            return tabla;
        }

        public bool EditarVariante(
    TipoVariante variante)
        {
            if (variante == null)
            {
                throw new ArgumentNullException(
                    nameof(variante));
            }

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_EditarVariante",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@IdVariante",
                SqlDbType.Int).Value =
                variante.IdVariante;

            cmd.Parameters.Add(
                "@Descripcion",
                SqlDbType.NVarChar,
                100).Value =
                string.IsNullOrWhiteSpace(
                    variante.Descripcion)
                        ? "Única"
                        : variante.Descripcion.Trim();

            SqlParameter precioUnidad =
                cmd.Parameters.Add(
                    "@PrecioUnidad",
                    SqlDbType.Decimal);

            precioUnidad.Precision = 10;
            precioUnidad.Scale = 2;
            precioUnidad.Value =
                variante.PrecioUnidad;

            SqlParameter precioMayor =
                cmd.Parameters.Add(
                    "@PrecioMayor",
                    SqlDbType.Decimal);

            precioMayor.Precision = 10;
            precioMayor.Scale = 2;
            precioMayor.Value =
                variante.PrecioMayor;

            cn.Open();

            object resultado =
                cmd.ExecuteScalar();

            return resultado != null &&
                   Convert.ToInt32(resultado) > 0;
        }

        public (int IdVariante, string CodigoBarras)
    AgregarVarianteAProducto(
        int idProducto,
        TipoVariante variante)
        {
            if (variante == null)
            {
                throw new ArgumentNullException(
                    nameof(variante));
            }

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_AgregarVarianteAProducto",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@IdProducto",
                SqlDbType.Int).Value =
                idProducto;

            cmd.Parameters.Add(
                "@Descripcion",
                SqlDbType.NVarChar,
                100).Value =
                string.IsNullOrWhiteSpace(
                    variante.Descripcion)
                        ? "Única"
                        : variante.Descripcion.Trim();

            SqlParameter precioUnidad =
                cmd.Parameters.Add(
                    "@PrecioUnidad",
                    SqlDbType.Decimal);

            precioUnidad.Precision = 10;
            precioUnidad.Scale = 2;
            precioUnidad.Value =
                variante.PrecioUnidad;

            SqlParameter precioMayor =
                cmd.Parameters.Add(
                    "@PrecioMayor",
                    SqlDbType.Decimal);

            precioMayor.Precision = 10;
            precioMayor.Scale = 2;
            precioMayor.Value =
                variante.PrecioMayor;

            // El procedimiento recibe @Stock.
            cmd.Parameters.Add(
                "@Stock",
                SqlDbType.Int).Value =
                variante.NuevoStock;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                return (0, string.Empty);
            }

            int idVariante =
                Convert.ToInt32(
                    reader["IdVariante"]);

            string codigoBarras =
                Convert.ToString(
                    reader["CodigoBarras"])?.Trim()
                ?? string.Empty;

            return (
                idVariante,
                codigoBarras);
        }

        public bool EliminarVariante(
    int idVariante)
        {
            if (idVariante <= 0)
            {
                throw new ArgumentException(
                    "El identificador de la variante no es válido.",
                    nameof(idVariante));
            }

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_EliminarVariante",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@IdVariante",
                SqlDbType.Int).Value =
                idVariante;

            cn.Open();

            object resultado =
                cmd.ExecuteScalar();

            return resultado != null &&
                   resultado != DBNull.Value &&
                   Convert.ToInt32(resultado) == 1;
        }

        public bool DesactivarProductoInventario(
    int idProducto)
        {
            if (idProducto <= 0)
            {
                throw new ArgumentException(
                    "El identificador del producto no es válido.",
                    nameof(idProducto));
            }

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_DesactivarProductoInventario",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@IdProducto",
                SqlDbType.Int).Value =
                idProducto;

            cn.Open();

            object resultado =
                cmd.ExecuteScalar();

            return resultado != null &&
                   resultado != DBNull.Value &&
                   Convert.ToInt32(resultado) == 1;
        }
    }
}