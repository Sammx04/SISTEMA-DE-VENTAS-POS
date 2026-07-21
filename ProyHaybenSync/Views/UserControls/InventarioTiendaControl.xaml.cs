using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Services.InventarioTienda;
using ProySistemaVentas.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProySistemaVentas.Views.UserControls
{
    public partial class InventarioTiendaControl : UserControl
    {
        private readonly InventarioTiendaService
            _inventarioService;

        private List<InventarioTiendaProducto>
            _todosLosProductos;

        private List<InventarioTiendaProducto>
            _productosFiltrados;

        private const int ProductosPorPagina = 30;

        private int _paginaActual = 1;

        private int _totalPaginas = 1;

        /*
         * Evita que los eventos de los ComboBox
         * se ejecuten mientras se están cargando.
         */
        private bool _cargandoFiltros;

        public InventarioTiendaControl()
        {
            /*
             * Se inicializan primero las variables porque algunos
             * eventos del XAML pueden ejecutarse dentro de
             * InitializeComponent().
             */
            _inventarioService =
                new InventarioTiendaService();

            _todosLosProductos =
                new List<InventarioTiendaProducto>();

            _productosFiltrados =
                new List<InventarioTiendaProducto>();

            /*
             * Bloqueamos temporalmente los eventos de los filtros
             * mientras WPF construye los controles.
             */
            _cargandoFiltros = true;

            InitializeComponent();

            _cargandoFiltros = false;

            Loaded +=
                InventarioTiendaControl_Loaded;
        }

        // =====================================================
        // CARGAR VISTA
        // =====================================================

        private void InventarioTiendaControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            /*
             * Evita volver a consultar cuando el mismo control
             * recibe nuevamente el evento Loaded.
             */
            Loaded -=
                InventarioTiendaControl_Loaded;

            CargarInventario();
        }

        private void CargarInventario()
        {
            try
            {
                MostrarCargando(true);

                /*
                 * Guardamos los filtros actuales para conservarlos
                 * después de una transferencia de stock.
                 */
                string textoBusqueda =
                    TxtBuscarProducto.Text?.Trim()
                    ?? string.Empty;

                int categoriaSeleccionada =
                    ObtenerCategoriaSeleccionada();

                string estadoSeleccionado =
                    ObtenerEstadoSeleccionado();

                _todosLosProductos =
                    _inventarioService
                        .ListarInventarioTienda()
                        .ToList();

                CargarFiltroCategorias(
                    categoriaSeleccionada);

                TxtBuscarProducto.Text =
                    textoBusqueda;

                SeleccionarEstado(
                    estadoSeleccionado);

                _paginaActual = 1;

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                _todosLosProductos.Clear();
                _productosFiltrados.Clear();

                MostrarPaginaActual();

                MensajeExitoDialog.Mostrar(
                    Window.GetWindow(this),
                    "No se pudo cargar el inventario",
                    "Ocurrió un error al consultar los productos " +
                    $"de tienda.\n\n{ex.Message}");
            }
            finally
            {
                MostrarCargando(false);
            }
        }

        // =====================================================
        // CARGAR CATEGORÍAS
        // =====================================================

        private void CargarFiltroCategorias(
            int categoriaAnterior = 0)
        {
            _cargandoFiltros = true;

            try
            {
                List<KeyValuePair<int, string>>
                    categorias =
                        _todosLosProductos
                            .Where(
                                producto =>
                                    producto.IdCategoria > 0)
                            .GroupBy(
                                producto =>
                                    producto.IdCategoria)
                            .Select(
                                grupo =>
                                    new KeyValuePair<int, string>(
                                        grupo.Key,
                                        grupo
                                            .First()
                                            .NombreCategoria))
                            .OrderBy(
                                categoria =>
                                    categoria.Value)
                            .ToList();

                categorias.Insert(
                    0,
                    new KeyValuePair<int, string>(
                        0,
                        "Todas las categorías"));

                CboFiltroCategoria.ItemsSource =
                    categorias;

                KeyValuePair<int, string>
                    categoriaASeleccionar =
                        categorias.FirstOrDefault(
                            categoria =>
                                categoria.Key ==
                                categoriaAnterior);

                if (categoriaASeleccionar.Key !=
                    categoriaAnterior)
                {
                    categoriaASeleccionar =
                        categorias[0];
                }

                CboFiltroCategoria.SelectedItem =
                    categoriaASeleccionar;
            }
            finally
            {
                _cargandoFiltros = false;
            }
        }

        private int ObtenerCategoriaSeleccionada()
        {
            if (CboFiltroCategoria.SelectedItem
                is KeyValuePair<int, string> categoria)
            {
                return categoria.Key;
            }

            return 0;
        }

        // =====================================================
        // FILTROS
        // =====================================================

        private void AplicarFiltros()
        {
            if (_todosLosProductos == null)
            {
                _todosLosProductos =
                    new List<InventarioTiendaProducto>();
            }

            if (_productosFiltrados == null)
            {
                _productosFiltrados =
                    new List<InventarioTiendaProducto>();
            }

            string textoBusqueda =
                TxtBuscarProducto?.Text?
                    .Trim()
                    .ToLowerInvariant()
                ?? string.Empty;

            int categoriaSeleccionada =
                ObtenerCategoriaSeleccionada();

            string estadoSeleccionado =
                ObtenerEstadoSeleccionado();

            _productosFiltrados =
                _todosLosProductos
                    .Where(
                        producto =>
                            CumpleFiltroBusqueda(
                                producto,
                                textoBusqueda))
                    .Where(
                        producto =>
                            categoriaSeleccionada == 0 ||
                            producto.IdCategoria ==
                            categoriaSeleccionada)
                    .Where(
                        producto =>
                            estadoSeleccionado == "TODOS" ||
                            string.Equals(
                                producto.EstadoTienda,
                                estadoSeleccionado,
                                StringComparison.OrdinalIgnoreCase))
                    .OrderBy(
                        producto =>
                            producto.NombreProducto)
                    .ToList();

            _totalPaginas =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        _productosFiltrados.Count /
                        (double)ProductosPorPagina));

            if (_paginaActual > _totalPaginas)
            {
                _paginaActual =
                    _totalPaginas;
            }

            if (_paginaActual < 1)
            {
                _paginaActual = 1;
            }

            MostrarPaginaActual();
        }

        private bool CumpleFiltroBusqueda(
            InventarioTiendaProducto producto,
            string textoBusqueda)
        {
            if (string.IsNullOrWhiteSpace(
                textoBusqueda))
            {
                return true;
            }

            bool coincideProducto =
                ContieneTexto(
                    producto.NombreProducto,
                    textoBusqueda) ||

                ContieneTexto(
                    producto.CodigoBase,
                    textoBusqueda) ||

                ContieneTexto(
                    producto.NombreCategoria,
                    textoBusqueda) ||

                ContieneTexto(
                    producto.DescripcionProducto,
                    textoBusqueda);

            if (coincideProducto)
            {
                return true;
            }

            if (producto.Variantes == null)
            {
                return false;
            }

            return producto.Variantes.Any(
                variante =>
                    ContieneTexto(
                        variante.VarianteDescripcion,
                        textoBusqueda) ||

                    ContieneTexto(
                        variante.CodigoBarras,
                        textoBusqueda));
        }

        private static bool ContieneTexto(
            string valor,
            string textoBusqueda)
        {
            return !string.IsNullOrWhiteSpace(
                       valor) &&
                   valor
                       .ToLowerInvariant()
                       .Contains(
                           textoBusqueda);
        }

        private string ObtenerEstadoSeleccionado()
        {
            if (CboFiltroEstado.SelectedItem
                is ComboBoxItem item)
            {
                return Convert.ToString(
                           item.Tag)
                       ?.Trim()
                       .ToUpperInvariant()
                       ?? "TODOS";
            }

            return "TODOS";
        }

        private void SeleccionarEstado(
            string estado)
        {
            string estadoBuscado =
                string.IsNullOrWhiteSpace(
                    estado)
                    ? "TODOS"
                    : estado.Trim()
                        .ToUpperInvariant();

            foreach (
                object elemento
                in CboFiltroEstado.Items)
            {
                if (elemento is not ComboBoxItem item)
                {
                    continue;
                }

                string valorItem =
                    Convert.ToString(
                        item.Tag)
                    ?.Trim()
                    .ToUpperInvariant()
                    ?? string.Empty;

                if (valorItem ==
                    estadoBuscado)
                {
                    CboFiltroEstado.SelectedItem =
                        item;

                    return;
                }
            }

            CboFiltroEstado.SelectedIndex =
                0;
        }

        // =====================================================
        // EVENTOS DE FILTROS
        // =====================================================

        private void TxtBuscarProducto_TextChanged(object sender,TextChangedEventArgs e)
        {
            if (TxtPlaceholderBusqueda != null)
            {
                TxtPlaceholderBusqueda.Visibility =
                    string.IsNullOrWhiteSpace(
                        TxtBuscarProducto?.Text)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }

            if (_cargandoFiltros ||
                !IsLoaded)
            {
                return;
            }

            _paginaActual = 1;

            AplicarFiltros();
        }

        private void CboFiltroCategoria_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            if (_cargandoFiltros ||
                !IsLoaded)
            {
                return;
            }

            _paginaActual = 1;

            AplicarFiltros();
        }

        private void CboFiltroEstado_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            if (_cargandoFiltros ||
                !IsLoaded)
            {
                return;
            }

            _paginaActual = 1;

            AplicarFiltros();
        }

        // =====================================================
        // MOSTRAR PÁGINA
        // =====================================================

        private void MostrarPaginaActual()
        {
            int totalProductos =
                _productosFiltrados.Count;

            _totalPaginas =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        totalProductos /
                        (double)ProductosPorPagina));

            if (_paginaActual >
                _totalPaginas)
            {
                _paginaActual =
                    _totalPaginas;
            }

            int indiceInicial =
                (_paginaActual - 1) *
                ProductosPorPagina;

            List<InventarioTiendaProducto>
                productosPagina =
                    _productosFiltrados
                        .Skip(
                            indiceInicial)
                        .Take(
                            ProductosPorPagina)
                        .ToList();

            ItemsProductos.ItemsSource =
                productosPagina;

            bool existenResultados =
                productosPagina.Count > 0;

            ItemsProductos.Visibility =
                existenResultados
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            PanelSinResultados.Visibility =
                existenResultados
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            TxtTotalProductos.Text =
                totalProductos == 1
                    ? "1 producto"
                    : $"{totalProductos:N0} productos";

            int desde =
                totalProductos == 0
                    ? 0
                    : indiceInicial + 1;

            int hasta =
                Math.Min(
                    indiceInicial +
                    ProductosPorPagina,
                    totalProductos);

            TxtInformacionPagina.Text =
                $"Mostrando {desde:N0} - {hasta:N0} " +
                $"de {totalProductos:N0} productos";

            TxtNumeroPagina.Text =
                $"Página {_paginaActual:N0} " +
                $"de {_totalPaginas:N0}";

            BtnPaginaAnterior.IsEnabled =
                _paginaActual > 1;

            BtnPaginaSiguiente.IsEnabled =
                _paginaActual <
                _totalPaginas;

            ConstruirBotonesPaginacion();
        }

        // =====================================================
        // PAGINACIÓN
        // =====================================================

        private void ConstruirBotonesPaginacion()
        {
            PanelPaginas.Children.Clear();

            if (_totalPaginas <= 1)
            {
                return;
            }

            /*
             * Mostramos como máximo cinco números de página
             * para no llenar demasiado el pie de la pantalla.
             */
            int paginaInicial =
                Math.Max(
                    1,
                    _paginaActual - 2);

            int paginaFinal =
                Math.Min(
                    _totalPaginas,
                    paginaInicial + 4);

            if (paginaFinal -
                paginaInicial < 4)
            {
                paginaInicial =
                    Math.Max(
                        1,
                        paginaFinal - 4);
            }

            for (int numeroPagina =
                     paginaInicial;
                 numeroPagina <=
                     paginaFinal;
                 numeroPagina++)
            {
                Button botonPagina =
                    new Button
                    {
                        Content =
                            numeroPagina.ToString(),

                        Tag =
                            numeroPagina,

                        Style =
                            TryFindResource(
                                "BotonPagina")
                            as Style
                    };

                if (numeroPagina ==
                    _paginaActual)
                {
                    botonPagina.Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                34,
                                211,
                                238));

                    botonPagina.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                7,
                                17,
                                29));

                    botonPagina.BorderBrush =
                        new SolidColorBrush(
                            Color.FromRgb(
                                103,
                                232,
                                249));
                }

                botonPagina.Click +=
                    BotonNumeroPagina_Click;

                PanelPaginas.Children.Add(
                    botonPagina);
            }
        }

        private void BotonNumeroPagina_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not int numeroPagina)
            {
                return;
            }

            CambiarPagina(
                numeroPagina);
        }

        private void BtnPaginaAnterior_Click(
            object sender,
            RoutedEventArgs e)
        {
            CambiarPagina(
                _paginaActual - 1);
        }

        private void BtnPaginaSiguiente_Click(
            object sender,
            RoutedEventArgs e)
        {
            CambiarPagina(
                _paginaActual + 1);
        }

        private void CambiarPagina(
            int nuevaPagina)
        {
            if (nuevaPagina < 1 ||
                nuevaPagina >
                _totalPaginas)
            {
                return;
            }

            _paginaActual =
                nuevaPagina;

            MostrarPaginaActual();

            ScrollCatalogo.ScrollToTop();
        }

        // =====================================================
        // MOSTRAR U OCULTAR VARIANTES
        // =====================================================

        private void BtnAlternarVariantes_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not InventarioTiendaProducto producto)
            {
                return;
            }

            /*
             * Solo se permite desplegar cuando realmente
             * existen varias variantes.
             */
            if (!producto.MostrarBotonVariantes)
            {
                return;
            }

            producto.EstaExpandido =
                !producto.EstaExpandido;

            e.Handled = true;
        }

        // =====================================================
        // TRANSFERENCIA DE STOCK
        // =====================================================

        private void BtnTransferirStock_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not InventarioTiendaVariante variante)
            {
                return;
            }

            e.Handled = true;

            if (variante.StockAlmacen <= 0)
            {
                MensajeExitoDialog.Mostrar(
                    Window.GetWindow(this),
                    "Sin stock disponible",
                    "Esta variante no tiene unidades disponibles " +
                    "en almacén para transferir.");

                return;
            }

            TransferenciaStockDialog dialogo =
                new TransferenciaStockDialog(
                    variante);

            Window ventanaPadre =
                Window.GetWindow(this);

            if (ventanaPadre != null)
            {
                dialogo.Owner =
                    ventanaPadre;
            }

            bool? resultado =
                dialogo.ShowDialog();

            if (resultado != true ||
                !dialogo.TransferenciaRealizada)
            {
                return;
            }

            /*
             * El diálogo actualiza la variante original.
             * Aquí recalculamos los totales de su producto.
             */
            InventarioTiendaProducto producto =
                _todosLosProductos
                    .FirstOrDefault(
                        item =>
                            item.IdProducto ==
                            variante.IdProducto);

            producto?.ActualizarTotales();

            /*
             * Se vuelven a aplicar los filtros porque una
             * transferencia puede cambiar el estado:
             * Agotado → Stock bajo → En stock.
             */
            AplicarFiltros();
        }

        // =====================================================
        // PANEL DE CARGA
        // =====================================================

        private void MostrarCargando(
            bool mostrar)
        {
            PanelCargando.Visibility =
                mostrar
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ItemsProductos.IsEnabled =
                !mostrar;

            TxtBuscarProducto.IsEnabled =
                !mostrar;

            CboFiltroCategoria.IsEnabled =
                !mostrar;

            CboFiltroEstado.IsEnabled =
                !mostrar;
        }
    }
}