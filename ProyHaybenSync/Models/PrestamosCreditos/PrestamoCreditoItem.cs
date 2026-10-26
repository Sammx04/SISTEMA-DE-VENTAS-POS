using System;

namespace ProySistemaVentas.Models.PrestamosCreditos
{
    public class PrestamoCreditoItem
    {
        public int IdPrestamoCredito { get; set; }

        public string NumeroOperacion { get; set; } =
            string.Empty;

        public string TipoOperacion { get; set; } =
            string.Empty;

        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; } =
            string.Empty;

        public string Variante { get; set; } =
            string.Empty;

        public string CodigoBarra { get; set; } =
            string.Empty;

        public string ClienteTienda { get; set; } =
            string.Empty;

        public int Cantidad { get; set; }

        public string TipoPrecio { get; set; } =
            string.Empty;

        public decimal PrecioUnitario { get; set; }

        public bool PrecioEditado { get; set; }

        public decimal Total { get; set; }

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaPagoEsperada { get; set; }

        public string Estado { get; set; } =
            string.Empty;

        public string AccionFinal { get; set; } =
            string.Empty;

        public DateTime? FechaFinalizacion { get; set; }

        public string MetodoPago { get; set; } =
            string.Empty;

        public bool StockDescontado { get; set; }

        public string Observacion { get; set; } =
            string.Empty;

        public int IdUsuarioRegistro { get; set; }

        public string NombreTrabajadorRegistro { get; set; } =
            string.Empty;

        public int? IdUsuarioUltimaAccion { get; set; }

        public string NombreTrabajadorUltimaAccion { get; set; } =
            string.Empty;

        public int? IdPagoCredito { get; set; }

        public string NumeroPago { get; set; } =
            string.Empty;

        public int? IdCaja { get; set; }

        public DateTime? FechaPago { get; set; }

        public string AccionActual { get; set; } =
            string.Empty;

        /*
         * Datos de paginación devueltos por los SP.
         */
        public int TotalRegistros { get; set; }

        public int TotalPaginas { get; set; }

        public int PaginaActual { get; set; }

        public string NombreCompleto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Variante) ||
                    Variante.Equals(
                        "Única",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return NombreProducto;
                }

                return $"{NombreProducto} - {Variante}";
            }
        }

        public string PrecioUnitarioTexto
        {
            get
            {
                return $"S/ {PrecioUnitario:N2}";
            }
        }

        public string TotalTexto
        {
            get
            {
                return $"S/ {Total:N2}";
            }
        }

        public string CantidadTexto
        {
            get
            {
                return $"{Cantidad} u.";
            }
        }

        public string FechaRegistroTexto
        {
            get
            {
                return FechaRegistro.ToString(
                    "dd/MM/yyyy HH:mm");
            }
        }

        public string FechaPagoEsperadaTexto
        {
            get
            {
                return FechaPagoEsperada.HasValue
                    ? FechaPagoEsperada.Value.ToString(
                        "dd/MM/yyyy")
                    : "-";
            }
        }

        public string FechaFinalizacionTexto
        {
            get
            {
                return FechaFinalizacion.HasValue
                    ? FechaFinalizacion.Value.ToString(
                        "dd/MM/yyyy HH:mm")
                    : "-";
            }
        }

        public string EstadoMostrar
        {
            get
            {
                if (EsCredito && EstaPendiente && FechaPagoEsperada.HasValue)
                {
                    if (FechaPagoEsperada.Value.Date < DateTime.Today)
                    {
                        return "Vencido";
                    }

                    if (FechaPagoEsperada.Value.Date == DateTime.Today)
                    {
                        return "Vence hoy";
                    }
                }

                if (Estado.Equals(
                        "PENDIENTE",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return "Pendiente";
                }

                return "Completado";
            }
        }

        public bool EstaPendiente
        {
            get
            {
                return Estado.Equals(
                    "PENDIENTE",
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool EsPrestamo
        {
            get
            {
                return TipoOperacion.Equals(
                    "PRESTAMO",
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool EsCredito
        {
            get
            {
                return TipoOperacion.Equals(
                    "CREDITO",
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool PuedeDevolver
        {
            get
            {
                return EsPrestamo &&
                       EstaPendiente &&
                       StockDescontado;
            }
        }

        public bool PuedePagar
        {
            get
            {
                return EsCredito &&
                       EstaPendiente;
            }
        }

        public bool PuedePasarACobranza
        {
            get
            {
                return EsPrestamo &&
                       EstaPendiente;
            }
        }

        public bool PuedePasarAPrestamo
        {
            get
            {
                return EsCredito &&
                       EstaPendiente &&
                       !IdPagoCredito.HasValue;
            }
        }

        public bool EstaVencido => EsCredito && EstaPendiente &&
            FechaPagoEsperada.HasValue &&
            FechaPagoEsperada.Value.Date < DateTime.Today;

        public bool VenceHoy => EsCredito && EstaPendiente &&
            FechaPagoEsperada.HasValue &&
            FechaPagoEsperada.Value.Date == DateTime.Today;
    }
}
