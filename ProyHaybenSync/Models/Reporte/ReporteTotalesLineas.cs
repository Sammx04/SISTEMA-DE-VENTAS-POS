namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Totales del reporte de lineas de venta
    /// (sp_Reporte_LineasVenta, segundo conjunto de
    /// resultados).
    /// </summary>
    public class ReporteTotalesLineas
    {
        public int CantidadLineas { get; set; }

        public int UnidadesVendidas { get; set; }

        public decimal Descuentos { get; set; }

        public decimal TotalGenerado { get; set; }

        public string CantidadLineasTexto =>
            CantidadLineas.ToString("N0");

        public string UnidadesVendidasTexto =>
            UnidadesVendidas.ToString("N0");

        public string DescuentosTexto =>
            Descuentos.ToString("N2");

        public string TotalGeneradoTexto =>
            TotalGenerado.ToString("N2");
    }
}
