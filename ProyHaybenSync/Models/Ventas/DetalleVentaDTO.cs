using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Ventas
{
    public class DetalleVentaDTO
    {
        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; }

        public string Categoria { get; set; }

        public string CodigoBarra { get; set; }

        public string TipoPrecio { get; set; }

        public decimal PrecioUnitario { get; set; }

        public int Cantidad { get; set; }

        public decimal Descuento { get; set; }

        public decimal TotalLinea { get; set; }
    }
}
