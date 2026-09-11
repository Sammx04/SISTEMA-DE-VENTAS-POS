using ProySistemaVentas.Models.VentasPOS;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class VentaCompletadaDialog : Window
    {
        private readonly ResultadoVentaPOS _venta;

        public bool NuevaVentaSolicitada { get; private set; }

        public VentaCompletadaDialog(
            ResultadoVentaPOS venta)
        {
            if (venta == null)
            {
                throw new ArgumentNullException(
                    nameof(venta));
            }

            _venta = venta;

            InitializeComponent();

            DataContext = _venta;

            ConfigurarVisibilidadTotales();
        }

        // =====================================================
        // MOSTRAR U OCULTAR DESCUENTO Y RECARGO
        // =====================================================

        private void ConfigurarVisibilidadTotales()
        {
            FilaDescuento.Visibility =
                _venta.TieneDescuento
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            FilaRecargoTarjeta.Visibility =
                _venta.TieneRecargoTarjeta
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        // =====================================================
        // MOVER VENTANA
        // =====================================================

        private void Encabezado_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton !=
                MouseButtonState.Pressed)
            {
                return;
            }

            try
            {
                DragMove();
            }
            catch
            {
                // No se requiere ninguna acción.
            }
        }

        // =====================================================
        // CERRAR
        // =====================================================

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            NuevaVentaSolicitada = false;

            CerrarVentana(false);
        }

        // =====================================================
        // NUEVA VENTA
        // =====================================================

        private void BtnNuevaVenta_Click(
            object sender,
            RoutedEventArgs e)
        {
            NuevaVentaSolicitada = true;

            CerrarVentana(true);
        }

        // =====================================================
        // ENVIAR RESUMEN POR WHATSAPP
        // =====================================================

        private void BtnWhatsApp_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string mensaje =
                    ConstruirMensajeWhatsApp();

                string mensajeCodificado =
                    Uri.EscapeDataString(mensaje);

                string direccion =
                    "https://wa.me/?text=" +
                    mensajeCodificado;

                ProcessStartInfo proceso =
                    new ProcessStartInfo
                    {
                        FileName = direccion,
                        UseShellExecute = true
                    };

                Process.Start(proceso);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "No se pudo abrir WhatsApp.\n\n" +
                    ex.Message,
                    "WhatsApp",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private string ConstruirMensajeWhatsApp()
        {
            StringBuilder texto =
                new StringBuilder();

            texto.AppendLine(
                "✅ VENTA COMPLETADA");

            texto.AppendLine(
                "SCAYA MOTOS");

            texto.AppendLine();

            texto.AppendLine(
                $"Venta: {_venta.NumeroVentaMostrar}");

            texto.AppendLine(
                $"Fecha: {_venta.FechaVentaTexto}");

            texto.AppendLine(
                $"Trabajador: {_venta.NombreTrabajadorMostrar}");

            texto.AppendLine(
                $"Cliente: {_venta.NombreClienteMostrar}");

            texto.AppendLine(
                $"Documento: {_venta.DocumentoClienteTexto}");

            texto.AppendLine(
                $"Método: {_venta.MetodoPagoTexto}");

            texto.AppendLine();

            texto.AppendLine(
                "PRODUCTOS:");

            if (_venta.Detalle != null &&
                _venta.Detalle.Count > 0)
            {
                foreach (
                    ItemCarritoVenta producto
                    in _venta.Detalle)
                {
                    texto.AppendLine(
                        $"• {producto.NombreProducto}");

                    texto.AppendLine(
                        $"  {producto.DetalleProducto}");

                    texto.AppendLine(
                        $"  {producto.Cantidad} x " +
                        $"{producto.PrecioUnitarioTexto} = " +
                        $"{producto.TotalLineaTexto}");

                    if (producto.Descuento > 0)
                    {
                        texto.AppendLine(
                            $"  Descuento: " +
                            $"{producto.DescuentoTexto}");
                    }
                }
            }

            texto.AppendLine();

            texto.AppendLine(
                $"Subtotal: {_venta.SubtotalTexto}");

            if (_venta.TieneDescuento)
            {
                texto.AppendLine(
                    $"Descuentos: " +
                    $"{_venta.DescuentoTotalTexto}");
            }

            if (_venta.TieneRecargoTarjeta)
            {
                texto.AppendLine(
                    $"Recargo tarjeta 5%: " +
                    $"{_venta.IGVTexto}");
            }

            texto.AppendLine(
                $"TOTAL: {_venta.TotalTexto}");

            texto.AppendLine();

            texto.AppendLine(
                "Gracias por su compra.");

            return texto.ToString();
        }

        // =====================================================
        // IMPRIMIR BOLETA
        // =====================================================

        private void BtnImprimir_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                PrintDialog dialogoImpresion =
                    new PrintDialog();

                bool? resultado =
                    dialogoImpresion.ShowDialog();

                if (resultado != true)
                {
                    return;
                }

                BoletaImprimible.UpdateLayout();

                double anchoBoleta =
                    BoletaImprimible.ActualWidth;

                double altoBoleta =
                    BoletaImprimible.ActualHeight;

                if (anchoBoleta <= 0 ||
                    altoBoleta <= 0)
                {
                    throw new InvalidOperationException(
                        "No se pudo calcular el tamaño de la boleta.");
                }

                const double margen = 25;

                double anchoDisponible =
                    Math.Max(
                        1,
                        dialogoImpresion
                            .PrintableAreaWidth -
                        margen * 2);

                double altoDisponible =
                    Math.Max(
                        1,
                        dialogoImpresion
                            .PrintableAreaHeight -
                        margen * 2);

                double escalaAncho =
                    anchoDisponible /
                    anchoBoleta;

                double escalaAlto =
                    altoDisponible /
                    altoBoleta;

                double escala =
                    Math.Min(
                        escalaAncho,
                        escalaAlto);

                /*
                 * No ampliamos demasiado una boleta pequeña.
                 * Solo la reducimos cuando supera el área imprimible.
                 */
                escala =
                    Math.Min(
                        escala,
                        1.15);

                double anchoImpresion =
                    anchoBoleta * escala;

                double altoImpresion =
                    altoBoleta * escala;

                double posicionX =
                    (
                        dialogoImpresion
                            .PrintableAreaWidth -
                        anchoImpresion
                    ) / 2;

                double posicionY =
                    margen;

                DrawingVisual visualImpresion =
                    new DrawingVisual();

                using (
                    DrawingContext contexto =
                        visualImpresion.RenderOpen())
                {
                    /*
                     * Fondo blanco de toda la página.
                     */
                    contexto.DrawRectangle(
                        Brushes.White,
                        null,
                        new Rect(
                            0,
                            0,
                            dialogoImpresion
                                .PrintableAreaWidth,
                            dialogoImpresion
                                .PrintableAreaHeight));

                    VisualBrush brochaBoleta =
                        new VisualBrush(
                            BoletaImprimible)
                        {
                            Stretch =
                                Stretch.Fill,
                            AlignmentX =
                                AlignmentX.Left,
                            AlignmentY =
                                AlignmentY.Top
                        };

                    contexto.DrawRectangle(
                        brochaBoleta,
                        null,
                        new Rect(
                            posicionX,
                            posicionY,
                            anchoImpresion,
                            altoImpresion));
                }

                dialogoImpresion.PrintVisual(
                    visualImpresion,
                    $"Boleta {_venta.NumeroVentaMostrar}");

                MessageBox.Show(
                    this,
                    "La boleta fue enviada a la impresora.",
                    "Impresión completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "No se pudo imprimir la boleta.\n\n" +
                    ex.Message,
                    "Error de impresión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =====================================================
        // CIERRE SEGURO
        // =====================================================

        private void CerrarVentana(
            bool resultado)
        {
            try
            {
                DialogResult = resultado;
            }
            catch
            {
                Close();
            }
        }

        // =====================================================
        // CERRAR CON ESC
        // =====================================================

        protected override void OnPreviewKeyDown(
            KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                NuevaVentaSolicitada = false;

                CerrarVentana(false);

                e.Handled = true;

                return;
            }

            base.OnPreviewKeyDown(e);
        }
    }
}