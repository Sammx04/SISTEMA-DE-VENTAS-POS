using ProySistemaVentas.Models;
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
    /// Lógica de interacción para UsuariosView.xaml
    /// </summary>
    public partial class UsuariosView : Window
    {
        private readonly UsuarioService _usuarioService;

        public UsuariosView()
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
            RegistroUsuarioView ventana =
                new RegistroUsuarioView();

            ventana.ShowDialog();

            CargarUsuarios();
        }
        private void CargarUsuarios()
        {
            dgUsuarios.ItemsSource =
                _usuarioService.ListarUsuarios();
        }

        private void BtnBuscar_Click(
            object sender,
            RoutedEventArgs e)
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
                    "Seleccione un usuario");

                return;
            }

            UsuarioListado usuario =
                (UsuarioListado)dgUsuarios.SelectedItem;

            bool nuevoEstado =
                !usuario.Estado;

            _usuarioService.CambiarEstadoUsuario(
                usuario.IdUsuario,
                nuevoEstado);

            CargarUsuarios();
        }
    }
}
