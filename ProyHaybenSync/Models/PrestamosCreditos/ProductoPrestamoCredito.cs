namespace ProySistemaVentas.Models.PrestamosCreditos
{
    public class ProductoPrestamoCredito
    {
        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; } =
            string.Empty;

        public string Categoria { get; set; } =
            string.Empty;

        public string Variante { get; set; } =
            string.Empty;

        public string CodigoBarra { get; set; } =
            string.Empty;

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        public int StockTienda { get; set; }

        public string NombreCompleto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Variante) ||
                    Variante.Equals(
                        "Única",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return NombreProducto;
                }

                return $"{NombreProducto} - {Variante}";
            }
        }

        public string PrecioUnidadTexto
        {
            get
            {
                return $"S/ {PrecioUnidad:N2}";
            }
        }

        public string PrecioMayorTexto
        {
            get
            {
                return PrecioMayor > 0
                    ? $"S/ {PrecioMayor:N2}"
                    : "No disponible";
            }
        }

        public string StockTiendaTexto
        {
            get
            {
                return $"Stock tienda: {StockTienda}";
            }
        }

        public bool TienePrecioMayor
        {
            get
            {
                return PrecioMayor > 0;
            }
        }

        public bool TieneStock
        {
            get
            {
                return StockTienda > 0;
            }
        }
    }
}