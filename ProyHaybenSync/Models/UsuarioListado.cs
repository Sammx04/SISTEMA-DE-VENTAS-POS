using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models
{
    public class UsuarioListado
    {
        public int IdUsuario { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public bool Estado { get; set; }
    }
}
