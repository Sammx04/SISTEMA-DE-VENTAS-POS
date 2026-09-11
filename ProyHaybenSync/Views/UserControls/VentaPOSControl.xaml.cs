using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Models.VentasPOS;
using ProySistemaVentas.Services.VentasPOS;
using ProySistemaVentas.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProySistemaVentas.Views.UserControls
{
    public partial class VentaPOSControl : UserControl
    {
        private readonly VentaPOSService _ventaService;

        private readonly List<ItemCarritoVenta> _carrito;

        private CancellationTokenSource _canceladorBusqueda;

        private string _metodoPagoSeleccionado;

        private bool _inicializando;

        private bool _controlCargado;

        private bool _ventaEnProceso;

        public VentaPOSControl()
        {
            /*
             * Se inicializan antes de InitializeComponent
             * porque algunos eventos del XAML pueden ejecutarse
             * mientras se construye la vista.
             */
            _ventaService =
                new VentaPOSService();

            _carrito =
                new List<ItemCarritoVenta>();

            _metodoPagoSeleccionado =
                "EFECTIVO";

            _inicializando = true;

            InitializeComponent();

            ItemsCarrito.ItemsSource =
                _carrito;

            _inicializando = false;

            ActualizarEstiloMetodosPago();

            ActualizarTotales();
        }

        // =====================================================
        // CARGA INICIAL
        // =====================================================

        private async void VentaPOSControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_controlCargado)
            {
                TxtBuscarProducto.Focus();
                return;
            }

            _controlCargado = true;

            try
            {
                MostrarCargando(
                    true,
                    "Cargando punto de venta...");

                await CargarTrabajadoresAsync();

                await CargarProductosFrecuentesAsync();

                ActualizarTotales();

                TxtBuscarProducto.Focus();
            }
            catch (Exception ex)
            {
                MostrarAlerta(
                    "No se pudo cargar ventas",
                    ex.Message);
            }
            finally
            {
                MostrarCargando(false);
            }
        }

        private async Task CargarTrabajadoresAsync()
        {
            List<UsuarioMovimiento> trabajadores =
                await Task.Run(
                    () =>
                        _ventaService
                            .ListarUsuariosActivos());

            CboTrabajador.ItemsSource =
                trabajadores;

            CboTrabajador.SelectedIndex =
                -1;
        }

        private async Task CargarProductosFrecuentesAsync()
        {
            List<ProductoVentaPOS> frecuentes =
                await Task.Run(
                    () =>
                        _ventaService
                            .ObtenerProductosFrecuentes());

            ItemsProductosFrecuentes.ItemsSource =
                frecuentes;

            bool tieneFrecuentes =
                frecuentes != null &&
                frecuentes.Count > 0;

            ItemsProductosFrecuentes.Visibility =
                tieneFrecuentes
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            PanelSinFrecuentes.Visibility =
                tieneFrecuentes
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        // =====================================================
        // BÚSQUEDA DE PRODUCTOS
        // =====================================================

        private async void TxtBuscarProducto_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            TxtPlaceholderBusqueda.Visibility =
                string.IsNullOrEmpty(
                    TxtBuscarProducto.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (_inicializando ||
                !_controlCargado)
            {
                return;
            }

            CancelarBusquedaPendiente();

            string texto =
                TxtBuscarProducto.Text?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(texto))
            {
                OcultarResultadosBusqueda();
                return;
            }

            CancellationTokenSource cancelador =
                new CancellationTokenSource();

            _canceladorBusqueda =
                cancelador;

            try
            {
                /*
                 * Esperamos un momento para no consultar la base
                 * con cada tecla escrita.
                 */
                await Task.Delay(
                    300,
                    cancelador.Token);

                List<ProductoVentaPOS> productos =
                    await Task.Run(
                        () =>
                            _ventaService
                                .BuscarProductos(texto));

                if (cancelador.IsCancellationRequested)
                {
                    return;
                }

                MostrarResultadosBusqueda(
                    productos);
            }
            catch (TaskCanceledException)
            {
                // La búsqueda fue reemplazada por una más reciente.
            }
            catch (Exception ex)
            {
                OcultarResultadosBusqueda();

                MostrarAlerta(
                    "Error de búsqueda",
                    ex.Message);
            }
        }

        private async void TxtBuscarProducto_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;

            CancelarBusquedaPendiente();

            string codigo =
                TxtBuscarProducto.Text?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return;
            }

            try
            {
                ProductoVentaPOS producto =
                    await Task.Run(
                        () =>
                            _ventaService
                                .ObtenerProductoPorCodigo(
                                    codigo));

                if (producto == null)
                {
                    MostrarAlerta(
                        "Producto no encontrado",
                        "No existe un producto disponible en tienda con ese código de barras.");

                    TxtBuscarProducto.SelectAll();
                    return;
                }

                AgregarProductoAlCarrito(
                    producto);

                LimpiarBusqueda();
            }
            catch (Exception ex)
            {
                MostrarAlerta(
                    "Error al buscar producto",
                    ex.Message);
            }
        }

        private void MostrarResultadosBusqueda(
            List<ProductoVentaPOS> productos)
        {
            productos ??=
                new List<ProductoVentaPOS>();

            ItemsResultadosBusqueda.ItemsSource =
                productos;

            PanelResultadosBusqueda.Visibility =
                Visibility.Visible;

            bool tieneResultados =
                productos.Count > 0;

            ItemsResultadosBusqueda.Visibility =
                tieneResultados
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            PanelSinResultadosBusqueda.Visibility =
                tieneResultados
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void OcultarResultadosBusqueda()
        {
            ItemsResultadosBusqueda.ItemsSource =
                null;

            PanelResultadosBusqueda.Visibility =
                Visibility.Collapsed;

            PanelSinResultadosBusqueda.Visibility =
                Visibility.Collapsed;
        }

        private void LimpiarBusqueda()
        {
            CancelarBusquedaPendiente();

            TxtBuscarProducto.Text =
                string.Empty;

            OcultarResultadosBusqueda();

            TxtBuscarProducto.Focus();
        }

        private void CancelarBusquedaPendiente()
        {
            if (_canceladorBusqueda == null)
            {
                return;
            }

            _canceladorBusqueda.Cancel();

            _canceladorBusqueda.Dispose();

            _canceladorBusqueda =
                null;
        }

        // =====================================================
        // AGREGAR PRODUCTOS
        // =====================================================

        private void BtnAgregarProductoBusqueda_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ProductoVentaPOS producto)
            {
                return;
            }

            AgregarProductoAlCarrito(
                producto);

            LimpiarBusqueda();
        }

        private void BtnAgregarProductoFrecuente_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ProductoVentaPOS producto)
            {
                return;
            }

            AgregarProductoAlCarrito(
                producto);

            TxtBuscarProducto.Focus();
        }

        private void AgregarProductoAlCarrito(
            ProductoVentaPOS producto)
        {
            if (producto == null)
            {
                return;
            }

            if (producto.Stock <= 0)
            {
                MostrarAlerta(
                    "Producto sin stock",
                    "El producto seleccionado no tiene stock disponible en tienda.");

                return;
            }

            ItemCarritoVenta existente =
                _carrito.FirstOrDefault(
                    item =>
                        item.IdVariante ==
                        producto.IdVariante);

            if (existente != null)
            {
                /*
                 * Actualizamos el stock por si el producto
                 * proviene de una consulta reciente.
                 */
                existente.Stock =
                    producto.Stock;

                if (existente.Cantidad >=
                    existente.Stock)
                {
                    MostrarAlerta(
                        "Stock insuficiente",
                        $"Solo hay {existente.Stock} unidades disponibles en tienda.");

                    return;
                }

                existente.Cantidad++;

                existente.ActualizarCalculos();

                ActualizarTotales();

                return;
            }

            ItemCarritoVenta nuevoItem =
                new ItemCarritoVenta
                {
                    IdProducto =
                        producto.IdProducto,

                    IdVariante =
                        producto.IdVariante,

                    NombreProducto =
                        producto.Nombre,

                    Variante =
                        producto.Variante,

                    Categoria =
                        producto.Categoria,

                    CodigoBarra =
                        producto.CodigoBarra,

                    PrecioUnidad =
                        producto.PrecioUnidad,

                    PrecioMayor =
                        producto.PrecioMayor,

                    PrecioUnitario =
                        producto.PrecioUnidad,

                    TipoPrecio =
                        "UNIDAD",

                    Cantidad =
                        1,

                    Descuento =
                        0,

                    Stock =
                        producto.Stock
                };

            nuevoItem.PropertyChanged +=
                ItemCarrito_PropertyChanged;

            _carrito.Add(
                nuevoItem);

            RefrescarCarrito();
        }

        private void ItemCarrito_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            ActualizarTotales();
        }

        // =====================================================
        // EDITAR PRODUCTO DEL CARRITO
        // =====================================================

        private void BtnEditarItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ItemCarritoVenta item)
            {
                return;
            }

            EditarItemVentaDialog ventana =
                new EditarItemVentaDialog(
                    item)
                {
                    Owner =
                        Window.GetWindow(this)
                };

            bool? resultado =
                ventana.ShowDialog();

            if (resultado == true &&
                ventana.CambiosAplicados)
            {
                item.ActualizarCalculos();

                RefrescarCarrito();
            }
        }

        // =====================================================
        // PRECIOS DEL CARRITO
        // =====================================================

        private void BtnUsarPrecioUnidad_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ItemCarritoVenta item)
            {
                return;
            }

            item.UsarPrecioUnidad();

            RefrescarCarrito();
        }

        private void BtnUsarPrecioMayor_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ItemCarritoVenta item)
            {
                return;
            }

            if (!item.TienePrecioMayor)
            {
                MostrarAlerta(
                    "Precio no disponible",
                    "Este producto no tiene configurado un precio por mayor.");

                return;
            }

            item.UsarPrecioMayor();

            RefrescarCarrito();
        }

        // =====================================================
        // CANTIDAD
        // =====================================================

        private void BtnSumarCantidad_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ItemCarritoVenta item)
            {
                return;
            }

            if (item.Cantidad >= item.Stock)
            {
                MostrarAlerta(
                    "Stock insuficiente",
                    $"Solo hay {item.Stock} unidades disponibles en tienda.");

                return;
            }

            item.Cantidad++;

            RefrescarCarrito();
        }

        private void BtnRestarCantidad_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ItemCarritoVenta item)
            {
                return;
            }

            if (item.Cantidad <= 1)
            {
                EliminarItemCarrito(
                    item);

                return;
            }

            item.Cantidad--;

            RefrescarCarrito();
        }

        // =====================================================
        // ELIMINAR
        // =====================================================

        private void BtnEliminarItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ItemCarritoVenta item)
            {
                return;
            }

            EliminarItemCarrito(
                item);
        }

        private void EliminarItemCarrito(
            ItemCarritoVenta item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -=
                ItemCarrito_PropertyChanged;

            _carrito.Remove(
                item);

            RefrescarCarrito();
        }

        private void RefrescarCarrito()
        {
            ItemsCarrito.Items.Refresh();

            ActualizarTotales();
        }

        // =====================================================
        // TRABAJADOR
        // =====================================================

        private void CboTrabajador_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_inicializando)
            {
                return;
            }

            ActualizarEstadoBotonConfirmar();
        }

        // =====================================================
        // DOCUMENTO DEL CLIENTE
        // =====================================================

        private void CboTipoDocumento_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_inicializando ||
                CboTipoDocumento == null ||
                TxtNumeroDocumento == null)
            {
                return;
            }

            string tipoDocumento =
                ObtenerTipoDocumentoSeleccionado();

            TxtNumeroDocumento.Text =
                string.Empty;

            switch (tipoDocumento)
            {
                case "DNI":

                    TxtNumeroDocumento.IsEnabled =
                        true;

                    TxtNumeroDocumento.MaxLength =
                        8;

                    TxtAyudaDocumento.Text =
                        "Ingrese los 8 dígitos del DNI.";

                    TxtNumeroDocumento.Focus();

                    break;

                case "RUC":

                    TxtNumeroDocumento.IsEnabled =
                        true;

                    TxtNumeroDocumento.MaxLength =
                        11;

                    TxtAyudaDocumento.Text =
                        "Ingrese los 11 dígitos del RUC.";

                    TxtNumeroDocumento.Focus();

                    break;

                default:

                    TxtNumeroDocumento.IsEnabled =
                        false;

                    TxtNumeroDocumento.MaxLength =
                        20;

                    TxtAyudaDocumento.Text =
                        "No se registrará documento del cliente.";

                    break;
            }
        }

        private void TxtNumeroDocumento_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            e.Handled =
                !Regex.IsMatch(
                    e.Text,
                    @"^\d+$");
        }

        private string ObtenerTipoDocumentoSeleccionado()
        {
            if (CboTipoDocumento.SelectedItem
                is ComboBoxItem item)
            {
                return Convert
                    .ToString(item.Tag)
                    ?.Trim()
                    .ToUpperInvariant()
                    ?? "OTROS";
            }

            return "OTROS";
        }

        // =====================================================
        // MÉTODOS DE PAGO
        // =====================================================

        private void BtnPagoEfectivo_Click(
            object sender,
            RoutedEventArgs e)
        {
            SeleccionarMetodoPago(
                "EFECTIVO");
        }

        private void BtnPagoYapePlin_Click(
            object sender,
            RoutedEventArgs e)
        {
            SeleccionarMetodoPago(
                "YAPE_PLIN");
        }

        private void BtnPagoTarjeta_Click(
            object sender,
            RoutedEventArgs e)
        {
            SeleccionarMetodoPago(
                "TARJETA");
        }

        private void SeleccionarMetodoPago(
            string metodoPago)
        {
            _metodoPagoSeleccionado =
                metodoPago;

            ActualizarEstiloMetodosPago();

            ActualizarTotales();
        }

        private void ActualizarEstiloMetodosPago()
        {
            ConfigurarBotonMetodoPago(
                BtnPagoEfectivo,
                _metodoPagoSeleccionado ==
                "EFECTIVO");

            ConfigurarBotonMetodoPago(
                BtnPagoYapePlin,
                _metodoPagoSeleccionado ==
                "YAPE_PLIN");

            ConfigurarBotonMetodoPago(
                BtnPagoTarjeta,
                _metodoPagoSeleccionado ==
                "TARJETA");
        }

        private static void ConfigurarBotonMetodoPago(
            Button boton,
            bool seleccionado)
        {
            if (boton == null)
            {
                return;
            }

            if (seleccionado)
            {
                boton.Background =
                    CrearBrocha("#123047");

                boton.Foreground =
                    CrearBrocha("#67E8F9");

                boton.BorderBrush =
                    CrearBrocha("#22D3EE");

                return;
            }

            boton.Background =
                CrearBrocha("#101B31");

            boton.Foreground =
                CrearBrocha("#CBD5E1");

            boton.BorderBrush =
                CrearBrocha("#24344F");
        }

        // =====================================================
        // TOTALES
        // =====================================================

        private decimal ObtenerSubtotal()
        {
            return decimal.Round(
                _carrito.Sum(
                    item =>
                        item.TotalLinea),
                2,
                MidpointRounding.AwayFromZero);
        }

        private decimal ObtenerRecargoTarjeta(
            decimal subtotal)
        {
            if (_metodoPagoSeleccionado !=
                "TARJETA")
            {
                return 0;
            }

            return decimal.Round(
                subtotal * 0.05m,
                2,
                MidpointRounding.AwayFromZero);
        }

        private void ActualizarTotales()
        {
            decimal subtotal =
                ObtenerSubtotal();

            decimal recargoTarjeta =
                ObtenerRecargoTarjeta(
                    subtotal);

            decimal total =
                subtotal +
                recargoTarjeta;

            int cantidadTipos =
                _carrito.Count;

            int cantidadUnidades =
                _carrito.Sum(
                    item =>
                        item.Cantidad);

            TxtCantidadCarrito.Text =
                cantidadUnidades.ToString(
                    CultureInfo.InvariantCulture);

            TxtTiposProducto.Text =
                cantidadTipos == 1
                    ? "1 tipo de producto"
                    : $"{cantidadTipos} tipos de producto";

            TxtCantidadProductosCarrito.Text =
                cantidadUnidades == 1
                    ? "1 producto"
                    : $"{cantidadUnidades} productos";

            TxtSubtotal.Text =
                FormatearMoneda(
                    subtotal);

            TxtRecargoTarjeta.Text =
                FormatearMoneda(
                    recargoTarjeta);

            TxtTotalCarrito.Text =
                FormatearMoneda(
                    total);

            TxtTotalActual.Text =
                FormatearMoneda(
                    total);

            TxtTotalPago.Text =
                FormatearMoneda(
                    total);

            FilaRecargoTarjetaCarrito.Visibility =
                _metodoPagoSeleccionado ==
                "TARJETA"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            PanelCarritoVacio.Visibility =
                _carrito.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ItemsCarrito.Visibility =
                _carrito.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ActualizarEstadoBotonConfirmar();
        }

        private void ActualizarEstadoBotonConfirmar()
        {
            if (BtnConfirmarVenta == null)
            {
                return;
            }

            BtnConfirmarVenta.IsEnabled =
                !_ventaEnProceso &&
                _carrito.Count > 0;
        }

        // =====================================================
        // CONFIRMAR VENTA
        // =====================================================

        private async void BtnConfirmarVenta_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (_ventaEnProceso)
            {
                return;
            }

            /*
             * Validar que existan productos en el carrito.
             */
            if (_carrito.Count == 0)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Carrito vacío",
                    "Agregue al menos un producto antes de registrar la venta.",
                    TipoMensaje.Advertencia);

                TxtBuscarProducto.Focus();

                return;
            }

            /*
             * El trabajador es obligatorio.
             */
            if (CboTrabajador.SelectedItem
                is not UsuarioMovimiento trabajador)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Trabajador obligatorio",
                    "Debe seleccionar al trabajador que está realizando la venta.",
                    TipoMensaje.Advertencia);

                CboTrabajador.Focus();

                return;
            }

            string tipoDocumento =
                ObtenerTipoDocumentoSeleccionado();

            string numeroDocumento =
                TxtNumeroDocumento.Text?.Trim()
                ?? string.Empty;

            /*
             * Validar DNI, RUC u otro documento.
             */
            if (!ValidarDocumento(
                    tipoDocumento,
                    numeroDocumento,
                    out string mensajeDocumento))
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Documento inválido",
                    mensajeDocumento,
                    TipoMensaje.Advertencia);

                TxtNumeroDocumento.Focus();
                TxtNumeroDocumento.SelectAll();

                return;
            }

            /*
             * Validar que las cantidades no superen
             * el stock disponible en tienda.
             */
            if (_carrito.Any(
                    item =>
                        item.Cantidad >
                        item.Stock))
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Stock insuficiente",
                    "Uno o más productos superan el stock disponible en tienda.",
                    TipoMensaje.Advertencia);

                return;
            }

            decimal subtotal =
                ObtenerSubtotal();

            decimal recargoTarjeta =
                ObtenerRecargoTarjeta(
                    subtotal);

            decimal total =
                subtotal +
                recargoTarjeta;

            /*
             * Confirmación reutilizando el diálogo
             * personalizado del sistema.
             */
            bool confirmacion =
                MensajeDialog.Confirmar(
                    Window.GetWindow(this),
                    "Confirmar venta",
                    "¿Desea registrar la venta?\n\n" +
                    $"Trabajador: {trabajador.NombreMostrar}\n" +
                    $"Método: {ObtenerMetodoPagoTexto()}\n" +
                    $"Total: {FormatearMoneda(total)}");

            if (!confirmacion)
            {
                return;
            }

            try
            {
                _ventaEnProceso = true;

                MostrarCargando(
                    true,
                    "Registrando venta...");

                ActualizarEstadoBotonConfirmar();

                /*
                 * Creamos una copia de la lista para evitar
                 * modificaciones mientras SQL registra la venta.
                 */
                List<ItemCarritoVenta> detalle =
                    _carrito.ToList();

                ResultadoVentaPOS resultado =
                    await Task.Run(
                        () =>
                            _ventaService
                                .RegistrarVenta(
                                    trabajador.IdUsuario,
                                    tipoDocumento,
                                    tipoDocumento == "OTROS"
                                        ? null
                                        : numeroDocumento,
                                    "Otros clientes",
                                    _metodoPagoSeleccionado,
                                    subtotal,
                                    recargoTarjeta,
                                    total,
                                    detalle));

                resultado.NombreTrabajador =
                    trabajador.NombreMostrar;

                MostrarCargando(false);

                /*
                 * La venta ya fue registrada. Se limpia la
                 * pantalla antes de mostrar el comprobante
                 * para evitar registrarla dos veces.
                 */
                LimpiarVenta();

                VentaCompletadaDialog ventana =
                    new VentaCompletadaDialog(
                        resultado)
                    {
                        Owner =
                            Window.GetWindow(this)
                    };

                ventana.ShowDialog();

                /*
                 * Recargar el TOP 5 para actualizar
                 * el stock y los productos frecuentes.
                 */
                await CargarProductosFrecuentesAsync();

                TxtBuscarProducto.Focus();
            }
            catch (Exception ex)
            {
                MostrarCargando(false);

                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "No se pudo registrar la venta",
                    ObtenerMensajeError(ex),
                    TipoMensaje.Error);
            }
            finally
            {
                _ventaEnProceso = false;

                ActualizarEstadoBotonConfirmar();
            }
        }

        private static bool ValidarDocumento(
            string tipoDocumento,
            string numeroDocumento,
            out string mensaje)
        {
            mensaje =
                string.Empty;

            if (tipoDocumento == "OTROS")
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(
                    numeroDocumento))
            {
                mensaje =
                    "Ingrese el número de documento del cliente.";

                return false;
            }

            if (!numeroDocumento.All(
                    char.IsDigit))
            {
                mensaje =
                    "El documento debe contener solamente números.";

                return false;
            }

            if (tipoDocumento == "DNI" &&
                numeroDocumento.Length != 8)
            {
                mensaje =
                    "El DNI debe contener exactamente 8 dígitos.";

                return false;
            }

            if (tipoDocumento == "RUC" &&
                numeroDocumento.Length != 11)
            {
                mensaje =
                    "El RUC debe contener exactamente 11 dígitos.";

                return false;
            }

            return true;
        }

        // =====================================================
        // CANCELAR VENTA
        // =====================================================

        private void BtnCancelarVenta_Click(
    object sender,
    RoutedEventArgs e)
        {
            /*
             * No hay una venta activa para cancelar.
             */
            if (_carrito.Count == 0)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Carrito vacío",
                    "No existen productos en el carrito para cancelar.",
                    TipoMensaje.Informacion);

                TxtBuscarProducto.Focus();

                return;
            }

            /*
             * Mostrar confirmación utilizando el diálogo
             * personalizado del sistema.
             */
            bool confirmacion =
                MensajeDialog.Confirmar(
                    Window.GetWindow(this),
                    "Cancelar venta",
                    "¿Desea cancelar la venta actual?\n\n" +
                    "Se eliminarán todos los productos del carrito.");

            if (!confirmacion)
            {
                return;
            }

            /*
             * Limpiar toda la información de la venta.
             */
            LimpiarVenta();

            TxtBuscarProducto.Focus();
        }

        private void LimpiarVenta()
        {
            CancelarBusquedaPendiente();

            foreach (
                ItemCarritoVenta item
                in _carrito)
            {
                item.PropertyChanged -=
                    ItemCarrito_PropertyChanged;
            }

            _carrito.Clear();

            ItemsCarrito.Items.Refresh();

            TxtBuscarProducto.Text =
                string.Empty;

            OcultarResultadosBusqueda();

            CboTrabajador.SelectedIndex =
                -1;

            CboTipoDocumento.SelectedIndex =
                0;

            TxtNumeroDocumento.Text =
                string.Empty;

            TxtNumeroDocumento.IsEnabled =
                false;

            TxtAyudaDocumento.Text =
                "No se registrará documento del cliente.";

            _metodoPagoSeleccionado =
                "EFECTIVO";

            ActualizarEstiloMetodosPago();

            ActualizarTotales();

            TxtBuscarProducto.Focus();
        }

        // =====================================================
        // CARGANDO
        // =====================================================

        private void MostrarCargando(
            bool mostrar,
            string mensaje = null)
        {
            if (PanelCargando == null)
            {
                return;
            }

            PanelCargando.Visibility =
                mostrar
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(
                    mensaje))
            {
                TxtMensajeCargando.Text =
                    mensaje;
            }
        }

        // =====================================================
        // MENSAJES
        // =====================================================

        private void MostrarAlerta(
            string titulo,
            string mensaje)
        {
            Window ventanaPadre =
                Window.GetWindow(this);

            MensajeExitoDialog.Mostrar(
                ventanaPadre,
                titulo,
                mensaje);
        }

        private static string ObtenerMensajeError(
            Exception ex)
        {
            if (ex == null)
            {
                return "Ocurrió un error inesperado.";
            }

            Exception error =
                ex;

            while (error.InnerException != null)
            {
                error =
                    error.InnerException;
            }

            return string.IsNullOrWhiteSpace(
                    error.Message)
                ? "Ocurrió un error inesperado."
                : error.Message;
        }

        // =====================================================
        // AUXILIARES
        // =====================================================

        private string ObtenerMetodoPagoTexto()
        {
            return _metodoPagoSeleccionado switch
            {
                "EFECTIVO" =>
                    "Efectivo",

                "YAPE_PLIN" =>
                    "Yape / Plin",

                "TARJETA" =>
                    "Tarjeta + 5%",

                _ =>
                    _metodoPagoSeleccionado
            };
        }

        private static string FormatearMoneda(
            decimal valor)
        {
            return $"S/ {valor:N2}";
        }

        private static Brush CrearBrocha(
            string colorHexadecimal)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    colorHexadecimal));
        }

        private async void BtnProductoFrecuente_Click(
    object sender,
    RoutedEventArgs e)
{
    if (sender is not Button boton ||
        boton.DataContext is not ProductoVentaPOS productoFrecuente)
    {
        return;
    }

    try
    {
        MostrarCargando(
            true,
            "Consultando stock...");

        /*
         * Volvemos a buscar el producto por código de barras
         * para obtener el stock actual de TIENDA.
         */
        ProductoVentaPOS productoActual =
            await Task.Run(
                () =>
                    _ventaService
                        .ObtenerProductoPorCodigo(
                            productoFrecuente.CodigoBarra));

        MostrarCargando(false);

        /*
         * El producto ya no existe, está inactivo
         * o no tiene stock en tienda.
         */
        if (productoActual == null)
        {
            MensajeDialog.Mostrar(
                Window.GetWindow(this),
                "Producto no disponible",
                "El producto seleccionado ya no se encuentra disponible para la venta.",
                TipoMensaje.Advertencia);

            await CargarProductosFrecuentesAsync();

            return;
        }

        if (productoActual.Stock <= 0)
        {
            MensajeDialog.Mostrar(
                Window.GetWindow(this),
                "Producto sin stock",
                "El producto seleccionado no tiene stock disponible en tienda.",
                TipoMensaje.Advertencia);

            await CargarProductosFrecuentesAsync();

            return;
        }

        /*
         * Agregamos el producto utilizando el stock
         * recién consultado.
         */
        AgregarProductoAlCarrito(
            productoActual);
    }
    catch (Exception ex)
    {
        MostrarCargando(false);

        MensajeDialog.Mostrar(
            Window.GetWindow(this),
            "Error al consultar el producto",
            ObtenerMensajeError(ex),
            TipoMensaje.Error);
    }
    finally
    {
        MostrarCargando(false);
    }
}
    }
}