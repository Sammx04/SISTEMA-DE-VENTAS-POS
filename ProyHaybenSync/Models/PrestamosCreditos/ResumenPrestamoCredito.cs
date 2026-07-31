namespace ProySistemaVentas.Models.PrestamosCreditos
{
    public class ResumenPrestamoCredito
    {
        public int PrestamosPendientes { get; set; }

        public int StockPrestadoTotal { get; set; }

        public int CreditosPendientes { get; set; }

        public decimal DeudaTotalAcumulada { get; set; }

        public string PrestamosPendientesTexto
        {
            get
            {
                return PrestamosPendientes.ToString("N0");
            }
        }

        public string StockPrestadoTotalTexto
        {
            get
            {
                return $"{StockPrestadoTotal:N0} unidades";
            }
        }

        public string CreditosPendientesTexto
        {
            get
            {
                return CreditosPendientes.ToString("N0");
            }
        }

        public string DeudaTotalAcumuladaTexto
        {
            get
            {
                return $"S/ {DeudaTotalAcumulada:N2}";
            }
        }
    }
}