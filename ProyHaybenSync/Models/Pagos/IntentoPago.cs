using System;

namespace ProySistemaVentas.Models.Pagos
{
    public class IntentoPago
    {
        public int IdIntentoPago { get; set; }

        public string NumeroOrdenPago { get; set; }

        public int IdUsuario { get; set; }

        public string Pasarela { get; set; }

        public string MetodoPago { get; set; }

        public string Moneda { get; set; }

        public decimal Monto { get; set; }

        public string Estado { get; set; }

        public string IdTransaccionExterna { get; set; }

        public string CodigoAutorizacion { get; set; }

        public string CodigoRespuesta { get; set; }

        public string MensajeRespuesta { get; set; }

        public int? IdVenta { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaResolucion { get; set; }
    }
}