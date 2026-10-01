using System;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Filtros del reporte de ranking de productos
    /// (sp_Reporte_ProductosVendidos).
    /// </summary>
    public class FiltroReporteProductos : FiltroReportePeriodo
    {
        /// <summary>
        /// NULL = todas las categorias.
        /// </summary>
        public int? IdCategoria { get; set; }

        /// <summary>
        /// Cantidad maxima de filas del ranking.
        /// 0 o menos significa que el servidor usa 200.
        /// </summary>
        public int TopN { get; set; } = 50;
    }
}
