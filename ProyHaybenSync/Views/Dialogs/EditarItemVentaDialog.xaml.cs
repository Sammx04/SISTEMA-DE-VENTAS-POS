using ProySistemaVentas.Models.VentasPOS;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class EditarItemVentaDialog : Window
    {
        private readonly ItemCarritoVenta _itemOriginal;

        private bool _inicializando;

        private string _tipoPrecioSeleccionado;

        public bool CambiosAplicados { get; private set; }

        public EditarItemVentaDialog(
            ItemCarritoVenta item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(
                    nameof(item));
            }

            _itemOriginal = item;

            _tipoPrecioSeleccionado =
                string.Equals(
                    item.TipoPrecio,
                    "MAYOR",
                    StringComparison.OrdinalIgnoreCase)
                    ? "MAYOR"
                    : "UNIDAD";

            _inicializando = true;

            InitializeComponent();

            CargarDatosProducto();

            _inicializando = false;

            ActualizarResumen();
        }

        // =====================================================
        // CARGAR INFORMACIÓN
        // =====================================================

        private void CargarDatosProducto()
        {
            TxtNombreProducto.Text =
                string.IsNullOrWhiteSpace(
                    _itemOriginal.NombreProducto)
                    ? "Producto"
                    : _itemOriginal.NombreProducto;

            TxtDetalleProducto.Text =
                _itemOriginal.DetalleProducto;

            TxtCodigoBarra.Text =
                string.IsNullOrWhiteSpace(
                    _itemOriginal.CodigoBarra)
                    ? "Código: Sin código"
                    : $"Código: {_itemOriginal.CodigoBarra}";

            TxtStockDisponible.Text =
                $"Stock: {_itemOriginal.Stock}";

            TxtCantidadMaxima.Text =
                $"Máximo: {_itemOriginal.Stock}";

            TxtPrecioUnidadOriginal.Text =
                _itemOriginal.PrecioUnidadTexto;

            TxtPrecioMayorOriginal.Text =
                _itemOriginal.PrecioMayorTexto;

            BtnPrecioMayor.IsEnabled =
                _itemOriginal.TienePrecioMayor;

            TxtPrecio.Text =
                _itemOriginal
                    .PrecioUnitario
                    .ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);

            TxtCantidad.Text =
                _itemOriginal
                    .Cantidad
                    .ToString(
                        CultureInfo.InvariantCulture);

            TxtDescuento.Text =
                _itemOriginal
                    .Descuento
                    .ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);

            ActualizarEstiloTipoPrecio();
        }

        // =====================================================
        // MOVER Y CERRAR VENTANA
        // =====================================================

        private void Encabezado_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
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

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            CambiosAplicados = false;

            DialogResult = false;
        }

        // =====================================================
        // SELECCIONAR TIPO DE PRECIO
        // =====================================================

        private void BtnPrecioUnidad_Click(
            object sender,
            RoutedEventArgs e)
        {
            _tipoPrecioSeleccionado =
                "UNIDAD";

            TxtPrecio.Text =
                _itemOriginal
                    .PrecioUnidad
                    .ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);

            ActualizarEstiloTipoPrecio();

            TxtPrecio.Focus();
            TxtPrecio.SelectAll();
        }

        private void BtnPrecioMayor_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_itemOriginal.TienePrecioMayor)
            {
                return;
            }

            _tipoPrecioSeleccionado =
                "MAYOR";

            TxtPrecio.Text =
                _itemOriginal
                    .PrecioMayor
                    .ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);

            ActualizarEstiloTipoPrecio();

            TxtPrecio.Focus();
            TxtPrecio.SelectAll();
        }

        private void ActualizarEstiloTipoPrecio()
        {
            Brush fondoNormal =
                CrearBrocha("#101B31");

            Brush textoNormal =
                CrearBrocha("#CBD5E1");

            Brush bordeNormal =
                CrearBrocha("#24344F");

            Brush fondoSeleccionado =
                CrearBrocha("#123047");

            Brush textoSeleccionado =
                CrearBrocha("#67E8F9");

            Brush bordeSeleccionado =
                CrearBrocha("#22D3EE");

            bool esUnidad =
                string.Equals(
                    _tipoPrecioSeleccionado,
                    "UNIDAD",
                    StringComparison.OrdinalIgnoreCase);

            BtnPrecioUnidad.Background =
                esUnidad
                    ? fondoSeleccionado
                    : fondoNormal;

            BtnPrecioUnidad.Foreground =
                esUnidad
                    ? textoSeleccionado
                    : textoNormal;

            BtnPrecioUnidad.BorderBrush =
                esUnidad
                    ? bordeSeleccionado
                    : bordeNormal;

            bool esMayor =
                string.Equals(
                    _tipoPrecioSeleccionado,
                    "MAYOR",
                    StringComparison.OrdinalIgnoreCase);

            BtnPrecioMayor.Background =
                esMayor
                    ? fondoSeleccionado
                    : fondoNormal;

            BtnPrecioMayor.Foreground =
                esMayor
                    ? textoSeleccionado
                    : textoNormal;

            BtnPrecioMayor.BorderBrush =
                esMayor
                    ? bordeSeleccionado
                    : bordeNormal;
        }

        // =====================================================
        // VALIDACIÓN DE ENTRADA
        // =====================================================

        private void TxtDecimal_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            string textoActual =
                textBox.Text ?? string.Empty;

            string textoNuevo =
                textoActual.Remove(
                    textBox.SelectionStart,
                    textBox.SelectionLength);

            textoNuevo =
                textoNuevo.Insert(
                    textBox.SelectionStart,
                    e.Text);

            /*
             * Permite:
             *
             * 10
             * 10.50
             * 10,50
             */
            bool esValido =
                Regex.IsMatch(
                    textoNuevo,
                    @"^\d*([.,]\d{0,2})?$");

            e.Handled = !esValido;
        }

        private void TxtEntero_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            e.Handled =
                !Regex.IsMatch(
                    e.Text,
                    @"^\d+$");
        }

        private void CamposCalculo_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_inicializando)
            {
                return;
            }

            ActualizarResumen();
        }

        // =====================================================
        // ACTUALIZAR TOTAL
        // =====================================================

        private void ActualizarResumen()
        {
            bool precioValido =
                IntentarObtenerDecimal(
                    TxtPrecio.Text,
                    out decimal precio);

            bool cantidadValida =
                int.TryParse(
                    TxtCantidad.Text,
                    out int cantidad);

            bool descuentoValido =
                IntentarObtenerDecimal(
                    TxtDescuento.Text,
                    out decimal descuento);

            if (!precioValido)
            {
                precio = 0;
            }

            if (!cantidadValida)
            {
                cantidad = 0;
            }

            if (!descuentoValido)
            {
                descuento = 0;
            }

            decimal importe =
                precio * cantidad;

            decimal total =
                Math.Max(
                    0,
                    importe - descuento);

            TxtResumenCalculo.Text =
                $"S/ {precio:N2} × {cantidad} - " +
                $"S/ {descuento:N2}";

            TxtTotalLinea.Text =
                $"S/ {total:N2}";

            BtnGuardarCambios.IsEnabled =
                precio > 0 &&
                cantidad > 0 &&
                cantidad <= _itemOriginal.Stock &&
                descuento >= 0 &&
                descuento <= importe;
        }

        // =====================================================
        // GUARDAR CAMBIOS
        // =====================================================

        private void BtnGuardarCambios_Click(
            object sender,
            RoutedEventArgs e)
        {
            OcultarErrores();

            bool formularioValido = true;

            if (!IntentarObtenerDecimal(
                    TxtPrecio.Text,
                    out decimal precio) ||
                precio <= 0)
            {
                TxtErrorPrecio.Text =
                    "Ingrese un precio mayor que cero.";

                TxtErrorPrecio.Visibility =
                    Visibility.Visible;

                formularioValido = false;
            }

            if (!int.TryParse(
                    TxtCantidad.Text,
                    out int cantidad) ||
                cantidad <= 0)
            {
                TxtErrorCantidad.Text =
                    "Ingrese una cantidad mayor que cero.";

                TxtErrorCantidad.Visibility =
                    Visibility.Visible;

                formularioValido = false;
            }
            else if (cantidad > _itemOriginal.Stock)
            {
                TxtErrorCantidad.Text =
                    $"Solo hay {_itemOriginal.Stock} unidades disponibles.";

                TxtErrorCantidad.Visibility =
                    Visibility.Visible;

                formularioValido = false;
            }

            if (!IntentarObtenerDecimal(
                    TxtDescuento.Text,
                    out decimal descuento) ||
                descuento < 0)
            {
                TxtErrorDescuento.Text =
                    "Ingrese un descuento válido.";

                TxtErrorDescuento.Visibility =
                    Visibility.Visible;

                formularioValido = false;
            }
            else if (precio > 0 &&
                     cantidad > 0 &&
                     descuento > precio * cantidad)
            {
                TxtErrorDescuento.Text =
                    "El descuento no puede superar el importe del producto.";

                TxtErrorDescuento.Visibility =
                    Visibility.Visible;

                formularioValido = false;
            }

            if (!formularioValido)
            {
                return;
            }

            precio =
                decimal.Round(
                    precio,
                    2,
                    MidpointRounding.AwayFromZero);

            descuento =
                decimal.Round(
                    descuento,
                    2,
                    MidpointRounding.AwayFromZero);

            /*
             * Los cambios se aplican únicamente al objeto
             * que está dentro del carrito.
             *
             * Aquí no se actualiza la tabla Variantes.
             */
            _itemOriginal.TipoPrecio =
                _tipoPrecioSeleccionado;

            _itemOriginal.PrecioUnitario =
                precio;

            _itemOriginal.Cantidad =
                cantidad;

            _itemOriginal.Descuento =
                descuento;

            _itemOriginal.ActualizarCalculos();

            CambiosAplicados = true;

            DialogResult = true;
        }

        private void OcultarErrores()
        {
            TxtErrorPrecio.Visibility =
                Visibility.Collapsed;

            TxtErrorCantidad.Visibility =
                Visibility.Collapsed;

            TxtErrorDescuento.Visibility =
                Visibility.Collapsed;
        }

        // =====================================================
        // MÉTODOS AUXILIARES
        // =====================================================

        private static bool IntentarObtenerDecimal(
            string texto,
            out decimal valor)
        {
            valor = 0;

            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            string textoNormalizado =
                texto
                    .Trim()
                    .Replace("S/", string.Empty)
                    .Replace(" ", string.Empty)
                    .Replace(',', '.');

            return decimal.TryParse(
                textoNormalizado,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out valor);
        }

        private static Brush CrearBrocha(
            string colorHexadecimal)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    colorHexadecimal));
        }
    }
}