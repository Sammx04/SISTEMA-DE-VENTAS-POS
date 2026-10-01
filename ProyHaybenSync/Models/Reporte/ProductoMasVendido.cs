using System;
using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Fila del reporte "Productos mas vendidos"
    /// (sp_Reporte_ProductosVendidos).
    /// </summary>
    public class ProductoMasVendido
    {
        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string Variante { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public string CodigoBarra { get; set; } = string.Empty;

        public int CantidadVendida { get; set; }

        public decimal TotalGenerado { get; set; }

        public decimal Descuentos { get; set; }

        public int NumeroVentas { get; set; }

        public decimal PrecioPromedio { get; set; }

        public DateTime UltimaVenta { get; set; }

        public string NombreCompleto =>
            string.IsNullOrWhiteSpace(Variante)
                ? NombreProducto
                : $"{NombreProducto} - {Variante}";

        public string TotalGeneradoTexto =>
            TotalGenerado.ToString("N2", CultureInfo.InvariantCulture);

        public string DescuentosTexto =>
            Descuentos.ToString("N2", CultureInfo.InvariantCulture);

        public string PrecioPromedioTexto =>
            PrecioPromedio.ToString("N2", CultureInfo.InvariantCulture);

        public string UltimaVentaTexto =>
            UltimaVenta == DateTime.MinValue
                ? string.Empty
                : UltimaVenta.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
