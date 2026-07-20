using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.Almacen;
using ProySistemaVentas.Models.Almacen;
using ProySistemaVentas.Services.Almacen;
using ProySistemaVentas.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProySistemaVentas.Views.UserControls
{
    public partial class AlmacenControl : UserControl
    {
        private readonly CategoriaService _categoriaService;
        private readonly ProductoInventarioService _productoService;
        private readonly StockService _stockService;

        private List<ProductoListaViewModel> _productosAgrupados;
        private List<ProductoListaViewModel> _productosFiltrados;

        private const int ProductosPorPagina = 20;

        private int _paginaActual = 1;

        private readonly ObservableCollection<TipoVariante> _variantes;

        private List<ProductoInventarioListado> _productos;

        private int _idProductoSeleccionado;
        private int _idVarianteSeleccionada;


        private bool _modoEdicion;

        private string _codigoBaseTemporal = string.Empty;

        private void DgProductos_MouseDoubleClick(
    object sender,
    MouseButtonEventArgs e)
        {
            if (DgProductos.SelectedItem
                is not ProductoListaViewModel producto)
            {
                return;
            }

            CargarProductoParaEditar(
                producto);
        }

        public AlmacenControl()
        {
            InitializeComponent();

            _categoriaService = new CategoriaService();
            _productoService = new ProductoInventarioService();
            _stockService = new StockService();

            _variantes = new ObservableCollection<TipoVariante>();
            _productos = new List<ProductoInventarioListado>();

            _productosAgrupados = new List<ProductoListaViewModel>();

            _productosFiltrados = new List<ProductoListaViewModel>();

            DgVariantes.ItemsSource = _variantes;

            Loaded += AlmacenControl_Loaded;

            ConfigurarModoEdicion(false);
        }

        private void AlmacenControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                CargarCategorias();
                CargarProductos();
                InicializarVarianteUnica();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo cargar la información del almacén.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // CARGA INICIAL
        // =========================================================

        private void CargarCategorias(int? idCategoriaSeleccionar = null)
        {
            List<Categoria> categorias =
                _categoriaService
                    .ListarCategorias()
                    .ToList();

            // ComboBox del formulario
            List<Categoria> categoriasFormulario =
                new List<Categoria>();

            categoriasFormulario.Add(
                new Categoria
                {
                    IdCategoria = 0,
                    Nombre = "Seleccione categoría"
                });

            categoriasFormulario.AddRange(categorias);

            CboCategoria.ItemsSource =
                categoriasFormulario;

            CboCategoria.DisplayMemberPath =
                "Nombre";

            CboCategoria.SelectedValuePath =
                "IdCategoria";

            // Si estamos editando o acabamos de crear una categoría,
            // seleccionamos esa categoría.
            if (idCategoriaSeleccionar.HasValue &&
                categoriasFormulario.Any(
                    c => c.IdCategoria ==
                         idCategoriaSeleccionar.Value))
            {
                CboCategoria.SelectedValue =
                    idCategoriaSeleccionar.Value;
            }
            else
            {
                CboCategoria.SelectedIndex = 0;
            }

            // ComboBox del filtro de productos
            List<Categoria> categoriasFiltro =
                new List<Categoria>();

            categoriasFiltro.Add(
                new Categoria
                {
                    IdCategoria = 0,
                    Nombre = "Todas las categorías"
                });

            categoriasFiltro.AddRange(categorias);

            CboFiltroCategoria.ItemsSource =
                categoriasFiltro;

            CboFiltroCategoria.DisplayMemberPath =
                "Nombre";

            CboFiltroCategoria.SelectedValuePath =
                "IdCategoria";

            if (CboFiltroCategoria.SelectedIndex < 0)
            {
                CboFiltroCategoria.SelectedIndex = 0;
            }
        }

        private void CargarProductos()
        {
            // Obtiene todos los productos junto con sus variantes.
            _productos =
                _productoService
                    .ListarProductosConVariantes()
                    .ToList();

            // Agrupa los registros por producto.
            _productosAgrupados =
                _productos
                    .GroupBy(
                        producto =>
                            producto.IdProducto)
                    .Select(grupo =>
                    {
                        ProductoInventarioListado primero =
                            grupo.First();

                        List<ProductoInventarioListado> variantesProducto =
                            grupo.ToList();

                        int cantidadVariantes =
                            variantesProducto.Count;

                        bool tieneVariantes =
                            cantidadVariantes > 1 ||
                            !string.Equals(
                                primero.VarianteDescripcion,
                                "Única",
                                StringComparison.OrdinalIgnoreCase);

                        return new ProductoListaViewModel
                        {
                            IdProducto =
                                primero.IdProducto,

                            CodigoBase =
                                primero.CodigoBase,

                            Nombre =
                                primero.Nombre,

                            Descripcion =
                                primero.Descripcion,

                            IdCategoria =
                                primero.IdCategoria,

                            NombreCategoria =
                                primero.NombreCategoria,

                            // Esta propiedad se utilizará en la
                            // ventana DetalleProductoDialog.
                            CantidadVariantes =
                                cantidadVariantes,

                            TieneVariantes =
                                tieneVariantes,

                            PrecioUnidadTexto =
                                cantidadVariantes > 1
                                    ? "—"
                                    : primero.PrecioUnidad
                                        .ToString("N2"),

                            PrecioMayorTexto =
                                cantidadVariantes > 1
                                    ? "—"
                                    : primero.PrecioMayor
                                        .ToString("N2"),

                            // Stock total disponible en almacén.
                            StockTotal =
                                variantesProducto.Sum(
                                    variante =>
                                        variante.StockAlmacen),

                            CodigoBarrasTexto =
                                cantidadVariantes > 1
                                    ? "Múltiples"
                                    : primero.CodigoBarras,

                            Variantes =
                                variantesProducto
                        };
                    })
                    .OrderBy(
                        producto =>
                            producto.Nombre)
                    .ToList();

            // Regresa a la primera página.
            _paginaActual = 1;

            // Aplica los filtros y carga el DataGrid.
            AplicarFiltros();
        }

        private void InicializarVarianteUnica()
        {
            if (_variantes.Count > 0)
            {
                return;
            }

            _variantes.Add(
                new TipoVariante
                {
                    IdVariante = 0,
                    Descripcion = "Única",
                    CodigoBarras = string.Empty,
                    PrecioUnidad = 0,
                    PrecioMayor = 0,

                    // Para registrar un producto.
                    Stock = 0,

                    // Para el modo edición.
                    StockActual = 0,
                    Ingreso = 0,
                    Salida = 0
                });

            ActualizarComboVariantes();
        }

        private void ActualizarComboVariantes(
    TipoVariante varianteSeleccionada = null)
        {
            List<TipoVariante> variantesConCodigo =
                _variantes
                    .Where(
                        v => !string.IsNullOrWhiteSpace(
                            v.CodigoBarras))
                    .ToList();

            CboVarianteCodigo.ItemsSource = null;

            CboVarianteCodigo.ItemsSource =
                variantesConCodigo;

            CboVarianteCodigo.DisplayMemberPath =
                "Descripcion";

            if (variantesConCodigo.Count == 0)
            {
                CboVarianteCodigo.SelectedIndex = -1;

                OcultarCodigoBarras();

                return;
            }

            if (varianteSeleccionada != null &&
                variantesConCodigo.Contains(
                    varianteSeleccionada))
            {
                CboVarianteCodigo.SelectedItem =
                    varianteSeleccionada;

                MostrarCodigoBarras(
                    varianteSeleccionada);
            }
            else
            {
                CboVarianteCodigo.SelectedIndex = -1;

                OcultarCodigoBarras();
            }
        }

        // =========================================================
        // CATEGORÍAS
        // =========================================================

        private void BtnNuevaCategoria_Click(
    object sender,
    RoutedEventArgs e)
        {
            NuevaCategoriaDialog ventana =
                new NuevaCategoriaDialog
                {
                    Owner = Window.GetWindow(this)
                };

            bool? resultado =
                ventana.ShowDialog();

            if (resultado != true)
            {
                return;
            }

            string nombreCategoria =
                ventana.NombreCategoria.Trim();

            try
            {
                _categoriaService.CrearCategoria(
                    nombreCategoria);

                CargarCategorias();

                // Busca la categoría recién creada
                // y la selecciona automáticamente.
                if (CboCategoria.ItemsSource
                    is IEnumerable<Categoria> categorias)
                {
                    Categoria categoriaCreada =
                        categorias.FirstOrDefault(
                            categoria =>
                                string.Equals(
                                    categoria.Nombre?.Trim(),
                                    nombreCategoria,
                                    StringComparison.OrdinalIgnoreCase));

                    if (categoriaCreada != null)
                    {
                        CboCategoria.SelectedValue =
                            categoriaCreada.IdCategoria;
                    }
                }

                MensajeExitoDialog.Mostrar(
    Window.GetWindow(this),
    "Categoría creada",
    $"La categoría \"{nombreCategoria}\" fue creada correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo crear la categoría.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        // =========================================================
        // VARIANTES
        // =========================================================

        private void BtnAgregarVariante_Click(
    object sender,
    RoutedEventArgs e)
        {
            TipoVariante nuevaVariante =
                new TipoVariante
                {
                    IdVariante = 0,
                    Descripcion = string.Empty,
                    CodigoBarras = string.Empty,
                    PrecioUnidad = 0,
                    PrecioMayor = 0,

                    Stock = 0,
                    StockActual = 0,
                    Ingreso = 0,
                    Salida = 0
                };

            _variantes.Add(
                nuevaVariante);

            DgVariantes.SelectedItem =
                nuevaVariante;

            DgVariantes.ScrollIntoView(
                nuevaVariante);

            ActualizarComboVariantes();
        }

        private void CboVarianteCodigo_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            if (CboVarianteCodigo.SelectedItem
                is TipoVariante variante &&
                !string.IsNullOrWhiteSpace(
                    variante.CodigoBarras))
            {
                MostrarCodigoBarras(variante);
            }
            else
            {
                OcultarCodigoBarras();
            }
        }

        // =========================================================
        // GUARDAR PRODUCTO
        // =========================================================
        private void BtnGuardarProducto_Click(
    object sender,
    RoutedEventArgs e)
        {
            // Confirma el último valor escrito dentro del DataGrid.
            DgVariantes.CommitEdit(
                DataGridEditingUnit.Cell,
                true);

            DgVariantes.CommitEdit(
                DataGridEditingUnit.Row,
                true);

            if (!ValidarFormulario())
            {
                return;
            }

            try
            {
                int idCategoria =
                    Convert.ToInt32(
                        CboCategoria.SelectedValue);

                bool tieneVariantes =
                    _variantes.Count > 1 ||
                    (_variantes.Count == 1 &&
                     !string.Equals(
                         _variantes[0].Descripcion,
                         "Única",
                         StringComparison.OrdinalIgnoreCase));

                /*
                 * Preparamos las variantes para enviarlas al servicio.
                 */
                List<TipoVariante> variantesParaGuardar =
                    _variantes
                        .Select(variante =>
                            new TipoVariante
                            {
                                IdVariante =
                                    variante.IdVariante,

                                Descripcion =
                                    variante.Descripcion?.Trim(),

                                CodigoBarras =
                                    variante.CodigoBarras,

                                PrecioUnidad =
                                    variante.PrecioUnidad,

                                PrecioMayor =
                                    variante.PrecioMayor,

                                Stock =
                                    !_modoEdicion
                                        ? variante.Stock
                                        : variante.IdVariante > 0
                                            ? variante.StockActual
                                            : variante.NuevoStock,

                                StockActual =
                                    variante.StockActual,

                                Ingreso =
                                    variante.Ingreso,

                                Salida =
                                    variante.Salida
                            })
                        .ToList();

                // =====================================================
                // REGISTRAR PRODUCTO NUEVO
                // =====================================================

                if (!_modoEdicion)
                {
                    _productoService.CrearProductoInventario(
                        TxtNombreProducto.Text.Trim(),
                        idCategoria,
                        TxtDescripcion.Text.Trim(),
                        tieneVariantes,
                        variantesParaGuardar);

                    MensajeExitoDialog.Mostrar(
                        Window.GetWindow(this),
                        "Producto registrado",
                        "El producto fue registrado correctamente.");
                }

                // =====================================================
                // EDITAR PRODUCTO EXISTENTE
                // =====================================================

                else
                {
                    // =================================================
                    // 1. ACTUALIZAR DATOS GENERALES DEL PRODUCTO
                    // =================================================

                    _productoService.EditarProductoInventario(
                        _idProductoSeleccionado,
                        TxtNombreProducto.Text.Trim(),
                        idCategoria,
                        TxtDescripcion.Text.Trim(),
                        tieneVariantes,
                        variantesParaGuardar);

                    /*
                     * Separamos las variantes antes de modificar sus IDs.
                     *
                     * Existentes: IdVariante > 0
                     * Nuevas:     IdVariante == 0
                     */
                    List<TipoVariante> variantesExistentes =
                        _variantes
                            .Where(
                                variante =>
                                    variante.IdVariante > 0)
                            .ToList();

                    List<TipoVariante> variantesNuevas =
                        _variantes
                            .Where(
                                variante =>
                                    variante.IdVariante == 0)
                            .ToList();

                    // =================================================
                    // 2. ACTUALIZAR VARIANTES EXISTENTES
                    // =================================================

                    foreach (
                        TipoVariante variante
                        in variantesExistentes)
                    {
                        bool varianteActualizada =
                            _productoService.EditarVariante(
                                variante);

                        if (!varianteActualizada)
                        {
                            throw new InvalidOperationException(
                                $"No se pudo actualizar la variante " +
                                $"\"{variante.Descripcion}\".");
                        }
                    }

                    // =================================================
                    // 3. REGISTRAR LAS VARIANTES NUEVAS
                    // =================================================

                    foreach (
                        TipoVariante varianteNueva
                        in variantesNuevas)
                    {
                        /*
                         * El procedimiento sp_AgregarVarianteAProducto:
                         *
                         * - Genera el código de barras.
                         * - Registra la variante.
                         * - Registra el stock de almacén.
                         * - Registra stock cero para tienda.
                         * - Actualiza el stock total del producto.
                         * - Devuelve IdVariante y CodigoBarras.
                         */
                        var resultadoVariante =
                            _productoService
                                .AgregarVarianteAProducto(
                                    _idProductoSeleccionado,
                                    varianteNueva);

                        if (resultadoVariante.IdVariante <= 0)
                        {
                            throw new InvalidOperationException(
                                $"No se pudo registrar la nueva variante " +
                                $"\"{varianteNueva.Descripcion}\".");
                        }

                        // Guardamos los valores generados por SQL Server.
                        varianteNueva.IdVariante =
                            resultadoVariante.IdVariante;

                        varianteNueva.CodigoBarras =
                            resultadoVariante.CodigoBarras;

                        /*
                         * No llamamos a ActualizarStockAlmacen aquí,
                         * porque sp_AgregarVarianteAProducto ya registró
                         * el stock de la nueva variante.
                         */
                    }

                    // =================================================
                    // 4. ACTUALIZAR STOCK SOLO DE VARIANTES EXISTENTES
                    // =================================================

                    foreach (
                        TipoVariante variante
                        in variantesExistentes.Where(
                            variante =>
                                variante.Ingreso > 0 ||
                                variante.Salida > 0))
                    {
                        bool stockActualizado =
                            _stockService
                                .ActualizarStockAlmacen(
                                    variante.IdVariante,
                                    variante.Ingreso,
                                    variante.Salida);

                        if (!stockActualizado)
                        {
                            throw new InvalidOperationException(
                                $"No se pudo actualizar el stock de la variante " +
                                $"\"{variante.Descripcion}\".");
                        }
                    }

                    // =================================================
                    // 5. MENSAJE REUTILIZABLE
                    // =================================================

                    MensajeExitoDialog.Mostrar(
                        Window.GetWindow(this),
                        "Producto actualizado",
                        variantesNuevas.Count > 0
                            ? "El producto y sus nuevas variantes fueron registrados correctamente."
                            : "El producto, sus variantes y el stock fueron actualizados correctamente.");
                }

                // Limpia el formulario y actualiza el listado.
                LimpiarFormulario();
                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo guardar el producto.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(
                TxtNombreProducto.Text))
            {
                MessageBox.Show(
                    "Ingrese el nombre del producto.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                TxtNombreProducto.Focus();
                return false;
            }

            if (CboCategoria.SelectedValue == null ||!int.TryParse(CboCategoria.SelectedValue.ToString(),out int idCategoriaSeleccionada) ||idCategoriaSeleccionada <= 0)
            {
                MessageBox.Show(
                    "Seleccione una categoría.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                CboCategoria.Focus();

                return false;
            }

            if (_variantes.Count == 0)
            {
                MessageBox.Show(
                    "Debe registrar al menos una variante.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            foreach (TipoVariante variante in _variantes)
            {
                if (string.IsNullOrWhiteSpace(
    variante.CodigoBarras))
                {
                    MessageBox.Show(
                        $"Debe generar el código de barras de la variante " +
                        $"\"{variante.Descripcion}\".",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                if (string.IsNullOrWhiteSpace(
                    variante.Descripcion))
                {
                    MessageBox.Show(
                        "Todas las variantes deben tener una descripción.",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                if (variante.PrecioUnidad < 0 ||
                    variante.PrecioMayor < 0)
                {
                    MessageBox.Show(
                        "Los precios no pueden ser negativos.",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                if (!_modoEdicion)
                {
                    // Registro de un producto nuevo.
                    if (variante.Stock < 0)
                    {
                        MessageBox.Show(
                            "El stock no puede ser negativo.",
                            "Validación",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return false;
                    }
                }
                else
                {
                    // Edición de un producto.
                    if (variante.Ingreso < 0 ||
                        variante.Salida < 0)
                    {
                        MessageBox.Show(
                            "El ingreso y la salida no pueden ser negativos.",
                            "Validación",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return false;
                    }

                    if (variante.Ingreso > 0 &&
                        variante.Salida > 0)
                    {
                        MessageBox.Show(
                            $"En la variante \"{variante.Descripcion}\" " +
                            "solo puede registrar un ingreso o una salida.",
                            "Validación",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return false;
                    }

                    if (variante.NuevoStock < 0)
                    {
                        MessageBox.Show(
                            $"La salida de la variante " +
                            $"\"{variante.Descripcion}\" supera su stock actual.",
                            "Validación",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return false;
                    }
                }
            }

            return true;
        }

        private void BtnCancelarEdicion_Click(
            object sender,
            RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _idProductoSeleccionado = 0;
            _idVarianteSeleccionada = 0;

            _codigoBaseTemporal =
    string.Empty;

            TxtNombreProducto.Clear();
            TxtDescripcion.Clear();

            if (CboCategoria.Items.Count > 0)
            {
                CboCategoria.SelectedIndex = 0;
            }

            _variantes.Clear();

            ConfigurarModoEdicion(false);

            InicializarVarianteUnica();

            CboVarianteCodigo.SelectedIndex = -1;

            OcultarCodigoBarras();

            DgProductos.SelectedItem = null;
        }
        
        // =========================================================
        // BÚSQUEDA Y FILTROS
        // =========================================================

        private void TxtBuscarProducto_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void CboFiltroCategoria_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            if (DgProductos == null ||
                TxtBuscarProducto == null ||
                CboFiltroCategoria == null)
            {
                return;
            }

            IEnumerable<ProductoListaViewModel> resultado =
                _productosAgrupados;

            string texto =
                TxtBuscarProducto.Text.Trim();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                resultado =
                    resultado.Where(producto =>
                        ContieneTexto(
                            producto.Nombre,
                            texto) ||

                        ContieneTexto(
                            producto.Descripcion,
                            texto) ||

                        ContieneTexto(
                            producto.CodigoBase,
                            texto) ||

                        producto.Variantes.Any(variante =>
                            ContieneTexto(
                                variante.CodigoBarras,
                                texto) ||

                            ContieneTexto(
                                variante.VarianteDescripcion,
                                texto)));
            }

            if (CboFiltroCategoria.SelectedValue != null &&
                int.TryParse(
                    CboFiltroCategoria.SelectedValue.ToString(),
                    out int idCategoria) &&
                idCategoria > 0)
            {
                resultado =
                    resultado.Where(
                        producto =>
                            producto.IdCategoria ==
                            idCategoria);
            }

            _productosFiltrados =
                resultado.ToList();

            _paginaActual = 1;

            MostrarPagina();
        }

        private static bool ContieneTexto(
            string valor,
            string texto)
        {
            return !string.IsNullOrWhiteSpace(valor) &&
                   valor.IndexOf(
                       texto,
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void BtnGenerarCodigoVariante_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not TipoVariante variante)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(
                variante.CodigoBarras))
            {
                MessageBox.Show(
                    "La variante ya tiene un código de barras generado.",
                    "Código de barras",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            try
            {
                variante.CodigoBarras =
                    GenerarCodigoBarrasVariante(variante);

                DgVariantes.Items.Refresh();

                ActualizarComboVariantes(
                    variante);

                CboVarianteCodigo.SelectedItem =
                    variante;

                MostrarCodigoBarras(
                    variante);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo generar el código de barras.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string GenerarCodigoBarrasVariante(
    TipoVariante varianteActual)
        {
            if (varianteActual == null)
            {
                throw new ArgumentNullException(
                    nameof(varianteActual));
            }

            /*
             * El código base debe obtenerse una sola vez
             * para el producto que se está registrando.
             *
             * Ejemplo:
             * Código base: 0000001
             *
             * Variante 1: 0000001 + 01 = 000000101
             * Variante 2: 0000001 + 02 = 000000102
             */
            if (string.IsNullOrWhiteSpace(
                _codigoBaseTemporal))
            {
                _codigoBaseTemporal =
                    _productoService
                        .ObtenerProximoCodigoBase()
                        ?.Trim();

                if (string.IsNullOrWhiteSpace(
                    _codigoBaseTemporal))
                {
                    throw new InvalidOperationException(
                        "No se pudo obtener el código base del producto.");
                }
            }

            HashSet<int> correlativosUtilizados =
                new HashSet<int>();

            foreach (TipoVariante variante in _variantes)
            {
                if (ReferenceEquals(
                    variante,
                    varianteActual))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                    variante.CodigoBarras))
                {
                    continue;
                }

                if (!variante.CodigoBarras.StartsWith(
                        _codigoBaseTemporal,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string sufijo =
                    variante.CodigoBarras.Substring(
                        _codigoBaseTemporal.Length);

                if (int.TryParse(
                        sufijo,
                        out int numeroVariante) &&
                    numeroVariante > 0)
                {
                    correlativosUtilizados.Add(
                        numeroVariante);
                }
            }

            // Busca el primer correlativo libre: 01, 02, 03...
            int correlativo =
                Enumerable
                    .Range(1, 99)
                    .FirstOrDefault(
                        numero =>
                            !correlativosUtilizados.Contains(
                                numero));

            if (correlativo == 0)
            {
                throw new InvalidOperationException(
                    "El producto alcanzó el máximo de 99 variantes.");
            }

            return
                $"{_codigoBaseTemporal}{correlativo:D2}";
        }

        private void BtnEliminarVariante_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not TipoVariante variante)
            {
                return;
            }

            /*
             * El producto debe conservar al menos una variante.
             */
            if (_variantes.Count <= 1)
            {
                MensajeExitoDialog.Mostrar(
                    Window.GetWindow(this),
                    "Variante requerida",
                    "El producto debe conservar al menos una variante.");

                e.Handled = true;
                return;
            }

            string nombreVariante =
                string.IsNullOrWhiteSpace(
                    variante.Descripcion)
                        ? "Sin descripción"
                        : variante.Descripcion.Trim();

            bool confirmar =
                MensajeExitoDialog.Confirmar(
                    Window.GetWindow(this),
                    "Eliminar variante",
                    $"¿Está seguro de eliminar la variante " +
                    $"\"{nombreVariante}\"?\n\n" +
                    "También se eliminará el stock asociado a esta variante.");

            if (!confirmar)
            {
                e.Handled = true;
                return;
            }

            try
            {
                /*
                 * IdVariante = 0:
                 * Es una variante nueva que todavía no está guardada
                 * en la base de datos.
                 */
                if (variante.IdVariante == 0)
                {
                    bool estabaSeleccionada =
                        ReferenceEquals(
                            CboVarianteCodigo.SelectedItem,
                            variante);

                    _variantes.Remove(
                        variante);

                    DgVariantes.Items.Refresh();

                    ActualizarComboVariantes();

                    if (estabaSeleccionada)
                    {
                        CboVarianteCodigo.SelectedIndex = -1;
                        OcultarCodigoBarras();
                    }

                    MensajeExitoDialog.Mostrar(
                        Window.GetWindow(this),
                        "Variante retirada",
                        $"La variante \"{nombreVariante}\" " +
                        "fue retirada del formulario.");

                    e.Handled = true;
                    return;
                }

                /*
                 * IdVariante > 0:
                 * La variante ya existe en la base de datos.
                 */
                bool eliminada =
                    _productoService.EliminarVariante(
                        variante.IdVariante);

                if (!eliminada)
                {
                    throw new InvalidOperationException(
                        "El procedimiento no confirmó la eliminación de la variante.");
                }

                bool eraVarianteSeleccionada =
                    ReferenceEquals(
                        CboVarianteCodigo.SelectedItem,
                        variante);

                // Retirarla de la colección mostrada en el DataGrid.
                _variantes.Remove(
                    variante);

                DgVariantes.Items.Refresh();

                // Actualizar el selector de códigos.
                ActualizarComboVariantes();

                if (eraVarianteSeleccionada)
                {
                    CboVarianteCodigo.SelectedIndex = -1;
                    OcultarCodigoBarras();
                }

                // Actualizar el listado principal y los totales.
                CargarProductos();

                MensajeExitoDialog.Mostrar(
                    Window.GetWindow(this),
                    "Variante eliminada",
                    $"La variante \"{nombreVariante}\" " +
                    "fue eliminada correctamente.");
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    $"No se pudo eliminar la variante.\n\n{ex.Message}",
                    "Error al eliminar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo eliminar la variante.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void MostrarCodigoBarras(
    TipoVariante variante)
        {
            if (variante == null ||
                string.IsNullOrWhiteSpace(
                    variante.CodigoBarras))
            {
                OcultarCodigoBarras();
                return;
            }

            TxtNumeroCodigo.Text =
                variante.CodigoBarras;

            string nombreProducto =
                TxtNombreProducto.Text.Trim();

            TxtProductoCodigo.Text =
                string.IsNullOrWhiteSpace(nombreProducto)
                    ? "Producto sin nombre"
                    : $"Producto: {nombreProducto}";

            string descripcionVariante =
                string.IsNullOrWhiteSpace(
                    variante.Descripcion)
                    ? "Única"
                    : variante.Descripcion;

            TxtVarianteCodigo.Text =
                $"Variante: {descripcionVariante}";

            PanelCodigoVacio.Visibility =
                Visibility.Collapsed;

            PanelCodigoGenerado.Visibility =
                Visibility.Visible;

            TxtSeleccioneVarianteCodigo.Visibility =
                Visibility.Collapsed;

            BtnImprimirEtiqueta.IsEnabled =
                true;
        }

        private void OcultarCodigoBarras()
        {
            if (PanelCodigoVacio == null ||
                PanelCodigoGenerado == null)
            {
                return;
            }

            PanelCodigoVacio.Visibility =
                Visibility.Visible;

            PanelCodigoGenerado.Visibility =
                Visibility.Collapsed;

            TxtSeleccioneVarianteCodigo.Visibility =
                Visibility.Visible;

            TxtNumeroCodigo.Text =
                string.Empty;

            TxtProductoCodigo.Text =
                string.Empty;

            TxtVarianteCodigo.Text =
                string.Empty;

            BtnImprimirEtiqueta.IsEnabled =
                false;
        }

        private void MostrarPagina()
        {
            int totalProductos =
                _productosFiltrados.Count;

            int totalPaginas =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        totalProductos /
                        (double)ProductosPorPagina));

            if (_paginaActual > totalPaginas)
            {
                _paginaActual = totalPaginas;
            }

            if (_paginaActual < 1)
            {
                _paginaActual = 1;
            }

            List<ProductoListaViewModel> productosPagina =
                _productosFiltrados
                    .Skip(
                        (_paginaActual - 1) *
                        ProductosPorPagina)
                    .Take(ProductosPorPagina)
                    .ToList();

            DgProductos.ItemsSource =
                productosPagina;

            TxtTotalProductos.Text =
                totalProductos == 1
                    ? "1 producto"
                    : $"{totalProductos} productos";

            if (totalProductos == 0)
            {
                TxtRangoProductos.Text =
                    "No hay productos para mostrar";
            }
            else
            {
                int desde =
                    ((_paginaActual - 1) *
                     ProductosPorPagina) + 1;

                int hasta =
                    Math.Min(
                        _paginaActual *
                        ProductosPorPagina,
                        totalProductos);

                TxtRangoProductos.Text =
                    $"Mostrando {desde} - {hasta} " +
                    $"de {totalProductos} productos";
            }

            BtnAnteriorPagina.IsEnabled =
                _paginaActual > 1;

            BtnSiguientePagina.IsEnabled =
                _paginaActual < totalPaginas;

            GenerarBotonesPaginas(
                totalPaginas);
        }

        private void GenerarBotonesPaginas(
    int totalPaginas)
        {
            PanelPaginas.Children.Clear();

            int primeraPagina =
                Math.Max(
                    1,
                    _paginaActual - 2);

            int ultimaPagina =
                Math.Min(
                    totalPaginas,
                    primeraPagina + 4);

            if (ultimaPagina - primeraPagina < 4)
            {
                primeraPagina =
                    Math.Max(
                        1,
                        ultimaPagina - 4);
            }

            for (int numeroPagina = primeraPagina;
                 numeroPagina <= ultimaPagina;
                 numeroPagina++)
            {
                Button botonPagina =
                    new Button
                    {
                        Content =
                            numeroPagina.ToString(),

                        Tag =
                            numeroPagina,

                        Width =
                            36,

                        Height =
                            34,

                        Padding =
                            new Thickness(0),

                        Margin =
                            new Thickness(3, 0, 3, 0)
                    };

                if (numeroPagina == _paginaActual)
                {
                    botonPagina.Style =
                        FindResource(
                            "PrimaryButton") as Style;
                }
                else
                {
                    botonPagina.Style =
                        FindResource(
                            "SecondaryButton") as Style;
                }

                botonPagina.Click +=
                    BtnNumeroPagina_Click;

                PanelPaginas.Children.Add(
                    botonPagina);
            }
        }

        private void BtnAnteriorPagina_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (_paginaActual <= 1)
            {
                return;
            }

            _paginaActual--;

            MostrarPagina();
        }

        private void BtnSiguientePagina_Click(
            object sender,
            RoutedEventArgs e)
        {
            int totalPaginas =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        _productosFiltrados.Count /
                        (double)ProductosPorPagina));

            if (_paginaActual >= totalPaginas)
            {
                return;
            }

            _paginaActual++;

            MostrarPagina();
        }

        private void BtnNumeroPagina_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag == null)
            {
                return;
            }

            _paginaActual =
                Convert.ToInt32(
                    boton.Tag);

            MostrarPagina();
        }

        private void BtnVerProducto_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ProductoListaViewModel producto)
            {
                return;
            }

            DetalleProductoDialog dialogo =
                new DetalleProductoDialog(
                    producto);

            Window ventanaPadre =
                Window.GetWindow(this);

            if (ventanaPadre != null)
            {
                dialogo.Owner =
                    ventanaPadre;
            }

            dialogo.ShowDialog();

            e.Handled = true;
        }

        private void MostrarDetalleProducto(
            ProductoListaViewModel producto)
        {
            string detalleVariantes =
                string.Join(
                    "\n",
                    producto.Variantes.Select(variante =>
                        $"• {variante.VarianteDescripcion} | " +
                        $"Código: {variante.CodigoBarras} | " +
                        $"Precio: S/ {variante.PrecioUnidad:N2} | " +
                        $"Stock: {variante.StockAlmacen}"));

            MessageBox.Show(
                $"Producto: {producto.Nombre}\n" +
                $"Categoría: {producto.NombreCategoria}\n" +
                $"Código base: {producto.CodigoBase}\n" +
                $"Stock total: {producto.StockTotal}\n\n" +
                $"Variantes:\n{detalleVariantes}",
                "Detalle del producto",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnEditarProducto_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ProductoListaViewModel producto)
            {
                return;
            }

            CargarProductoParaEditar(
                producto);
        }

        private void CargarProductoParaEditar(
    ProductoListaViewModel producto)
        {
            if (producto == null)
            {
                return;
            }

            _idProductoSeleccionado =
                producto.IdProducto;
            _codigoBaseTemporal =
    producto.CodigoBase?.Trim() ??
    string.Empty;

            TxtNombreProducto.Text =
                producto.Nombre ?? string.Empty;

            TxtDescripcion.Text =
                producto.Descripcion ?? string.Empty;

            CboCategoria.SelectedValue =
                producto.IdCategoria;

            _variantes.Clear();

            foreach (
                ProductoInventarioListado varianteProducto
                in producto.Variantes)
            {
                int stockActual =
                    varianteProducto.StockAlmacen;

                _variantes.Add(
                    new TipoVariante
                    {
                        IdVariante =
                            varianteProducto.IdVariante,

                        Descripcion =
                            string.IsNullOrWhiteSpace(
                                varianteProducto.VarianteDescripcion)
                                ? "Única"
                                : varianteProducto.VarianteDescripcion,

                        CodigoBarras =
                            varianteProducto.CodigoBarras,

                        PrecioUnidad =
                            varianteProducto.PrecioUnidad,

                        PrecioMayor =
                            varianteProducto.PrecioMayor,

                        // Guardamos también el stock original
                        // para enviarlo al procedimiento de edición.
                        Stock =
                            stockActual,

                        StockActual =
                            stockActual,

                        Ingreso =
                            0,

                        Salida =
                            0
                    });
            }

            DgVariantes.Items.Refresh();

            ActualizarComboVariantes();

            ConfigurarModoEdicion(true);

            TxtNombreProducto.Focus();

            DgVariantes.ScrollIntoView(
                _variantes.FirstOrDefault());
        }

        private void BtnEliminarProducto_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ProductoListaViewModel producto)
            {
                return;
            }

            e.Handled = true;

            string nombreProducto =
                string.IsNullOrWhiteSpace(producto.Nombre)
                    ? "Producto sin nombre"
                    : producto.Nombre.Trim();

            bool confirmar =
                MensajeExitoDialog.Confirmar(
                    Window.GetWindow(this),
                    "Eliminar producto",
                    $"¿Está seguro de eliminar el producto " +
                    $"\"{nombreProducto}\"?\n\n" +
                    "El producto y sus variantes dejarán de aparecer " +
                    "en el almacén y en el punto de venta.");

            if (!confirmar)
            {
                return;
            }

            try
            {
                bool productoEliminado =
                    _productoService
                        .DesactivarProductoInventario(
                            producto.IdProducto);

                if (!productoEliminado)
                {
                    throw new InvalidOperationException(
                        "El procedimiento no confirmó la eliminación del producto.");
                }

                /*
                 * Si el producto eliminado estaba cargado
                 * actualmente en el formulario de edición,
                 * se limpia el formulario.
                 */
                if (_modoEdicion &&
                    _idProductoSeleccionado ==
                    producto.IdProducto)
                {
                    LimpiarFormulario();
                }

                // Vuelve a consultar los productos activos.
                CargarProductos();

                MensajeExitoDialog.Mostrar(
                    Window.GetWindow(this),
                    "Producto eliminado",
                    $"El producto \"{nombreProducto}\" " +
                    "y sus variantes fueron eliminados correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo eliminar el producto.\n\n{ex.Message}",
                    "Error al eliminar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ConfigurarModoEdicion(
    bool editando)
        {
            _modoEdicion = editando;

            // Cuando se registra un producto nuevo,
            // solamente aparece la columna Stock.
            ColStockInicial.Visibility =
                editando
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            // Cuando se edita aparecen los movimientos.
            ColStockActual.Visibility =
                editando
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ColIngreso.Visibility =
                editando
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ColSalida.Visibility =
                editando
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ColNuevoStock.Visibility =
                editando
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            BtnGuardarProducto.Content =
                editando
                    ? "Actualizar producto"
                    : "Guardar producto";

            BtnCancelarEdicion.Visibility =
                editando
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void BtnDesplegarVariantes_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (sender is not Button boton ||
                boton.Tag is not ProductoListaViewModel producto)
            {
                return;
            }

            if (!producto.TieneVariantes)
            {
                return;
            }

            DataGridRow fila =
                DgProductos.ItemContainerGenerator
                    .ContainerFromItem(producto)
                as DataGridRow;

            if (fila == null)
            {
                return;
            }

            bool estaDesplegado =
                fila.DetailsVisibility ==
                Visibility.Visible;

            fila.DetailsVisibility =
                estaDesplegado
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            boton.Content =
                estaDesplegado
                    ? "›"
                    : "⌄";

            boton.ToolTip =
                estaDesplegado
                    ? "Mostrar variantes"
                    : "Ocultar variantes";

            // Evita que el clic continúe hacia otros controles.
            e.Handled = true;
        }

        // =========================================================
        // CÓDIGO DE BARRAS
        // =========================================================

        private void BtnImprimirEtiquetas_Click(
    object sender,
    RoutedEventArgs e)
        {
            /*
             * El ComboBox debe tener seleccionada
             * una variante del producto.
             */
            if (CboVarianteCodigo.SelectedItem
                is not TipoVariante varianteSeleccionada)
            {
                MensajeExitoDialog.Mostrar(
                    Window.GetWindow(this),
                    "Seleccione una variante",
                    "Debe seleccionar una variante antes de imprimir la etiqueta.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                varianteSeleccionada.CodigoBarras))
            {
                MensajeExitoDialog.Mostrar(
                    Window.GetWindow(this),
                    "Código no disponible",
                    "La variante seleccionada no tiene un código de barras generado.");

                return;
            }

            string nombreProducto =
                string.IsNullOrWhiteSpace(
                    TxtNombreProducto.Text)
                        ? "Producto"
                        : TxtNombreProducto.Text.Trim();

            string nombreVariante =
                string.IsNullOrWhiteSpace(
                    varianteSeleccionada.Descripcion)
                        ? "Única"
                        : varianteSeleccionada.Descripcion.Trim();

            EtiquetaCodigoBarrasDialog dialogo =
                new EtiquetaCodigoBarrasDialog(
                    nombreProducto,
                    nombreVariante,
                    varianteSeleccionada.CodigoBarras);

            Window ventanaPadre =
                Window.GetWindow(this);

            if (ventanaPadre != null)
            {
                dialogo.Owner =
                    ventanaPadre;
            }

            dialogo.ShowDialog();

            e.Handled = true;
        }
    }
}