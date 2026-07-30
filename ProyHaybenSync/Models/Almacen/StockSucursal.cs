using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Almacen
{
    public class StockSucursal
    {
        public int IdStock { get; set; }

        public int IdVariante { get; set; }

        public string TipoUbicacion { get; set; }

        public int Stock { get; set; }
    }
}
