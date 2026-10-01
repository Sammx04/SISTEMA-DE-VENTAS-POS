using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Fila del reporte de rendimiento por vendedor
    /// (sp_Reporte_VentasPorUsuario).
    /// </summary>
    public class VentaPorUsuarioReporte
    {
        public int IdUsuario { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public int CantidadVentas { get; set; }

        public int UnidadesVendidas { get; set; }

        public decimal Subtotal { get; set; }

        public decimal RecargoTarjeta { get; set; }

        public decimal Total { get; set; }

        public decimal TicketPromedio { get; set; }

        public decimal TotalEfectivo { get; set; }

        public decimal TotalYapePlin { get; set; }

        public decimal TotalTarjeta { get; set; }

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

        public string TicketPromedioTexto =>
            TicketPromedio.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalEfectivoTexto =>
            TotalEfectivo.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalYapePlinTexto =>
            TotalYapePlin.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalTarjetaTexto =>
            TotalTarjeta.ToString("N2", CultureInfo.InvariantCulture);
    }
}
