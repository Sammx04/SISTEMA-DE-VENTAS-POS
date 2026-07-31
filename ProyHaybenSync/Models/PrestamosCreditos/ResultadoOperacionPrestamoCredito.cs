using System;

namespace ProySistemaVentas.Models.PrestamosCreditos
{
    public class ResultadoOperacionPrestamoCredito
    {
        public int IdPrestamoCredito { get; set; }

        public string NumeroOperacion { get; set; } =
            string.Empty;

        public string TipoOperacion { get; set; } =
            string.Empty;

        public string Estado { get; set; } =
            string.Empty;

        public string AccionFinal { get; set; } =
            string.Empty;

        public string NombreProducto { get; set; } =
            string.Empty;

        public string Variante { get; set; } =
            string.Empty;

        public string ClienteTienda { get; set; } =
            string.Empty;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Total { get; set; }

        public int StockTienda { get; set; }

        public int CantidadDevuelta { get; set; }

        public DateTime? FechaDevolucion { get; set; }

        public DateTime? FechaPagoEsperada { get; set; }

        public string TotalTexto
        {
            get
            {
                return $"S/ {Total:N2}";
            }
        }
    }
}