using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Ventas
{
    public class ProductoBusqueda
    {
        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string Nombre { get; set; }

        public string Variante { get; set; }

        public string Categoria { get; set; }

        public string CodigoBarra { get; set; }

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        public int Stock { get; set; }  
    }
}
