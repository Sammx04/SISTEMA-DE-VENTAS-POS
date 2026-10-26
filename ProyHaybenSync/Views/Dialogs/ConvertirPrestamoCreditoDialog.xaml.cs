using System;
using System.Windows;
using ProySistemaVentas.Models.PrestamosCreditos;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class ConvertirPrestamoCreditoDialog : Window
    {
        private readonly PrestamoCreditoItem _prestamo;

        public DateTime FechaPagoEsperada { get; private set; }

        public string Observacion { get; private set; } = string.Empty;

        public bool Confirmado { get; private set; }

        public ConvertirPrestamoCreditoDialog(
            PrestamoCreditoItem prestamo)
        {
            _prestamo = prestamo ?? throw new ArgumentNullException(
                nameof(prestamo));

            InitializeComponent();

            DataContext = _prestamo;
            DtpFechaPagoEsperada.SelectedDate = DateTime.Today;
        }

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void BtnConfirmar_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!DtpFechaPagoEsperada.SelectedDate.HasValue)
            {
                MensajeDialog.Mostrar(
                    this,
                    "Fecha requerida",
                    "Seleccione la fecha esperada de pago.",
                    TipoMensaje.Advertencia);

                return;
            }

            DateTime fechaSeleccionada =
                DtpFechaPagoEsperada.SelectedDate.Value.Date;

            if (fechaSeleccionada < DateTime.Today)
            {
                MensajeDialog.Mostrar(
                    this,
                    "Fecha no válida",
                    "La fecha esperada de pago no puede ser anterior a hoy.",
                    TipoMensaje.Advertencia);

                return;
            }

            FechaPagoEsperada = fechaSeleccionada;
            Observacion = TxtObservacion.Text.Trim();
            Confirmado = true;
            DialogResult = true;
        }
    }
}
