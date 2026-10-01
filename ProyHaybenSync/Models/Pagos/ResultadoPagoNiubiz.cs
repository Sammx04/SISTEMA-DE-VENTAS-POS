namespace ProySistemaVentas.Models.Pagos
{
    public class ResultadoPagoNiubiz
    {
        public bool Aprobado { get; set; }

        public string CodigoRespuesta { get; set; }

        public string CodigoAutorizacion { get; set; }

        public string IdTransaccionExterna { get; set; }

        public string TarjetaEnmascarada { get; set; }

        public string MensajeRespuesta { get; set; }

        public string RespuestaCruda { get; set; }
    }

    public class DatosCheckoutNiubiz
    {
        public string SessionKey { get; set; }

        public string TransactionToken { get; set; }
    }
}