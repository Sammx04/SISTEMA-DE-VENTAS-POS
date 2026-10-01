using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.Pagos;
using System;
using System.Data;

namespace ProySistemaVentas.Services.Pagos
{
    public class IntentoPagoRepository
    {
        public string ObtenerProximoNumeroPago()
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_pago_ObtenerProximoNumeroPago", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cn.Open();

            return cmd.ExecuteScalar()
                ?.ToString()
                ?? throw new InvalidOperationException(
                    "No se pudo generar el número de orden de pago.");
        }

        public int CrearIntento(
            string numeroOrdenPago,
            int idUsuario,
            string pasarela,
            string metodoPago,
            string moneda,
            decimal monto)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_pago_CrearIntento", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NumeroOrdenPago", numeroOrdenPago);
            cmd.Parameters.AddWithValue("@id_usuario", idUsuario);
            cmd.Parameters.AddWithValue("@Pasarela", pasarela);
            cmd.Parameters.AddWithValue("@MetodoPago", metodoPago);
            cmd.Parameters.AddWithValue("@Moneda", moneda);
            cmd.Parameters.AddWithValue("@Monto", monto);

            cn.Open();

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void ActualizarResultado(
            string numeroOrdenPago,
            string estado,
            string idTransaccionExterna = "",
            string codigoAutorizacion = "",
            string codigoRespuesta = "",
            string mensajeRespuesta = "",
            int? idVenta = null)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_pago_ActualizarResultado", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NumeroOrdenPago", numeroOrdenPago);
            cmd.Parameters.AddWithValue("@Estado", estado);
            cmd.Parameters.AddWithValue("@IdTransaccionExterna", string.IsNullOrWhiteSpace(idTransaccionExterna) ? DBNull.Value : (object)idTransaccionExterna);
            cmd.Parameters.AddWithValue("@CodigoAutorizacion", string.IsNullOrWhiteSpace(codigoAutorizacion) ? DBNull.Value : (object)codigoAutorizacion);
            cmd.Parameters.AddWithValue("@CodigoRespuesta", string.IsNullOrWhiteSpace(codigoRespuesta) ? DBNull.Value : (object)codigoRespuesta);
            cmd.Parameters.AddWithValue("@MensajeRespuesta", string.IsNullOrWhiteSpace(mensajeRespuesta) ? DBNull.Value : (object)mensajeRespuesta);
            cmd.Parameters.AddWithValue("@IdVenta", idVenta.HasValue ? (object)idVenta.Value : DBNull.Value);

            cn.Open();

            cmd.ExecuteNonQuery();
        }

        public IntentoPago? BuscarPorNumeroOrden(string numeroOrdenPago)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_pago_BuscarPorNumeroOrden", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NumeroOrdenPago", numeroOrdenPago);

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read())
            {
                return null;
            }

            return new IntentoPago
            {
                IdIntentoPago = Convert.ToInt32(dr["IdIntentoPago"]),
                NumeroOrdenPago = dr["NumeroOrdenPago"].ToString()!,
                IdUsuario = Convert.ToInt32(dr["id_usuario"]),
                Pasarela = dr["Pasarela"].ToString()!,
                MetodoPago = dr["MetodoPago"].ToString()!,
                Moneda = dr["Moneda"].ToString()!,
                Monto = Convert.ToDecimal(dr["Monto"]),
                Estado = dr["Estado"].ToString()!,
                IdTransaccionExterna = dr["IdTransaccionExterna"] == DBNull.Value ? "" : dr["IdTransaccionExterna"].ToString()!,
                CodigoAutorizacion = dr["CodigoAutorizacion"] == DBNull.Value ? "" : dr["CodigoAutorizacion"].ToString()!,
                CodigoRespuesta = dr["CodigoRespuesta"] == DBNull.Value ? "" : dr["CodigoRespuesta"].ToString()!,
                MensajeRespuesta = dr["MensajeRespuesta"] == DBNull.Value ? "" : dr["MensajeRespuesta"].ToString()!,
                IdVenta = dr["IdVenta"] == DBNull.Value ? null : Convert.ToInt32(dr["IdVenta"]),
                FechaInicio = Convert.ToDateTime(dr["FechaInicio"]),
                FechaResolucion = dr["FechaResolucion"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaResolucion"])
            };
        }
    }
}