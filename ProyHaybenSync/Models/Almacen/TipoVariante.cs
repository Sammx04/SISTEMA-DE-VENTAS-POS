using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Almacen
{
    public class TipoVariante
    {
        public string Descripcion { get; set; }

        public string CodigoBarras { get; set; }

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        public int Stock { get; set; }
    }
}
