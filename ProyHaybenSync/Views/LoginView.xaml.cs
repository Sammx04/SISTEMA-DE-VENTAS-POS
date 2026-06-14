using ProySistemaVentas.Services;
using ProySistemaVentas.ViewModels;
using ProySistemaVentas.Views;
using System.Windows;

namespace ProySistemaVentas.Views
{
    public partial class LoginView : Window
    {
        private readonly UsuarioService _usuarioService;

        public LoginView()
        {
            InitializeComponent();

            _usuarioService = new UsuarioService();

            DataContext = new LoginViewModel();
        }

        private void BtnIngresar_Click(object sender, RoutedEventArgs e)
        {
            var vm = (LoginViewModel)DataContext;

            var usuario =
                _usuarioService.ObtenerUsuario(vm.Usuario);

            if (usuario == null)
            {
                MessageBox.Show("Usuario no existe");
                return;
            }

            bool valido =
                PasswordService.VerifyPassword(
                    txtPassword.Password,
                    usuario.Password);

            if (!valido)
            {
                MessageBox.Show("Contraseña incorrecta");
                return;
            }

            UsuariosView usuarios = new UsuariosView();

            usuarios.Show();

            this.Close();
        }
    }
}
