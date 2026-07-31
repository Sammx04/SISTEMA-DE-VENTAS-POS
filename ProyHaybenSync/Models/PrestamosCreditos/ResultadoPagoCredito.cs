using System;

namespace ProySistemaVentas.Models.PrestamosCreditos
{
    public class ResultadoPagoCredito
    {
        public int IdPrestamoCredito { get; set; }

        public int IdPagoCredito { get; set; }

        public string NumeroOperacion { get; set; } =
            string.Empty;

        public string NumeroPago { get; set; } =
            string.Empty;

        public string ClienteTienda { get; set; } =
            string.Empty;

        public string NombreProducto { get; set; } =
            string.Empty;

        public string Variante { get; set; } =
            string.Empty;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal TotalPagado { get; set; }

        public string MetodoPago { get; set; } =
            string.Empty;

        public int? IdCaja { get; set; }

        public DateTime FechaPago { get; set; }

        public string Estado { get; set; } =
            string.Empty;

        public string TotalPagadoTexto
        {
            get
            {
                return $"S/ {TotalPagado:N2}";
            }
        }

        public string FechaPagoTexto
        {
            get
            {
                return FechaPago.ToString(
                    "dd/MM/yyyy HH:mm");
            }
        }

        public string ProductoCompleto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Variante) ||
                    Variante.Equals(
                        "Única",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return NombreProducto;
                }

                return $"{NombreProducto} - {Variante}";
            }
        }
    }
}