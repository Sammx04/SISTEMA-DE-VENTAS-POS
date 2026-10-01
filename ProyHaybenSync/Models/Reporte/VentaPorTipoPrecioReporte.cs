using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Fila del reporte de ventas por tipo de precio
    /// (UNIDAD vs MAYOR), sp_Reporte_VentasPorTipoPrecio.
    /// </summary>
    public class VentaPorTipoPrecioReporte
    {
        public string TipoPrecio { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public int NumeroVentas { get; set; }

        public decimal TotalGenerado { get; set; }

        public string TipoPrecioDescripcion =>
            TipoPrecio switch
            {
                "UNIDAD" => "Precio unidad",
                "MAYOR" => "Precio por mayor",
                _ => TipoPrecio
            };

        public string CantidadTexto =>
            Cantidad.ToString("N0", CultureInfo.InvariantCulture);

        public string NumeroVentasTexto =>
            NumeroVentas.ToString("N0", CultureInfo.InvariantCulture);

        public string TotalGeneradoTexto =>
            TotalGenerado.ToString("N2", CultureInfo.InvariantCulture);
    }
}
