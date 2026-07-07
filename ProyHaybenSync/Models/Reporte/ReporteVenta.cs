using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Reporte
{
    internal class ReporteVenta
    {
        public DateTime Fecha { get; set; }

        public int CantidadVentas { get; set; }

        public decimal Subtotal { get; set; }

        public decimal IGV { get; set; }

        public decimal Total { get; set; }
    }
}
