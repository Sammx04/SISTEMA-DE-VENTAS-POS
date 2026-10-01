using System;
using System.Globalization;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Fila del reporte kardex de movimientos de stock
    /// (sp_Reporte_MovimientosStock).
    ///
    /// NOTA: la tabla dbo.MovimientoStock esta definida en
    /// bd_svhbn.sql pero ningun procedimiento la escribe
    /// todavia, por lo que este reporte devuelve cero filas
    /// hasta que se instrumente el registro de movimientos.
    /// </summary>
    public class MovimientoStockReporte
    {
        public int IdMovimiento { get; set; }

        public int IdProducto { get; set; }

        public int IdVariante { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string Variante { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public string TipoMovimiento { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public int StockAlmacenAntes { get; set; }

        public int StockAlmacenDespues { get; set; }

        public int StockTiendaAntes { get; set; }

        public int StockTiendaDespues { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public string Observacion { get; set; } = string.Empty;

        public DateTime FechaMovimiento { get; set; }

        public string NombreCompleto =>
            string.IsNullOrWhiteSpace(Variante)
                ? NombreProducto
                : $"{NombreProducto} - {Variante}";

        public string FechaMovimientoTexto =>
            FechaMovimiento == DateTime.MinValue
                ? string.Empty
                : FechaMovimiento.ToString(
                    "dd/MM/yyyy HH:mm",
                    CultureInfo.InvariantCulture);
    }
}
