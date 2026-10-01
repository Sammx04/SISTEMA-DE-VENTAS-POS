using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Totales generales de un reporte de ventas.
    /// Viene del segundo y tercer conjunto de resultados
    /// de sp_Reporte_VentasDetalle y se usa en la barra
    /// de pie de la pantalla de reportes.
    /// </summary>
    public class ReporteTotales
    {
        public int CantidadVentas { get; set; }

        public int UnidadesVendidas { get; set; }

        public decimal Subtotal { get; set; }

        public decimal RecargoTarjeta { get; set; }

        public decimal Total { get; set; }

        public decimal TotalEfectivo { get; set; }

        public decimal TotalYapePlin { get; set; }

        public decimal TotalTarjeta { get; set; }

        public decimal TotalOtros { get; set; }

        public string CantidadVentasTexto =>
            CantidadVentas.ToString("N0", CultureInfo.InvariantCulture);

        public string UnidadesVendidasTexto =>
            UnidadesVendidas.ToString("N0", CultureInfo.InvariantCulture);

        public string SubtotalTexto =>
            Subtotal.ToString("N2", CultureInfo.InvariantCulture);

        public string RecargoTarjetaTexto =>
            RecargoTarjeta.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalTexto =>
            Total.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalEfectivoTexto =>
            TotalEfectivo.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalYapePlinTexto =>
            TotalYapePlin.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalTarjetaTexto =>
            TotalTarjeta.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalOtrosTexto =>
            TotalOtros.ToString("N2", CultureInfo.InvariantCulture);
    }
}
