using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProySistemaVentas.Models.InventarioTienda
{
    public class InventarioTiendaProducto :
        INotifyPropertyChanged
    {
        private bool _estaExpandido;

        public int IdProducto { get; set; }

        public string CodigoBase { get; set; }

        public string NombreProducto { get; set; }

        public string DescripcionProducto { get; set; }

        public int IdCategoria { get; set; }

        public string NombreCategoria { get; set; }

        public bool TieneVariantes { get; set; }

        /*
         * Contiene todas las variantes correspondientes
         * al producto.
         */
        public ObservableCollection<InventarioTiendaVariante>
            Variantes
        { get; set; }
                = new ObservableCollection<
                    InventarioTiendaVariante>();

        /*
         * Controla si las variantes se encuentran
         * desplegadas dentro de la tarjeta.
         */
        public bool EstaExpandido
        {
            get => _estaExpandido;

            set
            {
                _estaExpandido = value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(TextoBotonVariantes));

                OnPropertyChanged(
                    nameof(SimboloExpansion));
            }
        }

        public string TextoBotonVariantes =>
            EstaExpandido
                ? "Ocultar variantes"
                : "Variantes";

        public string SimboloExpansion =>
            EstaExpandido
                ? "⌄"
                : "›";

        public int CantidadVariantes =>
            Variantes?.Count ?? 0;

        public int StockTotalAlmacen =>
            Variantes?.Sum(
                variante =>
                    variante.StockAlmacen)
            ?? 0;

        public int StockTotalTienda =>
            Variantes?.Sum(
                variante =>
                    variante.StockTienda)
            ?? 0;

        /*
         * Estado general de la tarjeta.
         */
        public string EstadoTienda
        {
            get
            {
                if (StockTotalTienda <= 0)
                {
                    return "AGOTADO";
                }

                if (StockTotalTienda <= 5)
                {
                    return "STOCK BAJO";
                }

                return "EN STOCK";
            }
        }

        public bool EsAgotado =>
            StockTotalTienda <= 0;

        public bool EsStockBajo =>
            StockTotalTienda > 0 &&
            StockTotalTienda <= 5;

        public bool EsStockNormal =>
            StockTotalTienda > 5;

        public string CodigoBarrasTexto
        {
            get
            {
                if (Variantes == null ||
                    Variantes.Count == 0)
                {
                    return "Sin código";
                }

                if (Variantes.Count > 1)
                {
                    return "Múltiples códigos de barras";
                }

                return Variantes[0]
                    .CodigoBarras;
            }
        }

        public string PrecioUnidadTexto
        {
            get
            {
                if (Variantes == null ||
                    Variantes.Count == 0)
                {
                    return "—";
                }

                if (Variantes.Count > 1)
                {
                    return "Múltiples precios";
                }

                return
                    $"S/ {Variantes[0].PrecioUnidad:N2}";
            }
        }

        public string PrecioMayorTexto
        {
            get
            {
                if (Variantes == null ||
                    Variantes.Count == 0)
                {
                    return "—";
                }

                if (Variantes.Count > 1)
                {
                    return "Múltiples";
                }

                return
                    $"S/ {Variantes[0].PrecioMayor:N2}";
            }
        }

        /*
         * Aunque la base indique TieneVariantes,
         * también comprobamos que realmente exista
         * más de una variante.
         */
        public bool MostrarBotonVariantes =>
            TieneVariantes &&
            CantidadVariantes > 1;

        public InventarioTiendaVariante
            VarianteUnica
        {
            get
            {
                if (Variantes == null ||
                    Variantes.Count != 1)
                {
                    return null;
                }

                return Variantes[0];
            }
        }

        public event PropertyChangedEventHandler
            PropertyChanged;

        public void ActualizarTotales()
        {
            OnPropertyChanged(
                nameof(CantidadVariantes));

            OnPropertyChanged(
                nameof(StockTotalAlmacen));

            OnPropertyChanged(
                nameof(StockTotalTienda));

            OnPropertyChanged(
                nameof(EstadoTienda));

            OnPropertyChanged(
                nameof(EsAgotado));

            OnPropertyChanged(
                nameof(EsStockBajo));

            OnPropertyChanged(
                nameof(EsStockNormal));

            OnPropertyChanged(
                nameof(CodigoBarrasTexto));

            OnPropertyChanged(
                nameof(PrecioUnidadTexto));

            OnPropertyChanged(
                nameof(PrecioMayorTexto));

            OnPropertyChanged(
                nameof(MostrarBotonVariantes));

            OnPropertyChanged(
                nameof(VarianteUnica));
        }

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