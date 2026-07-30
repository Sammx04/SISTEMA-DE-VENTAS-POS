using System.Windows;
using System.Windows.Input;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class NuevaCategoriaDialog : Window
    {
        public string NombreCategoria { get; private set; }

        public NuevaCategoriaDialog()
        {
            InitializeComponent();

            Loaded += NuevaCategoriaDialog_Loaded;
        }

        private void NuevaCategoriaDialog_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            TxtNombreCategoria.Focus();
        }

        private void BtnGuardar_Click(
            object sender,
            RoutedEventArgs e)
        {
            string nombre =
                TxtNombreCategoria.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                TxtMensajeError.Visibility =
                    Visibility.Visible;

                TxtNombreCategoria.Focus();

                return;
            }

            NombreCategoria = nombre;

            DialogResult = true;
        }

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TxtNombreCategoria_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnGuardar_Click(
                    sender,
                    new RoutedEventArgs());

                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                DialogResult = false;

                e.Handled = true;
            }
        }

        private void Encabezado_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton ==
                MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}