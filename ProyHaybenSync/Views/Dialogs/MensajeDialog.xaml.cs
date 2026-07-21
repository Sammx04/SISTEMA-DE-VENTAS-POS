using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ProySistemaVentas.Views.Dialogs
{
    public enum TipoMensaje
    {
        Exito,
        Informacion,
        Advertencia,
        Error,
        Confirmacion
    }

    public partial class MensajeDialog : Window
    {
        public bool Confirmado { get; private set; }

        private MensajeDialog(
            Window owner,
            string titulo,
            string mensaje,
            TipoMensaje tipo)
        {
            InitializeComponent();

            if (owner != null)
            {
                Owner = owner;
            }

            TxtTitulo.Text = titulo;
            TxtMensaje.Text = mensaje;

            ConfigurarTipo(tipo);
        }

        public static void Mostrar(
            Window owner,
            string titulo,
            string mensaje,
            TipoMensaje tipo)
        {
            MensajeDialog dialogo =
                new MensajeDialog(
                    owner,
                    titulo,
                    mensaje,
                    tipo);

            dialogo.ShowDialog();
        }

        public static bool Confirmar(
            Window owner,
            string titulo,
            string mensaje)
        {
            MensajeDialog dialogo =
                new MensajeDialog(
                    owner,
                    titulo,
                    mensaje,
                    TipoMensaje.Confirmacion);

            dialogo.ShowDialog();

            return dialogo.Confirmado;
        }

        private void ConfigurarTipo(
            TipoMensaje tipo)
        {
            BtnNo.Visibility = Visibility.Collapsed;
            BtnSi.Content = "Aceptar";

            switch (tipo)
            {
                case TipoMensaje.Exito:

                    TxtEncabezado.Text =
                        "Proceso realizado correctamente";

                    TxtIcono.Text = "✓";

                    AplicarColor("#22C55E");

                    break;

                case TipoMensaje.Advertencia:

                    TxtEncabezado.Text =
                        "Revisa la información";

                    TxtIcono.Text = "!";

                    AplicarColor("#F59E0B");

                    break;

                case TipoMensaje.Error:

                    TxtEncabezado.Text =
                        "No se pudo completar la operación";

                    TxtIcono.Text = "×";

                    AplicarColor("#EF4444");

                    break;

                case TipoMensaje.Confirmacion:

                    TxtEncabezado.Text =
                        "Confirmación requerida";

                    TxtIcono.Text = "?";

                    AplicarColor("#22D3EE");

                    BtnNo.Visibility =
                        Visibility.Visible;

                    BtnSi.Content = "Sí";

                    break;

                default:

                    TxtEncabezado.Text =
                        "Información";

                    TxtIcono.Text = "i";

                    AplicarColor("#67E8F9");

                    break;
            }
        }

        private void AplicarColor(
            string colorHexadecimal)
        {
            SolidColorBrush color =
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        colorHexadecimal));

            BordeIcono.BorderBrush = color;
            BordeIcono.Background =
                new SolidColorBrush(
                    Color.FromArgb(
                        25,
                        color.Color.R,
                        color.Color.G,
                        color.Color.B));

            TxtIcono.Foreground = color;
        }

        private void BtnSi_Click(
            object sender,
            RoutedEventArgs e)
        {
            Confirmado = true;
            DialogResult = true;
        }

        private void BtnNo_Click(
            object sender,
            RoutedEventArgs e)
        {
            Confirmado = false;
            DialogResult = false;
        }

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Confirmado = false;
            Close();
        }
    }
}