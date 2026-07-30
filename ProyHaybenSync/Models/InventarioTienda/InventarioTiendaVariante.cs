using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProySistemaVentas.Models.InventarioTienda
{
    public class InventarioTiendaVariante :
        INotifyPropertyChanged
    {
        private int _stockAlmacen;
        private int _stockTienda;

        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; }

        public string VarianteDescripcion { get; set; }

        public string CodigoBarras { get; set; }

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        public int StockAlmacen
        {
            get => _stockAlmacen;

            set
            {
                _stockAlmacen = value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(PuedeTransferir));
            }
        }

        public int StockTienda
        {
            get => _stockTienda;

            set
            {
                _stockTienda = value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(EstadoTienda));

                OnPropertyChanged(
                    nameof(EsAgotado));

                OnPropertyChanged(
                    nameof(EsStockBajo));

                OnPropertyChanged(
                    nameof(EsStockNormal));
            }
        }

        /*
         * Estado calculado según el stock disponible
         * actualmente en tienda.
         */
        public string EstadoTienda
        {
            get
            {
                if (StockTienda <= 0)
                {
                    return "AGOTADO";
                }

                if (StockTienda <= 5)
                {
                    return "STOCK BAJO";
                }

                return "EN STOCK";
            }
        }

        public bool EsAgotado =>
            StockTienda <= 0;

        public bool EsStockBajo =>
            StockTienda > 0 &&
            StockTienda <= 5;

        public bool EsStockNormal =>
            StockTienda > 5;

        /*
         * El botón de transferencia estará habilitado
         * solamente cuando exista stock en almacén.
         */
        public bool PuedeTransferir =>
            StockAlmacen > 0;

        public string PrecioUnidadTexto =>
            $"S/ {PrecioUnidad:N2}";

        public string PrecioMayorTexto =>
            $"S/ {PrecioMayor:N2}";

        public string NombreVarianteMostrar =>
            string.IsNullOrWhiteSpace(
                VarianteDescripcion)
                    ? "Única"
                    : VarianteDescripcion.Trim();

        public event PropertyChangedEventHandler
            PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName]
            string nombrePropiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    nombrePropiedad));
        }
    }
}