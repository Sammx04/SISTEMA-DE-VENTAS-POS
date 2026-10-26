using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProySistemaVentas.Models.PrestamosCreditos;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class NotaCobranzaDialog : Window
    {
        private readonly BoletaPagoCredito _boleta;

        public NotaCobranzaDialog(BoletaPagoCredito boleta)
        {
            _boleta = boleta ?? throw new ArgumentNullException(nameof(boleta));

            InitializeComponent();
            DataContext = _boleta;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog dialogoImpresion = new PrintDialog();
                if (dialogoImpresion.ShowDialog() != true)
                {
                    return;
                }

                NotaImprimible.UpdateLayout();
                double anchoNota = NotaImprimible.ActualWidth;
                double altoNota = NotaImprimible.ActualHeight;

                if (anchoNota <= 0 || altoNota <= 0)
                {
                    throw new InvalidOperationException("No se pudo calcular el tamaño de la nota.");
                }

                const double margen = 25;
                double anchoDisponible = Math.Max(1, dialogoImpresion.PrintableAreaWidth - margen * 2);
                double altoDisponible = Math.Max(1, dialogoImpresion.PrintableAreaHeight - margen * 2);
                double escala = Math.Min(anchoDisponible / anchoNota, altoDisponible / altoNota);
                escala = Math.Min(escala, 1.15);

                double anchoImpresion = anchoNota * escala;
                double altoImpresion = altoNota * escala;
                double posicionX = (dialogoImpresion.PrintableAreaWidth - anchoImpresion) / 2;

                DrawingVisual visualImpresion = new DrawingVisual();
                using (DrawingContext contexto = visualImpresion.RenderOpen())
                {
                    contexto.DrawRectangle(Brushes.White, null, new Rect(0, 0, dialogoImpresion.PrintableAreaWidth, dialogoImpresion.PrintableAreaHeight));

                    VisualBrush brochaNota = new VisualBrush(NotaImprimible)
                    {
                        Stretch = Stretch.Fill,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top
                    };

                    contexto.DrawRectangle(brochaNota, null, new Rect(posicionX, margen, anchoImpresion, altoImpresion));
                }

                dialogoImpresion.PrintVisual(visualImpresion, $"Nota de cobranza {_boleta.NumeroPago}");
                MessageBox.Show(this, "La nota de cobranza fue enviada a la impresora.", "Impresión completada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo imprimir la nota de cobranza.\n\n" + ex.Message, "Error de impresión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
