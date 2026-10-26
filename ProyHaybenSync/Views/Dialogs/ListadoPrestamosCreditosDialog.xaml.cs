using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ProySistemaVentas.Helpers;
using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Models.PrestamosCreditos;
using ProySistemaVentas.Services.PrestamosCreditos;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class ListadoPrestamosCreditosDialog : Window
    {
        private const int TamanoPagina = 10;
        private readonly bool _esCredito;
        private readonly UsuarioMovimiento _trabajador;
        private readonly PrestamoCreditoService _service = new PrestamoCreditoService();
        private int _paginaActual = 1;
        private int _totalPaginas = 1;
        private bool _cargando;

        public event EventHandler? OperacionActualizada;

        public ListadoPrestamosCreditosDialog(bool esCredito, UsuarioMovimiento trabajador)
        {
            _esCredito = esCredito;
            _trabajador = trabajador ?? throw new ArgumentNullException(nameof(trabajador));
            InitializeComponent();
            TxtTitulo.Text = esCredito ? "Cobranzas" : "Préstamos";
            TxtSubtitulo.Text = esCredito ? "Consulta, cobra y administra los créditos registrados." : "Consulta, devuelve y administra los préstamos registrados.";
            Title = esCredito ? "Listado de cobranzas" : "Listado de préstamos";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e) => await CargarAsync();

        private async Task CargarAsync()
        {
            if (_cargando) return;
            string texto = TxtTexto.Text.Trim();
            string? estado = (CboEstado.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            DateTime? inicio = DpInicio.SelectedDate;
            DateTime? fin = DpFin.SelectedDate;
            int pagina = _paginaActual;
            if (inicio.HasValue && fin.HasValue && inicio.Value.Date > fin.Value.Date)
            {
                MensajeDialog.Mostrar(this, "Fechas inválidas", "La fecha de inicio no puede ser posterior a la fecha final.", TipoMensaje.Advertencia);
                return;
            }
            try
            {
                _cargando = true;
                IsEnabled = false;
                List<PrestamoCreditoItem> items = await Task.Run(() => _esCredito
                    ? _service.ListarCreditos(texto, estado, inicio, fin, pagina, TamanoPagina)
                    : _service.ListarPrestamos(texto, estado, inicio, fin, pagina, TamanoPagina));
                _totalPaginas = items.Count == 0 ? 1 : Math.Max(1, items[0].TotalPaginas);
                if (_paginaActual > _totalPaginas)
                {
                    _paginaActual = _totalPaginas;
                    _cargando = false;
                    IsEnabled = true;
                    await CargarAsync();
                    return;
                }
                DgOperaciones.ItemsSource = items;
                TxtPagina.Text = $"Página {_paginaActual} de {_totalPaginas}";
                BtnAnterior.IsEnabled = _paginaActual > 1;
                BtnSiguiente.IsEnabled = _paginaActual < _totalPaginas;
            }
            catch (Exception ex)
            {
                MensajeDialog.Mostrar(this, "Error al cargar", MensajeError(ex), TipoMensaje.Error);
            }
            finally
            {
                _cargando = false;
                IsEnabled = true;
            }
        }

        private async void FiltroCambiado(object sender, EventArgs e)
        {
            if (_cargando || !IsLoaded) return;
            _paginaActual = 1;
            await CargarAsync();
        }

        private async void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            TxtTexto.Clear(); CboEstado.SelectedIndex = 0; DpInicio.SelectedDate = null; DpFin.SelectedDate = null;
            _paginaActual = 1;
            await CargarAsync();
        }

        private async void BtnAnterior_Click(object sender, RoutedEventArgs e) { if (_paginaActual > 1) { _paginaActual--; await CargarAsync(); } }
        private async void BtnSiguiente_Click(object sender, RoutedEventArgs e) { if (_paginaActual < _totalPaginas) { _paginaActual++; await CargarAsync(); } }

        private async void BtnVer_Click(object sender, RoutedEventArgs e)
        {
            if (!TryItem(sender, out PrestamoCreditoItem item)) return;
            try
            {
                PrestamoCreditoDetalle? detalle = await Task.Run(() => _service.ObtenerDetalle(item.IdPrestamoCredito));
                if (detalle == null) { MensajeDialog.Mostrar(this, "Operación no encontrada", "No se encontró la información solicitada.", TipoMensaje.Advertencia); return; }
                new DetallePrestamoCreditoDialog(detalle) { Owner = this }.ShowDialog();
            }
            catch (Exception ex) { MostrarError("No se pudo abrir el detalle", ex); }
        }

        private async void BtnDevolver_Click(object sender, RoutedEventArgs e)
        {
            if (!TryItem(sender, out PrestamoCreditoItem item) || !item.PuedeDevolver) return;
            ConfirmarObservacionDialog dialogo = new ConfirmarObservacionDialog("Confirmar devolución", $"Se devolverán {item.Cantidad} unidad(es) de {item.NombreCompleto} al stock de tienda.", "Devolver") { Owner = this };
            dialogo.ShowDialog();
            if (!dialogo.Confirmado) return;
            int idOperacion = item.IdPrestamoCredito;
            int idUsuario = _trabajador.IdUsuario;
            string observacion = dialogo.Observacion;
            await EjecutarMutacionAsync(() => _service.RegistrarDevolucion(idOperacion, idUsuario, observacion), "Devolución registrada", "El producto fue devuelto al stock de tienda.");
        }

        private async void BtnCobranza_Click(object sender, RoutedEventArgs e)
        {
            if (!TryItem(sender, out PrestamoCreditoItem item) || !item.PuedePasarACobranza) return;
            ConvertirPrestamoCreditoDialog dialogo = new ConvertirPrestamoCreditoDialog(item) { Owner = this };
            dialogo.ShowDialog();
            if (!dialogo.Confirmado) return;
            int idOperacion = item.IdPrestamoCredito;
            int idUsuario = _trabajador.IdUsuario;
            DateTime fechaEsperada = dialogo.FechaPagoEsperada;
            string observacion = dialogo.Observacion;
            await EjecutarMutacionAsync(() => _service.ConvertirPrestamoACredito(idOperacion, idUsuario, fechaEsperada, observacion), "Operación convertida", "El préstamo pasó a cobranza sin modificar el stock.");
        }

        private async void BtnPagar_Click(object sender, RoutedEventArgs e)
        {
            if (!TryItem(sender, out PrestamoCreditoItem item) || !item.PuedePagar) return;
            PagoCreditoDialog dialogo = new PagoCreditoDialog(item, _trabajador) { Owner = this };
            dialogo.ShowDialog();
            if (!dialogo.Confirmado) return;
            int idOperacion = item.IdPrestamoCredito;
            int idUsuario = _trabajador.IdUsuario;
            string metodoPago = dialogo.MetodoPago;
            string observacion = dialogo.Observacion;
            await EjecutarMutacionAsync(() => _service.RegistrarPago(idOperacion, idUsuario, metodoPago, observacion), "Pago registrado", "La cobranza fue completada correctamente.");
            try
            {
                BoletaPagoCredito? boleta = await Task.Run(() => _service.ObtenerBoletaPago(idOperacion));
                if (boleta != null)
                {
                    new NotaCobranzaDialog(boleta) { Owner = this }.ShowDialog();
                }
            }
            catch (Exception ex) { MostrarError("No se pudo abrir la nota de cobranza", ex); }
        }

        private async void BtnPrestamo_Click(object sender, RoutedEventArgs e)
        {
            if (!TryItem(sender, out PrestamoCreditoItem item) || !item.PuedePasarAPrestamo) return;
            if (!MensajeDialog.Confirmar(this, "Pasar a préstamo", "La cobranza volverá a préstamo sin modificar el stock.")) return;
            await EjecutarMutacionAsync(() => _service.ConvertirCreditoAPrestamo(item.IdPrestamoCredito, _trabajador.IdUsuario), "Operación convertida", "La cobranza pasó a préstamo sin modificar el stock.");
        }

        private async void BtnBoleta_Click(object sender, RoutedEventArgs e)
        {
            if (!TryItem(sender, out PrestamoCreditoItem item) || !item.IdPagoCredito.HasValue) return;
            try
            {
                BoletaPagoCredito? boleta = await Task.Run(() => _service.ObtenerBoletaPago(item.IdPrestamoCredito));
                if (boleta == null) { MensajeDialog.Mostrar(this, "Boleta no encontrada", "No se encontró la boleta de pago para esta operación.", TipoMensaje.Advertencia); return; }
                new NotaCobranzaDialog(boleta) { Owner = this }.ShowDialog();
            }
            catch (Exception ex) { MostrarError("No se pudo abrir la boleta", ex); }
        }

        private async Task EjecutarMutacionAsync(Action accion, string titulo, string mensaje)
        {
            try
            {
                IsEnabled = false;
                await Task.Run(accion);
                OperacionActualizada?.Invoke(this, EventArgs.Empty);
                await CargarAsync();
                MensajeDialog.Mostrar(this, titulo, mensaje, TipoMensaje.Exito);
            }
            catch (Exception ex) { MostrarError(titulo, ex); }
            finally { IsEnabled = true; }
        }

        private static bool TryItem(object sender, out PrestamoCreditoItem item)
        {
            item = null!;
            if (sender is Button { Tag: PrestamoCreditoItem operacion }) { item = operacion; return true; }
            return false;
        }

        private void MostrarError(string titulo, Exception ex) => MensajeDialog.Mostrar(this, titulo, MensajeError(ex), TipoMensaje.Error);
        private static string MensajeError(Exception ex) => ex.InnerException?.Message ?? ex.Message;

        private async void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            string texto = TxtTexto.Text.Trim();
            string? estado = (CboEstado.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            DateTime? inicio = DpInicio.SelectedDate;
            DateTime? fin = DpFin.SelectedDate;
            if (inicio.HasValue && fin.HasValue && inicio.Value.Date > fin.Value.Date)
            {
                MensajeDialog.Mostrar(this, "Fechas inválidas", "La fecha de inicio no puede ser posterior a la fecha final.", TipoMensaje.Advertencia);
                return;
            }

            SaveFileDialog selector = new SaveFileDialog
            {
                Filter = "Archivo Excel (*.xlsx)|*.xlsx",
                FileName = _esCredito ? "Cobranzas.xlsx" : "Prestamos.xlsx"
            };
            if (selector.ShowDialog(this) != true)
            {
                return;
            }

            string ruta = selector.FileName;
            try
            {
                IsEnabled = false;
                List<PrestamoCreditoItem> items = await Task.Run(() => ObtenerTodosLosFiltrados(texto, estado, inicio, fin));
                await Task.Run(() =>
                {
                    if (_esCredito) PrestamoCreditoExcelExporter.ExportarCreditos(items, ruta);
                    else PrestamoCreditoExcelExporter.ExportarPrestamos(items, ruta);
                });
                MensajeDialog.Mostrar(this, "Exportación completada", $"Se exportaron {items.Count} registro(s) a {Path.GetFileName(ruta)}.", TipoMensaje.Exito);
            }
            catch (Exception ex) { MostrarError("No se pudo exportar Excel", ex); }
            finally { IsEnabled = true; }
        }

        private List<PrestamoCreditoItem> ObtenerTodosLosFiltrados(string texto, string? estado, DateTime? inicio, DateTime? fin)
        {
            List<PrestamoCreditoItem> resultado = new List<PrestamoCreditoItem>();
            const int tamanoExportacion = 100;
            List<PrestamoCreditoItem> primeraPagina = _esCredito
                ? _service.ListarCreditos(texto, estado, inicio, fin, 1, tamanoExportacion)
                : _service.ListarPrestamos(texto, estado, inicio, fin, 1, tamanoExportacion);
            resultado.AddRange(primeraPagina);
            int totalPaginas = primeraPagina.Count == 0 ? 0 : Math.Max(1, primeraPagina[0].TotalPaginas);
            for (int pagina = 2; pagina <= totalPaginas; pagina++)
            {
                resultado.AddRange(_esCredito
                    ? _service.ListarCreditos(texto, estado, inicio, fin, pagina, tamanoExportacion)
                    : _service.ListarPrestamos(texto, estado, inicio, fin, pagina, tamanoExportacion));
            }
            return resultado;
        }
    }
}
