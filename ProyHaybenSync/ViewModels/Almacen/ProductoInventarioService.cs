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
            string descripcion)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_EditarProductoInventario", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdProducto", idProducto);
            cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@IdCategoria", idCategoria);
            cmd.Parameters.AddWithValue("@Descripcion", descripcion);

            cn.Open();

            return cmd.ExecuteNonQuery() > 0;
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
    }
}