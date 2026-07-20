using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class EtiquetaCodigoBarrasDialog : Window
    {
        private readonly string _nombreProducto;
        private readonly string _nombreVariante;
        private readonly string _codigoBarras;

        /*
         * Patrones oficiales de CODE128.
         *
         * Cada número representa el ancho de una barra
         * o espacio. Comienza siempre con una barra.
         */
        private static readonly string[] PatronesCode128 =
        {
            "212222", "222122", "222221", "121223", "121322",
            "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231",
            "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222",
            "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123",
            "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131",
            "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123",
            "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422",
            "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211",
            "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112",
            "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141",
            "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214",
            "211232", "2331112"
        };

        public EtiquetaCodigoBarrasDialog(
            string nombreProducto,
            string nombreVariante,
            string codigoBarras)
        {
            InitializeComponent();

            _nombreProducto =
                string.IsNullOrWhiteSpace(nombreProducto)
                    ? "Producto"
                    : nombreProducto.Trim();

            _nombreVariante =
                string.IsNullOrWhiteSpace(nombreVariante)
                    ? "Única"
                    : nombreVariante.Trim();

            _codigoBarras =
                codigoBarras?.Trim()
                ?? string.Empty;

            TxtProducto.Text =
                $"Producto: {_nombreProducto}";

            TxtVariante.Text =
                $"Variante: {_nombreVariante}";

            TxtCodigo.Text =
                _codigoBarras;

            Loaded +=
                EtiquetaCodigoBarrasDialog_Loaded;
        }

        private void EtiquetaCodigoBarrasDialog_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            DibujarCodigoBarras();
        }

        // =====================================================
        // GENERAR CODE128
        // =====================================================

        private List<int> ObtenerValoresCode128(
            string texto)
        {
            List<int> valores =
                new List<int>();

            /*
             * 104 corresponde a START B.
             * CODE128 B permite números, letras y símbolos.
             */
            const int startCodeB = 104;

            valores.Add(
                startCodeB);

            int checksum =
                startCodeB;

            for (int indice = 0;
                 indice < texto.Length;
                 indice++)
            {
                char caracter =
                    texto[indice];

                if (caracter < 32 ||
                    caracter > 126)
                {
                    throw new InvalidOperationException(
                        $"El carácter \"{caracter}\" no es válido " +
                        "para un código CODE128.");
                }

                int valor =
                    caracter - 32;

                valores.Add(
                    valor);

                checksum +=
                    valor * (indice + 1);
            }

            checksum %= 103;

            valores.Add(
                checksum);

            // 106 corresponde al carácter STOP.
            valores.Add(
                106);

            return valores;
        }

        private void DibujarCodigoBarras()
        {
            CanvasCodigo.Children.Clear();

            if (string.IsNullOrWhiteSpace(
                _codigoBarras))
            {
                return;
            }

            List<int> valores =
                ObtenerValoresCode128(
                    _codigoBarras);

            const double altoBarra =
                95;

            const double margenLateral =
                18;

            double totalModulos =
                20;

            foreach (int valor in valores)
            {
                string patron =
                    PatronesCode128[valor];

                foreach (char modulo in patron)
                {
                    totalModulos +=
                        modulo - '0';
                }
            }

            double anchoDisponible =
                CanvasCodigo.Width -
                (margenLateral * 2);

            double anchoModulo =
                anchoDisponible /
                totalModulos;

            double posicionX =
                margenLateral +
                (10 * anchoModulo);

            foreach (int valor in valores)
            {
                string patron =
                    PatronesCode128[valor];

                bool esBarra =
                    true;

                foreach (char caracterAncho in patron)
                {
                    int cantidadModulos =
                        caracterAncho - '0';

                    double ancho =
                        cantidadModulos *
                        anchoModulo;

                    if (esBarra)
                    {
                        Rectangle barra =
                            new Rectangle
                            {
                                Width =
                                    Math.Max(
                                        1,
                                        ancho),

                                Height =
                                    altoBarra,

                                Fill =
                                    Brushes.Black,

                                SnapsToDevicePixels =
                                    true
                            };

                        Canvas.SetLeft(
                            barra,
                            posicionX);

                        Canvas.SetTop(
                            barra,
                            5);

                        CanvasCodigo.Children.Add(
                            barra);
                    }

                    posicionX +=
                        ancho;

                    esBarra =
                        !esBarra;
                }
            }
        }

        // =====================================================
        // IMPRIMIR
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

                /*
                 * Se imprime únicamente BordeEtiqueta.
                 * Los botones y el fondo de la ventana
                 * no serán enviados a la impresora.
                 */
                dialogoImpresion.PrintVisual(
                    BordeEtiqueta,
                    $"Etiqueta {_nombreProducto} - {_nombreVariante}");
            }
            catch (Exception ex)
            {
                MensajeExitoDialog.Mostrar(
                    this,
                    "No se pudo imprimir",
                    $"Ocurrió un error al imprimir la etiqueta.\n\n" +
                    ex.Message);
            }
        }

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
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