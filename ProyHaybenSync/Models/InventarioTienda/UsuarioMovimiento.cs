namespace ProySistemaVentas.Models.InventarioTienda
{
    public class UsuarioMovimiento
    {
        public int IdUsuario { get; set; }

        public string Nombre { get; set; }

        public string Usuario { get; set; }

        public string Rol { get; set; }

        /*
         * Texto que aparecerá dentro del ComboBox
         * de la ventana de transferencia.
         */
        public string NombreMostrar
        {
            get
            {
                string nombre =
                    string.IsNullOrWhiteSpace(
                        Nombre)
                        ? "Usuario"
                        : Nombre.Trim();

                string rol =
                    string.IsNullOrWhiteSpace(
                        Rol)
                        ? string.Empty
                        : $" - {Rol.Trim()}";

                return nombre + rol;
            }
        }
    }
}