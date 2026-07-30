using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Ventas
{
    public class VentaPOS
    {
        public int IdVenta { get; set; }

        public string NumeroVenta { get; set; }

        public int? IdClientePOS { get; set; }

        public int IdUsuario { get; set; }

        public string MetodoPago { get; set; }

        public decimal Subtotal { get; set; }

        public decimal IGV { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; }

        public DateTime FechaVenta { get; set; }
    }
}
