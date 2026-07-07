using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Almacen
{
    public class ProductoInventario
    {
        public int IdProductoInventario { get; set; }

        public int IdProducto { get; set; }

        public int IdCategoria { get; set; }

        public string Descripcion { get; set; }

        public string CodigoBase { get; set; }

        public bool TieneVariantes { get; set; }

        public bool Estado { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
