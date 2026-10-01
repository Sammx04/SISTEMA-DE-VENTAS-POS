using System;
using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Fila del reporte "Ventas por dia" (sp_Reporte_VentasPorDia).
    ///
    /// ADVERTENCIA: la columna VentaPOS.IGV de la base
    /// SistemaVentas NO es el IGV tributario peruano.
    /// VentaPOSControl.xaml.cs la calcula como
    /// Subtotal * 0.05 y solo la aplica cuando el metodo
    /// de pago es 'TARJETA', es decir, es el recargo por
    /// uso de tarjeta. Por eso aqui se expone con el
    /// nombre RecargoTarjeta.
    /// </summary>
    public class ReporteVenta
    {
        public DateTime Fecha { get; set; }

        public int CantidadVentas { get; set; }

        public int UnidadesVendidas { get; set; }

        public decimal Subtotal { get; set; }

        public decimal RecargoTarjeta { get; set; }

        public decimal Total { get; set; }

        public decimal TotalEfectivo { get; set; }

        public decimal TotalYapePlin { get; set; }

        public decimal TotalTarjeta { get; set; }

        public decimal TotalOtros { get; set; }

        public string FechaTexto =>
            Fecha == DateTime.MinValue
                ? string.Empty
                : Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

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

        /*
         * Ticket promedio del dia.
         */
        public decimal TicketPromedio =>
            CantidadVentas == 0
                ? 0m
                : Math.Round(
                    Total / CantidadVentas,
                    2,
                    MidpointRounding.AwayFromZero);

        public string TicketPromedioTexto =>
            TicketPromedio.ToString("N2", CultureInfo.InvariantCulture);
    }
}
