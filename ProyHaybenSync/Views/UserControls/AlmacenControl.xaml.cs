using ProySistemaVentas.Models.Almacen;
using ProySistemaVentas.Services.Almacen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProySistemaVentas.Models.Almacen;

namespace ProySistemaVentas.Views.UserControls
{
    public partial class AlmacenControl : UserControl
    {
        private readonly CategoriaService _categoriaService;
        private readonly ProductoInventarioService _productoService;
        private readonly StockService _stockService;

        private readonly ObservableCollection<TipoVariante> _variantes;

        private List<ProductoInventarioListado> _productos;

        private int _idProductoSeleccionado;
        private int _idVarianteSeleccionada;

        public AlmacenControl()
        {
            InitializeComponent();

            _categoriaService = new CategoriaService();
            _productoService = new ProductoInventarioService();
            _stockService = new StockService();

            _variantes = new ObservableCollection<TipoVariante>();
            _productos = new List<ProductoInventarioListado>();

            DgVariantes.ItemsSource = _variantes;

            Loaded += AlmacenControl_Loaded;
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

        private void CargarCategorias()
        {
            var categorias = _categoriaService.ListarCategorias();

            CboCategoria.ItemsSource = categorias;

            var categoriasFiltro = new List<Categoria>
            {
                new Categoria
                {
                    IdCategoria = 0,
                    Nombre = "Todas las categorías"
                }
            };

            categoriasFiltro.AddRange(categorias);

            CboFiltroCategoria.ItemsSource = categoriasFiltro;
            CboFiltroCategoria.DisplayMemberPath = "Nombre";
            CboFiltroCategoria.SelectedValuePath = "IdCategoria";
            CboFiltroCategoria.SelectedIndex = 0;

            if (CboCategoria.Items.Count > 0)
            {
                CboCategoria.SelectedIndex = 0;
            }
        }

        private void CargarProductos()
        {
            _productos =
                _productoService.ListarProductosConVariantes();

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
                    Descripcion = "Única",
                    CodigoBarras = string.Empty,
                    PrecioUnidad = 0,
                    PrecioMayor = 0,
                    Stock = 0
                });

            ActualizarComboVariantes();
        }

        private void ActualizarComboVariantes()
        {
            CboVarianteCodigo.ItemsSource = null;
            CboVarianteCodigo.ItemsSource = _variantes;
            CboVarianteCodigo.DisplayMemberPath = "Descripcion";

            if (_variantes.Count > 0)
            {
                CboVarianteCodigo.SelectedIndex = 0;
            }
        }

        // =========================================================
        // CATEGORÍAS
        // =========================================================

        private void BtnNuevaCategoria_Click(
            object sender,
            RoutedEventArgs e)
        {
            string nombreCategoria =
                Microsoft.VisualBasic.Interaction.InputBox(
                    "Ingrese el nombre de la nueva categoría:",
                    "Nueva categoría",
                    string.Empty);

            nombreCategoria = nombreCategoria.Trim();

            if (string.IsNullOrWhiteSpace(nombreCategoria))
            {
                return;
            }

            try
            {
                _categoriaService.CrearCategoria(nombreCategoria);

                CargarCategorias();

                MessageBox.Show(
                    "La categoría fue creada correctamente.",
                    "Categoría",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
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
            _variantes.Add(
                new TipoVariante
                {
                    Descripcion = "Nueva variante",
                    CodigoBarras = string.Empty,
                    PrecioUnidad = 0,
                    PrecioMayor = 0,
                    Stock = 0
                });

            ActualizarComboVariantes();
        }

        private void CboVarianteCodigo_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (CboVarianteCodigo.SelectedItem
                is TipoVariante variante)
            {
                TxtNumeroCodigo.Text =
                    string.IsNullOrWhiteSpace(variante.CodigoBarras)
                        ? "000000000"
                        : variante.CodigoBarras;
            }
            else
            {
                TxtNumeroCodigo.Text = "000000000";
            }
        }

        // =========================================================
        // GUARDAR PRODUCTO
        // =========================================================

        private void BtnGuardarProducto_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!ValidarFormulario())
            {
                return;
            }

            try
            {
                int idCategoria =
                    Convert.ToInt32(CboCategoria.SelectedValue);

                bool tieneVariantes =
                    _variantes.Count > 1 ||
                    (_variantes.Count == 1 &&
                     !string.Equals(
                         _variantes[0].Descripcion,
                         "Única",
                         StringComparison.OrdinalIgnoreCase));

                if (_idProductoSeleccionado == 0)
                {
                    _productoService.CrearProductoInventario(
                        TxtNombreProducto.Text.Trim(),
                        idCategoria,
                        TxtDescripcion.Text.Trim(),
                        tieneVariantes,
                        _variantes.ToList());

                    MessageBox.Show(
                        "El producto fue registrado correctamente.",
                        "Producto registrado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    _productoService.EditarProductoInventario(
                        _idProductoSeleccionado,
                        TxtNombreProducto.Text.Trim(),
                        idCategoria,
                        TxtDescripcion.Text.Trim(),
                        tieneVariantes,
                        _variantes.ToList());

                    MessageBox.Show(
                        "El producto fue actualizado correctamente.",
                        "Producto actualizado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

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

            if (CboCategoria.SelectedValue == null)
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
                    variante.PrecioMayor < 0 ||
                    variante.Stock < 0)
                {
                    MessageBox.Show(
                        "Los precios y el stock no pueden ser negativos.",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
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

            TxtNombreProducto.Clear();
            TxtDescripcion.Clear();

            if (CboCategoria.Items.Count > 0)
            {
                CboCategoria.SelectedIndex = 0;
            }

            _variantes.Clear();
            InicializarVarianteUnica();

            TxtStockActual.Text = "0";
            TxtIngreso.Text = "0";
            TxtSalida.Text = "0";
            TxtNuevoStock.Text = "0";
            TxtNumeroCodigo.Text = "000000000";

            BtnGuardarProducto.Content =
                "Guardar producto";

            BtnCancelarEdicion.Visibility =
                Visibility.Collapsed;

            DgProductos.SelectedItem = null;
        }

        // =========================================================
        // CÓDIGO DE BARRAS
        // =========================================================

        private void BtnImprimirEtiqueta_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CboVarianteCodigo.SelectedItem
                is not TipoVariante variante)
            {
                MessageBox.Show(
                    "Seleccione una variante.",
                    "Código de barras",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(
                variante.CodigoBarras))
            {
                MessageBox.Show(
                    "La variante seleccionada no tiene código de barras.",
                    "Código de barras",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MessageBox.Show(
                $"Código listo para imprimir:\n\n{variante.CodigoBarras}",
                "Imprimir etiqueta",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================================
        // MOVIMIENTO DE STOCK
        // =========================================================

        private void Stock_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            CalcularNuevoStock();
        }

        private void CalcularNuevoStock()
        {
            if (TxtStockActual == null ||
                TxtIngreso == null ||
                TxtSalida == null ||
                TxtNuevoStock == null)
            {
                return;
            }

            int.TryParse(
                TxtStockActual.Text,
                out int stockActual);

            int.TryParse(
                TxtIngreso.Text,
                out int ingreso);

            int.TryParse(
                TxtSalida.Text,
                out int salida);

            TxtNuevoStock.Text =
                (stockActual + ingreso - salida).ToString();
        }

        private void BtnActualizarStock_Click(object sender, RoutedEventArgs e)
        {
            if (_idProductoSeleccionado <= 0 ||
                _idVarianteSeleccionada <= 0)
            {
                MessageBox.Show(
                    "Seleccione un producto de la lista.",
                    "Movimiento de stock",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!int.TryParse(
                    TxtIngreso.Text,
                    out int ingreso))
            {
                MessageBox.Show(
                    "El ingreso debe ser un número válido.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!int.TryParse(
                    TxtSalida.Text,
                    out int salida))
            {
                MessageBox.Show(
                    "La salida debe ser un número válido.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (ingreso < 0 || salida < 0)
            {
                MessageBox.Show(
                    "El ingreso y la salida no pueden ser negativos.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (ingreso == 0 && salida == 0)
            {
                MessageBox.Show(
                    "Ingrese una cantidad de ingreso o salida.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (ingreso > 0 && salida > 0)
            {
                MessageBox.Show(
                    "Solo puede registrar un ingreso o una salida a la vez.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            int nuevoStock =
                Convert.ToInt32(TxtNuevoStock.Text);

            if (nuevoStock < 0)
            {
                MessageBox.Show(
                    "El nuevo stock no puede ser negativo.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                // MODIFICADO:
                // Antes se calculaba movimiento = ingreso - salida
                // y se enviaban solo 2 parámetros.
                // Ahora se envían los 3 parámetros requeridos por el servicio.
                bool actualizado =
                    _stockService.ActualizarStockAlmacen(
                        _idVarianteSeleccionada,
                        ingreso,
                        salida);

                if (!actualizado)
                {
                    MessageBox.Show(
                        "No se pudo actualizar el stock.",
                        "Movimiento de stock",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                MessageBox.Show(
                    "El stock fue actualizado correctamente.",
                    "Stock actualizado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                TxtStockActual.Text =
                    nuevoStock.ToString();

                TxtIngreso.Text = "0";
                TxtSalida.Text = "0";
                TxtNuevoStock.Text =
                    nuevoStock.ToString();

                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo actualizar el stock.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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

            IEnumerable<ProductoInventarioListado> resultado =
                _productos;

            string texto =
                TxtBuscarProducto.Text.Trim();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                resultado =
                    resultado.Where(
                        p =>
                            ContieneTexto(p.Nombre, texto) ||
                            ContieneTexto(p.CodigoBase, texto) ||
                            ContieneTexto(p.CodigoBarras, texto) ||
                            ContieneTexto(
                                p.VarianteDescripcion,
                                texto));
            }

            if (CboFiltroCategoria.SelectedValue != null &&
                int.TryParse(
                    CboFiltroCategoria.SelectedValue.ToString(),
                    out int idCategoria) &&
                idCategoria > 0)
            {
                resultado =
                    resultado.Where(
                        p => p.IdCategoria == idCategoria);
            }

            List<ProductoInventarioListado> lista =
                resultado.ToList();

            DgProductos.ItemsSource = lista;

            TxtTotalProductos.Text =
                lista.Count == 1
                    ? "1 producto"
                    : $"{lista.Count} productos";
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

        // =========================================================
        // SELECCIÓN DE PRODUCTO
        // =========================================================

        private void DgProductos_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (DgProductos.SelectedItem
                is not ProductoInventarioListado producto)
            {
                return;
            }

            SeleccionarProducto(producto);
        }

        private void SeleccionarProducto(
            ProductoInventarioListado producto)
        {
            _idProductoSeleccionado =
                producto.IdProducto;

            _idVarianteSeleccionada =
                producto.IdVariante;

            TxtNombreProducto.Text =
                producto.Nombre ?? string.Empty;

            TxtDescripcion.Text =
                producto.Descripcion ?? string.Empty;

            CboCategoria.SelectedValue =
                producto.IdCategoria;

            TxtStockActual.Text =
                producto.StockAlmacen.ToString();

            TxtIngreso.Text = "0";
            TxtSalida.Text = "0";

            TxtNumeroCodigo.Text =
                string.IsNullOrWhiteSpace(
                    producto.CodigoBarras)
                    ? "000000000"
                    : producto.CodigoBarras;

            BtnGuardarProducto.Content =
                "Actualizar producto";

            BtnCancelarEdicion.Visibility =
                Visibility.Visible;
        }
    }
}