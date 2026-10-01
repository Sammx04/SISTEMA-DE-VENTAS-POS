using System;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Filtros de los reportes de inventario, que son
    /// de estado actual y NO dependen de un rango de
    /// fechas (sp_Reporte_StockActual).
    ///
    /// El corte de stock bajo replica la regla de
    /// InventarioTiendaProducto.EstadoTienda:
    ///   AGOTADO : StockTienda &lt;= 0
    ///   BAJO    : 0 &lt; StockTienda &lt;= 5
    ///   NORMAL  : StockTienda &gt; 5
    /// </summary>
    public class FiltroReporteInventario
    {
        /// <summary>
        /// NULL = todas las categorias.
        /// </summary>
        public int? IdCategoria { get; set; }

        /// <summary>
        /// ALMACEN, TIENDA o NULL (ambas ubicaciones).
        /// </summary>
        public string? TipoUbicacion { get; set; }

        /// <summary>
        /// AGOTADO, BAJO, NORMAL o NULL (todos).
        /// </summary>
        public string? EstadoStock { get; set; }

        /// <summary>
        /// Texto libre sobre nombre, variante, codigo de
        /// barras, codigo base o categoria.
        /// </summary>
        public string? Texto { get; set; }

        /// <summary>
        /// true = incluye productos y variantes con
        /// Estado = 0. Por defecto solo activos.
        /// </summary>
        public bool IncluirInactivos { get; set; }
    }
}
