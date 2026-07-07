using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Almacen
{
    public class Variante
    {
        public int IdVariante { get; set; }

        public int IdProducto { get; set; }

        public string Descripcion { get; set; }

        public string CodigoBarras { get; set; }

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        public bool Estado { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
