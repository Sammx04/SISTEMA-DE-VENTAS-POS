using ProySistemaVentas.Models.Almacen;
using System;
using System.Windows;
using System.Windows.Input;

namespace ProySistemaVentas.Views.Dialogs
{
    public partial class DetalleProductoDialog : Window
    {
        public DetalleProductoDialog(
            ProductoListaViewModel producto)
        {
            InitializeComponent();

            if (producto == null)
            {
                throw new ArgumentNullException(
                    nameof(producto));
            }

            DataContext =
                producto;

            TxtDescripcion.Text =
                string.IsNullOrWhiteSpace(
                    producto.Descripcion)
                    ? "No se registró una descripción para este producto."
                    : producto.Descripcion.Trim();
        }

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void Encabezado_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton ==
                MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}