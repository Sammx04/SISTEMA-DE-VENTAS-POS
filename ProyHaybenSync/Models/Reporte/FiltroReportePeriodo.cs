using System;

namespace ProySistemaVentas.Models.Reporte
{
    /// <summary>
    /// Filtros de los reportes basados en el periodo
    /// (ventas por dia, detalle de ventas, lineas de
    /// venta, ranking de productos/usuarios/categorias).
    ///
    /// FechaDesde y FechaHasta son INCLUSIVOS: el servicio
    /// los convierte a un rango semiabierto en el servidor
    /// (FechaVenta &gt;= Desde AND FechaVenta &lt; Hasta + 1 dia)
    /// para que un filtro por fecha no dependa de la hora
    /// en que se guardo la venta.
    /// </summary>
    public class FiltroReportePeriodo
    {
        public DateTime FechaDesde { get; set; } =
            new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                DateTime.Today.Day);

        public DateTime FechaHasta { get; set; } =
            new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                DateTime.Today.Day);

        /// <summary>
        /// NULL o 0 = todos los usuarios (incluidos los inactivos).
        /// </summary>
        public int? IdUsuario { get; set; }

        /// <summary>
        /// NULL = todos. Valores admitidos por el POS:
        /// EFECTIVO, YAPE_PLIN, TARJETA.
        /// </summary>
        public string? MetodoPago { get; set; }

        /// <summary>
        /// NULL = todos. VentaPOS.Estado se fija en
        /// 'COMPLETADA' al registrar la venta.
        /// </summary>
        public string? Estado { get; set; }

        /// <summary>
        /// Texto libre de busqueda: numero de venta,
        /// usuario o cliente.
        /// </summary>
        public string? Texto { get; set; }

        public string DescripcionPeriodo =>
            $"{FechaDesde:dd/MM/yyyy} a {FechaHasta:dd/MM/yyyy}";

        public bool PeriodoValido =>
            FechaDesde.Date <= FechaHasta.Date;
    }
}
