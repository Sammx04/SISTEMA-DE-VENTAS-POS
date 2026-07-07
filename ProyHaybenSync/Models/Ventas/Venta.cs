using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Ventas
{
    public class Venta
    {
        public int IdVenta { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        // Navegación EF Core
        public Usuario? Usuario { get; set; }

        // Una venta tiene muchos detalles
        public ICollection<DetalleVenta> Detalles { get; set; }
            = new List<DetalleVenta>();
    }
}
