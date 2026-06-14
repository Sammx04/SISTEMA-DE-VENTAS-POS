using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models
{
    public class Caja
    {
        public int IdCaja { get; set; }
        public int IdUsuario { get; set; }
        public DateTime FechaApertura { get; set; }
        public decimal MontoInicial { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal? MontoFinal { get; set; }
        public string Estado { get; set; } = string.Empty;

        // Navegación EF Core
        public Usuario? Usuario { get; set; }
    }
}
