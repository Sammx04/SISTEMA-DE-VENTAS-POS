using ProySistemaVentas.Helpers;
using ProySistemaVentas.Services;
using System.Windows;
using System.Windows.Input;

namespace ProySistemaVentas.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly UsuarioService _usuarioService;
        private string _usuario = string.Empty;
        private string _password = string.Empty;

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string Usuario
        {
            get => _usuario;
            set
            {
                _usuario = value;
                OnPropertyChanged();
            }
        }

        public ICommand IngresarCommand { get; }

        public LoginViewModel()
        {
            _usuarioService = new UsuarioService();

            IngresarCommand =
                new RelayCommand(Ingresar);
        }

        private void Ingresar()
        {
            var usuario =
                _usuarioService.ObtenerUsuario(Usuario);

            if (usuario == null)
            {
                MessageBox.Show(
                    "Usuario no existe");

                return;
            }

            MessageBox.Show(
                $"Bienvenido {usuario.Nombre}\nRol: {usuario.Rol}");
        }


    }
}
