using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Fila del reporte de ventas por categoria
    /// (sp_Reporte_VentasPorCategoria).
    /// </summary>
    public class VentaPorCategoriaReporte
    {
        public int IdCategoria { get; set; }

        public string Categoria { get; set; } = string.Empty;

        public int NumeroVentas { get; set; }

        public int Productos { get; set; }

        public int CantidadVendida { get; set; }

        public decimal TotalGenerado { get; set; }

        public decimal Descuentos { get; set; }

        /// <summary>
        /// Porcentaje del importe total del reporte. Lo
        /// calcula la vista usando el total de todas las
        /// filas, por eso se deja con valor por defecto 0.
        /// </summary>
        public decimal Participacion { get; set; }

        public string NumeroVentasTexto =>
            NumeroVentas.ToString("N0", CultureInfo.InvariantCulture);

        public string CantidadVendidaTexto =>
            CantidadVendida.ToString("N0", CultureInfo.InvariantCulture);

        public string TotalGeneradoTexto =>
            TotalGenerado.ToString("N2", CultureInfo.InvariantCulture);

        public string DescuentosTexto =>
            Descuentos.ToString("N2", CultureInfo.InvariantCulture);

        public string ParticipacionTexto =>
            $"{Participacion.ToString("N1", CultureInfo.InvariantCulture)} %";
    }
}
