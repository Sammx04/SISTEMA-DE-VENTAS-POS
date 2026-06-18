using ProySistemaVentas.Models;
using ProySistemaVentas.Services;
using System.Windows;
using System.Windows.Controls;
namespace ProySistemaVentas.Views.UserControls
{
    /// <summary>
    /// Lógica de interacción para UsuariosControl.xaml
    /// </summary>
    public partial class UsuariosControl : UserControl
    {
        private readonly UsuarioService _usuarioService;

        public UsuariosControl()
        {
            InitializeComponent();

            _usuarioService =
                new UsuarioService();

            CargarUsuarios();
        }

        private void BtnNuevo_Click(
    object sender,
    RoutedEventArgs e)
        {
            var ventana =
                new RegistroUsuarioView();

            ventana.ShowDialog();

            if (ventana.UsuarioCreado)
            {
                CargarUsuarios();
            }
        }

        private void CargarUsuarios()
        {
            var lista = _usuarioService.ListarUsuarios();

            dgUsuarios.ItemsSource = null;
            dgUsuarios.ItemsSource = lista;

            dgUsuarios.Items.Refresh();
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            dgUsuarios.ItemsSource =
                _usuarioService.BuscarUsuarios(
                    txtBuscar.Text);
        }

        private void BtnEstado_Click(
     object sender,
     RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem == null)
            {
                MessageBox.Show(
                    "Seleccione un usuario.");

                return;
            }

            UsuarioListado usuario =
                (UsuarioListado)dgUsuarios.SelectedItem;

            bool nuevoEstado =
                !usuario.Estado;

            string accion =
                nuevoEstado
                    ? "activar"
                    : "desactivar";

            var confirmar =
                MessageBox.Show(
                    $"¿Desea {accion} este usuario?",
                    "Confirmación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes)
                return;

            bool actualizado =
                _usuarioService.CambiarEstadoUsuario(
                    usuario.IdUsuario,
                    nuevoEstado);

            if (actualizado)
            {
                MessageBox.Show(
                    nuevoEstado
                        ? "Usuario activado correctamente."
                        : "Usuario desactivado correctamente.",
                    "Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CargarUsuarios();
            }
            else
            {
                MessageBox.Show(
                    "No se pudo actualizar el estado del usuario.",
                    "Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem == null)
            {
                MessageBox.Show(
                    "Seleccione un usuario.");

                return;
            }

            UsuarioListado usuario =
                (UsuarioListado)
                dgUsuarios.SelectedItem;

            EditarUsuarioView ventana =
                new EditarUsuarioView(
                    usuario.IdUsuario);
            ventana.ShowDialog();

            if (ventana.UsuarioActualizado)
            {
                CargarUsuarios();
            }
        }
    }
}
