using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Models.Almacen
{
    public class ProductoListaViewModel
    {
        public int IdProducto { get; set; }

        public string CodigoBase { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public int IdCategoria { get; set; }

        public string NombreCategoria { get; set; }

        public string PrecioUnidadTexto { get; set; }

        public string PrecioMayorTexto { get; set; }

        public int StockTotal { get; set; }

        public string CodigoBarrasTexto { get; set; }

        public int CantidadVariantes { get; set; }

        public bool TieneVariantes { get; set; }

        public List<ProductoInventarioListado> Variantes { get; set; }
            = new List<ProductoInventarioListado>();

    }
}
