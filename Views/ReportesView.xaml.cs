using Microsoft.Win32;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Data;
using WpfApp1.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iTextDocument = iText.Layout.Document;
using iTextParagraph = iText.Layout.Element.Paragraph;
using iTextTable = iText.Layout.Element.Table;
using iTextCell = iText.Layout.Element.Cell;
using iTextTextAlignment = iText.Layout.Properties.TextAlignment;
using iTextUnitValue = iText.Layout.Properties.UnitValue;

namespace WpfApp1.Views
{
    public partial class ReportesView : UserControl
    {
        private string _periodo = "DIARIO";
        private string _categoria = "TODOS";
        private List<ReporteDetallado> _datosReporte = new();

        public ReportesView()
        {
            InitializeComponent();
            dpFecha.SelectedDate = DateTime.Today;
        }

        // ═══════════════════════════════════════════
        // FILTROS
        // ═══════════════════════════════════════════
        private void BtnPeriodo_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            _periodo = btn?.Tag?.ToString() ?? "DIARIO";

            btnDiario.Style = (Style)FindResource("ToggleBtn");
            btnSemanal.Style = (Style)FindResource("ToggleBtn");
            btnMensual.Style = (Style)FindResource("ToggleBtn");
            if (btn != null) btn.Style = (Style)FindResource("ToggleBtnActive");

            CargarReporte();
        }

        private void BtnCategoria_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            _categoria = btn?.Tag?.ToString() ?? "TODOS";

            btnCatTodos.Style = (Style)FindResource("ToggleBtn");
            btnCatCafe.Style = (Style)FindResource("ToggleBtn");
            btnCatJuegos.Style = (Style)FindResource("ToggleBtn");
            if (btn != null) btn.Style = (Style)FindResource("ToggleBtnActive");

            CargarReporte();
        }

        private void DpFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpFecha.SelectedDate.HasValue)
                CargarReporte();
        }

        // ═══════════════════════════════════════════
        // CARGAR REPORTE
        // ═══════════════════════════════════════════
        private void CargarReporte()
        {
            if (!dpFecha.SelectedDate.HasValue) return;

            DateTime fechaSel = dpFecha.SelectedDate.Value;
            string fechaInicio, fechaFin, rangoTexto;

            switch (_periodo)
            {
                case "SEMANAL":
                    int diff = (7 + ((int)fechaSel.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                    DateTime inicioSemana = fechaSel.AddDays(-diff);
                    DateTime finSemana = inicioSemana.AddDays(7);
                    fechaInicio = inicioSemana.ToString("yyyy-MM-dd");
                    fechaFin = finSemana.ToString("yyyy-MM-dd");
                    rangoTexto = $"Semana: {inicioSemana:dd/MM} — {finSemana.AddDays(-1):dd/MM/yyyy}";
                    break;

                case "MENSUAL":
                    DateTime inicioMes = new DateTime(fechaSel.Year, fechaSel.Month, 1);
                    DateTime finMes = inicioMes.AddMonths(1);
                    fechaInicio = inicioMes.ToString("yyyy-MM-dd");
                    fechaFin = finMes.ToString("yyyy-MM-dd");
                    rangoTexto = $"Mes: {inicioMes:MMMM yyyy}";
                    break;

                default: // DIARIO
                    fechaInicio = fechaSel.ToString("yyyy-MM-dd");
                    fechaFin = fechaSel.AddDays(1).ToString("yyyy-MM-dd");
                    rangoTexto = $"Día: {fechaSel:dddd dd/MM/yyyy}";
                    break;
            }

            lblRangoFechas.Text = rangoTexto;

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                string sql = @"
                    SELECT 
                        p.Nombre as NombreProducto,
                        p.Tipo,
                        CAST(SUM(d.Cantidad) AS INTEGER) as CantidadVendida,
                        AVG(d.PrecioVendido) as PrecioPromedioVenta,
                        AVG(d.CostoAplicado) as CostoPromedio,
                        SUM(d.Subtotal) as TotalVentas,
                        SUM(d.Cantidad * d.CostoAplicado) as TotalCosto,
                        SUM(d.Subtotal) - SUM(d.Cantidad * d.CostoAplicado) as Ganancia
                    FROM VentasDetalle d
                    INNER JOIN VentasCabecera v ON d.IdVenta = v.IdVenta
                    INNER JOIN Productos p ON d.IdProducto = p.IdProducto
                    WHERE v.Estado = 'Pagada' 
                      AND v.FechaVenta >= @Inicio 
                      AND v.FechaVenta < @Fin";

                if (_categoria != "TODOS")
                    sql += " AND p.Tipo = @Cat";

                sql += @"
                    GROUP BY p.IdProducto, p.Nombre, p.Tipo
                    ORDER BY TotalVentas DESC";

                _datosReporte = conexion.Query<ReporteDetallado>(sql, new
                {
                    Inicio = fechaInicio,
                    Fin = fechaFin,
                    Cat = _categoria
                }).AsList();

                GridReporte.ItemsSource = _datosReporte;

                double totalVentas = _datosReporte.Sum(r => r.TotalVentas);
                double totalCosto = _datosReporte.Sum(r => r.TotalCosto);
                double ganancia = _datosReporte.Sum(r => r.Ganancia);

                lblTotalVentas.Text = $"{totalVentas:F2} Bs";
                lblTotalCosto.Text = $"{totalCosto:F2} Bs";
                lblGanancia.Text = $"{ganancia:F2} Bs";
                lblGranTotal.Text = $"{totalVentas:F2} Bs";

                string sqlTrans = @"
                    SELECT COUNT(DISTINCT v.IdVenta) 
                    FROM VentasCabecera v
                    INNER JOIN VentasDetalle d ON v.IdVenta = d.IdVenta
                    INNER JOIN Productos p ON d.IdProducto = p.IdProducto
                    WHERE v.Estado = 'Pagada' 
                      AND v.FechaVenta >= @Inicio AND v.FechaVenta < @Fin";

                if (_categoria != "TODOS")
                    sqlTrans += " AND p.Tipo = @Cat";

                int trans = conexion.ExecuteScalar<int>(sqlTrans, new
                {
                    Inicio = fechaInicio,
                    Fin = fechaFin,
                    Cat = _categoria
                });
                lblTransacciones.Text = trans.ToString();
            }
        }

        // ═══════════════════════════════════════════
        // EXPORTAR PDF
        // ═══════════════════════════════════════════
        private void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_datosReporte == null || _datosReporte.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFile = new SaveFileDialog
            {
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                FileName = $"Reporte_{_periodo}_{dpFecha.SelectedDate?.ToString("yyyyMMdd") ?? "sin_fecha"}.pdf"
            };

            if (saveFile.ShowDialog() == true)
            {
                try
                {
                    using (PdfWriter writer = new PdfWriter(saveFile.FileName))
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        var doc = new iTextDocument(pdf);
                        var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                        doc.Add(new iTextParagraph("COCOCAFE JOGOS")
                            .SetTextAlignment(iTextTextAlignment.CENTER)
                            .SetFont(boldFont)
                            .SetFontSize(22));

                        doc.Add(new iTextParagraph($"Reporte de Ventas — {lblRangoFechas.Text}")
                            .SetTextAlignment(iTextTextAlignment.CENTER)
                            .SetFontSize(13));

                        if (_categoria != "TODOS")
                        {
                            doc.Add(new iTextParagraph($"Categoría: {_categoria}")
                                .SetTextAlignment(iTextTextAlignment.CENTER)
                                .SetFontSize(11));
                        }

                        doc.Add(new iTextParagraph("\n"));

                        // Resumen
                        doc.Add(new iTextParagraph("RESUMEN").SetFontSize(14).SetFont(boldFont));

                        var tablaResumen = new iTextTable(iTextUnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1 }))
                            .UseAllAvailableWidth();
                        tablaResumen.AddHeaderCell(CrearCeldaHeader("Total Ventas", boldFont));
                        tablaResumen.AddHeaderCell(CrearCeldaHeader("Total Costo", boldFont));
                        tablaResumen.AddHeaderCell(CrearCeldaHeader("Ganancia", boldFont));
                        tablaResumen.AddHeaderCell(CrearCeldaHeader("Transacciones", boldFont));
                        tablaResumen.AddCell(lblTotalVentas.Text);
                        tablaResumen.AddCell(lblTotalCosto.Text);
                        tablaResumen.AddCell(lblGanancia.Text);
                        tablaResumen.AddCell(lblTransacciones.Text);
                        doc.Add(tablaResumen);

                        doc.Add(new iTextParagraph("\n"));

                        // Agrupar por categoría
                        var categorias = _datosReporte.GroupBy(r => r.Tipo).OrderBy(g => g.Key);

                        foreach (var grupo in categorias)
                        {
                            string tituloGrupo = grupo.Key == "JUEGOS" ? "JUEGOS" : "CAFETERIA";
                            doc.Add(new iTextParagraph($"\n{tituloGrupo}")
                                .SetFontSize(14).SetFont(boldFont));

                            var tabla = new iTextTable(iTextUnitValue.CreatePercentArray(
                                new float[] { 3, 1, 2, 2, 2, 2 })).UseAllAvailableWidth();

                            tabla.AddHeaderCell(CrearCeldaHeader("Producto", boldFont));
                            tabla.AddHeaderCell(CrearCeldaHeader("Cant.", boldFont));
                            tabla.AddHeaderCell(CrearCeldaHeader("Precio Prom.", boldFont));
                            tabla.AddHeaderCell(CrearCeldaHeader("Costo Prom.", boldFont));
                            tabla.AddHeaderCell(CrearCeldaHeader("Total", boldFont));
                            tabla.AddHeaderCell(CrearCeldaHeader("Ganancia", boldFont));

                            double subTotalVentas = 0, subGanancia = 0;

                            foreach (var item in grupo)
                            {
                                tabla.AddCell(item.NombreProducto);
                                tabla.AddCell(item.CantidadVendida.ToString());
                                tabla.AddCell($"{item.PrecioPromedioVenta:F2} Bs");
                                tabla.AddCell($"{item.CostoPromedio:F2} Bs");
                                tabla.AddCell($"{item.TotalVentas:F2} Bs");
                                tabla.AddCell($"{item.Ganancia:F2} Bs");
                                subTotalVentas += item.TotalVentas;
                                subGanancia += item.Ganancia;
                            }

                            doc.Add(tabla);
                            doc.Add(new iTextParagraph($"Subtotal {grupo.Key}: {subTotalVentas:F2} Bs | Ganancia: {subGanancia:F2} Bs")
                                .SetTextAlignment(iTextTextAlignment.RIGHT)
                                .SetFontSize(11));
                        }

                        doc.Add(new iTextParagraph("\n"));
                        doc.Add(new iTextParagraph($"TOTAL GENERAL: {lblGranTotal.Text}")
                            .SetTextAlignment(iTextTextAlignment.RIGHT)
                            .SetFontSize(16)
                            .SetFont(boldFont));

                        doc.Add(new iTextParagraph($"\nGenerado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .SetTextAlignment(iTextTextAlignment.RIGHT)
                            .SetFontSize(9));

                        doc.Close();
                    }

                    MessageBox.Show("¡PDF generado correctamente!", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private iTextCell CrearCeldaHeader(string texto, PdfFont font)
        {
            return new iTextCell()
                .Add(new iTextParagraph(texto).SetFontSize(10).SetFont(font))
                .SetBackgroundColor(new DeviceRgb(61, 43, 31))
                .SetFontColor(ColorConstants.WHITE)
                .SetPadding(5);
        }
    }
}