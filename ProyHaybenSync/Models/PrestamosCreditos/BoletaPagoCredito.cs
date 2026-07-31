using System;

namespace ProySistemaVentas.Models.PrestamosCreditos
{
    public class BoletaPagoCredito
    {
        public int IdPagoCredito { get; set; }

        public string NumeroPago { get; set; } =
            string.Empty;

        public DateTime FechaPago { get; set; }

        public string MetodoPago { get; set; } =
            string.Empty;

        public decimal TotalPagado { get; set; }

        public int? IdCaja { get; set; }

        public int IdPrestamoCredito { get; set; }

        public string NumeroOperacion { get; set; } =
            string.Empty;

        public string ClienteTienda { get; set; } =
            string.Empty;

        public string NombreProducto { get; set; } =
            string.Empty;

        public string Variante { get; set; } =
            string.Empty;

        public string CodigoBarra { get; set; } =
            string.Empty;

        public int Cantidad { get; set; }

        public string TipoPrecio { get; set; } =
            string.Empty;

        public decimal PrecioUnitario { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; } =
            string.Empty;

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaPagoEsperada { get; set; }

        public int IdUsuario { get; set; }

        public string NombreTrabajador { get; set; } =
            string.Empty;

        public string UsuarioTrabajador { get; set; } =
            string.Empty;

        public string RolTrabajador { get; set; } =
            string.Empty;

        public string Observacion { get; set; } =
            string.Empty;

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

        public string FechaPagoTexto
        {
            get
            {
                return FechaPago.ToString(
                    "dd/MM/yyyy HH:mm");
            }
        }

        public string FechaRegistroTexto
        {
            get
            {
                return FechaRegistro.ToString(
                    "dd/MM/yyyy HH:mm");
            }
        }

        public string FechaPagoEsperadaTexto
        {
            get
            {
                return FechaPagoEsperada.HasValue
                    ? FechaPagoEsperada.Value.ToString(
                        "dd/MM/yyyy")
                    : "-";
            }
        }

        public string PrecioUnitarioTexto
        {
            get
            {
                return $"S/ {PrecioUnitario:N2}";
            }
        }

        public string TotalPagadoTexto
        {
            get
            {
                return $"S/ {TotalPagado:N2}";
            }
        }
    }
}