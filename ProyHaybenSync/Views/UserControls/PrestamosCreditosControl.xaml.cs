using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Models.PrestamosCreditos;
using ProySistemaVentas.Services.PrestamosCreditos;
using ProySistemaVentas.Views.Dialogs;

namespace ProySistemaVentas.Views.UserControls
{
    public partial class PrestamosCreditosControl :
        UserControl
    {
        private readonly PrestamoCreditoService
            _prestamoCreditoService;

        private ProductoPrestamoCredito
            _productoSeleccionado;

        private CancellationTokenSource
            _busquedaCancellationTokenSource;

        private bool _cargandoControles;

        private bool _operacionEnProceso;

        private bool _datosInicialesCargados;


        public PrestamosCreditosControl()
        {
            _prestamoCreditoService =
                new PrestamoCreditoService();

            _cargandoControles = true;

            InitializeComponent();

            CboTipoPrecio.SelectedIndex = 0;

            RbPrestamo.IsChecked = true;

            PnlFechaPago.Visibility =
                Visibility.Collapsed;

            DpFechaPago.SelectedDate =
                DateTime.Today.AddDays(7);

            _cargandoControles = false;

            ActualizarTotal();
        }


        /*====================================================
          CARGA INICIAL
        ====================================================*/

        private async void UserControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_datosInicialesCargados)
            {
                return;
            }

            _datosInicialesCargados = true;

            await CargarDatosInicialesAsync();
        }


        private async Task CargarDatosInicialesAsync()
        {
            try
            {
                BtnGuardarOperacion.IsEnabled = false;

                Task<List<UsuarioMovimiento>> usuariosTask =
                    Task.Run(
                        () =>
                            _prestamoCreditoService
                                .ListarUsuariosActivos());

                Task<ResumenPrestamoCredito> resumenTask =
                    Task.Run(
                        () =>
                            _prestamoCreditoService
                                .ObtenerResumen());

                Task<List<PrestamoCreditoItem>> prestamosTask =
                    Task.Run(
                        () =>
                            _prestamoCreditoService
                                .ListarPrestamos(
                                    pagina: 1,
                                    tamanoPagina: 5));

                Task<List<PrestamoCreditoItem>> creditosTask =
                    Task.Run(
                        () =>
                            _prestamoCreditoService
                                .ListarCreditos(
                                    pagina: 1,
                                    tamanoPagina: 5));

                await Task.WhenAll(
                    usuariosTask,
                    resumenTask,
                    prestamosTask,
                    creditosTask);

                CboTrabajador.ItemsSource =
                    usuariosTask.Result;

                /*
                 * El trabajador debe seleccionarse manualmente.
                 */
                CboTrabajador.SelectedIndex = -1;

                MostrarResumen(
                    resumenTask.Result);

                DgPrestamos.ItemsSource =
                    prestamosTask.Result;

                DgCreditos.ItemsSource =
                    creditosTask.Result;
            }
            catch (Exception ex)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Error al cargar",
                    ObtenerMensajeError(ex),
                    TipoMensaje.Error);
            }
            finally
            {
                BtnGuardarOperacion.IsEnabled = true;
            }
        }


        private void MostrarResumen(
            ResumenPrestamoCredito resumen)
        {
            TxtPrestamosPendientes.Text =
                resumen.PrestamosPendientesTexto;

            TxtStockPrestado.Text =
                resumen.StockPrestadoTotalTexto;

            TxtCreditosPendientes.Text =
                resumen.CreditosPendientesTexto;

            TxtDeudaTotal.Text =
                resumen.DeudaTotalAcumuladaTexto;
        }


        private async Task RecargarResumenYListadosAsync()
        {
            Task<ResumenPrestamoCredito> resumenTask =
                Task.Run(
                    () =>
                        _prestamoCreditoService
                            .ObtenerResumen());

            Task<List<PrestamoCreditoItem>> prestamosTask =
                Task.Run(
                    () =>
                        _prestamoCreditoService
                            .ListarPrestamos(
                                pagina: 1,
                                tamanoPagina: 5));

            Task<List<PrestamoCreditoItem>> creditosTask =
                Task.Run(
                    () =>
                        _prestamoCreditoService
                            .ListarCreditos(
                                pagina: 1,
                                tamanoPagina: 5));

            await Task.WhenAll(
                resumenTask,
                prestamosTask,
                creditosTask);

            MostrarResumen(
                resumenTask.Result);

            DgPrestamos.ItemsSource =
                prestamosTask.Result;

            DgCreditos.ItemsSource =
                creditosTask.Result;
        }


        /*====================================================
          BÚSQUEDA DE PRODUCTOS
        ====================================================*/

        private async void TxtBuscarProducto_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_cargandoControles)
            {
                return;
            }

            string texto =
                TxtBuscarProducto.Text.Trim();

            _busquedaCancellationTokenSource?.Cancel();

            if (texto.Length < 1)
            {
                BrdResultadosProductos.Visibility =
                    Visibility.Collapsed;

                LstResultadosProductos.ItemsSource =
                    null;

                return;
            }

            CancellationTokenSource cancellationTokenSource =
                new CancellationTokenSource();

            _busquedaCancellationTokenSource =
                cancellationTokenSource;

            try
            {
                await Task.Delay(
                    300,
                    cancellationTokenSource.Token);

                List<ProductoPrestamoCredito> productos =
                    await Task.Run(
                        () =>
                            _prestamoCreditoService
                                .BuscarProductos(texto),
                        cancellationTokenSource.Token);

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                LstResultadosProductos.ItemsSource =
                    productos;

                BrdResultadosProductos.Visibility =
                    productos.Count > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }
            catch (TaskCanceledException)
            {
                // Una nueva búsqueda reemplazó a la anterior.
            }
            catch (Exception ex)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Error de búsqueda",
                    ObtenerMensajeError(ex),
                    TipoMensaje.Error);
            }
        }


        private async void TxtBuscarProducto_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            string codigo =
                TxtBuscarProducto.Text.Trim();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return;
            }

            try
            {
                ProductoPrestamoCredito producto =
                    await Task.Run(
                        () =>
                            _prestamoCreditoService
                                .ObtenerProductoPorCodigo(
                                    codigo));

                if (producto == null)
                {
                    return;
                }

                SeleccionarProducto(producto);
            }
            catch (Exception ex)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Error al consultar",
                    ObtenerMensajeError(ex),
                    TipoMensaje.Error);
            }
        }


        private void LstResultadosProductos_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (LstResultadosProductos.SelectedItem
                is not ProductoPrestamoCredito producto)
            {
                return;
            }

            SeleccionarProducto(producto);
        }


        private void SeleccionarProducto(
    ProductoPrestamoCredito producto)
        {
            _productoSeleccionado =
                producto;

            _cargandoControles = true;

            try
            {
                BrdProductoSeleccionado.DataContext =
                    producto;

                TxtCantidad.Text = "1";

                CboTipoPrecio.SelectedIndex = 0;

                TxtPrecio.Text =
                    producto.PrecioUnidad.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);

                TxtBuscarProducto.Text =
                    producto.CodigoBarra;

                BrdProductoSeleccionado.Visibility =
                    Visibility.Visible;

                PnlOpcionesProducto.Visibility =
                    Visibility.Visible;

                BrdResultadosProductos.Visibility =
                    Visibility.Collapsed;

                LstResultadosProductos.SelectedItem =
                    null;

                ActualizarTotal();
            }
            finally
            {
                _cargandoControles = false;
            }

            TxtClienteTienda.Focus();
        }


        /*====================================================
          CANTIDAD
        ====================================================*/

        private void BtnRestarCantidad_Click(
            object sender,
            RoutedEventArgs e)
        {
            int cantidad =
                ObtenerCantidad();

            if (cantidad <= 1)
            {
                cantidad = 1;
            }
            else
            {
                cantidad--;
            }

            TxtCantidad.Text =
                cantidad.ToString();

            ActualizarTotal();
        }


        private void BtnSumarCantidad_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_productoSeleccionado == null)
            {
                return;
            }

            int cantidad =
                ObtenerCantidad();

            if (cantidad >=
                _productoSeleccionado.StockTienda)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Stock insuficiente",
                    "La cantidad no puede superar el stock disponible en tienda.",
                    TipoMensaje.Advertencia);

                return;
            }

            cantidad++;

            TxtCantidad.Text =
                cantidad.ToString();

            ActualizarTotal();
        }


        private void TxtCantidad_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_cargandoControles)
            {
                return;
            }

            ActualizarTotal();
        }


        /*====================================================
          PRECIO
        ====================================================*/

        private void CboTipoPrecio_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_cargandoControles ||
                _productoSeleccionado == null)
            {
                return;
            }

            string tipoPrecio =
                ObtenerTipoPrecio();

            _cargandoControles = true;

            try
            {
                switch (tipoPrecio)
                {
                    case "MAYOR":

                        if (!_productoSeleccionado
                                .TienePrecioMayor)
                        {
                            MensajeDialog.Mostrar(
                                Window.GetWindow(this),
                                "Precio no disponible",
                                "Este producto no tiene un precio por mayor configurado.",
                                TipoMensaje.Advertencia);

                            CboTipoPrecio.SelectedIndex = 0;

                            TxtPrecio.Text =
                                _productoSeleccionado
                                    .PrecioUnidad
                                    .ToString(
                                        "0.00",
                                        CultureInfo.InvariantCulture);

                            break;
                        }

                        TxtPrecio.Text =
                            _productoSeleccionado
                                .PrecioMayor
                                .ToString(
                                    "0.00",
                                    CultureInfo.InvariantCulture);

                        break;

                    case "ESPECIAL":

                        TxtPrecio.SelectAll();

                        TxtPrecio.Focus();

                        break;

                    default:

                        TxtPrecio.Text =
                            _productoSeleccionado
                                .PrecioUnidad
                                .ToString(
                                    "0.00",
                                    CultureInfo.InvariantCulture);

                        break;
                }
            }
            finally
            {
                _cargandoControles = false;
            }

            ActualizarTotal();
        }


        private void TxtPrecio_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_cargandoControles)
            {
                return;
            }

            ActualizarTotal();
        }


        private void ActualizarTotal()
        {
            /*
             * Durante InitializeComponent algunos controles
             * todavía pueden no existir.
             */
            if (TxtCantidad == null ||
                TxtPrecio == null ||
                TxtTotal == null)
            {
                return;
            }

            int cantidad =
                ObtenerCantidad();

            decimal precio =
                ObtenerPrecio();

            decimal total =
                cantidad * precio;

            TxtTotal.Text =
                $"S/ {total:N2}";
        }


        private int ObtenerCantidad()
        {
            if (TxtCantidad == null)
            {
                return 0;
            }

            if (!int.TryParse(
                    TxtCantidad.Text?.Trim(),
                    out int cantidad))
            {
                return 0;
            }

            return cantidad;
        }


        private decimal ObtenerPrecio()
        {
            if (TxtPrecio == null)
            {
                return 0;
            }

            string texto =
                (TxtPrecio.Text ?? string.Empty)
                    .Trim()
                    .Replace(
                        "S/",
                        string.Empty)
                    .Trim();

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal precioInvariante))
            {
                return precioInvariante;
            }

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal precioLocal))
            {
                return precioLocal;
            }

            return 0;
        }


        private string ObtenerTipoPrecio()
        {
            if (CboTipoPrecio.SelectedItem
                is ComboBoxItem item)
            {
                return Convert.ToString(
                           item.Tag)
                           ?.Trim()
                           .ToUpper()
                       ?? "UNIDAD";
            }

            return "UNIDAD";
        }


        /*====================================================
          TIPO DE OPERACIÓN
        ====================================================*/

        private void RbTipoOperacion_Checked(object sender,RoutedEventArgs e)
        {
            if (PnlFechaPago == null)
            {
                return;
            }

            bool esCredito =
                RbCredito?.IsChecked == true;

            PnlFechaPago.Visibility =
                esCredito
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (esCredito &&
                !DpFechaPago.SelectedDate.HasValue)
            {
                DpFechaPago.SelectedDate =
                    DateTime.Today.AddDays(7);
            }
        }


        /*====================================================
          REGISTRAR
        ====================================================*/

        private async void BtnGuardarOperacion_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_operacionEnProceso)
            {
                return;
            }

            if (CboTrabajador.SelectedItem
                is not UsuarioMovimiento trabajador)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Trabajador obligatorio",
                    "Debe seleccionar al trabajador que está realizando la operación.",
                    TipoMensaje.Advertencia);

                CboTrabajador.Focus();

                return;
            }

            if (_productoSeleccionado == null)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Producto obligatorio",
                    "Debe buscar y seleccionar un producto.",
                    TipoMensaje.Advertencia);

                TxtBuscarProducto.Focus();

                return;
            }

            string cliente =
                TxtClienteTienda.Text.Trim();

            if (string.IsNullOrWhiteSpace(cliente))
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Cliente obligatorio",
                    "Debe ingresar el cliente o tienda.",
                    TipoMensaje.Advertencia);

                TxtClienteTienda.Focus();

                return;
            }

            int cantidad =
                ObtenerCantidad();

            if (cantidad <= 0)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Cantidad inválida",
                    "La cantidad debe ser mayor que cero.",
                    TipoMensaje.Advertencia);

                TxtCantidad.Focus();

                return;
            }

            if (cantidad >
                _productoSeleccionado.StockTienda)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Stock insuficiente",
                    "La cantidad supera el stock disponible en tienda.",
                    TipoMensaje.Advertencia);

                TxtCantidad.Focus();

                return;
            }

            decimal precio =
                ObtenerPrecio();

            if (precio <= 0)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Precio inválido",
                    "El precio debe ser mayor que cero.",
                    TipoMensaje.Advertencia);

                TxtPrecio.Focus();

                return;
            }

            bool esCredito =
                RbCredito.IsChecked == true;

            if (esCredito &&
                !DpFechaPago.SelectedDate.HasValue)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Fecha obligatoria",
                    "Debe seleccionar la fecha esperada de pago.",
                    TipoMensaje.Advertencia);

                DpFechaPago.Focus();

                return;
            }

            if (esCredito &&
                DpFechaPago.SelectedDate.Value.Date <
                DateTime.Today)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "Fecha inválida",
                    "La fecha esperada de pago no puede ser anterior a hoy.",
                    TipoMensaje.Advertencia);

                DpFechaPago.Focus();

                return;
            }

            string tipoPrecio =
                ObtenerTipoPrecio();

            string observacion =
                TxtObservacion.Text.Trim();

            try
            {
                _operacionEnProceso = true;

                BtnGuardarOperacion.IsEnabled = false;

                BtnGuardarOperacion.Content =
                    "Registrando...";

                /*
 * Copiamos todos los valores de la interfaz antes
 * de ingresar al hilo secundario.
 */
                int idProducto =
                    _productoSeleccionado.IdProducto;

                int idVariante =
                    _productoSeleccionado.IdVariante;

                int idUsuario =
                    trabajador.IdUsuario;

                DateTime? fechaPagoEsperada =
                    DpFechaPago.SelectedDate;

                ResultadoOperacionPrestamoCredito resultado;

                if (esCredito)
                {
                    resultado =
                        await Task.Run(
                            () =>
                                _prestamoCreditoService
                                    .RegistrarCredito(
                                        idProducto,
                                        idVariante,
                                        cliente,
                                        cantidad,
                                        tipoPrecio,
                                        precio,
                                        fechaPagoEsperada.Value,
                                        idUsuario,
                                        observacion));
                }
                else
                {
                    resultado =
                        await Task.Run(
                            () =>
                                _prestamoCreditoService
                                    .RegistrarPrestamo(
                                        idProducto,
                                        idVariante,
                                        cliente,
                                        cantidad,
                                        tipoPrecio,
                                        precio,
                                        idUsuario,
                                        observacion));
                }

                await RecargarResumenYListadosAsync();

                LimpiarFormulario();

                string nombreOperacion =
                    esCredito
                        ? "Crédito registrado"
                        : "Préstamo registrado";

                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    nombreOperacion,
                    $"La operación {resultado.NumeroOperacion} se registró correctamente.",
                    TipoMensaje.Exito);
            }
            catch (Exception ex)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "No se pudo registrar",
                    ObtenerMensajeError(ex),
                    TipoMensaje.Error);
            }
            finally
            {
                _operacionEnProceso = false;

                BtnGuardarOperacion.IsEnabled = true;

                BtnGuardarOperacion.Content =
                    "Guardar registro";
            }
        }


        private void LimpiarFormulario()
        {
            _cargandoControles = true;

            try
            {
                _productoSeleccionado = null;

                TxtBuscarProducto.Clear();

                TxtClienteTienda.Clear();

                TxtCantidad.Text = "1";

                TxtPrecio.Clear();

                TxtObservacion.Clear();

                TxtTotal.Text = "S/ 0.00";

                CboTipoPrecio.SelectedIndex = 0;

                RbPrestamo.IsChecked = true;

                DpFechaPago.SelectedDate =
                    DateTime.Today.AddDays(7);

                BrdProductoSeleccionado.Visibility =
                    Visibility.Collapsed;

                PnlOpcionesProducto.Visibility =
                    Visibility.Collapsed;

                BrdResultadosProductos.Visibility =
                    Visibility.Collapsed;

                LstResultadosProductos.ItemsSource =
                    null;
            }
            finally
            {
                _cargandoControles = false;
            }

            TxtBuscarProducto.Focus();
        }


        /*====================================================
          ACCIONES PENDIENTES PARA LOS DIÁLOGOS
        ====================================================*/

        private async void BtnVerOperacion_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not PrestamoCreditoItem operacion)
            {
                return;
            }

            try
            {
                boton.IsEnabled = false;

                PrestamoCreditoDetalle detalle =
                    await Task.Run(
                        () =>
                            _prestamoCreditoService
                                .ObtenerDetalle(
                                    operacion.IdPrestamoCredito));

                if (detalle == null)
                {
                    MensajeDialog.Mostrar(
                        Window.GetWindow(this),
                        "Operación no encontrada",
                        "No se encontró la información de la operación seleccionada.",
                        TipoMensaje.Advertencia);

                    return;
                }

                DetallePrestamoCreditoDialog dialogo =
                    new DetallePrestamoCreditoDialog(
                        detalle);

                dialogo.Owner =
                    Window.GetWindow(this);

                dialogo.ShowDialog();
            }
            catch (Exception ex)
            {
                MensajeDialog.Mostrar(
                    Window.GetWindow(this),
                    "No se pudo abrir el detalle",
                    ObtenerMensajeError(ex),
                    TipoMensaje.Error);
            }
            finally
            {
                boton.IsEnabled = true;
            }
        }


        private async void BtnDevolverPrestamo_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!TryObtenerOperacionYTrabajador(sender, out PrestamoCreditoItem operacion, out UsuarioMovimiento trabajador) ||
                !operacion.PuedeDevolver)
            {
                return;
            }

            ConfirmarObservacionDialog dialogo = new ConfirmarObservacionDialog(
                "Confirmar devolución",
                $"Se devolverán {operacion.Cantidad} unidad(es) de {operacion.NombreCompleto} al stock de tienda.",
                "Devolver");
            dialogo.Owner = Window.GetWindow(this);
            dialogo.ShowDialog();
            if (!dialogo.Confirmado)
            {
                return;
            }

            int idOperacion = operacion.IdPrestamoCredito;
            int idUsuario = trabajador.IdUsuario;
            string observacion = dialogo.Observacion;
            try
            {
                await Task.Run(() => _prestamoCreditoService.RegistrarDevolucion(idOperacion, idUsuario, observacion));
                await RecargarResumenYListadosAsync();
                MensajeDialog.Mostrar(Window.GetWindow(this), "Devolución registrada", "El producto fue devuelto al stock de tienda.", TipoMensaje.Exito);
            }
            catch (Exception ex)
            {
                MostrarErrorOperacion("No se pudo registrar la devolución", ex);
            }
        }


        private async void BtnPasarACobranza_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!TryObtenerOperacionYTrabajador(sender, out PrestamoCreditoItem operacion, out UsuarioMovimiento trabajador) ||
                !operacion.PuedePasarACobranza)
            {
                return;
            }

            ConvertirPrestamoCreditoDialog dialogo = new ConvertirPrestamoCreditoDialog(operacion)
            {
                Owner = Window.GetWindow(this)
            };
            dialogo.ShowDialog();
            if (!dialogo.Confirmado)
            {
                return;
            }

            int idOperacion = operacion.IdPrestamoCredito;
            int idUsuario = trabajador.IdUsuario;
            DateTime fechaEsperada = dialogo.FechaPagoEsperada;
            string observacion = dialogo.Observacion;
            try
            {
                await Task.Run(() => _prestamoCreditoService.ConvertirPrestamoACredito(idOperacion, idUsuario, fechaEsperada, observacion));
                await RecargarResumenYListadosAsync();
                MensajeDialog.Mostrar(Window.GetWindow(this), "Operación convertida", "El préstamo pasó a cobranza sin modificar el stock.", TipoMensaje.Exito);
            }
            catch (Exception ex)
            {
                MostrarErrorOperacion("No se pudo pasar a cobranza", ex);
            }
        }


        private async void BtnPagarCredito_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!TryObtenerOperacionYTrabajador(sender, out PrestamoCreditoItem operacion, out UsuarioMovimiento trabajador) ||
                !operacion.PuedePagar)
            {
                return;
            }

            PagoCreditoDialog dialogo = new PagoCreditoDialog(operacion, trabajador)
            {
                Owner = Window.GetWindow(this)
            };
            dialogo.ShowDialog();
            if (!dialogo.Confirmado)
            {
                return;
            }

            int idOperacion = operacion.IdPrestamoCredito;
            int idUsuario = trabajador.IdUsuario;
            string metodoPago = dialogo.MetodoPago;
            string observacion = dialogo.Observacion;
            try
            {
                await Task.Run(() => _prestamoCreditoService.RegistrarPago(idOperacion, idUsuario, metodoPago, observacion));
                BoletaPagoCredito boleta = await Task.Run(() => _prestamoCreditoService.ObtenerBoletaPago(idOperacion));
                await RecargarResumenYListadosAsync();
                MensajeDialog.Mostrar(Window.GetWindow(this), "Pago registrado", "La cobranza fue completada correctamente.", TipoMensaje.Exito);
                AbrirNotaCobranza(boleta);
            }
            catch (Exception ex)
            {
                MostrarErrorOperacion("No se pudo registrar el pago", ex);
            }
        }


        private async void BtnPasarAPrestamo_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!TryObtenerOperacionYTrabajador(sender, out PrestamoCreditoItem operacion, out UsuarioMovimiento trabajador) ||
                !operacion.PuedePasarAPrestamo)
            {
                return;
            }

            if (!MensajeDialog.Confirmar(Window.GetWindow(this), "Pasar a préstamo", "La cobranza volverá a préstamo. No se modificará el stock."))
            {
                return;
            }

            int idOperacion = operacion.IdPrestamoCredito;
            int idUsuario = trabajador.IdUsuario;
            try
            {
                await Task.Run(() => _prestamoCreditoService.ConvertirCreditoAPrestamo(idOperacion, idUsuario));
                await RecargarResumenYListadosAsync();
                MensajeDialog.Mostrar(Window.GetWindow(this), "Operación convertida", "La cobranza pasó a préstamo sin modificar el stock.", TipoMensaje.Exito);
            }
            catch (Exception ex)
            {
                MostrarErrorOperacion("No se pudo pasar a préstamo", ex);
            }
        }

        private async void BtnVerBoleta_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton || boton.Tag is not PrestamoCreditoItem operacion || !operacion.IdPagoCredito.HasValue)
            {
                return;
            }

            int idOperacion = operacion.IdPrestamoCredito;
            try
            {
                BoletaPagoCredito boleta = await Task.Run(() => _prestamoCreditoService.ObtenerBoletaPago(idOperacion));
                AbrirNotaCobranza(boleta);
            }
            catch (Exception ex)
            {
                MostrarErrorOperacion("No se pudo abrir la nota de cobranza", ex);
            }
        }

        private void AbrirNotaCobranza(BoletaPagoCredito boleta)
        {
            if (boleta == null)
            {
                MensajeDialog.Mostrar(Window.GetWindow(this), "Nota no encontrada", "No se encontró la nota de cobranza para esta operación.", TipoMensaje.Advertencia);
                return;
            }

            NotaCobranzaDialog dialogo = new NotaCobranzaDialog(boleta)
            {
                Owner = Window.GetWindow(this)
            };
            dialogo.ShowDialog();
        }

        private bool TryObtenerOperacionYTrabajador(object sender, out PrestamoCreditoItem operacion, out UsuarioMovimiento trabajador)
        {
            operacion = null;
            trabajador = null;
            if (sender is not Button boton || boton.Tag is not PrestamoCreditoItem item)
            {
                return false;
            }

            if (CboTrabajador.SelectedItem is not UsuarioMovimiento usuario)
            {
                MensajeDialog.Mostrar(Window.GetWindow(this), "Trabajador obligatorio", "Debe seleccionar al trabajador responsable antes de continuar.", TipoMensaje.Advertencia);
                CboTrabajador.Focus();
                return false;
            }

            operacion = item;
            trabajador = usuario;
            return true;
        }

        private void MostrarErrorOperacion(string titulo, Exception ex)
        {
            string mensaje = ObtenerMensajeError(ex);
            if (mensaje.Contains("caja", StringComparison.OrdinalIgnoreCase) && mensaje.Contains("abierta", StringComparison.OrdinalIgnoreCase))
            {
                mensaje = "Debe abrir una caja antes de cobrar en efectivo.";
            }
            MensajeDialog.Mostrar(Window.GetWindow(this), titulo, mensaje, TipoMensaje.Error);
        }


        private async void BtnVerTodosPrestamos_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CboTrabajador.SelectedItem is not UsuarioMovimiento trabajador)
            {
                MensajeDialog.Mostrar(Window.GetWindow(this), "Trabajador obligatorio", "Seleccione un trabajador para administrar operaciones desde el listado.", TipoMensaje.Advertencia);
                return;
            }

            ListadoPrestamosCreditosDialog dialogo = new ListadoPrestamosCreditosDialog(false, trabajador)
            {
                Owner = Window.GetWindow(this)
            };
            dialogo.OperacionActualizada += async (_, _) => await RecargarResumenYListadosAsync();
            dialogo.ShowDialog();
            await RecargarResumenYListadosAsync();
        }


        private async void BtnVerTodosCreditos_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CboTrabajador.SelectedItem is not UsuarioMovimiento trabajador)
            {
                MensajeDialog.Mostrar(Window.GetWindow(this), "Trabajador obligatorio", "Seleccione un trabajador para administrar operaciones desde el listado.", TipoMensaje.Advertencia);
                return;
            }

            ListadoPrestamosCreditosDialog dialogo = new ListadoPrestamosCreditosDialog(true, trabajador)
            {
                Owner = Window.GetWindow(this)
            };
            dialogo.OperacionActualizada += async (_, _) => await RecargarResumenYListadosAsync();
            dialogo.ShowDialog();
            await RecargarResumenYListadosAsync();
        }


        /*====================================================
          ERRORES
        ====================================================*/

        private static string ObtenerMensajeError(
            Exception ex)
        {
            if (ex.InnerException != null &&
                !string.IsNullOrWhiteSpace(
                    ex.InnerException.Message))
            {
                return ex.InnerException.Message;
            }

            return ex.Message;
        }
    }
}
