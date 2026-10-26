using System;
using System.Windows;
using ProySistemaVentas.Models.PrestamosCreditos;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class DetallePrestamoCreditoDialog :
        Window
    {
        public PrestamoCreditoDetalle Detalle { get; }


        public DetallePrestamoCreditoDialog(
            PrestamoCreditoDetalle detalle)
        {
            Detalle =
                detalle ??
                throw new ArgumentNullException(
                    nameof(detalle));

            InitializeComponent();

            DataContext = Detalle;

            ConfigurarDetalle();
        }


        private void ConfigurarDetalle()
        {
            if (Detalle.EsPrestamo)
            {
                TxtTipoOperacion.Text =
                    "PRÉSTAMO";

                TxtTipoOperacion.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            74,
                            222,
                            128));

                /*
                 * Los préstamos no utilizan fecha
                 * esperada de pago.
                 */
                BrdFechaEsperada.Visibility =
                    Visibility.Collapsed;
            }
            else
            {
                TxtTipoOperacion.Text =
                    "COBRANZA";

                TxtTipoOperacion.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            251,
                            113,
                            133));

                BrdFechaEsperada.Visibility =
                    Visibility.Visible;
            }

            if (Detalle.EstaPendiente)
            {
                BrdEstado.Background =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            120,
                            53,
                            15));

                TxtEstado.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            252,
                            211,
                            77));
            }
            else
            {
                BrdEstado.Background =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            20,
                            83,
                            45));

                TxtEstado.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            134,
                            239,
                            172));
            }

            /*
             * Los datos propios del pago solo se muestran
             * cuando existe un pago registrado.
             */
            Visibility visibilidadPago =
                Detalle.IdPagoCredito.HasValue
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            BrdMetodoPago.Visibility =
                visibilidadPago;

            BrdTipoPrecio.Visibility =
                visibilidadPago;

            BrdImportePago.Visibility =
                visibilidadPago;

            BrdObservacionPago.Visibility =
                visibilidadPago;
        }


        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
