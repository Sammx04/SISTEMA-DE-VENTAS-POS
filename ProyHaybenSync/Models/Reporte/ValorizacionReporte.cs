using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Fila del reporte de valorizacion de inventario
    /// agrupado por categoria
    /// (sp_Reporte_ValorizacionInventario).
    /// </summary>
    public class ValorizacionReporte
    {
        public int IdCategoria { get; set; }

        public string Categoria { get; set; } = string.Empty;

        public int Productos { get; set; }

        public int Variantes { get; set; }

        public int Unidades { get; set; }

        public decimal ValorAlmacen { get; set; }

        public decimal ValorTienda { get; set; }

        public decimal ValorTotal { get; set; }

        public string ProductosTexto =>
            Productos.ToString("N0", CultureInfo.InvariantCulture);

        public string VariantesTexto =>
            Variantes.ToString("N0", CultureInfo.InvariantCulture);

        public string UnidadesTexto =>
            Unidades.ToString("N0", CultureInfo.InvariantCulture);

        public string ValorAlmacenTexto =>
            ValorAlmacen.ToString("N2", CultureInfo.InvariantCulture);

        public string ValorTiendaTexto =>
            ValorTienda.ToString("N2", CultureInfo.InvariantCulture);

        public string ValorTotalTexto =>
            ValorTotal.ToString("N2", CultureInfo.InvariantCulture);
    }
}
