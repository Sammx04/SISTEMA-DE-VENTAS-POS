using System;
using System.Collections.Generic;
using System.Linq;

namespace ProySistemaVentas.Models.VentasPOS
{
    public class ResultadoVentaPOS
    {
        // =====================================================
        // DATOS GENERALES DE LA VENTA
        // =====================================================

        public int IdVentaPOS { get; set; }

        public int IdVentaSistema { get; set; }

        public string NumeroVenta { get; set; }

        public DateTime FechaVenta { get; set; }

        public int IdUsuario { get; set; }

        public string NombreTrabajador { get; set; }

        // =====================================================
        // DATOS DEL CLIENTE
        // =====================================================

        public int IdClientePOS { get; set; }

        public string TipoDocumento { get; set; }

        public string NumeroDocumento { get; set; }

        public string NombreCliente { get; set; }

        // =====================================================
        // DATOS DEL PAGO
        // =====================================================

        public string MetodoPago { get; set; }

        public decimal Subtotal { get; set; }

        /*
         * En este sistema el campo IGV representa el recargo
         * del 5 % aplicado cuando el pago es con tarjeta.
         */
        public decimal IGV { get; set; }

        public decimal DescuentoTotal { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; }

        public int CantidadUnidades { get; set; }

        public int CantidadProductos { get; set; }

        // =====================================================
        // PRODUCTOS INCLUIDOS EN LA VENTA
        // =====================================================

        public List<ItemCarritoVenta> Detalle { get; set; }
            = new List<ItemCarritoVenta>();

        // =====================================================
        // PROPIEDADES PARA MOSTRAR EN PANTALLA Y BOLETA
        // =====================================================

        public string NumeroVentaMostrar =>
            string.IsNullOrWhiteSpace(NumeroVenta)
                ? "Sin número"
                : NumeroVenta.Trim();

        public string FechaVentaTexto =>
            FechaVenta == DateTime.MinValue
                ? DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                : FechaVenta.ToString("dd/MM/yyyy HH:mm");

        public string NombreTrabajadorMostrar =>
            string.IsNullOrWhiteSpace(NombreTrabajador)
                ? "No especificado"
                : NombreTrabajador.Trim();

        public string NombreClienteMostrar =>
            string.IsNullOrWhiteSpace(NombreCliente)
                ? "Otros clientes"
                : NombreCliente.Trim();

        public string TipoDocumentoMostrar =>
            string.IsNullOrWhiteSpace(TipoDocumento)
                ? "OTROS"
                : TipoDocumento.Trim().ToUpperInvariant();

        public string DocumentoClienteTexto
        {
            get
            {
                if (TipoDocumentoMostrar == "OTROS" ||
                    string.IsNullOrWhiteSpace(NumeroDocumento))
                {
                    return "Sin documento";
                }

                return $"{TipoDocumentoMostrar}: " +
                       $"{NumeroDocumento.Trim()}";
            }
        }

        public string MetodoPagoTexto
        {
            get
            {
                string metodo =
                    string.IsNullOrWhiteSpace(MetodoPago)
                        ? string.Empty
                        : MetodoPago.Trim().ToUpperInvariant();

                return metodo switch
                {
                    "EFECTIVO" => "Efectivo",
                    "YAPE_PLIN" => "Yape / Plin",
                    "TARJETA" => "Tarjeta",
                    _ => string.IsNullOrWhiteSpace(metodo)
                        ? "No especificado"
                        : metodo
                };
            }
        }

        public bool EsPagoTarjeta =>
            string.Equals(
                MetodoPago,
                "TARJETA",
                StringComparison.OrdinalIgnoreCase);

        public bool TieneRecargoTarjeta =>
            IGV > 0;

        public bool TieneDescuento =>
            DescuentoTotal > 0;

        public string SubtotalTexto =>
            $"S/ {Subtotal:N2}";

        public string IGVTexto =>
            $"S/ {IGV:N2}";

        public string DescuentoTotalTexto =>
            $"S/ {DescuentoTotal:N2}";

        public string TotalTexto =>
            $"S/ {Total:N2}";

        public string CantidadProductosTexto
        {
            get
            {
                int cantidad =
                    CantidadProductos > 0
                        ? CantidadProductos
                        : Detalle?.Count ?? 0;

                return cantidad == 1
                    ? "1 producto"
                    : $"{cantidad:N0} productos";
            }
        }

        public string CantidadUnidadesTexto
        {
            get
            {
                int cantidad =
                    CantidadUnidades > 0
                        ? CantidadUnidades
                        : Detalle?.Sum(
                            producto =>
                                producto.Cantidad) ?? 0;

                return cantidad == 1
                    ? "1 unidad"
                    : $"{cantidad:N0} unidades";
            }
        }

        /*
         * Este método se utiliza cuando acabamos de registrar
         * la venta y todavía no hemos consultado nuevamente
         * sp_POS_ObtenerVentaCompleta.
         */
        public void RecalcularResumen()
        {
            if (Detalle == null)
            {
                Detalle =
                    new List<ItemCarritoVenta>();
            }

            CantidadProductos =
                Detalle.Count;

            CantidadUnidades =
                Detalle.Sum(
                    producto =>
                        producto.Cantidad);

            DescuentoTotal =
                Detalle.Sum(
                    producto =>
                        producto.Descuento);
        }
    }
}