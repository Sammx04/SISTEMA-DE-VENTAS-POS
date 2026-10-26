using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using ProySistemaVentas.Models.PrestamosCreditos;

namespace ProySistemaVentas.Helpers
{
    public static class PrestamoCreditoExcelExporter
    {
        public static void ExportarPrestamos(
            IEnumerable<PrestamoCreditoItem> prestamos,
            string rutaArchivo)
        {
            List<PrestamoCreditoItem> items = ValidarArgumentos(
                prestamos,
                rutaArchivo);

            using XLWorkbook libro = new XLWorkbook();
            IXLWorksheet hoja = libro.Worksheets.Add("Prestamos");

            string[] encabezados =
            {
                "N. OPERACION", "PRODUCTO", "VARIANTE", "CODIGO BARRA",
                "CLIENTE / TIENDA", "CANTIDAD", "TIPO PRECIO", "PRECIO UNITARIO",
                "TOTAL", "FECHA PRESTAMO", "ESTADO", "FECHA DEVOLUCION", "ACCION FINAL", "REGISTRADO POR",
                "OBSERVACION"
            };

            EscribirEncabezados(hoja, encabezados);

            int fila = 2;
            foreach (PrestamoCreditoItem item in items)
            {
                hoja.Cell(fila, 1).Value = item.NumeroOperacion;
                hoja.Cell(fila, 2).Value = item.NombreProducto;
                hoja.Cell(fila, 3).Value = item.Variante;
                hoja.Cell(fila, 4).Value = item.CodigoBarra;
                hoja.Cell(fila, 5).Value = item.ClienteTienda;
                hoja.Cell(fila, 6).Value = item.Cantidad;
                hoja.Cell(fila, 7).Value = item.TipoPrecio;
                hoja.Cell(fila, 8).Value = item.PrecioUnitario;
                hoja.Cell(fila, 9).Value = item.Total;
                hoja.Cell(fila, 10).Value = item.FechaRegistro;
                hoja.Cell(fila, 11).Value = item.EstadoMostrar;
                hoja.Cell(fila, 12).Value = item.FechaFinalizacion;
                hoja.Cell(fila, 13).Value = item.AccionFinal;
                hoja.Cell(fila, 14).Value = item.NombreTrabajadorRegistro;
                hoja.Cell(fila, 15).Value = item.Observacion;
                fila++;
            }

            DarFormatoYGuardar(hoja, rutaArchivo);
        }

        public static void ExportarCreditos(
            IEnumerable<PrestamoCreditoItem> creditos,
            string rutaArchivo)
        {
            List<PrestamoCreditoItem> items = ValidarArgumentos(
                creditos,
                rutaArchivo);

            using XLWorkbook libro = new XLWorkbook();
            IXLWorksheet hoja = libro.Worksheets.Add("Creditos");

            string[] encabezados =
            {
                "N. OPERACION", "CLIENTE / TIENDA", "PRODUCTO", "VARIANTE",
                "CANTIDAD", "PRECIO UNITARIO", "DEUDA TOTAL", "FECHA REGISTRO",
                "FECHA VENCIMIENTO", "ESTADO", "FECHA PAGO", "N. PAGO",
                "METODO PAGO", "REGISTRADO POR", "OBSERVACION"
            };

            EscribirEncabezados(hoja, encabezados);

            int fila = 2;
            foreach (PrestamoCreditoItem item in items)
            {
                hoja.Cell(fila, 1).Value = item.NumeroOperacion;
                hoja.Cell(fila, 2).Value = item.ClienteTienda;
                hoja.Cell(fila, 3).Value = item.NombreProducto;
                hoja.Cell(fila, 4).Value = item.Variante;
                hoja.Cell(fila, 5).Value = item.Cantidad;
                hoja.Cell(fila, 6).Value = item.PrecioUnitario;
                hoja.Cell(fila, 7).Value = item.Total;
                hoja.Cell(fila, 8).Value = item.FechaRegistro;
                hoja.Cell(fila, 9).Value = item.FechaPagoEsperada;
                hoja.Cell(fila, 10).Value = item.EstadoMostrar;
                hoja.Cell(fila, 11).Value = item.FechaPago;
                hoja.Cell(fila, 12).Value = item.NumeroPago;
                hoja.Cell(fila, 13).Value = item.MetodoPago;
                hoja.Cell(fila, 14).Value = item.NombreTrabajadorRegistro;
                hoja.Cell(fila, 15).Value = item.Observacion;
                fila++;
            }

            DarFormatoYGuardar(hoja, rutaArchivo);
        }

        private static List<PrestamoCreditoItem> ValidarArgumentos(
            IEnumerable<PrestamoCreditoItem> items,
            string rutaArchivo)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentException.ThrowIfNullOrWhiteSpace(rutaArchivo);

            List<PrestamoCreditoItem> lista = items.ToList();
            if (lista.Any(item => item is null))
            {
                throw new ArgumentException(
                    "La colección no puede contener elementos nulos.",
                    nameof(items));
            }

            return lista;
        }

        private static void EscribirEncabezados(
            IXLWorksheet hoja,
            IReadOnlyList<string> encabezados)
        {
            for (int columna = 0; columna < encabezados.Count; columna++)
            {
                hoja.Cell(1, columna + 1).Value = encabezados[columna];
            }

            IXLRange rangoEncabezados = hoja.Range(1, 1, 1, encabezados.Count);
            rangoEncabezados.Style.Font.Bold = true;
            rangoEncabezados.Style.Fill.BackgroundColor = XLColor.FromHtml("1F4E78");
            rangoEncabezados.Style.Font.FontColor = XLColor.White;
        }

        private static void DarFormatoYGuardar(
            IXLWorksheet hoja,
            string rutaArchivo)
        {
            // Los encabezados se escriben antes de aplicar el filtro.
            IXLRange rangoUsado = hoja.RangeUsed()!;
            rangoUsado.SetAutoFilter();
            hoja.SheetView.FreezeRows(1);
            hoja.Columns().AdjustToContents();

            foreach (IXLColumn columna in hoja.Columns())
            {
                if (columna.Width > 40)
                {
                    columna.Width = 40;
                }
            }

            hoja.ColumnsUsed()
                .Where(columna => columna.FirstCell().GetString().Contains("FECHA"))
                .ToList()
                .ForEach(columna => columna.Style.DateFormat.Format = "dd/MM/yyyy HH:mm");

            hoja.ColumnsUsed()
                .Where(columna => columna.FirstCell().GetString().Contains("PRECIO") ||
                                   columna.FirstCell().GetString().Contains("DEUDA") ||
                                   columna.FirstCell().GetString().Equals("TOTAL"))
                .ToList()
                .ForEach(columna => columna.Style.NumberFormat.Format = "#,##0.00");

            hoja.Workbook.SaveAs(rutaArchivo);
        }
    }
}
