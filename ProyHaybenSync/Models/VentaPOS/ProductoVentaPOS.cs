namespace ProySistemaVentas.Models.VentasPOS
{
    public class ProductoVentaPOS
    {
        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string Nombre { get; set; }

        public string Variante { get; set; }

        public string Categoria { get; set; }

        public string CodigoBarra { get; set; }

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        /*
         * Stock disponible únicamente en TIENDA.
         * Este será el máximo que se podrá vender.
         */
        public int Stock { get; set; }

        /*
         * Se utiliza cuando el producto procede del
         * procedimiento de productos frecuentes.
         */
        public int VecesVendido { get; set; }

        public string NombreVarianteMostrar
        {
            get
            {
                return string.IsNullOrWhiteSpace(Variante)
                    ? "Única"
                    : Variante.Trim();
            }
        }

        public string NombreCompleto
        {
            get
            {
                string nombreProducto =
                    string.IsNullOrWhiteSpace(Nombre)
                        ? "Producto"
                        : Nombre.Trim();

                if (string.IsNullOrWhiteSpace(Variante) ||
                    Variante.Trim()
                        .Equals(
                            "Única",
                            System.StringComparison.OrdinalIgnoreCase))
                {
                    return nombreProducto;
                }

                return $"{nombreProducto} - {Variante.Trim()}";
            }
        }

        public string PrecioUnidadTexto =>
            $"S/ {PrecioUnidad:N2}";

        public string PrecioMayorTexto
        {
            get
            {
                if (PrecioMayor <= 0)
                {
                    return "No disponible";
                }

                return $"S/ {PrecioMayor:N2}";
            }
        }

        public string StockTexto =>
            $"{Stock:N0} disponibles";

        public bool TieneStock =>
            Stock > 0;

        public bool TienePrecioMayor =>
            PrecioMayor > 0;
    }
}