using System;
using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Una linea de venta en el reporte de detalle de
    /// productos (sp_Reporte_LineasVenta, primer conjunto
    /// de resultados).
    /// </summary>
    public class LineaVentaReporte
    {
        public int IdVenta { get; set; }

        public string NumeroVenta { get; set; } = string.Empty;

        public DateTime FechaVenta { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public string MetodoPago { get; set; } = string.Empty;

        public int IdDetalle { get; set; }

        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string Variante { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public string CodigoBarra { get; set; } = string.Empty;

        public string TipoPrecio { get; set; } = string.Empty;

        public decimal PrecioUnitario { get; set; }

        public int Cantidad { get; set; }

        public decimal Descuento { get; set; }

        public decimal TotalLinea { get; set; }

        public string NombreCompleto =>
            string.IsNullOrWhiteSpace(Variante)
                ? NombreProducto
                : $"{NombreProducto} - {Variante}";

        public string FechaVentaTexto =>
            FechaVenta == DateTime.MinValue
                ? string.Empty
                : FechaVenta.ToString(
                    "dd/MM/yyyy HH:mm",
                    CultureInfo.InvariantCulture);

        public string PrecioUnitarioTexto =>
            PrecioUnitario.ToString("N2", CultureInfo.InvariantCulture);

        public string DescuentoTexto =>
            Descuento.ToString("N2", CultureInfo.InvariantCulture);

        public string TotalLineaTexto =>
            TotalLinea.ToString("N2", CultureInfo.InvariantCulture);
    }
}
