using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;

namespace ProySistemaVentas.Services.Reporte
{
    /// <summary>
    /// Tabla genérica de reporte: encabezados, filas y
    /// una línea de pie con los totales.
    ///
    /// Es el formato intermedio que consumen tanto la
    /// exportación a CSV como la impresión, para que las
    /// dos salidas siempre muestren exactamente la misma
    /// información.
    /// </summary>
    public class TablaReporte
    {
        public string Titulo { get; set; } = string.Empty;

        public string Subtitulo { get; set; } = string.Empty;

        public string Pie { get; set; } = string.Empty;

        public List<string> Encabezados { get; } =
            new List<string>();

        public List<string[]> Filas { get; } =
            new List<string[]>();

        public int TotalFilas =>
            Filas.Count;
    }

    /// <summary>
    /// Exporta e imprime los reportes de SistemaVentas.
    ///
    /// CSV: separador ';' y BOM UTF-8, que es lo que
    /// Excel en español abre sin preguntar nada.
    /// Impresión: FlowDocument con paginación
    /// automática, el mecanismo estándar de WPF para
    /// tablas largas.
    /// </summary>
    public static class ReporteExportService
    {
        private const string SeparadorCsv = ";";

        // =====================================================
        // EXPORTACIÓN A CSV
        // =====================================================

        /// <summary>
        /// Abre el diálogo de guardado y escribe el
        /// archivo CSV. Devuelve la ruta escrita o
        /// cadena vacía si el usuario cancela.
        /// </summary>
        public static string
            GuardarCsv(
                TablaReporte tabla,
                string nombreSugerido)
        {
            if (tabla == null ||
                tabla.Encabezados.Count == 0)
            {
                return string.Empty;
            }

            SaveFileDialog dialogo =
                new SaveFileDialog
                {
                    Title =
                        "Guardar reporte en CSV",

                    Filter =
                        "Archivo CSV (*.csv)|*.csv",

                    FileName =
                        $"{nombreSugerido}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",

                    AddExtension = true,

                    DefaultExt = ".csv"
                };

            bool? resultado =
                dialogo.ShowDialog();

            if (resultado != true)
            {
                return string.Empty;
            }

            EscribirCsv(
                tabla,
                dialogo.FileName);

            return dialogo.FileName;
        }

        /// <summary>
        /// Escribe el CSV en la ruta indicada. Lanza
        /// IOException si no se puede escribir.
        /// </summary>
        public static void
            EscribirCsv(
                TablaReporte tabla,
                string rutaArchivo)
        {
            /*
             * new UTF8Encoding(true) agrega el BOM, sin
             * el cual Excel rompe los acentos de
             * "Almacén", "Categoría" y los nombres.
             */
            using StreamWriter escritor =
                new StreamWriter(
                    rutaArchivo,
                    false,
                    new UTF8Encoding(true));

            if (!string.IsNullOrWhiteSpace(
                    tabla.Titulo))
            {
                escritor.WriteLine(
                    EscaparCsv(tabla.Titulo));
            }

            if (!string.IsNullOrWhiteSpace(
                    tabla.Subtitulo))
            {
                escritor.WriteLine(
                    EscaparCsv(tabla.Subtitulo));
            }

            if (!string.IsNullOrWhiteSpace(
                    tabla.Titulo) ||
                !string.IsNullOrWhiteSpace(
                    tabla.Subtitulo))
            {
                escritor.WriteLine();
            }

            escritor.WriteLine(
                string.Join(
                    SeparadorCsv,
                    tabla.Encabezados.Select(
                        EscaparCsv)));

            foreach (
                string[] fila
                in tabla.Filas)
            {
                escritor.WriteLine(
                    string.Join(
                        SeparadorCsv,
                        fila.Select(EscaparCsv)));
            }

            if (!string.IsNullOrWhiteSpace(
                    tabla.Pie))
            {
                escritor.WriteLine();
                escritor.WriteLine(
                    EscaparCsv(tabla.Pie));
            }
        }

        private static string
            EscaparCsv(string? valor)
        {
            string texto =
                valor ?? string.Empty;

            bool necesitaComillas =
                texto.Contains('"') ||
                texto.Contains(SeparadorCsv) ||
                texto.Contains('\n') ||
                texto.Contains('\r');

            texto = texto.Replace(
                "\"",
                "\"\"");

            return necesitaComillas
                ? $"\"{texto}\""
                : texto;
        }

        // =====================================================
        // IMPRESIÓN
        // =====================================================

        /// <summary>
        /// Abre el diálogo de impresión y envía el
        /// reporte. Devuelve true si el usuario confirmó
        /// y el documento se imprimió.
        /// </summary>
        public static bool
            ImprimirReporte(TablaReporte tabla)
        {
            if (tabla == null ||
                tabla.Encabezados.Count == 0)
            {
                return false;
            }

            PrintDialog dialogoImpresion =
                new PrintDialog();

            bool? resultado =
                dialogoImpresion.ShowDialog();

            if (resultado != true)
            {
                return false;
            }

            FlowDocument documento =
                ConstruirDocumento(tabla);

            /*
             * FlowDocument implementa IDocumentPaginatorSource
             * de forma explicita, asi que el paginador se
             * obtiene por la interfaz y no por propiedad.
             */
            IDocumentPaginatorSource paginador =
                documento;

            dialogoImpresion.PrintDocument(
                paginador.DocumentPaginator,
                tabla.Titulo);

            return true;
        }

        private static FlowDocument
            ConstruirDocumento(TablaReporte tabla)
        {
            FlowDocument documento =
                new FlowDocument
                {
                    FontFamily =
                        new FontFamily("Segoe UI"),

                    FontSize = 9,

                    PagePadding =
                        new Thickness(32),

                    ColumnWidth =
                        double.PositiveInfinity
                };

            /*
             * Orientación horizontal: los reportes de
             * ventas y de inventario tienen muchas
             * columnas y en vertical se partirían de
             * forma ilegible.
             */
            documento.PageWidth =
                29.7 * 96 / 2.54;

            documento.PageHeight =
                21 * 96 / 2.54;

            documento.PagePadding =
                new Thickness(32);

            documento.ColumnGap = 0;

            /*
             * Los titulos se anaden como bloques
             * sueltos: en FlowDocument no se puede
             * insertar un StackPanel porque no es un
             * Block.
             */
            AgregarEncabezado(
                documento,
                tabla);

            documento.Blocks.Add(
                ConstruirTabla(tabla));

            if (!string.IsNullOrWhiteSpace(
                    tabla.Pie))
            {
                documento.Blocks.Add(
                    ConstruirPie(tabla));
            }

            return documento;
        }

        private static void
            AgregarEncabezado(
                FlowDocument documento,
                TablaReporte tabla)
        {
            if (!string.IsNullOrWhiteSpace(
                    tabla.Titulo))
            {
                documento.Blocks.Add(
                    new Paragraph(
                        new Run(
                            tabla.Titulo))
                    {
                        FontSize = 16,

                        FontWeight =
                            FontWeights.Bold
                    });
            }

            if (!string.IsNullOrWhiteSpace(
                    tabla.Subtitulo))
            {
                documento.Blocks.Add(
                    new Paragraph(
                        new Run(
                            tabla.Subtitulo))
                    {
                        FontSize = 9,

                        Foreground =
                            Brushes.DimGray,

                        Margin =
                            new Thickness(0, 4, 0, 10)
                    });
            }

            documento.Blocks.Add(
                new Paragraph(
                    new Run(
                        $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}"))
                {
                    FontSize = 8,

                    Foreground =
                        Brushes.Gray,

                    Margin =
                        new Thickness(0, 0, 0, 10)
                });
        }

        private static Table
                ConstruirTabla(
                    TablaReporte tabla)
        {
            Table grid =
                new Table
                {
                    CellSpacing = 0,

                    BorderThickness =
                        new Thickness(0.5),

                    BorderBrush =
                        Brushes.Gray
                };

            /*
             * La primera columna (codigo o fecha) es
             * estrecha, la segunda identifiica la fila y
             * el resto se reparte por igual.
             */
            for (int indice = 0;
                 indice < tabla.Encabezados.Count;
                 indice++)
            {
                bool esEstrecha = indice < 2;

                grid.Columns.Add(
                    new TableColumn
                    {
                        Width =
                            esEstrecha
                                ? new GridLength(
                                    indice == 0
                                        ? 90
                                        : 130)
                                : new GridLength(
                                    1,
                                    GridUnitType.Star)
                    });
            }

            /*
             * En WPF las filas cuelgan de un TableRowGroup,
             * no directamente de la Table.
             */
            TableRowGroup grupo =
                new TableRowGroup();

            grupo.Rows.Add(
                ConstruirFilaEncabezado(
                    tabla.Encabezados));

            foreach (
                string[] fila
                in tabla.Filas)
            {
                grupo.Rows.Add(
                    ConstruirFila(fila));
            }

            grid.RowGroups.Add(grupo);

            return grid;
        }

        private static TableRow
            ConstruirFilaEncabezado(
                List<string> encabezados)
        {
            TableRow fila =
                new TableRow
                {
                    Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                0xE2,
                                0xE8,
                                0xF0))
                };

            foreach (
                string encabezado
                in encabezados)
            {
                fila.Cells.Add(
                    ConstruirCelda(
                        encabezado,
                        FontWeights.Bold,
                        Brushes.Black));
            }

            return fila;
        }

        private static TableRow
            ConstruirFila(string[] valores)
        {
            TableRow fila =
                new TableRow();

            foreach (
                string valor
                in valores)
            {
                fila.Cells.Add(
                    ConstruirCelda(
                        valor ?? string.Empty,
                        FontWeights.Normal,
                        Brushes.Black));
            }

            return fila;
        }

        private static TableCell
            ConstruirCelda(
                string texto,
                FontWeight peso,
                Brush color)
        {
            /*
             * TableCell solo admite un Block, por eso
             * el borde y el texto se componen con los
             * elementos de System.Windows.Documents
             * (Paragraph + Border de Documents) en
             * lugar de controles de UI.
             */
            return new TableCell(
                new Paragraph(
                    new Run(
                        texto)
                    {
                        FontSize = 8,

                        FontWeight = peso,

                        Foreground = color
                    })
                {
                    Margin =
                        new Thickness(
                            4, 2, 4, 2)
                })
            {
                BorderThickness =
                    new Thickness(
                        0.3, 0.3, 0.3, 0.3),

                BorderBrush =
                    Brushes.LightGray
            };
        }

        private static Block
                ConstruirPie(
                    TablaReporte tabla)
        {
            return new Paragraph(
                new Run(tabla.Pie))
            {
                FontSize = 9,

                FontWeight =
                    FontWeights.SemiBold,

                Margin =
                    new Thickness(0, 10, 0, 0)
            };
        }

        // =====================================================
        // FORMATEO
        // =====================================================

        public static string
            Moneda(decimal valor)
        {
            return valor.ToString(
                "N2",
                CultureInfo.InvariantCulture);
        }

        public static string
            Entero(int valor)
        {
            return valor.ToString(
                "N0",
                CultureInfo.InvariantCulture);
        }

        public static string
            Fecha(DateTime valor)
        {
            return valor.ToString(
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture);
        }

        public static string
            FechaHora(DateTime valor)
        {
            return valor.ToString(
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture);
        }
    }
}
