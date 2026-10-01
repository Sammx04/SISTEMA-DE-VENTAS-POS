using System;
using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Cabecera de una venta en el reporte de detalle
    /// (sp_Reporte_VentasDetalle, primer conjunto de
    /// resultados).
    /// </summary>
    public class VentaReporteDetalle
    {
        public int IdVenta { get; set; }

        public string NumeroVenta { get; set; } = string.Empty;

        public DateTime FechaVenta { get; set; }

        public int IdUsuario { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public string TipoDocumento { get; set; } = string.Empty;

        public string NumeroDocumento { get; set; } = string.Empty;

        public string Cliente { get; set; } = string.Empty;

        public string MetodoPago { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        /// <summary>
        /// VentaPOS.IGV, expuesto como recargo por tarjeta.
        /// </summary>
        public decimal RecargoTarjeta { get; set; }

        public decimal Total { get; set; }

        public decimal Descuento { get; set; }

        public string Estado { get; set; } = string.Empty;

        public int Unidades { get; set; }

        public int Items { get; set; }

        public string FechaVentaTexto =>
            FechaVenta == DateTime.MinValue
                ? string.Empty
                : FechaVenta.ToString(
                    "dd/MM/yyyy HH:mm",
                    CultureInfo.InvariantCulture);

        public string SubtotalTexto =>
            Subtotal.ToString("N2", CultureInfo.InvariantCulture);

        public string RecargoTarjetaTexto =>
            RecargoTarjeta.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalTexto =>
            Total.ToString("N2", CultureInfo.InvariantCulture);

        public string DescuentoTexto =>
            Descuento.ToString("N2", CultureInfo.InvariantCulture);
    }
}
