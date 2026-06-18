using ProySistemaVentas.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProySistemaVentas.Views
{
    /// <summary>
    /// Lógica de interacción para EditarUsuarioView.xaml
    /// </summary>
    public partial class EditarUsuarioView : Window
    {
        private readonly UsuarioService _usuarioService;
        private readonly int _idUsuario;
        public bool UsuarioActualizado { get; private set; }

        public EditarUsuarioView(int idUsuario)
        {
            InitializeComponent();

            _usuarioService =
                new UsuarioService();

            _idUsuario = idUsuario;

            cmbRol.ItemsSource =
                _usuarioService.ObtenerRoles();

            CargarUsuario();
        }
        private void CargarUsuario()
        {
            var usuario =
                _usuarioService.ObtenerUsuarioPorId(
                    _idUsuario);

            txtNombre.Text =
                usuario.Nombre;

            txtUsuario.Text =
                usuario.UsuarioLogin;

            cmbRol.SelectedValue =
                usuario.IdRol;

            chkActivo.IsChecked =
                usuario.Estado;
        }

        private void BtnGuardar_Click( object sender, RoutedEventArgs e)        {
            bool actualizado =
                _usuarioService.ActualizarUsuario(
                    _idUsuario,
                    txtNombre.Text,
                    txtUsuario.Text,
                    (int)cmbRol.SelectedValue,
                    chkActivo.IsChecked ?? true);

            if (actualizado)
            {
                UsuarioActualizado = true;

                MessageBox.Show(
                    "Usuario actualizado correctamente.",
                    "Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            else
            {
                MessageBox.Show(
                    "No se pudo actualizar el usuario.");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
