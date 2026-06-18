using ProySistemaVentas.Services;
using System.Windows;


namespace ProySistemaVentas.Views
{
    /// <summary>
    /// Lógica de interacción para RegistroUsuarioView.xaml
    /// </summary>
    public partial class RegistroUsuarioView : Window
    {
        
        private readonly UsuarioService _usuarioService;
        public bool UsuarioCreado { get; private set; }

        public RegistroUsuarioView()
        {
            InitializeComponent();

            _usuarioService =
                new UsuarioService();

            cmbRol.ItemsSource =
                _usuarioService.ObtenerRoles();
        }

        private void BtnGuardar_Click(
            object sender,
            RoutedEventArgs e)
        {
            string hash =
                PasswordService.HashPassword(
                    txtPassword.Password);

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show(
                    "Ingrese el nombre");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            {
                MessageBox.Show(
                    "Ingrese el usuario");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show(
                    "Ingrese la contraseña");
                return;
            }

            if (cmbRol.SelectedValue == null)
            {
                MessageBox.Show(
                    "Seleccione un rol");
                return;
            }

            bool guardado =
                _usuarioService.CrearUsuario(
                    txtNombre.Text,
                    txtUsuario.Text,
                    hash,
                    (int)cmbRol.SelectedValue);

            if (guardado)
            {
                UsuarioCreado = true;

                MessageBox.Show(
                    "Usuario registrado correctamente.",
                    "Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            else
            {
                MessageBox.Show(
                    "No se pudo registrar el usuario.",
                    "Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}

