using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Dashboard
{
    public class DashboardPOS
    {
        public decimal VentasHoy { get; set; }

        public int CantidadVentasHoy { get; set; }

        public int ProductosVendidosHoy { get; set; }

        public int StockBajo { get; set; }
    }
}
