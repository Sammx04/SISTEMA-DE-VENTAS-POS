using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Services.InventarioTienda;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class TransferenciaStockDialog : Window
    {
        private readonly InventarioTiendaVariante _variante;
        private readonly InventarioTiendaService _inventarioService;

        /*
         * Estas propiedades permitirán que la vista principal
         * sepa si la transferencia fue realizada correctamente.
         */
        public bool TransferenciaRealizada { get; private set; }

        public int CantidadTransferida { get; private set; }

        public UsuarioMovimiento ResponsableSeleccionado
        {
            get;
            private set;
        }

        public TransferenciaStockDialog(
            InventarioTiendaVariante variante)
        {
            InitializeComponent();

            _variante =
                variante
                ?? throw new ArgumentNullException(
                    nameof(variante));

            _inventarioService =
                new InventarioTiendaService();

            /*
             * También valida cuando el usuario pega contenido
             * dentro del TextBox de cantidad.
             */
            DataObject.AddPastingHandler(
                TxtCantidad,
                TxtCantidad_Pasting);

            Loaded +=
                TransferenciaStockDialog_Loaded;
        }

        // =====================================================
        // CARGAR VENTANA
        // =====================================================

        private void TransferenciaStockDialog_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            CargarDatosVariante();

            CargarResponsables();

            ActualizarValidacion();

            TxtCantidad.Focus();
        }

        private void CargarDatosVariante()
        {
            TxtProducto.Text =
                string.IsNullOrWhiteSpace(
                    _variante.NombreProducto)
                    ? "Producto sin nombre"
                    : _variante.NombreProducto.Trim();

            TxtVariante.Text =
                _variante.NombreVarianteMostrar;

            TxtCodigoBarras.Text =
                string.IsNullOrWhiteSpace(
                    _variante.CodigoBarras)
                    ? "Sin código"
                    : _variante.CodigoBarras.Trim();

            TxtStockAlmacen.Text =
                _variante.StockAlmacen
                    .ToString("N0");

            TxtStockTienda.Text =
                _variante.StockTienda
                    .ToString("N0");

            TxtMaximoDisponible.Text =
                $"Máximo disponible: " +
                $"{_variante.StockAlmacen:N0}";

            /*
             * Si no existe stock en almacén,
             * se bloquea la cantidad.
             */
            if (_variante.StockAlmacen <= 0)
            {
                TxtCantidad.IsEnabled =
                    false;

                TxtCantidad.Text =
                    string.Empty;

                TxtErrorCantidad.Text =
                    "No existe stock disponible en almacén.";

                TxtErrorCantidad.Visibility =
                    Visibility.Visible;
            }
        }

        // =====================================================
        // CARGAR RESPONSABLES
        // =====================================================

        private void CargarResponsables()
        {
            try
            {
                List<UsuarioMovimiento> usuarios =
                    _inventarioService
                        .ListarUsuariosActivos();

                /*
                 * Se agrega una primera opción para indicar
                 * que debe seleccionar al responsable.
                 */
                usuarios.Insert(
                    0,
                    new UsuarioMovimiento
                    {
                        IdUsuario = 0,
                        Nombre =
                            "Seleccione un responsable",
                        Usuario =
                            string.Empty,
                        Rol =
                            string.Empty
                    });

                CboResponsable.ItemsSource =
                    usuarios;

                CboResponsable.SelectedIndex =
                    0;

                if (usuarios.Count <= 1)
                {
                    TxtErrorResponsable.Text =
                        "No existen usuarios activos disponibles.";

                    TxtErrorResponsable.Visibility =
                        Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                CboResponsable.ItemsSource =
                    new List<UsuarioMovimiento>
                    {
                        new UsuarioMovimiento
                        {
                            IdUsuario = 0,
                            Nombre =
                                "No se pudieron cargar los responsables"
                        }
                    };

                CboResponsable.SelectedIndex =
                    0;

                TxtErrorResponsable.Text =
                    "No se pudieron cargar los responsables.";

                TxtErrorResponsable.Visibility =
                    Visibility.Visible;

                MensajeExitoDialog.Mostrar(
                    this,
                    "No se pudieron cargar los usuarios",
                    $"Ocurrió un problema al cargar los responsables.\n\n" +
                    ex.Message);
            }
        }

        // =====================================================
        // VALIDACIÓN DEL TEXTO
        // =====================================================

        private void TxtCantidad_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            /*
             * Solo se permiten números enteros positivos.
             */
            e.Handled =
                !e.Text.All(
                    char.IsDigit);
        }

        private void TxtCantidad_Pasting(
            object sender,
            DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(
                DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string textoPegado =
                Convert.ToString(
                    e.DataObject.GetData(
                        DataFormats.Text))
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    textoPegado) ||
                !textoPegado.All(
                    char.IsDigit))
            {
                e.CancelCommand();
            }
        }

        private void ValidarFormulario_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ActualizarValidacion();
        }

        private void CboResponsable_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            ActualizarValidacion();
        }

        private void ActualizarValidacion()
        {
            bool cantidadValida =
                ObtenerCantidadValida(
                    out int cantidad);

            bool responsableValido =
                ObtenerResponsableValido(
                    out _);

            /*
             * No mostramos el error mientras el campo
             * todavía se encuentre vacío.
             */
            if (string.IsNullOrWhiteSpace(
                    TxtCantidad.Text))
            {
                if (_variante.StockAlmacen > 0)
                {
                    TxtErrorCantidad.Visibility =
                        Visibility.Collapsed;
                }
            }
            else if (!int.TryParse(
                         TxtCantidad.Text.Trim(),
                         out int cantidadIngresada) ||
                     cantidadIngresada <= 0)
            {
                TxtErrorCantidad.Text =
                    "Ingrese una cantidad mayor que cero.";

                TxtErrorCantidad.Visibility =
                    Visibility.Visible;
            }
            else if (cantidadIngresada >
                     _variante.StockAlmacen)
            {
                TxtErrorCantidad.Text =
                    $"La cantidad no puede superar " +
                    $"{_variante.StockAlmacen:N0} unidades.";

                TxtErrorCantidad.Visibility =
                    Visibility.Visible;
            }
            else
            {
                TxtErrorCantidad.Visibility =
                    Visibility.Collapsed;
            }

            /*
             * Cuando ya se ingresó una cantidad válida,
             * se indica que falta escoger responsable.
             */
            if (cantidadValida &&
                !responsableValido)
            {
                TxtErrorResponsable.Text =
                    "Seleccione al responsable de la transferencia.";

                TxtErrorResponsable.Visibility =
                    Visibility.Visible;
            }
            else if (responsableValido)
            {
                TxtErrorResponsable.Visibility =
                    Visibility.Collapsed;
            }

            BtnTransferir.IsEnabled =
                cantidadValida &&
                responsableValido &&
                _variante.StockAlmacen > 0;
        }

        private bool ObtenerCantidadValida(
            out int cantidad)
        {
            cantidad = 0;

            if (!int.TryParse(
                    TxtCantidad.Text?.Trim(),
                    out cantidad))
            {
                return false;
            }

            if (cantidad <= 0)
            {
                return false;
            }

            if (cantidad >
                _variante.StockAlmacen)
            {
                return false;
            }

            return true;
        }

        private bool ObtenerResponsableValido(
            out UsuarioMovimiento responsable)
        {
            responsable =
                CboResponsable.SelectedItem
                as UsuarioMovimiento;

            return responsable != null &&
                   responsable.IdUsuario > 0;
        }

        // =====================================================
        // TRANSFERIR STOCK
        // =====================================================

        private void BtnTransferir_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!ObtenerCantidadValida(
                    out int cantidad))
            {
                TxtErrorCantidad.Text =
                    "Ingrese una cantidad válida para transferir.";

                TxtErrorCantidad.Visibility =
                    Visibility.Visible;

                TxtCantidad.Focus();

                return;
            }

            if (!ObtenerResponsableValido(
                    out UsuarioMovimiento responsable))
            {
                TxtErrorResponsable.Text =
                    "Seleccione al responsable de la transferencia.";

                TxtErrorResponsable.Visibility =
                    Visibility.Visible;

                CboResponsable.Focus();

                return;
            }

            try
            {
                CambiarEstadoProcesando(
                    true);

                (
                    bool Exitoso,
                    int StockAlmacen,
                    int StockTienda
                ) resultado =
                    _inventarioService
                        .TransferirStockATienda(
                            _variante.IdVariante,
                            cantidad,
                            responsable.IdUsuario);

                if (!resultado.Exitoso)
                {
                    MensajeExitoDialog.Mostrar(
                        this,
                        "Transferencia no realizada",
                        "La base de datos no confirmó la transferencia.");

                    return;
                }

                /*
                 * Actualizamos el objeto original para que
                 * la tarjeta pueda reflejar el nuevo stock.
                 */
                _variante.StockAlmacen =
                    resultado.StockAlmacen;

                _variante.StockTienda =
                    resultado.StockTienda;

                TxtStockAlmacen.Text =
                    resultado.StockAlmacen
                        .ToString("N0");

                TxtStockTienda.Text =
                    resultado.StockTienda
                        .ToString("N0");

                TransferenciaRealizada =
                    true;

                CantidadTransferida =
                    cantidad;

                ResponsableSeleccionado =
                    responsable;

                MensajeExitoDialog.Mostrar(
                    this,
                    "Transferencia realizada",
                    $"Se transfirieron {cantidad:N0} unidades " +
                    $"de \"{_variante.NombreProducto}\" " +
                    $"hacia tienda correctamente.");

                /*
                 * DialogResult = true cierra la ventana
                 * y permite que la vista principal recargue.
                 */
                DialogResult =
                    true;
            }
            catch (Exception ex)
            {
                MensajeExitoDialog.Mostrar(
                    this,
                    "No se pudo transferir",
                    $"Ocurrió un error durante la transferencia.\n\n" +
                    ex.Message);
            }
            finally
            {
                /*
                 * Si la ventana continúa abierta debido a un
                 * error, volvemos a habilitar sus controles.
                 */
                if (IsVisible)
                {
                    CambiarEstadoProcesando(
                        false);

                    ActualizarValidacion();
                }
            }
        }

        private void CambiarEstadoProcesando(
            bool procesando)
        {
            TxtCantidad.IsEnabled =
                !procesando &&
                _variante.StockAlmacen > 0;

            CboResponsable.IsEnabled =
                !procesando;

            BtnCancelar.IsEnabled =
                !procesando;

            BtnTransferir.Content =
                procesando
                    ? "Procesando..."
                    : "⇄  Transferir stock";

            if (procesando)
            {
                BtnTransferir.IsEnabled =
                    false;
            }
        }

        // =====================================================
        // CERRAR Y MOVER VENTANA
        // =====================================================

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