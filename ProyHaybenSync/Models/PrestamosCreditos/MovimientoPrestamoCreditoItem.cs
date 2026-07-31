using System;

namespace ProySistemaVentas.Models.PrestamosCreditos
{
    public class MovimientoPrestamoCreditoItem
    {
        public int IdMovimientoPC { get; set; }

        public int IdPrestamoCredito { get; set; }

        public string TipoMovimiento { get; set; } =
            string.Empty;

        public string TipoOperacionAnterior { get; set; } =
            string.Empty;

        public string TipoOperacionNuevo { get; set; } =
            string.Empty;

        public string EstadoAnterior { get; set; } =
            string.Empty;

        public string EstadoNuevo { get; set; } =
            string.Empty;

        public int IdUsuario { get; set; }

        public string NombreTrabajador { get; set; } =
            string.Empty;

        public DateTime FechaMovimiento { get; set; }

        public string Observacion { get; set; } =
            string.Empty;

        public string FechaMovimientoTexto
        {
            get
            {
                return FechaMovimiento.ToString(
                    "dd/MM/yyyy HH:mm");
            }
        }

        public string TipoMovimientoTexto
        {
            get
            {
                switch (TipoMovimiento)
                {
                    case "REGISTRO_PRESTAMO":
                        return "Préstamo registrado";

                    case "REGISTRO_CREDITO":
                        return "Crédito registrado";

                    case "CONVERSION_A_CREDITO":
                        return "Pasado a cobranza";

                    case "CONVERSION_A_PRESTAMO":
                        return "Pasado a préstamo";

                    case "DEVOLUCION":
                        return "Producto devuelto";

                    case "PAGO":
                        return "Pago registrado";

                    default:
                        return TipoMovimiento;
                }
            }
        }
    }
}