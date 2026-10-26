using System.Windows;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class ConfirmarObservacionDialog : Window
    {
        public bool Confirmado { get; private set; }

        public string Observacion { get; private set; } = string.Empty;

        public ConfirmarObservacionDialog(
            string titulo,
            string mensaje,
            string textoConfirmar)
        {
            InitializeComponent();

            TxtTitulo.Text = titulo ?? string.Empty;
            TxtMensaje.Text = mensaje ?? string.Empty;
            BtnConfirmar.Content = textoConfirmar ?? string.Empty;
        }

        private void BtnConfirmar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Observacion = TxtObservacion.Text.Trim();
            Confirmado = true;
            DialogResult = true;
        }

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Confirmado = false;
            DialogResult = false;
        }
    }
}
