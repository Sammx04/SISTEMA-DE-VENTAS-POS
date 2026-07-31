using System.Collections.Generic;

namespace ProySistemaVentas.Models.PrestamosCreditos
{
    public class PrestamoCreditoDetalle :
        PrestamoCreditoItem
    {
        public decimal? MontoPagado { get; set; }

        public string MetodoPagoRegistrado { get; set; } =
            string.Empty;

        public string ObservacionPago { get; set; } =
            string.Empty;

        public List<MovimientoPrestamoCreditoItem>
            Historial
        { get; set; }

        public PrestamoCreditoDetalle()
        {
            Historial =
                new List<MovimientoPrestamoCreditoItem>();
        }

        public string MontoPagadoTexto
        {
            get
            {
                return MontoPagado.HasValue
                    ? $"S/ {MontoPagado.Value:N2}"
                    : "-";
            }
        }
    }
}