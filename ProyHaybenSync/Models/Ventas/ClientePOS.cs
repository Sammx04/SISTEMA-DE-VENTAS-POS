using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Ventas
{
    public class ClientePOS
    {
        public int IdClientePOS { get; set; }

        public string TipoDocumento { get; set; }

        public string NumeroDocumento { get; set; }

        public string NombreCompleto { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}
