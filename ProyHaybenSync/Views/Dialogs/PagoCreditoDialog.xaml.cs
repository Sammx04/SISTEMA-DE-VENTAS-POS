using System;
using System.Windows;
using System.Windows.Controls;
using ProySistemaVentas.Models.InventarioTienda;
using ProySistemaVentas.Models.PrestamosCreditos;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class PagoCreditoDialog : Window
    {
        private readonly PrestamoCreditoItem _operacion;
        private readonly UsuarioMovimiento _trabajador;

        public string MetodoPago { get; private set; } = string.Empty;

        public string Observacion { get; private set; } = string.Empty;

        public bool Confirmado { get; private set; }

        public PagoCreditoDialog(
            PrestamoCreditoItem operacion,
            UsuarioMovimiento trabajador)
        {
            _operacion =
                operacion ?? throw new ArgumentNullException(
                    nameof(operacion));

            _trabajador =
                trabajador ?? throw new ArgumentNullException(
                    nameof(trabajador));

            InitializeComponent();

            CargarDatos();
        }

        private void CargarDatos()
        {
            TxtOperacion.Text = TextoO(_operacion.NumeroOperacion);
            TxtCliente.Text = TextoO(_operacion.ClienteTienda);
            TxtProducto.Text = TextoO(_operacion.NombreProducto);
            TxtVariante.Text = TextoO(_operacion.Variante);
            TxtCantidad.Text = _operacion.CantidadTexto;
            TxtDeuda.Text = _operacion.TotalTexto;
            TxtTrabajador.Text = _trabajador.NombreMostrar;
        }

        private void CboMetodoPago_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            MetodoPago =
                (CboMetodoPago.SelectedItem as ComboBoxItem)?.Tag
                    ?.ToString() ?? string.Empty;
        }

        private void BtnConfirmar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Observacion = TxtObservacion.Text.Trim();

            if (MetodoPago != "EFECTIVO" && MetodoPago != "YAPE")
            {
                MensajeDialog.Mostrar(
                    this,
                    "Método de pago requerido",
                    "Seleccione Efectivo o Yape para continuar.",
                    TipoMensaje.Advertencia);

                return;
            }

            Confirmado = true;
            DialogResult = true;
        }

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Confirmado = false;
            DialogResult = false;
        }

        private static string TextoO(
            string texto)
        {
            return string.IsNullOrWhiteSpace(texto)
                ? "-"
                : texto.Trim();
        }
    }
}
