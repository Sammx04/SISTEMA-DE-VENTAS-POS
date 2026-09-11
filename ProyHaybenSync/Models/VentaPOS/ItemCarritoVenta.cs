using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProySistemaVentas.Models.VentasPOS
{
    public class ItemCarritoVenta : INotifyPropertyChanged
    {
        private string _tipoPrecio = "UNIDAD";
        private decimal _precioUnitario;
        private int _cantidad = 1;
        private decimal _descuento;

        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; }

        public string Variante { get; set; }

        public string Categoria { get; set; }

        public string CodigoBarra { get; set; }

        /*
         * Precios originales obtenidos desde la base de datos.
         */
        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        /*
         * Stock disponible únicamente en TIENDA.
         */
        public int Stock { get; set; }

        /*
         * UNIDAD o MAYOR.
         */
        public string TipoPrecio
        {
            get => _tipoPrecio;

            set
            {
                string nuevoValor =
                    string.IsNullOrWhiteSpace(value)
                        ? "UNIDAD"
                        : value.Trim().ToUpperInvariant();

                if (_tipoPrecio == nuevoValor)
                {
                    return;
                }

                _tipoPrecio = nuevoValor;

                OnPropertyChanged();
                OnPropertyChanged(nameof(EsPrecioUnidad));
                OnPropertyChanged(nameof(EsPrecioMayor));
            }
        }

        /*
         * Precio usado solamente en esta venta.
         *
         * Puede ser el precio por unidad, precio por mayor
         * o un precio modificado mediante la ventana de descuento.
         *
         * No modifica el precio general del producto.
         */
        public decimal PrecioUnitario
        {
            get => _precioUnitario;

            set
            {
                decimal nuevoPrecio =
                    value < 0
                        ? 0
                        : value;

                if (_precioUnitario == nuevoPrecio)
                {
                    return;
                }

                _precioUnitario = nuevoPrecio;

                NotificarCalculos();
            }
        }

        public int Cantidad
        {
            get => _cantidad;

            set
            {
                int nuevaCantidad =
                    value < 1
                        ? 1
                        : value;

                if (_cantidad == nuevaCantidad)
                {
                    return;
                }

                _cantidad = nuevaCantidad;

                NotificarCalculos();
            }
        }

        /*
         * Descuento monetario aplicado solamente a esta línea.
         *
         * Ejemplo:
         * Precio × cantidad = S/ 100
         * Descuento = S/ 10
         * Total línea = S/ 90
         */
        public decimal Descuento
        {
            get => _descuento;

            set
            {
                decimal nuevoDescuento =
                    value < 0
                        ? 0
                        : value;

                if (_descuento == nuevoDescuento)
                {
                    return;
                }

                _descuento = nuevoDescuento;

                NotificarCalculos();
            }
        }

        public decimal TotalSinDescuento =>
            PrecioUnitario * Cantidad;

        public decimal TotalLinea =>
            Math.Max(
                0,
                TotalSinDescuento - Descuento);

        public bool EsPrecioUnidad =>
            string.Equals(
                TipoPrecio,
                "UNIDAD",
                StringComparison.OrdinalIgnoreCase);

        public bool EsPrecioMayor =>
            string.Equals(
                TipoPrecio,
                "MAYOR",
                StringComparison.OrdinalIgnoreCase);

        public bool TienePrecioMayor =>
            PrecioMayor > 0;

        public bool TieneDescuento =>
            Descuento > 0;

        public bool PuedeAumentarCantidad =>
            Cantidad < Stock;

        public string NombreVarianteMostrar =>
            string.IsNullOrWhiteSpace(Variante)
                ? "Única"
                : Variante.Trim();

        public string DetalleProducto
        {
            get
            {
                string categoria =
                    string.IsNullOrWhiteSpace(Categoria)
                        ? "Sin categoría"
                        : Categoria.Trim();

                return $"{categoria} - {NombreVarianteMostrar}";
            }
        }

        public string PrecioUnitarioTexto =>
            $"S/ {PrecioUnitario:N2}";

        public string PrecioUnidadTexto =>
            $"S/ {PrecioUnidad:N2}";

        public string PrecioMayorTexto =>
            PrecioMayor > 0
                ? $"S/ {PrecioMayor:N2}"
                : "No disponible";

        public string DescuentoTexto =>
            $"S/ {Descuento:N2}";

        public string TotalLineaTexto =>
            $"S/ {TotalLinea:N2}";

        public string ResumenPrecioCantidad =>
            $"S/ {PrecioUnitario:N2} × {Cantidad:N0}";

        public event PropertyChangedEventHandler PropertyChanged;

        /*
         * Cambiar al precio normal.
         */
        public void UsarPrecioUnidad()
        {
            TipoPrecio = "UNIDAD";
            PrecioUnitario = PrecioUnidad;
        }

        /*
         * Cambiar al precio por mayor.
         */
        public void UsarPrecioMayor()
        {
            if (!TienePrecioMayor)
            {
                return;
            }

            TipoPrecio = "MAYOR";
            PrecioUnitario = PrecioMayor;
        }

        /*
         * Actualiza todas las propiedades calculadas utilizadas
         * en la tarjeta del carrito.
         */
        public void ActualizarCalculos()
        {
            NotificarCalculos();
        }

        private void NotificarCalculos()
        {
            OnPropertyChanged(nameof(PrecioUnitario));
            OnPropertyChanged(nameof(Cantidad));
            OnPropertyChanged(nameof(Descuento));
            OnPropertyChanged(nameof(TotalSinDescuento));
            OnPropertyChanged(nameof(TotalLinea));
            OnPropertyChanged(nameof(TieneDescuento));
            OnPropertyChanged(nameof(PuedeAumentarCantidad));
            OnPropertyChanged(nameof(PrecioUnitarioTexto));
            OnPropertyChanged(nameof(DescuentoTexto));
            OnPropertyChanged(nameof(TotalLineaTexto));
            OnPropertyChanged(nameof(ResumenPrecioCantidad));
        }

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string nombrePropiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}