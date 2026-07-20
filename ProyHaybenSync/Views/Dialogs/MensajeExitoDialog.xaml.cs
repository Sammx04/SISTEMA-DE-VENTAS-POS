using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class MensajeExitoDialog : Window
    {
        private readonly bool _esConfirmacion;

        // =====================================================
        // CONSTRUCTOR PARA MENSAJES NORMALES
        // =====================================================

        public MensajeExitoDialog(
            string titulo,
            string mensaje)
            : this(
                titulo,
                mensaje,
                false)
        {
        }

        // =====================================================
        // CONSTRUCTOR INTERNO
        // =====================================================

        private MensajeExitoDialog(
            string titulo,
            string mensaje,
            bool esConfirmacion)
        {
            InitializeComponent();

            _esConfirmacion =
                esConfirmacion;

            TxtTitulo.Text =
                titulo;

            TxtMensaje.Text =
                mensaje;

            if (_esConfirmacion)
            {
                ConfigurarComoConfirmacion();
            }
            else
            {
                ConfigurarComoExito();
            }
        }

        // =====================================================
        // MOSTRAR MENSAJE DE ÉXITO
        // =====================================================

        public static void Mostrar(
            Window ventanaPadre,
            string titulo,
            string mensaje)
        {
            MensajeExitoDialog dialogo =
                new MensajeExitoDialog(
                    titulo,
                    mensaje,
                    false);

            if (ventanaPadre != null)
            {
                dialogo.Owner =
                    ventanaPadre;
            }

            dialogo.ShowDialog();
        }

        // =====================================================
        // MOSTRAR MENSAJE DE CONFIRMACIÓN
        // =====================================================

        public static bool Confirmar(
            Window ventanaPadre,
            string titulo,
            string mensaje)
        {
            MensajeExitoDialog dialogo =
                new MensajeExitoDialog(
                    titulo,
                    mensaje,
                    true);

            if (ventanaPadre != null)
            {
                dialogo.Owner =
                    ventanaPadre;
            }

            return dialogo.ShowDialog() == true;
        }

        // =====================================================
        // CONFIGURACIÓN COMO MENSAJE EXITOSO
        // =====================================================

        private void ConfigurarComoExito()
        {
            TxtSubtitulo.Text =
                "Proceso realizado correctamente";

            TxtIcono.Text =
                "✓";

            BordeIcono.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        23,
                        60,
                        42));

            BordeIcono.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        34,
                        197,
                        94));

            TxtIcono.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        34,
                        197,
                        94));

            BtnAceptar.Content =
                "Aceptar";

            BtnCancelar.Visibility =
                Visibility.Collapsed;
        }

        // =====================================================
        // CONFIGURACIÓN COMO CONFIRMACIÓN
        // =====================================================

        private void ConfigurarComoConfirmacion()
        {
            TxtSubtitulo.Text =
                "Confirme la operación";

            TxtIcono.Text =
                "?";

            BordeIcono.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        66,
                        45,
                        18));

            BordeIcono.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(
                        245,
                        158,
                        11));

            TxtIcono.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        245,
                        158,
                        11));

            BtnAceptar.Content =
                "Eliminar";

            BtnCancelar.Visibility =
                Visibility.Visible;
        }

        // =====================================================
        // EVENTOS
        // =====================================================

        private void BtnAceptar_Click(
            object sender,
            RoutedEventArgs e)
        {
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
            DialogResult =
                _esConfirmacion
                    ? false
                    : true;
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