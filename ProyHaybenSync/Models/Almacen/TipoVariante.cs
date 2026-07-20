using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProySistemaVentas.Models.Almacen
{
    public class TipoVariante : INotifyPropertyChanged
    {
        private int _stock;
        private int _stockActual;
        private int _ingreso;
        private int _salida;

        public int IdVariante { get; set; }

        public string Descripcion { get; set; }

        public string CodigoBarras { get; set; }

        public decimal PrecioUnidad { get; set; }

        public decimal PrecioMayor { get; set; }

        // Se utiliza cuando se registra un producto nuevo.
        public int Stock
        {
            get => _stock;
            set
            {
                _stock = value;
                OnPropertyChanged();
            }
        }

        // Se utiliza al editar. Este valor será de solo lectura.
        public int StockActual
        {
            get => _stockActual;
            set
            {
                _stockActual = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NuevoStock));
            }
        }

        public int Ingreso
        {
            get => _ingreso;
            set
            {
                _ingreso = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NuevoStock));
            }
        }

        public int Salida
        {
            get => _salida;
            set
            {
                _salida = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NuevoStock));
            }
        }

        public int NuevoStock
        {
            get
            {
                return StockActual + Ingreso - Salida;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propiedad));
        }
    }
}
