using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Reporte
{
    internal class ProductoMasVendido
    {
        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; }

        public string Categoria { get; set; }

        public string CodigoBarra { get; set; }

        public int CantidadVendida { get; set; }

        public decimal TotalGenerado { get; set; }
    }
}
