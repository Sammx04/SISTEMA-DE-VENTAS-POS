using ProySistemaVentas.Models.Almacen;
using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Models.Reporte;
using ProySistemaVentas.Services.Reporte;
using ProySistemaVentas.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProySistemaVentas.Views.UserControls
{
    /// <summary>
    /// Pantalla de reportes de SistemaVentas.
    ///
    /// Un solo control muestra los diez reportes: el
    /// combo superior elige cual es, se muestran solo los
    /// filtros que ese reporte necesita y el DataGrid se
    /// arma por codigo segun el tipo elegido.
    ///
    /// Todos los datos salen de los procedimientos
    /// sp_Reporte_* definidos en
    /// SQL/sp_reportes_SistemaVentas.sql.
    /// </summary>
    public partial class ReportesControl : UserControl
    {
        private enum TipoReporte
        {
            VentasPorDia,
            DetalleVentas,
            DetalleProductos,
            ProductosMasVendidos,
            VentasPorUsuario,
            VentasPorCategoria,
            VentasPorTipoPrecio,
            StockActual,
            ValorizacionInventario,
            MovimientosStock
        }

        /// <summary>
        /// Opcion del combo de reportes. Ademas del tipo,
        /// declara que filtros necesita cada reporte para
        /// no mostrar los que no aplican.
        /// </summary>
        private class ReporteOpcion
        {
            public TipoReporte Tipo { get; set; }

            public string Descripcion { get; set; } = string.Empty;

            public bool UsaPeriodo { get; set; }

            public bool UsaUsuario { get; set; }

            public bool UsaMetodoPago { get; set; }

            public bool UsaCategoria { get; set; }

            public bool UsaTopN { get; set; }

            public bool UsaUbicacion { get; set; }

            public bool UsaEstadoStock { get; set; }

            public bool UsaBuscar { get; set; }
        }

        /// <summary>
        /// Item de los combos de filtro. Valor puede ser
        /// null, que el servicio traduce a NULL y el SP
        /// interpreta como "sin filtro".
        /// </summary>
        private class FiltroTexto
        {
            public string? Valor { get; set; }

            public string Texto { get; set; } = string.Empty;

            public override string ToString() => Texto;
        }

        private readonly ReporteService _servicio = new ReporteService();

        private readonly ObservableCollection<object> _filas = new ObservableCollection<object>();

        private readonly List<ReporteOpcion> _opciones = new List<ReporteOpcion>();

        private TablaReporte _tablaActual = new TablaReporte();

        private ReporteTotales _totales = new ReporteTotales();

        private ReporteTotalesLineas _totalesLineas = new ReporteTotalesLineas();

        private ReporteOpcion? _opcionActual;

        public ReportesControl()
        {
            InitializeComponent();

            CargarOpciones();
            CargarFiltros();

            AplicarVisibilidadFiltros();

            dgReporte.ItemsSource = _filas;
        }

        // =====================================================
        // CARGA INICIAL
        // =====================================================

        private void CargarOpciones()
        {
            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.VentasPorDia,
                Descripcion = "Ventas por dia",
                UsaPeriodo = true,
                UsaUsuario = true,
                UsaMetodoPago = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.DetalleVentas,
                Descripcion = "Detalle de ventas",
                UsaPeriodo = true,
                UsaUsuario = true,
                UsaMetodoPago = true,
                UsaBuscar = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.DetalleProductos,
                Descripcion = "Detalle de productos vendidos",
                UsaPeriodo = true,
                UsaUsuario = true,
                UsaMetodoPago = true,
                UsaCategoria = true,
                UsaBuscar = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.ProductosMasVendidos,
                Descripcion = "Productos mas vendidos",
                UsaPeriodo = true,
                UsaCategoria = true,
                UsaTopN = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.VentasPorUsuario,
                Descripcion = "Ventas por usuario",
                UsaPeriodo = true,
                UsaUsuario = true,
                UsaMetodoPago = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.VentasPorCategoria,
                Descripcion = "Ventas por categoria",
                UsaPeriodo = true,
                UsaCategoria = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.VentasPorTipoPrecio,
                Descripcion = "Ventas por tipo de precio",
                UsaPeriodo = true,
                UsaCategoria = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.StockActual,
                Descripcion = "Stock actual",
                UsaCategoria = true,
                UsaUbicacion = true,
                UsaEstadoStock = true,
                UsaBuscar = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.ValorizacionInventario,
                Descripcion = "Valorizacion de inventario",
                UsaCategoria = true,
                UsaUbicacion = true
            });

            _opciones.Add(new ReporteOpcion
            {
                Tipo = TipoReporte.MovimientosStock,
                Descripcion = "Movimientos de stock (kardex)",
                UsaPeriodo = true,
                UsaUsuario = true,
                UsaBuscar = true
            });

            cmbTipoReporte.ItemsSource = _opciones;
            cmbTipoReporte.DisplayMemberPath = nameof(ReporteOpcion.Descripcion);
            cmbTipoReporte.SelectedIndex = 0;
        }

        private void CargarFiltros()
        {
            DateTime hoy = DateTime.Today;

            txtDesde.Text = hoy.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            txtHasta.Text = hoy.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            CargarFiltroMetodosPago();
            CargarFiltroUsuarios();
            CargarFiltroCategorias();

            cmbTopN.ItemsSource = new List<FiltroTexto>
            {
                new FiltroTexto { Texto = "Top 10", Valor = "10" },
                new FiltroTexto { Texto = "Top 20", Valor = "20" },
                new FiltroTexto { Texto = "Top 50", Valor = "50" },
                new FiltroTexto { Texto = "Top 100", Valor = "100" },
                new FiltroTexto { Texto = "Top 200", Valor = "200" }
            };

            cmbTopN.SelectedIndex = 2;

            cmbUbicacion.ItemsSource = new List<FiltroTexto>
            {
                new FiltroTexto { Texto = "Todas" },
                new FiltroTexto { Texto = "Almacen", Valor = "ALMACEN" },
                new FiltroTexto { Texto = "Tienda", Valor = "TIENDA" }
            };

            cmbUbicacion.SelectedIndex = 0;

            cmbEstadoStock.ItemsSource = new List<FiltroTexto>
            {
                new FiltroTexto { Texto = "Todos" },
                new FiltroTexto { Texto = "Agotado", Valor = "AGOTADO" },
                new FiltroTexto { Texto = "Stock bajo", Valor = "BAJO" },
                new FiltroTexto { Texto = "En stock", Valor = "NORMAL" }
            };

            cmbEstadoStock.SelectedIndex = 0;
        }

        private void CargarFiltroMetodosPago()
        {
            List<FiltroTexto> items = new List<FiltroTexto>
            {
                new FiltroTexto { Texto = "Todos" }
            };

            try
            {
                foreach (
                    string metodo
                    in _servicio.ObtenerMetodosPago())
                {
                    items.Add(
                        new FiltroTexto
                        {
                            Texto = metodo,
                            Valor = metodo
                        });
                }
            }
            catch (Exception ex)
            {
                MostrarError(
                    "No se pudieron cargar los metodos de pago",
                    ex.Message);
            }

            cmbMetodoPago.ItemsSource = items;
            cmbMetodoPago.SelectedIndex = 0;
        }

        private void CargarFiltroUsuarios()
        {
            List<UsuarioMovimiento> items =
                new List<UsuarioMovimiento>
                {
                    new UsuarioMovimiento
                    {
                        IdUsuario = 0,
                        Nombre = "Todos los usuarios",
                        Usuario = string.Empty,
                        Rol = string.Empty
                    }
                };

            try
            {
                items.AddRange(
                    _servicio.ListarUsuarios());
            }
            catch (Exception ex)
            {
                MostrarError(
                    "No se pudieron cargar los usuarios",
                    ex.Message);
            }

            cmbUsuario.ItemsSource = items;
            cmbUsuario.SelectedIndex = 0;
        }

        private void CargarFiltroCategorias()
        {
            List<Categoria> items =
                new List<Categoria>
                {
                    new Categoria
                    {
                        IdCategoria = 0,
                        Nombre = "Todas las categorias"
                    }
                };

            try
            {
                items.AddRange(
                    _servicio.ListarCategorias());
            }
            catch (Exception ex)
            {
                MostrarError(
                    "No se pudieron cargar las categorias",
                    ex.Message);
            }

            cmbCategoria.ItemsSource = items;
            cmbCategoria.SelectedIndex = 0;
        }

        // =====================================================
        // FILTROS
        // =====================================================

        private void cmbTipoReporte_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            _opcionActual =
                cmbTipoReporte.SelectedItem as ReporteOpcion;

            if (_opcionActual == null)
            {
                return;
            }

            LimpiarResultados();

            AplicarVisibilidadFiltros();
        }

        private void AplicarVisibilidadFiltros()
        {
            bool periodo = _opcionActual?.UsaPeriodo ?? false;
            bool usuario = _opcionActual?.UsaUsuario ?? false;
            bool metodoPago = _opcionActual?.UsaMetodoPago ?? false;
            bool categoria = _opcionActual?.UsaCategoria ?? false;
            bool topN = _opcionActual?.UsaTopN ?? false;
            bool ubicacion = _opcionActual?.UsaUbicacion ?? false;
            bool estadoStock = _opcionActual?.UsaEstadoStock ?? false;
            bool buscar = _opcionActual?.UsaBuscar ?? false;

            pnlDesde.Visibility = periodo ? Visibility.Visible : Visibility.Collapsed;
            pnlHasta.Visibility = periodo ? Visibility.Visible : Visibility.Collapsed;
            cmbUsuario.Visibility = usuario ? Visibility.Visible : Visibility.Collapsed;

            StackPanel? contenedorUsuario =
                pnlUsuario;

            if (contenedorUsuario != null)
            {
                contenedorUsuario.Visibility = usuario ? Visibility.Visible : Visibility.Collapsed;
            }

            pnlMetodoPago.Visibility = metodoPago ? Visibility.Visible : Visibility.Collapsed;
            pnlCategoria.Visibility = categoria ? Visibility.Visible : Visibility.Collapsed;
            pnlTopN.Visibility = topN ? Visibility.Visible : Visibility.Collapsed;
            pnlUbicacion.Visibility = ubicacion ? Visibility.Visible : Visibility.Collapsed;
            pnlEstadoStock.Visibility = estadoStock ? Visibility.Visible : Visibility.Collapsed;
            pnlBuscar.Visibility = buscar ? Visibility.Visible : Visibility.Collapsed;

            txtAvisoKardex.Visibility =
                _opcionActual?.Tipo == TipoReporte.MovimientosStock
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private FiltroReportePeriodo
            CrearFiltroPeriodo()
        {
            DateTime desde =
                LeerFecha(
                    txtDesde.Text,
                    DateTime.Today);

            DateTime hasta =
                LeerFecha(
                    txtHasta.Text,
                    DateTime.Today);

            return new FiltroReportePeriodo
            {
                FechaDesde = desde,
                FechaHasta = hasta,
                IdUsuario = ObtenerIdUsuario(),
                MetodoPago = ObtenerValorFiltro(cmbMetodoPago),
                Texto = ObtenerTextoBusqueda()
            };
        }

        private FiltroReporteProductos
            CrearFiltroProductos()
        {
            FiltroReportePeriodo baseFiltro =
                CrearFiltroPeriodo();

            FiltroReporteProductos filtro =
                new FiltroReporteProductos
                {
                    FechaDesde = baseFiltro.FechaDesde,
                    FechaHasta = baseFiltro.FechaHasta,
                    IdUsuario = baseFiltro.IdUsuario,
                    MetodoPago = baseFiltro.MetodoPago,
                    Texto = baseFiltro.Texto,
                    IdCategoria = ObtenerIdCategoria(),
                    TopN = ObtenerTopN()
                };

            return filtro;
        }

        private FiltroReporteInventario
            CrearFiltroInventario()
        {
            return new FiltroReporteInventario
            {
                IdCategoria = ObtenerIdCategoria(),
                TipoUbicacion = ObtenerValorFiltro(cmbUbicacion),
                EstadoStock = ObtenerValorFiltro(cmbEstadoStock),
                Texto = ObtenerTextoBusqueda()
            };
        }

        private int? ObtenerIdUsuario()
        {
            UsuarioMovimiento? usuario =
                cmbUsuario.SelectedItem as UsuarioMovimiento;

            if (usuario == null ||
                usuario.IdUsuario == 0)
            {
                return null;
            }

            return usuario.IdUsuario;
        }

        private int? ObtenerIdCategoria()
        {
            Categoria? categoria =
                cmbCategoria.SelectedItem as Categoria;

            if (categoria == null ||
                categoria.IdCategoria == 0)
            {
                return null;
            }

            return categoria.IdCategoria;
        }

        private int ObtenerTopN()
        {
            FiltroTexto? opcion =
                cmbTopN.SelectedItem as FiltroTexto;

            if (opcion?.Valor == null)
            {
                return 50;
            }

            return int.TryParse(
                opcion.Valor,
                out int topN)
                    ? topN
                    : 50;
        }

        private string? ObtenerValorFiltro(ComboBox combo)
        {
            FiltroTexto? opcion =
                combo.SelectedItem as FiltroTexto;

            return opcion?.Valor;
        }

        private string? ObtenerTextoBusqueda()
        {
            return string.IsNullOrWhiteSpace(txtBuscar.Text)
                ? null
                : txtBuscar.Text.Trim();
        }

        private static DateTime LeerFecha(
            string texto,
            DateTime porDefecto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return porDefecto;
            }

            string[] formatos =
            {
                "dd/MM/yyyy",
                "dd/MM/yyyy HH:mm",
                "d/M/yyyy",
                "yyyy-MM-dd"
            };

            if (DateTime.TryParseExact(
                texto.Trim(),
                formatos,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fecha))
            {
                return fecha;
            }

            if (DateTime.TryParse(
                texto.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fecha))
            {
                return fecha;
            }

            return porDefecto;
        }

        private void txtDesde_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            NormalizarCampoFecha(txtDesde);
        }

        private void txtHasta_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            NormalizarCampoFecha(txtHasta);
        }

        private static void NormalizarCampoFecha(TextBox campo)
        {
            DateTime fecha =
                LeerFecha(
                    campo.Text,
                    DateTime.MinValue);

            if (fecha == DateTime.MinValue)
            {
                campo.Text = string.Empty;
                return;
            }

            campo.Text = fecha.ToString(
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture);
        }

        private void txtBuscar_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            GenerarReporte();

            e.Handled = true;
        }

        // =====================================================
        // GENERACIÓN DEL REPORTE
        // =====================================================

        private void btnGenerar_Click(
            object sender,
            RoutedEventArgs e)
        {
            GenerarReporte();
        }

        private void GenerarReporte()
        {
            if (_opcionActual == null)
            {
                return;
            }

            _filas.Clear();
            _tablaActual = new TablaReporte();
            _totales = new ReporteTotales();
            _totalesLineas = new ReporteTotalesLineas();

            try
            {
                switch (_opcionActual.Tipo)
                {
                    case TipoReporte.VentasPorDia:
                        GenerarVentasPorDia();
                        break;

                    case TipoReporte.DetalleVentas:
                        GenerarDetalleVentas();
                        break;

                    case TipoReporte.DetalleProductos:
                        GenerarDetalleProductos();
                        break;

                    case TipoReporte.ProductosMasVendidos:
                        GenerarProductosMasVendidos();
                        break;

                    case TipoReporte.VentasPorUsuario:
                        GenerarVentasPorUsuario();
                        break;

                    case TipoReporte.VentasPorCategoria:
                        GenerarVentasPorCategoria();
                        break;

                    case TipoReporte.VentasPorTipoPrecio:
                        GenerarVentasPorTipoPrecio();
                        break;

                    case TipoReporte.StockActual:
                        GenerarStockActual();
                        break;

                    case TipoReporte.ValorizacionInventario:
                        GenerarValorizacion();
                        break;

                    case TipoReporte.MovimientosStock:
                        GenerarMovimientosStock();
                        break;
                }
            }
            catch (Exception ex)
            {
                _filas.Clear();
                _tablaActual = new TablaReporte();

                MostrarError(
                    "No se pudo generar el reporte",
                    $"{ex.Message}\n\n" +
                    "Verifica que los procedimientos sp_Reporte_* " +
                    "estén creados en la base SistemaVentas.");
            }

            dgReporte.ItemsSource = _filas;

            ActualizarIndicadores();

            txtConteoFilas.Text =
                $"{_filas.Count} " +
                $"{(_filas.Count == 1 ? "registro" : "registros")}";

            txtSinDatos.Visibility =
                _filas.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            txtSinDatos.Text =
                _filas.Count == 0
                    ? "No hay datos para los filtros seleccionados."
                    : string.Empty;

            btnExportarCsv.IsEnabled = _filas.Count > 0;
            btnImprimir.IsEnabled = _filas.Count > 0;
        }

        private void LimpiarResultados()
        {
            _filas.Clear();
            _tablaActual = new TablaReporte();
            _totales = new ReporteTotales();
            _totalesLineas = new ReporteTotalesLineas();

            dgReporte.ItemsSource = _filas;

            dgReporte.Columns.Clear();

            txtConteoFilas.Text = string.Empty;
            txtResumenPago.Text = string.Empty;

            txtSinDatos.Text = "Selecciona un reporte y pulsa Generar.";
            txtSinDatos.Visibility = Visibility.Visible;

            btnExportarCsv.IsEnabled = false;
            btnImprimir.IsEnabled = false;

            ActualizarIndicadores();
        }

        // ---------- Reportes de ventas ----------

        private void GenerarVentasPorDia()
        {
            List<ReporteVenta> datos =
                _servicio.ObtenerVentasPorDia(
                    CrearFiltroPeriodo());

            foreach (
                ReporteVenta fila
                in datos)
            {
                _filas.Add(fila);
            }

            _totales = new ReporteTotales
            {
                CantidadVentas = datos.Sum(x => x.CantidadVentas),
                UnidadesVendidas = datos.Sum(x => x.UnidadesVendidas),
                Subtotal = datos.Sum(x => x.Subtotal),
                RecargoTarjeta = datos.Sum(x => x.RecargoTarjeta),
                Total = datos.Sum(x => x.Total),
                TotalEfectivo = datos.Sum(x => x.TotalEfectivo),
                TotalYapePlin = datos.Sum(x => x.TotalYapePlin),
                TotalTarjeta = datos.Sum(x => x.TotalTarjeta),
                TotalOtros = datos.Sum(x => x.TotalOtros)
            };

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE VENTAS POR DIA",
                Subtitulo = DescribirFiltros(),
                Pie = ResumenTotales()
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Fecha",
                "Ventas",
                "Unidades",
                "Subtotal",
                "Recargo tarjeta",
                "Total",
                "Efectivo",
                "Yape / Plin",
                "Tarjeta"
            });

            foreach (
                ReporteVenta fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.FechaTexto,
                    ReporteExportService.Entero(fila.CantidadVentas),
                    ReporteExportService.Entero(fila.UnidadesVendidas),
                    ReporteExportService.Moneda(fila.Subtotal),
                    ReporteExportService.Moneda(fila.RecargoTarjeta),
                    ReporteExportService.Moneda(fila.Total),
                    ReporteExportService.Moneda(fila.TotalEfectivo),
                    ReporteExportService.Moneda(fila.TotalYapePlin),
                    ReporteExportService.Moneda(fila.TotalTarjeta)
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("FECHA", nameof(ReporteVenta.FechaTexto), 110),
                new ColumnaReporte("VENTAS", nameof(ReporteVenta.CantidadVentas), 90, "Derecha"),
                new ColumnaReporte("UNIDADES", nameof(ReporteVenta.UnidadesVendidas), 100, "Derecha"),
                new ColumnaReporte("SUBTOTAL", nameof(ReporteVenta.SubtotalTexto), 120, "Derecha"),
                new ColumnaReporte("RECARGO TARJETA 5%", nameof(ReporteVenta.RecargoTarjetaTexto), 160, "Derecha"),
                new ColumnaReporte("TOTAL", nameof(ReporteVenta.TotalTexto), 130, "Derecha"),
                new ColumnaReporte("EFECTIVO", nameof(ReporteVenta.TotalEfectivoTexto), 120, "Derecha"),
                new ColumnaReporte("YAPE / PLIN", nameof(ReporteVenta.TotalYapePlinTexto), 120, "Derecha"),
                new ColumnaReporte("TARJETA", nameof(ReporteVenta.TotalTarjetaTexto), 120, "Derecha")
            });

            MostrarResumenPago();
        }

        private void GenerarDetalleVentas()
        {
            List<VentaReporteDetalle> datos =
                _servicio.ObtenerDetalleVentas(
                    CrearFiltroPeriodo(),
                    out ReporteTotales totales);

            foreach (
                VentaReporteDetalle fila
                in datos)
            {
                _filas.Add(fila);
            }

            _totales = totales;

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE DETALLE DE VENTAS",
                Subtitulo = DescribirFiltros(),
                Pie = ResumenTotales()
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Venta",
                "Fecha",
                "Usuario",
                "Rol",
                "Documento",
                "Cliente",
                "Metodo de pago",
                "Unid.",
                "Subtotal",
                "Recargo tarjeta",
                "Total"
            });

            foreach (
                VentaReporteDetalle fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.NumeroVenta,
                    fila.FechaVentaTexto,
                    fila.Usuario,
                    fila.Rol,
                    string.IsNullOrWhiteSpace(fila.NumeroDocumento)
                        ? fila.TipoDocumento
                        : fila.NumeroDocumento,
                    fila.Cliente,
                    fila.MetodoPago,
                    ReporteExportService.Entero(fila.Unidades),
                    ReporteExportService.Moneda(fila.Subtotal),
                    ReporteExportService.Moneda(fila.RecargoTarjeta),
                    ReporteExportService.Moneda(fila.Total)
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("VENTA", nameof(VentaReporteDetalle.NumeroVenta), 100),
                new ColumnaReporte("FECHA", nameof(VentaReporteDetalle.FechaVentaTexto), 150),
                new ColumnaReporte("USUARIO", nameof(VentaReporteDetalle.Usuario), "*"),
                new ColumnaReporte("ROL", nameof(VentaReporteDetalle.Rol), 130),
                new ColumnaReporte("DOCUMENTO", nameof(VentaReporteDetalle.NumeroDocumento), 130),
                new ColumnaReporte("CLIENTE", nameof(VentaReporteDetalle.Cliente), 200),
                new ColumnaReporte("METODO DE PAGO", nameof(VentaReporteDetalle.MetodoPago), 130),
                new ColumnaReporte("UNID.", nameof(VentaReporteDetalle.Unidades), 70, "Derecha"),
                new ColumnaReporte("SUBTOTAL", nameof(VentaReporteDetalle.SubtotalTexto), 110, "Derecha"),
                new ColumnaReporte("RECARGO 5%", nameof(VentaReporteDetalle.RecargoTarjetaTexto), 120, "Derecha"),
                new ColumnaReporte("TOTAL", nameof(VentaReporteDetalle.TotalTexto), 120, "Derecha")
            });

            MostrarResumenPago();
        }

        private void GenerarDetalleProductos()
        {
            FiltroReporteProductos filtro =
                CrearFiltroProductos();

            List<LineaVentaReporte> datos =
                _servicio.ObtenerLineasVenta(
                    filtro,
                    out ReporteTotalesLineas totales);

            foreach (
                LineaVentaReporte fila
                in datos)
            {
                _filas.Add(fila);
            }

            _totalesLineas = totales;

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE PRODUCTOS VENDIDOS (POR LINEA)",
                Subtitulo = DescribirFiltros(),
                Pie =
                    $"Lineas: {totales.CantidadLineasTexto}   " +
                    $"Unidades: {totales.UnidadesVendidasTexto}   " +
                    $"Descuentos: {totales.DescuentosTexto}   " +
                    $"Total generado: {totales.TotalGeneradoTexto}"
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Venta",
                "Fecha",
                "Usuario",
                "Producto",
                "Variante",
                "Categoria",
                "Codigo de barras",
                "Tipo precio",
                "P. unitario",
                "Cant.",
                "Descuento",
                "Total linea"
            });

            foreach (
                LineaVentaReporte fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.NumeroVenta,
                    fila.FechaVentaTexto,
                    fila.Usuario,
                    fila.NombreProducto,
                    fila.Variante,
                    fila.Categoria,
                    fila.CodigoBarra,
                    fila.TipoPrecio,
                    ReporteExportService.Moneda(fila.PrecioUnitario),
                    ReporteExportService.Entero(fila.Cantidad),
                    ReporteExportService.Moneda(fila.Descuento),
                    ReporteExportService.Moneda(fila.TotalLinea)
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("VENTA", nameof(LineaVentaReporte.NumeroVenta), 90),
                new ColumnaReporte("FECHA", nameof(LineaVentaReporte.FechaVentaTexto), 140),
                new ColumnaReporte("USUARIO", nameof(LineaVentaReporte.Usuario), 150),
                new ColumnaReporte("PRODUCTO", nameof(LineaVentaReporte.NombreProducto), 200),
                new ColumnaReporte("VARIANTE", nameof(LineaVentaReporte.Variante), 130),
                new ColumnaReporte("CATEGORIA", nameof(LineaVentaReporte.Categoria), 140),
                new ColumnaReporte("COD. BARRAS", nameof(LineaVentaReporte.CodigoBarra), 110),
                new ColumnaReporte("TIPO PRECIO", nameof(LineaVentaReporte.TipoPrecio), 110),
                new ColumnaReporte("P. UNITARIO", nameof(LineaVentaReporte.PrecioUnitarioTexto), 110, "Derecha"),
                new ColumnaReporte("CANT.", nameof(LineaVentaReporte.Cantidad), 70, "Derecha"),
                new ColumnaReporte("DESCUENTO", nameof(LineaVentaReporte.DescuentoTexto), 110, "Derecha"),
                new ColumnaReporte("TOTAL LINEA", nameof(LineaVentaReporte.TotalLineaTexto), 120, "Derecha")
            });

            txtIndicador1.Text = totales.CantidadLineasTexto;
        }

        private void GenerarProductosMasVendidos()
        {
            FiltroReporteProductos filtro =
                CrearFiltroProductos();

            List<ProductoMasVendido> datos =
                _servicio.ObtenerProductosMasVendidos(
                    filtro);

            foreach (
                ProductoMasVendido fila
                in datos)
            {
                _filas.Add(fila);
            }

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE PRODUCTOS MAS VENDIDOS",
                Subtitulo = DescribirFiltros(),
                Pie =
                    $"Productos en el ranking: {datos.Count}   " +
                    $"Unidades vendidas: " +
                    $"{ReporteExportService.Entero(datos.Sum(x => x.CantidadVendida))}   " +
                    $"Total generado: " +
                    $"{ReporteExportService.Moneda(datos.Sum(x => x.TotalGenerado))}"
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Producto",
                "Variante",
                "Categoria",
                "Codigo de barras",
                "Unidades",
                "Ventas",
                "Total generado",
                "Descuentos",
                "Precio promedio",
                "Ultima venta"
            });

            foreach (
                ProductoMasVendido fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.NombreProducto,
                    fila.Variante,
                    fila.Categoria,
                    fila.CodigoBarra,
                    ReporteExportService.Entero(fila.CantidadVendida),
                    ReporteExportService.Entero(fila.NumeroVentas),
                    ReporteExportService.Moneda(fila.TotalGenerado),
                    ReporteExportService.Moneda(fila.Descuentos),
                    ReporteExportService.Moneda(fila.PrecioPromedio),
                    fila.UltimaVentaTexto
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("PRODUCTO", nameof(ProductoMasVendido.NombreProducto), 220),
                new ColumnaReporte("VARIANTE", nameof(ProductoMasVendido.Variante), 130),
                new ColumnaReporte("CATEGORIA", nameof(ProductoMasVendido.Categoria), 150),
                new ColumnaReporte("COD. BARRAS", nameof(ProductoMasVendido.CodigoBarra), 110),
                new ColumnaReporte("UNIDADES", nameof(ProductoMasVendido.CantidadVendida), 100, "Derecha"),
                new ColumnaReporte("VENTAS", nameof(ProductoMasVendido.NumeroVentas), 90, "Derecha"),
                new ColumnaReporte("TOTAL GENERADO", nameof(ProductoMasVendido.TotalGeneradoTexto), 140, "Derecha"),
                new ColumnaReporte("DESCUENTOS", nameof(ProductoMasVendido.DescuentosTexto), 120, "Derecha"),
                new ColumnaReporte("PRECIO PROMEDIO", nameof(ProductoMasVendido.PrecioPromedioTexto), 140, "Derecha"),
                new ColumnaReporte("ULTIMA VENTA", nameof(ProductoMasVendido.UltimaVentaTexto), 120)
            });
        }

        private void GenerarVentasPorUsuario()
        {
            List<VentaPorUsuarioReporte> datos =
                _servicio.ObtenerVentasPorUsuario(
                    CrearFiltroPeriodo());

            foreach (
                VentaPorUsuarioReporte fila
                in datos)
            {
                _filas.Add(fila);
            }

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE VENTAS POR USUARIO",
                Subtitulo = DescribirFiltros(),
                Pie =
                    $"Vendedores: {datos.Count}   " +
                    $"Ventas: {ReporteExportService.Entero(datos.Sum(x => x.CantidadVentas))}   " +
                    $"Total: {ReporteExportService.Moneda(datos.Sum(x => x.Total))}"
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Usuario",
                "Rol",
                "Ventas",
                "Unidades",
                "Subtotal",
                "Recargo tarjeta",
                "Total",
                "Ticket promedio",
                "Efectivo",
                "Yape / Plin",
                "Tarjeta"
            });

            foreach (
                VentaPorUsuarioReporte fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.Usuario,
                    fila.Rol,
                    ReporteExportService.Entero(fila.CantidadVentas),
                    ReporteExportService.Entero(fila.UnidadesVendidas),
                    ReporteExportService.Moneda(fila.Subtotal),
                    ReporteExportService.Moneda(fila.RecargoTarjeta),
                    ReporteExportService.Moneda(fila.Total),
                    ReporteExportService.Moneda(fila.TicketPromedio),
                    ReporteExportService.Moneda(fila.TotalEfectivo),
                    ReporteExportService.Moneda(fila.TotalYapePlin),
                    ReporteExportService.Moneda(fila.TotalTarjeta)
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("USUARIO", nameof(VentaPorUsuarioReporte.Usuario), 200),
                new ColumnaReporte("ROL", nameof(VentaPorUsuarioReporte.Rol), 130),
                new ColumnaReporte("VENTAS", nameof(VentaPorUsuarioReporte.CantidadVentas), 90, "Derecha"),
                new ColumnaReporte("UNIDADES", nameof(VentaPorUsuarioReporte.UnidadesVendidas), 100, "Derecha"),
                new ColumnaReporte("SUBTOTAL", nameof(VentaPorUsuarioReporte.SubtotalTexto), 120, "Derecha"),
                new ColumnaReporte("RECARGO 5%", nameof(VentaPorUsuarioReporte.RecargoTarjetaTexto), 130, "Derecha"),
                new ColumnaReporte("TOTAL", nameof(VentaPorUsuarioReporte.TotalTexto), 130, "Derecha"),
                new ColumnaReporte("TICKET PROMEDIO", nameof(VentaPorUsuarioReporte.TicketPromedioTexto), 150, "Derecha"),
                new ColumnaReporte("EFECTIVO", nameof(VentaPorUsuarioReporte.TotalEfectivoTexto), 120, "Derecha"),
                new ColumnaReporte("YAPE / PLIN", nameof(VentaPorUsuarioReporte.TotalYapePlinTexto), 120, "Derecha"),
                new ColumnaReporte("TARJETA", nameof(VentaPorUsuarioReporte.TotalTarjetaTexto), 120, "Derecha")
            });
        }

        private void GenerarVentasPorCategoria()
        {
            FiltroReporteProductos filtro =
                CrearFiltroProductos();

            List<VentaPorCategoriaReporte> datos =
                _servicio.ObtenerVentasPorCategoria(
                    filtro);

            decimal totalGeneral =
                datos.Sum(x => x.TotalGenerado);

            foreach (
                VentaPorCategoriaReporte fila
                in datos)
            {
                fila.Participacion =
                    totalGeneral == 0
                        ? 0m
                        : Math.Round(
                            fila.TotalGenerado / totalGeneral * 100m,
                            1,
                            MidpointRounding.AwayFromZero);

                _filas.Add(fila);
            }

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE VENTAS POR CATEGORIA",
                Subtitulo = DescribirFiltros(),
                Pie =
                    $"Categorias: {datos.Count}   " +
                    $"Unidades: {ReporteExportService.Entero(datos.Sum(x => x.CantidadVendida))}   " +
                    $"Total generado: {ReporteExportService.Moneda(totalGeneral)}"
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Categoria",
                "Ventas",
                "Productos",
                "Unidades",
                "Total generado",
                "Descuentos",
                "Participacion"
            });

            foreach (
                VentaPorCategoriaReporte fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.Categoria,
                    ReporteExportService.Entero(fila.NumeroVentas),
                    ReporteExportService.Entero(fila.Productos),
                    ReporteExportService.Entero(fila.CantidadVendida),
                    ReporteExportService.Moneda(fila.TotalGenerado),
                    ReporteExportService.Moneda(fila.Descuentos),
                    fila.ParticipacionTexto
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("CATEGORIA", nameof(VentaPorCategoriaReporte.Categoria), "*"),
                new ColumnaReporte("VENTAS", nameof(VentaPorCategoriaReporte.NumeroVentas), 100, "Derecha"),
                new ColumnaReporte("PRODUCTOS", nameof(VentaPorCategoriaReporte.Productos), 110, "Derecha"),
                new ColumnaReporte("UNIDADES", nameof(VentaPorCategoriaReporte.CantidadVendida), 110, "Derecha"),
                new ColumnaReporte("TOTAL GENERADO", nameof(VentaPorCategoriaReporte.TotalGeneradoTexto), 150, "Derecha"),
                new ColumnaReporte("DESCUENTOS", nameof(VentaPorCategoriaReporte.DescuentosTexto), 130, "Derecha"),
                new ColumnaReporte("PARTICIPACION", nameof(VentaPorCategoriaReporte.ParticipacionTexto), 130, "Derecha")
            });
        }

        private void GenerarVentasPorTipoPrecio()
        {
            FiltroReporteProductos filtro =
                CrearFiltroProductos();

            List<VentaPorTipoPrecioReporte> datos =
                _servicio.ObtenerVentasPorTipoPrecio(
                    filtro);

            foreach (
                VentaPorTipoPrecioReporte fila
                in datos)
            {
                _filas.Add(fila);
            }

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE VENTAS POR TIPO DE PRECIO",
                Subtitulo = DescribirFiltros(),
                Pie =
                    $"Unidades: {ReporteExportService.Entero(datos.Sum(x => x.Cantidad))}   " +
                    $"Total generado: {ReporteExportService.Moneda(datos.Sum(x => x.TotalGenerado))}"
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Tipo de precio",
                "Unidades",
                "Ventas",
                "Total generado"
            });

            foreach (
                VentaPorTipoPrecioReporte fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.TipoPrecioDescripcion,
                    ReporteExportService.Entero(fila.Cantidad),
                    ReporteExportService.Entero(fila.NumeroVentas),
                    ReporteExportService.Moneda(fila.TotalGenerado)
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("TIPO DE PRECIO", nameof(VentaPorTipoPrecioReporte.TipoPrecioDescripcion), "*"),
                new ColumnaReporte("UNIDADES", nameof(VentaPorTipoPrecioReporte.Cantidad), 140, "Derecha"),
                new ColumnaReporte("VENTAS", nameof(VentaPorTipoPrecioReporte.NumeroVentas), 140, "Derecha"),
                new ColumnaReporte("TOTAL GENERADO", nameof(VentaPorTipoPrecioReporte.TotalGeneradoTexto), 180, "Derecha")
            });
        }

        // ---------- Reportes de inventario ----------

        private void GenerarStockActual()
        {
            List<InventarioReporte> datos =
                _servicio.ObtenerStockActual(
                    CrearFiltroInventario());

            foreach (
                InventarioReporte fila
                in datos)
            {
                _filas.Add(fila);
            }

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE STOCK ACTUAL",
                Subtitulo = DescribirFiltros(),
                Pie =
                    $"Variantes: {datos.Count}   " +
                    $"Unidades almacen: {ReporteExportService.Entero(datos.Sum(x => x.StockAlmacen))}   " +
                    $"Unidades tienda: {ReporteExportService.Entero(datos.Sum(x => x.StockTienda))}   " +
                    $"Valor en tienda: {ReporteExportService.Moneda(datos.Sum(x => x.ValorVenta))}"
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Codigo base",
                "Codigo de barras",
                "Producto",
                "Variante",
                "Categoria",
                "P. unidad",
                "P. mayor",
                "Almacen",
                "Tienda",
                "Total",
                "Valor tienda",
                "Estado"
            });

            foreach (
                InventarioReporte fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.CodigoBase,
                    fila.CodigoBarras,
                    fila.NombreProducto,
                    fila.Variante,
                    fila.Categoria,
                    ReporteExportService.Moneda(fila.PrecioUnidad),
                    ReporteExportService.Moneda(fila.PrecioMayor),
                    ReporteExportService.Entero(fila.StockAlmacen),
                    ReporteExportService.Entero(fila.StockTienda),
                    ReporteExportService.Entero(fila.StockTotal),
                    ReporteExportService.Moneda(fila.ValorVenta),
                    fila.EstadoTienda
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("COD. BASE", nameof(InventarioReporte.CodigoBase), 100),
                new ColumnaReporte("COD. BARRAS", nameof(InventarioReporte.CodigoBarras), 110),
                new ColumnaReporte("PRODUCTO", nameof(InventarioReporte.NombreProducto), 200),
                new ColumnaReporte("VARIANTE", nameof(InventarioReporte.Variante), 130),
                new ColumnaReporte("CATEGORIA", nameof(InventarioReporte.Categoria), 140),
                new ColumnaReporte("P. UNIDAD", nameof(InventarioReporte.PrecioUnidadTexto), 110, "Derecha"),
                new ColumnaReporte("P. MAYOR", nameof(InventarioReporte.PrecioMayorTexto), 110, "Derecha"),
                new ColumnaReporte("ALMACEN", nameof(InventarioReporte.StockAlmacen), 100, "Derecha"),
                new ColumnaReporte("TIENDA", nameof(InventarioReporte.StockTienda), 90, "Derecha"),
                new ColumnaReporte("TOTAL", nameof(InventarioReporte.StockTotal), 90, "Derecha"),
                new ColumnaReporte("VALOR TIENDA", nameof(InventarioReporte.ValorVentaTexto), 130, "Derecha"),
                new ColumnaReporte("ESTADO", nameof(InventarioReporte.EstadoTienda), 120)
            });
        }

        private void GenerarValorizacion()
        {
            List<ValorizacionReporte> datos =
                _servicio.ObtenerValorizacionInventario(
                    CrearFiltroInventario());

            foreach (
                ValorizacionReporte fila
                in datos)
            {
                _filas.Add(fila);
            }

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE VALORIZACION DE INVENTARIO",
                Subtitulo = DescribirFiltros(),
                Pie =
                    $"Categorias: {datos.Count}   " +
                    $"Unidades: {ReporteExportService.Entero(datos.Sum(x => x.Unidades))}   " +
                    $"Valor total: {ReporteExportService.Moneda(datos.Sum(x => x.ValorTotal))}"
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Categoria",
                "Productos",
                "Variantes",
                "Unidades",
                "Valor almacen",
                "Valor tienda",
                "Valor total"
            });

            foreach (
                ValorizacionReporte fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.Categoria,
                    ReporteExportService.Entero(fila.Productos),
                    ReporteExportService.Entero(fila.Variantes),
                    ReporteExportService.Entero(fila.Unidades),
                    ReporteExportService.Moneda(fila.ValorAlmacen),
                    ReporteExportService.Moneda(fila.ValorTienda),
                    ReporteExportService.Moneda(fila.ValorTotal)
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("CATEGORIA", nameof(ValorizacionReporte.Categoria), "*"),
                new ColumnaReporte("PRODUCTOS", nameof(ValorizacionReporte.ProductosTexto), 110, "Derecha"),
                new ColumnaReporte("VARIANTES", nameof(ValorizacionReporte.VariantesTexto), 110, "Derecha"),
                new ColumnaReporte("UNIDADES", nameof(ValorizacionReporte.UnidadesTexto), 110, "Derecha"),
                new ColumnaReporte("VALOR ALMACEN", nameof(ValorizacionReporte.ValorAlmacenTexto), 150, "Derecha"),
                new ColumnaReporte("VALOR TIENDA", nameof(ValorizacionReporte.ValorTiendaTexto), 150, "Derecha"),
                new ColumnaReporte("VALOR TOTAL", nameof(ValorizacionReporte.ValorTotalTexto), 150, "Derecha")
            });
        }

        private void GenerarMovimientosStock()
        {
            List<MovimientoStockReporte> datos =
                _servicio.ObtenerMovimientosStock(
                    CrearFiltroPeriodo());

            foreach (
                MovimientoStockReporte fila
                in datos)
            {
                _filas.Add(fila);
            }

            _tablaActual = new TablaReporte
            {
                Titulo = "REPORTE DE MOVIMIENTOS DE STOCK (KARDEX)",
                Subtitulo = DescribirFiltros(),
                Pie =
                    $"Movimientos: {datos.Count}   " +
                    $"Unidades movidas: {ReporteExportService.Entero(datos.Sum(x => x.Cantidad))}"
            };

            _tablaActual.Encabezados.AddRange(new[]
            {
                "Fecha",
                "Producto",
                "Variante",
                "Categoria",
                "Movimiento",
                "Cantidad",
                "Almacen antes",
                "Almacen despues",
                "Tienda antes",
                "Tienda despues",
                "Usuario",
                "Observacion"
            });

            foreach (
                MovimientoStockReporte fila
                in datos)
            {
                _tablaActual.Filas.Add(new[]
                {
                    fila.FechaMovimientoTexto,
                    fila.NombreProducto,
                    fila.Variante,
                    fila.Categoria,
                    fila.TipoMovimiento,
                    ReporteExportService.Entero(fila.Cantidad),
                    ReporteExportService.Entero(fila.StockAlmacenAntes),
                    ReporteExportService.Entero(fila.StockAlmacenDespues),
                    ReporteExportService.Entero(fila.StockTiendaAntes),
                    ReporteExportService.Entero(fila.StockTiendaDespues),
                    fila.Usuario,
                    fila.Observacion
                });
            }

            ConfigurarColumnas(new[]
            {
                new ColumnaReporte("FECHA", nameof(MovimientoStockReporte.FechaMovimientoTexto), 140),
                new ColumnaReporte("PRODUCTO", nameof(MovimientoStockReporte.NombreProducto), 190),
                new ColumnaReporte("VARIANTE", nameof(MovimientoStockReporte.Variante), 120),
                new ColumnaReporte("CATEGORIA", nameof(MovimientoStockReporte.Categoria), 130),
                new ColumnaReporte("MOVIMIENTO", nameof(MovimientoStockReporte.TipoMovimiento), 150),
                new ColumnaReporte("CANTIDAD", nameof(MovimientoStockReporte.Cantidad), 90, "Derecha"),
                new ColumnaReporte("ALM. ANTES", nameof(MovimientoStockReporte.StockAlmacenAntes), 110, "Derecha"),
                new ColumnaReporte("ALM. DESPUES", nameof(MovimientoStockReporte.StockAlmacenDespues), 120, "Derecha"),
                new ColumnaReporte("TIENDA ANTES", nameof(MovimientoStockReporte.StockTiendaAntes), 120, "Derecha"),
                new ColumnaReporte("TIENDA DESPUES", nameof(MovimientoStockReporte.StockTiendaDespues), 130, "Derecha"),
                new ColumnaReporte("USUARIO", nameof(MovimientoStockReporte.Usuario), 150),
                new ColumnaReporte("OBSERVACION", nameof(MovimientoStockReporte.Observacion), "*")
            });
        }

        // =====================================================
        // COLUMNAS DEL DATAGRID
        // =====================================================

        /// <summary>
        /// Descripcion de una columna del reporte.
        /// Ancho admite "*" para ocupar el espacio libre.
        /// </summary>
        private class ColumnaReporte
        {
            public string Encabezado { get; set; }

            public string Propiedad { get; set; }

            public object Ancho { get; set; }

            public string? Alineacion { get; set; }

            public ColumnaReporte(
                string encabezado,
                string propiedad,
                object ancho,
                string? alineacion = null)
            {
                Encabezado = encabezado;
                Propiedad = propiedad;
                Ancho = ancho;
                Alineacion = alineacion;
            }
        }

        private void ConfigurarColumnas(
            ColumnaReporte[] columnas)
        {
            dgReporte.Columns.Clear();

            foreach (
                ColumnaReporte columna
                in columnas)
            {
                DataGridTextColumn texto =
                    new DataGridTextColumn
                    {
                        Header = columna.Encabezado,

                        Binding =
                            new System.Windows.Data.Binding(
                                columna.Propiedad)
                    };

                texto.Width =
                    columna.Ancho is string asterisco &&
                    asterisco == "*"
                        ? new DataGridLength(1, DataGridLengthUnitType.Star)
                        : new DataGridLength(
                            System.Convert.ToDouble(columna.Ancho));

                texto.ElementStyle =
                    CrearEstiloCelda(
                        columna.Alineacion == "Derecha"
                            ? TextAlignment.Right
                            : TextAlignment.Left);

                dgReporte.Columns.Add(texto);
            }
        }

        private static Style CrearEstiloCelda(
            TextAlignment alineacion)
        {
            Style estilo =
                new Style(
                    typeof(TextBlock));

            estilo.Setters.Add(
                new Setter(
                    TextBlock.ForegroundProperty,
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            0xE2, 0xE8, 0xF0))));

            estilo.Setters.Add(
                new Setter(
                    TextBlock.TextAlignmentProperty,
                    alineacion));

            estilo.Setters.Add(
                new Setter(
                    TextBlock.FontSizeProperty,
                    12.5));

            estilo.Setters.Add(
                new Setter(
                    TextBlock.VerticalAlignmentProperty,
                    VerticalAlignment.Center));

            return estilo;
        }

        // =====================================================
        // INDICADORES Y RESÚMENES
        // =====================================================

        private void ActualizarIndicadores()
        {
            switch (_opcionActual?.Tipo)
            {
                case TipoReporte.StockActual:
                    ActualizarIndicadoresInventario();
                    break;

                case TipoReporte.ValorizacionInventario:
                    ActualizarIndicadoresValorizacion();
                    break;

                case TipoReporte.DetalleProductos:
                    ActualizarIndicadoresLineas();
                    break;

                case TipoReporte.ProductosMasVendidos:
                    ActualizarIndicadoresProductos();
                    break;

                case TipoReporte.VentasPorCategoria:
                    ActualizarIndicadoresCategorias();
                    break;

                case TipoReporte.VentasPorTipoPrecio:
                    ActualizarIndicadoresTipoPrecio();
                    break;

                case TipoReporte.MovimientosStock:
                    ActualizarIndicadoresMovimientos();
                    break;

                default:
                    ActualizarIndicadoresVentas();
                    break;
            }
        }

        private void ActualizarIndicadoresVentas()
        {
            MostrarTarjeta5(true);

            lblIndicador1.Text = "VENTAS";
            lblIndicador2.Text = "SUBTOTAL";
            lblIndicador3.Text = "TOTAL";
            lblIndicador4.Text = "UNIDADES";
            lblIndicador5.Text = "TICKET PROMEDIO";

            txtIndicador1.Text = _totales.CantidadVentasTexto;
            txtIndicador2.Text = _totales.SubtotalTexto;
            txtIndicador3.Text = _totales.TotalTexto;
            txtIndicador4.Text = _totales.UnidadesVendidasTexto;
            txtIndicador5.Text = _totales.CantidadVentas == 0
                ? "0.00"
                : ReporteExportService.Moneda(
                    _totales.Total / _totales.CantidadVentas);
        }

        private void ActualizarIndicadoresLineas()
        {
            MostrarTarjeta5(false);

            lblIndicador1.Text = "LINEAS";
            lblIndicador2.Text = "TOTAL GENERADO";
            lblIndicador3.Text = "DESCUENTOS";
            lblIndicador4.Text = "UNIDADES";

            txtIndicador1.Text = _totalesLineas.CantidadLineasTexto;
            txtIndicador2.Text = _totalesLineas.TotalGeneradoTexto;
            txtIndicador3.Text = _totalesLineas.DescuentosTexto;
            txtIndicador4.Text = _totalesLineas.UnidadesVendidasTexto;
        }

        private void ActualizarIndicadoresProductos()
        {
            MostrarTarjeta5(false);

            List<ProductoMasVendido> datos =
                _filas.OfType<ProductoMasVendido>().ToList();

            lblIndicador1.Text = "PRODUCTOS";
            lblIndicador2.Text = "UNIDADES";
            lblIndicador3.Text = "TOTAL GENERADO";
            lblIndicador4.Text = "DESCUENTOS";

            txtIndicador1.Text = ReporteExportService.Entero(datos.Count);
            txtIndicador2.Text = ReporteExportService.Entero(datos.Sum(x => x.CantidadVendida));
            txtIndicador3.Text = ReporteExportService.Moneda(datos.Sum(x => x.TotalGenerado));
            txtIndicador4.Text = ReporteExportService.Moneda(datos.Sum(x => x.Descuentos));
        }

        private void ActualizarIndicadoresCategorias()
        {
            MostrarTarjeta5(false);

            List<VentaPorCategoriaReporte> datos =
                _filas.OfType<VentaPorCategoriaReporte>().ToList();

            lblIndicador1.Text = "CATEGORIAS";
            lblIndicador2.Text = "UNIDADES";
            lblIndicador3.Text = "TOTAL GENERADO";
            lblIndicador4.Text = "DESCUENTOS";

            txtIndicador1.Text = ReporteExportService.Entero(datos.Count);
            txtIndicador2.Text = ReporteExportService.Entero(datos.Sum(x => x.CantidadVendida));
            txtIndicador3.Text = ReporteExportService.Moneda(datos.Sum(x => x.TotalGenerado));
            txtIndicador4.Text = ReporteExportService.Moneda(datos.Sum(x => x.Descuentos));
        }

        private void ActualizarIndicadoresTipoPrecio()
        {
            MostrarTarjeta5(false);

            List<VentaPorTipoPrecioReporte> datos =
                _filas.OfType<VentaPorTipoPrecioReporte>().ToList();

            lblIndicador1.Text = "CANALES";
            lblIndicador2.Text = "UNIDADES";
            lblIndicador3.Text = "TOTAL GENERADO";
            lblIndicador4.Text = "VENTAS";

            txtIndicador1.Text = ReporteExportService.Entero(datos.Count);
            txtIndicador2.Text = ReporteExportService.Entero(datos.Sum(x => x.Cantidad));
            txtIndicador3.Text = ReporteExportService.Moneda(datos.Sum(x => x.TotalGenerado));
            txtIndicador4.Text = ReporteExportService.Entero(datos.Sum(x => x.NumeroVentas));
        }

        private void ActualizarIndicadoresInventario()
        {
            MostrarTarjeta5(false);

            List<InventarioReporte> datos =
                _filas.OfType<InventarioReporte>().ToList();

            lblIndicador1.Text = "VARIANTES";
            lblIndicador2.Text = "UNIDADES ALMACEN";
            lblIndicador3.Text = "UNIDADES TIENDA";
            lblIndicador4.Text = "VALOR TIENDA";

            txtIndicador1.Text = ReporteExportService.Entero(datos.Count);
            txtIndicador2.Text = ReporteExportService.Entero(datos.Sum(x => x.StockAlmacen));
            txtIndicador3.Text = ReporteExportService.Entero(datos.Sum(x => x.StockTienda));
            txtIndicador4.Text = ReporteExportService.Moneda(datos.Sum(x => x.ValorVenta));
        }

        private void ActualizarIndicadoresValorizacion()
        {
            MostrarTarjeta5(false);

            List<ValorizacionReporte> datos =
                _filas.OfType<ValorizacionReporte>().ToList();

            lblIndicador1.Text = "CATEGORIAS";
            lblIndicador2.Text = "UNIDADES";
            lblIndicador3.Text = "VALOR TOTAL";
            lblIndicador4.Text = "VARIANTES";

            txtIndicador1.Text = ReporteExportService.Entero(datos.Count);
            txtIndicador2.Text = ReporteExportService.Entero(datos.Sum(x => x.Unidades));
            txtIndicador3.Text = ReporteExportService.Moneda(datos.Sum(x => x.ValorTotal));
            txtIndicador4.Text = ReporteExportService.Entero(datos.Sum(x => x.Variantes));
        }

        private void ActualizarIndicadoresMovimientos()
        {
            MostrarTarjeta5(false);

            List<MovimientoStockReporte> datos =
                _filas.OfType<MovimientoStockReporte>().ToList();

            lblIndicador1.Text = "MOVIMIENTOS";
            lblIndicador2.Text = "UNIDADES MOVIDAS";
            lblIndicador3.Text = "SALIDAS ALMACEN";
            lblIndicador4.Text = "SALIDAS TIENDA";

            txtIndicador1.Text = ReporteExportService.Entero(datos.Count);
            txtIndicador2.Text = ReporteExportService.Entero(datos.Sum(x => x.Cantidad));
            txtIndicador3.Text = ReporteExportService.Entero(datos.Sum(x => x.StockAlmacenAntes - x.StockAlmacenDespues));
            txtIndicador4.Text = ReporteExportService.Entero(datos.Sum(x => x.StockTiendaAntes - x.StockTiendaDespues));
        }

        private void MostrarTarjeta5(bool visible)
        {
            tarjetaIndicador5.Visibility =
                visible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void MostrarResumenPago()
        {
            if (_totales.CantidadVentas == 0)
            {
                txtResumenPago.Text = string.Empty;
                return;
            }

            txtResumenPago.Text =
                $"Efectivo: {_totales.TotalEfectivoTexto}   " +
                $"Yape / Plin: {_totales.TotalYapePlinTexto}   " +
                $"Tarjeta: {_totales.TotalTarjetaTexto}   " +
                $"Otros: {_totales.TotalOtrosTexto}   |   " +
                "El recargo por tarjeta es el 5% del subtotal " +
                "y solo aplica a ventas con tarjeta.";
        }

        private string ResumenTotales()
        {
            return
                $"Ventas: {_totales.CantidadVentasTexto}   " +
                $"Unidades: {_totales.UnidadesVendidasTexto}   " +
                $"Subtotal: {_totales.SubtotalTexto}   " +
                $"Recargo tarjeta 5%: {_totales.RecargoTarjetaTexto}   " +
                $"TOTAL: {_totales.TotalTexto}   |   " +
                $"Efectivo: {_totales.TotalEfectivoTexto}   " +
                $"Yape / Plin: {_totales.TotalYapePlinTexto}   " +
                $"Tarjeta: {_totales.TotalTarjetaTexto}   " +
                $"Otros: {_totales.TotalOtrosTexto}";
        }

        private string DescribirFiltros()
        {
            List<string> partes = new List<string>();

            if (_opcionActual?.UsaPeriodo == true)
            {
                partes.Add(
                    $"Periodo: {CrearFiltroPeriodo().DescripcionPeriodo}");
            }

            UsuarioMovimiento? usuario =
                cmbUsuario.SelectedItem as UsuarioMovimiento;

            if (_opcionActual?.UsaUsuario == true &&
                usuario != null &&
                usuario.IdUsuario != 0)
            {
                partes.Add(
                    $"Usuario: {usuario.NombreMostrar}");
            }

            FiltroTexto? metodo =
                cmbMetodoPago.SelectedItem as FiltroTexto;

            if (_opcionActual?.UsaMetodoPago == true &&
                metodo?.Valor != null)
            {
                partes.Add(
                    $"Metodo de pago: {metodo.Texto}");
            }

            Categoria? categoria =
                cmbCategoria.SelectedItem as Categoria;

            if (_opcionActual?.UsaCategoria == true &&
                categoria != null &&
                categoria.IdCategoria != 0)
            {
                partes.Add(
                    $"Categoria: {categoria.Nombre}");
            }

            FiltroTexto? ubicacion =
                cmbUbicacion.SelectedItem as FiltroTexto;

            if (_opcionActual?.UsaUbicacion == true &&
                ubicacion?.Valor != null)
            {
                partes.Add(
                    $"Ubicacion: {ubicacion.Texto}");
            }

            FiltroTexto? estado =
                cmbEstadoStock.SelectedItem as FiltroTexto;

            if (_opcionActual?.UsaEstadoStock == true &&
                estado?.Valor != null)
            {
                partes.Add(
                    $"Estado de stock: {estado.Texto}");
            }

            if (_opcionActual?.UsaTopN == true)
            {
                FiltroTexto? top =
                    cmbTopN.SelectedItem as FiltroTexto;

                partes.Add(
                    $"Cantidad: {top?.Texto ?? "Top 50"}");
            }

            if (!string.IsNullOrWhiteSpace(
                    ObtenerTextoBusqueda()))
            {
                partes.Add(
                    $"Busqueda: {txtBuscar.Text.Trim()}");
            }

            return partes.Count == 0
                ? "Sin filtros aplicados"
                : string.Join("   |   ", partes);
        }

        // =====================================================
        // EXPORTACIÓN E IMPRESIÓN
        // =====================================================

        private void btnExportarCsv_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_filas.Count == 0)
            {
                return;
            }

            try
            {
                string nombre =
                    SanearNombreArchivo(
                        _tablaActual.Titulo);

                string ruta =
                    ReporteExportService.GuardarCsv(
                        _tablaActual,
                        nombre);

                if (string.IsNullOrWhiteSpace(ruta))
                {
                    return;
                }

                MensajeExitoDialog.Mostrar(
                    Window.GetWindow(this),
                    "Reporte exportado",
                    $"El archivo se guardó en:\n\n{ruta}");
            }
            catch (Exception ex)
            {
                MostrarError(
                    "No se pudo exportar el reporte",
                    ex.Message);
            }
        }

        private void btnImprimir_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_filas.Count == 0)
            {
                return;
            }

            try
            {
                ReporteExportService.ImprimirReporte(
                    _tablaActual);
            }
            catch (Exception ex)
            {
                MostrarError(
                    "No se pudo imprimir el reporte",
                    ex.Message);
            }
        }

        private void dgReporte_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            /*
             * Doble click sobre la fila "Detalle de
             * ventas" lleva al modulo POS para ver el
             * comprobante de esa venta.
             */
            if (_opcionActual?.Tipo != TipoReporte.DetalleVentas)
            {
                return;
            }

            if (dgReporte.SelectedItem is not
                VentaReporteDetalle venta)
            {
                return;
            }

            MessageBox.Show(
                Window.GetWindow(this),
                $"Venta {venta.NumeroVenta}\n\n" +
                $"Fecha: {venta.FechaVentaTexto}\n" +
                $"Usuario: {venta.Usuario}\n" +
                $"Cliente: {venta.Cliente}\n" +
                $"Metodo de pago: {venta.MetodoPago}\n" +
                $"Subtotal: {venta.SubtotalTexto}\n" +
                $"Recargo tarjeta: {venta.RecargoTarjetaTexto}\n" +
                $"Total: {venta.TotalTexto}",
                "Detalle de la venta",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static string
            SanearNombreArchivo(string texto)
        {
            char[] prohibidos =
                Path.GetInvalidFileNameChars();

            string resultado =
                texto.ToLowerInvariant()
                    .Replace(" ", "_");

            foreach (
                char caracter
                in prohibidos)
            {
                resultado =
                    resultado.Replace(
                        caracter,
                        '_');
            }

            return resultado;
        }

        // =====================================================
        // UTILIDADES
        // =====================================================

        private void MostrarError(
            string titulo,
            string mensaje)
        {
            MensajeExitoDialog.Mostrar(
                Window.GetWindow(this),
                titulo,
                mensaje);
        }
    }
}
