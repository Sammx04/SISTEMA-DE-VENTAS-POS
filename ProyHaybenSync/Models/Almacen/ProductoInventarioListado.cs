using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Almacen
{
    public class ProductoInventarioListado
    {
        // Datos del producto
        public int IdProducto { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        // Datos del inventario
        public string Descripcion { get; set; } = string.Empty;

        public int IdCategoria { get; set; }

        public string NombreCategoria { get; set; } = string.Empty;

        public string CodigoBase { get; set; } = string.Empty;

        public bool TieneVariantes { get; set; }

        public bool Estado { get; set; }

        // Datos de la variante
        public int IdVariante { get; set; }

        public string VarianteDescripcion { get; set; } = string.Empty;

        public string CodigoBarras { get; set; } = string.Empty;

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        // Stock
        public int StockAlmacen { get; set; }

        public int StockTienda { get; set; }
    }
}
