using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.Ventas;
using System.Data;

namespace ProySistemaVentas.Services.Ventas
{
    public class BusquedaProductoService
    {
        public List<ProductoBusqueda> BuscarProductos(string texto)
        {
            List<ProductoBusqueda> lista = new();

            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_POS_BuscarProductos", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Texto", texto);

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new ProductoBusqueda
                {
                    IdProducto = Convert.ToInt32(dr["id_producto"]),
                    IdVariante = Convert.ToInt32(dr["IdVariante"]),
                    Nombre = dr["nombre"].ToString()!,
                    Variante = dr["Variante"].ToString()!,
                    Categoria = dr["Categoria"].ToString()!,
                    CodigoBarra = dr["CodigoBarra"].ToString()!,
                    PrecioUnidad = Convert.ToDecimal(dr["PrecioUnidad"]),
                    PrecioMayor = dr["PrecioMayor"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["PrecioMayor"]),
                    Stock = Convert.ToInt32(dr["Stock"])
                });
            }

            return lista;
        }

        public ProductoBusqueda? ObtenerProductoPorCodigo(string codigoBarra)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_POS_ObtenerProductoPorCodigo", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CodigoBarra", codigoBarra);

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                return new ProductoBusqueda
                {
                    IdProducto = Convert.ToInt32(dr["id_producto"]),
                    IdVariante = Convert.ToInt32(dr["IdVariante"]),
                    Nombre = dr["nombre"].ToString()!,
                    Variante = dr["Variante"].ToString()!,
                    Categoria = dr["Categoria"].ToString()!,
                    CodigoBarra = dr["CodigoBarra"].ToString()!,
                    PrecioUnidad = Convert.ToDecimal(dr["PrecioUnidad"]),
                    PrecioMayor = dr["PrecioMayor"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["PrecioMayor"]),
                    Stock = Convert.ToInt32(dr["Stock"])
                };
            }

            return null;
        }
    }
}