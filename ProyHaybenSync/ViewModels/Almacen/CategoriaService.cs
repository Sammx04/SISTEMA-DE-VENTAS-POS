using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.Almacen;
using System.Data;

namespace ProySistemaVentas.Services.Almacen
{
    public class CategoriaService
    {
        public bool CrearCategoria(string nombre)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_CrearCategoria", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Nombre", nombre);

            cn.Open();

            return cmd.ExecuteNonQuery() > 0;
        }

        public List<Categoria> ListarCategorias()
        {
            List<Categoria> lista = new();

            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_ListarCategorias", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Categoria
                {
                    IdCategoria = Convert.ToInt32(dr["IdCategoria"]),
                    Nombre = dr["Nombre"].ToString()!,
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }
    }
}