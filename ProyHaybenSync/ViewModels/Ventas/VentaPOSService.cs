using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.Ventas;
using System.Data;

namespace ProySistemaVentas.Services.Ventas
{
    public class VentaPOSService
    {
        public bool RegistrarVenta(
            int idUsuario,
            string tipoDocumento,
            string? numeroDocumento,
            string nombreCliente,
            string metodoPago,
            decimal subtotal,
            decimal igv,
            decimal total,
            List<DetalleVentaDTO> detalle)
        {
            using SqlConnection cn =
                new SqlConnection(ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand("sp_POS_RegistrarVenta", cn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@id_usuario", idUsuario);
            cmd.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
            cmd.Parameters.AddWithValue("@NumeroDocumento", (object?)numeroDocumento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NombreCliente", nombreCliente);
            cmd.Parameters.AddWithValue("@MetodoPago", metodoPago);
            cmd.Parameters.AddWithValue("@SubTotal", subtotal);
            cmd.Parameters.AddWithValue("@IGV", igv);
            cmd.Parameters.AddWithValue("@Total", total);

            SqlParameter parametroDetalle = cmd.Parameters.AddWithValue(
                "@Detalle",
                ConvertirDetalleADataTable(detalle));

            parametroDetalle.SqlDbType = SqlDbType.Structured;
            parametroDetalle.TypeName = "TVP_DetalleVentaPOS";

            cn.Open();

            return cmd.ExecuteNonQuery() >= 0;
        }

        private DataTable ConvertirDetalleADataTable(List<DetalleVentaDTO> detalle)
        {
            DataTable tabla = new();

            tabla.Columns.Add("IdProducto", typeof(int));
            tabla.Columns.Add("IdVariante", typeof(int));
            tabla.Columns.Add("NombreProducto", typeof(string));
            tabla.Columns.Add("Categoria", typeof(string));
            tabla.Columns.Add("CodigoBarra", typeof(string));
            tabla.Columns.Add("TipoPrecio", typeof(string));
            tabla.Columns.Add("PrecioUnitario", typeof(decimal));
            tabla.Columns.Add("Cantidad", typeof(int));
            tabla.Columns.Add("Descuento", typeof(decimal));
            tabla.Columns.Add("TotalLinea", typeof(decimal));

            foreach (DetalleVentaDTO item in detalle)
            {
                tabla.Rows.Add(
                    item.IdProducto,
                    item.IdVariante,
                    item.NombreProducto,
                    item.Categoria,
                    item.CodigoBarra,
                    item.TipoPrecio,
                    item.PrecioUnitario,
                    item.Cantidad,
                    item.Descuento,
                    item.TotalLinea);
            }

            return tabla;
        }
    }
}