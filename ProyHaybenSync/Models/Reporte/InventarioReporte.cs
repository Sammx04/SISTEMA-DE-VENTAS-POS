using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Fila del reporte de stock actual
    /// (sp_Reporte_StockActual).
    /// </summary>
    public class InventarioReporte
    {
        public int IdProducto { get; set; }

        public string CodigoBase { get; set; } = string.Empty;

        public string NombreProducto { get; set; } = string.Empty;

        public string DescripcionProducto { get; set; } = string.Empty;

        public int IdCategoria { get; set; }

        public string Categoria { get; set; } = string.Empty;

        public bool TieneVariantes { get; set; }

        public int IdVariante { get; set; }

        public string Variante { get; set; } = string.Empty;

        public string CodigoBarras { get; set; } = string.Empty;

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        public int StockAlmacen { get; set; }

        public int StockTienda { get; set; }

        public int StockTotal { get; set; }

        public decimal ValorVenta { get; set; }

        public string NombreCompleto =>
            string.IsNullOrWhiteSpace(Variante)
                ? NombreProducto
                : $"{NombreProducto} - {Variante}";

        public string PrecioUnidadTexto =>
            PrecioUnidad.ToString("N2", CultureInfo.InvariantCulture);

        public string PrecioMayorTexto =>
            PrecioMayor.ToString("N2", CultureInfo.InvariantCulture);

        public string ValorVentaTexto =>
            ValorVenta.ToString("N2", CultureInfo.InvariantCulture);

        public bool EsAgotado => StockTienda <= 0;

        public bool EsStockBajo =>
            StockTienda > 0 && StockTienda <= 5;

        public string EstadoTienda =>
            EsAgotado
                ? "AGOTADO"
                : EsStockBajo
                    ? "STOCK BAJO"
                    : "EN STOCK";
    }
}
