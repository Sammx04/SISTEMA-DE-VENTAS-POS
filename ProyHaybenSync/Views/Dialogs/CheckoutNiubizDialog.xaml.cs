using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class CheckoutNiubizDialog : Window
    {
        private const string NombreHostCheckout =
            "checkoutniubiz.invalid";

        private readonly string _sessionKey;
        private readonly string _merchantId;
        private readonly string _numeroOrdenPago;
        private readonly decimal _monto;
        private readonly string _moneda;
        private readonly string _checkoutJs;

        public string TransactionToken { get; private set; }

        public bool PagoCompletado { get; private set; }

        public CheckoutNiubizDialog(
            string sessionKey,
            string merchantId,
            string numeroOrdenPago,
            decimal monto,
            string moneda,
            string checkoutJs)
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                throw new ArgumentNullException(nameof(sessionKey));
            }

            _sessionKey = sessionKey;
            _merchantId = merchantId;
            _numeroOrdenPago = numeroOrdenPago;
            _monto = monto;
            _moneda = moneda;
            _checkoutJs = checkoutJs;

            InitializeComponent();

            TxtMonto.Text =
                $"Total a pagar: S/ {monto:N2}";
        }

        // =====================================================
        // MOVER VENTANA
        // =====================================================

        private void Encabezado_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // =====================================================
        // INICIALIZAR CHECKOUT
        // =====================================================

        private async void BtnCargar_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                await WebPago.EnsureCoreWebView2Async();

                WebPago.CoreWebView2.WebMessageReceived +=
                    CoreWebView2_WebMessageReceived;

                WebPago.CoreWebView2.NavigationCompleted +=
                    CoreWebView2_NavigationCompleted;

                string rutaCheckout =
                    BuscarCarpetaCheckout();

                WebPago.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    NombreHostCheckout,
                    rutaCheckout,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                TxtSubTitulo.Text =
                    "Cargando la pasarela de pago...";

                WebPago.Source =
                    new Uri(CrearUrlCheckout());
            }
            catch (Exception ex)
            {
                TxtSubTitulo.Text =
                    "No se pudo conectar con la pasarela.";

                TxtMensaje.Text =
                    ex.Message;
            }
        }

        private static string BuscarCarpetaCheckout()
        {
            string? baseDir =
                AppDomain.CurrentDomain.BaseDirectory;

            string? raiz = baseDir;

            if (Directory.Exists(Path.Combine(baseDir, "Views", "Dialogs", "NiubizCheckout")))
            {
                return Path.Combine(baseDir, "Views", "Dialogs", "NiubizCheckout");
            }

            while (raiz != null)
            {
                string candidato =
                    Path.Combine(raiz, "ProyHaybenSync", "Views", "Dialogs", "NiubizCheckout");

                if (Directory.Exists(candidato))
                {
                    return candidato;
                }

                raiz =
                    Directory.GetParent(raiz)?.FullName;
            }

            throw new DirectoryNotFoundException(
                "No se encontró la carpeta NiubizCheckout.");
        }

        private string CrearUrlCheckout()
        {
            string montoTexto =
                _monto.ToString(
                    "F2",
                    CultureInfo.InvariantCulture);

            return "https://" + NombreHostCheckout + "/checkout.html?" +
                "session=" + Uri.EscapeDataString(_sessionKey) +
                "&merchant=" + Uri.EscapeDataString(_merchantId) +
                "&purchase=" + Uri.EscapeDataString(_numeroOrdenPago) +
                "&amount=" + Uri.EscapeDataString(montoTexto) +
                "&currency=" + Uri.EscapeDataString(_moneda) +
                "&checkoutJs=" + Uri.EscapeDataString(_checkoutJs);
        }

        private void CoreWebView2_NavigationCompleted(
            object sender,
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Dispatcher.Invoke(
                    () =>
                    {
                        TxtSubTitulo.Text =
                            "No se pudo cargar la pasarela.";

                        TxtMensaje.Text =
                            "Estado de navegación: " + e.WebErrorStatus +
                            " (revise su conexión a internet).";
                    });

                return;
            }

            Dispatcher.Invoke(
                () =>
                {
                    TxtSubTitulo.Text =
                        "Prepare la tarjeta para el pago.";

                    PanelCargando.Visibility =
                        Visibility.Collapsed;

                    WebPago.Visibility =
                        Visibility.Visible;
                });
        }

        // =====================================================
        // PROCESAR MENSAJES DEL CHECKOUT
        // =====================================================

        private void CoreWebView2_WebMessageReceived(
            object sender,
            Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string mensaje =
                e.TryGetWebMessageAsString();

            if (string.IsNullOrWhiteSpace(mensaje))
            {
                return;
            }

            try
            {
                using System.Text.Json.JsonDocument documento =
                    System.Text.Json.JsonDocument.Parse(mensaje);

                if (documento.RootElement.TryGetProperty("token", out var tokenProp))
                {
                    TransactionToken =
                        tokenProp.GetString();

                    if (!string.IsNullOrWhiteSpace(TransactionToken))
                    {
                        PagoCompletado = true;

                        this.DialogResult = true;

                        return;
                    }
                }

                if (documento.RootElement.TryGetProperty("error", out var errorProp) &&
                    errorProp.GetBoolean())
                {
                    string codigo =
                        documento.RootElement.TryGetProperty("codigo", out var c)
                            ? c.GetString()
                            : "0";

                    string mensajeError =
                        documento.RootElement.TryGetProperty("mensaje", out var m)
                            ? m.GetString()
                            : "La tarjeta fue rechazada.";

                    string tipo =
                        documento.RootElement.TryGetProperty("tipo", out var t)
                            ? t.GetString()
                            : null;

                    Dispatcher.Invoke(
                        () =>
                        {
                            bool errorCarga =
                                string.Equals(tipo, "js", StringComparison.OrdinalIgnoreCase);

                            TxtSubTitulo.Text =
                                errorCarga
                                    ? "El formulario de pago no pudo iniciarse."
                                    : "El pago fue rechazado.";

                            TxtMensaje.Text =
                                errorCarga
                                    ? mensajeError
                                    : $"{mensajeError} (código {codigo})";
                        });
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Se ignora si el mensaje no es JSON válido.
            }
        }

        // =====================================================
        // CANCELAR
        // =====================================================

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            PagoCompletado = false;
            TransactionToken = null;

            this.DialogResult = false;
        }
    }
}