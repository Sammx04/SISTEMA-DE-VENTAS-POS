using ProySistemaVentas.Models;
using ProySistemaVentas.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProySistemaVentas.Views.UserControls
{
    public partial class UsuariosControl : UserControl
    {
        private readonly UsuarioService _usuarioService;
        private List<UsuarioListado> _usuariosOriginales; // Datos completos desde BD
        private List<UsuarioListado> _listaFiltrada;     // Resultado del filtro actual
        private int _paginaActual = 1;
        private const int PAGE_SIZE = 6; // Registros por página

        public UsuariosControl()
        {
            InitializeComponent();
            _usuarioService = new UsuarioService();
            CargarUsuarios();
        }

        private void CargarUsuarios()
        {
            _usuariosOriginales = _usuarioService.ListarUsuarios();
            AplicarFiltro(); // Aplica filtro actual (vacío al inicio) y muestra página 1
        }

        private void AplicarFiltro()
        {
            string filtro = txtBuscar.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(filtro))
            {
                _listaFiltrada = _usuariosOriginales;
            }
            else
            {
                _listaFiltrada = _usuariosOriginales
                    .Where(u => (u.Nombre != null && u.Nombre.ToLower().Contains(filtro))
                             || (u.Usuario != null && u.Usuario.ToLower().Contains(filtro)))
                    .ToList();
            }

            // Al cambiar el filtro, volvemos a la primera página
            _paginaActual = 1;
            MostrarPagina(_paginaActual);
            ActualizarPaginacion();
        }

        private void MostrarPagina(int pagina)
        {
            if (_listaFiltrada == null || _listaFiltrada.Count == 0)
            {
                dgUsuarios.ItemsSource = null;
                return;
            }

            int totalRegistros = _listaFiltrada.Count;
            int totalPaginas = (totalRegistros + PAGE_SIZE - 1) / PAGE_SIZE;

            // Ajustar página si se sale de rango
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas) pagina = totalPaginas;
            _paginaActual = pagina;

            int inicio = (pagina - 1) * PAGE_SIZE;
            int fin = inicio + PAGE_SIZE;
            if (fin > totalRegistros) fin = totalRegistros;

            // Obtener subconjunto
            var paginaDatos = _listaFiltrada.GetRange(inicio, fin - inicio);
            dgUsuarios.ItemsSource = paginaDatos;

            // Actualizar texto de estado
            int mostrandoDesde = inicio + 1;
            int mostrandoHasta = fin;
            txtEstadoPagina.Text = $"Mostrando {mostrandoDesde} a {mostrandoHasta} de {totalRegistros} usuarios";

            // Reconstruir botones de páginas
            GenerarBotonesPagina(totalPaginas, pagina);
        }

        private void GenerarBotonesPagina(int totalPaginas, int paginaActual)
        {
            pnlPageNumbers.Children.Clear();

            if (totalPaginas <= 1) return; // No mostrar números si solo hay una página

            // Lógica simplificada: mostrar todos los números si ≤ 7, si no, mostrar primeros, actual, últimos con puntos suspensivos
            int maxBotones = 7;
            int inicio, fin;

            if (totalPaginas <= maxBotones)
            {
                inicio = 1;
                fin = totalPaginas;
            }
            else
            {
                int rango = maxBotones - 2; // Dejamos espacio para primer y último fijo
                int mitad = rango / 2;

                inicio = paginaActual - mitad;
                fin = paginaActual + (rango - mitad - 1);

                if (inicio < 2)
                {
                    inicio = 2;
                    fin = inicio + rango - 1;
                }
                if (fin > totalPaginas - 1)
                {
                    fin = totalPaginas - 1;
                    inicio = fin - rango + 1;
                }

                // Siempre mostramos la primera página
                AgregarBotonPagina(1, paginaActual == 1);
                if (inicio > 2)
                {
                    AgregarTextoElipsis();
                }
            }

            for (int i = inicio; i <= fin; i++)
            {
                AgregarBotonPagina(i, i == paginaActual);
            }

            if (totalPaginas > maxBotones && fin < totalPaginas - 1)
            {
                AgregarTextoElipsis();
            }

            // Última página
            if (totalPaginas > maxBotones && totalPaginas > 1)
            {
                AgregarBotonPagina(totalPaginas, paginaActual == totalPaginas);
            }
        }

        private void AgregarBotonPagina(int numero, bool esActual)
        {
            Border borde = new Border
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(2),
                Cursor = Cursors.Hand
            };

            if (esActual)
            {
                borde.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22D3EE"));
                borde.BorderThickness = new Thickness(0);
            }
            else
            {
                borde.Background = Brushes.Transparent;
                borde.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                borde.BorderThickness = new Thickness(1);
            }

            TextBlock texto = new TextBlock
            {
                Text = numero.ToString(),
                Foreground = esActual ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = esActual ? FontWeights.Bold : FontWeights.Normal
            };

            borde.Child = texto;
            int pagina = numero; // Capturar para el evento
            borde.MouseLeftButtonDown += (s, e) => IrPagina(pagina);

            pnlPageNumbers.Children.Add(borde);
        }

        private void AgregarTextoElipsis()
        {
            TextBlock puntos = new TextBlock
            {
                Text = "...",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
            pnlPageNumbers.Children.Add(puntos);
        }

        private void IrPagina(int pagina)
        {
            MostrarPagina(pagina);
        }

        private void IrPaginaAnterior(object sender, MouseButtonEventArgs e)
        {
            int totalPaginas = (_listaFiltrada.Count + PAGE_SIZE - 1) / PAGE_SIZE;
            if (_paginaActual > 1)
                MostrarPagina(_paginaActual - 1);
        }

        private void IrPaginaSiguiente(object sender, MouseButtonEventArgs e)
        {
            int totalPaginas = (_listaFiltrada.Count + PAGE_SIZE - 1) / PAGE_SIZE;
            if (_paginaActual < totalPaginas)
                MostrarPagina(_paginaActual + 1);
        }

        private void ActualizarPaginacion()
        {
            // Se llama desde AplicarFiltro para regenerar botones
            // En realidad MostrarPagina ya la llama, así que no hace falta.
        }

        // Eventos de botones existentes
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltro();
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            CargarUsuarios();
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new RegistroUsuarioView();
            ventana.ShowDialog();
            if (ventana.UsuarioCreado)
                CargarUsuarios();
        }

        private void BtnEstado_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un usuario.");
                return;
            }

            UsuarioListado usuario = (UsuarioListado)dgUsuarios.SelectedItem;
            bool nuevoEstado = !usuario.Estado;
            string accion = nuevoEstado ? "activar" : "desactivar";

            var confirmar = MessageBox.Show(
                $"¿Desea {accion} este usuario?",
                "Confirmación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes) return;

            bool actualizado = _usuarioService.CambiarEstadoUsuario(usuario.IdUsuario, nuevoEstado);
            if (actualizado)
            {
                MessageBox.Show(
                    nuevoEstado ? "Usuario activado correctamente." : "Usuario desactivado correctamente.",
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
                MessageBox.Show("Seleccione un usuario.");
                return;
            }

            UsuarioListado usuario = (UsuarioListado)dgUsuarios.SelectedItem;
            EditarUsuarioView ventana = new EditarUsuarioView(usuario.IdUsuario);
            ventana.ShowDialog();
            if (ventana.UsuarioActualizado)
                CargarUsuarios();
        }
    }
}