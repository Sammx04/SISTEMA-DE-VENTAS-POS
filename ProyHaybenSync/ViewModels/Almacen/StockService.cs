using Microsoft.Data.SqlClient;
using System.Data;

namespace ProySistemaVentas.Services.Almacen
{
    public class StockService
    {
        public bool TransferirStockATienda(int idVariante, int cantidad)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_TransferirStockATienda", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdVariante", idVariante);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);

            cn.Open();

            return cmd.ExecuteNonQuery() >= 0;
        }

        public bool ActualizarStockAlmacen(
            int idVariante,
            int cantidadIngreso,
            int cantidadSalida)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_ActualizarStockAlmacen", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdVariante", idVariante);
            cmd.Parameters.AddWithValue("@CantidadIngreso", cantidadIngreso);
            cmd.Parameters.AddWithValue("@CantidadSalida", cantidadSalida);

            cn.Open();

            return cmd.ExecuteNonQuery() >= 0;
        }
    }
}