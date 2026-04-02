using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using Microsoft.Win32;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WpfApp1.Data;
using WpfApp1.Models;


namespace WpfApp1.Views
{
    public partial class ReportesView : UserControl
    {
        // Guardamos la lista en memoria para poder exportarla luego
        private List<ReporteVenta> _ventasDelDia;

        public ReportesView()
        {
            InitializeComponent();
            dpFecha.SelectedDate = DateTime.Today; // Selecciona hoy por defecto
        }

        private void DpFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpFecha.SelectedDate.HasValue)
            {
                CargarReporte(dpFecha.SelectedDate.Value);
            }
        }

        private void CargarReporte(DateTime fecha)
        {
            string fechaFiltro = fecha.ToString("yyyy-MM-dd"); // Formato de SQLite

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Buscamos todas las ventas PAGADAS de ese día (usamos LIKE para ignorar la hora)
                string sql = "SELECT IdVenta, FechaVenta, IdMesa, TotalPagado FROM VentasCabecera WHERE Estado = 'Pagada' AND FechaVenta LIKE @Filtro";

                _ventasDelDia = conexion.Query<ReporteVenta>(sql, new { Filtro = fechaFiltro + "%" }).AsList();

                GridReporte.ItemsSource = _ventasDelDia;

                // Sumamos el total del día
                double granTotal = _ventasDelDia.Sum(v => v.TotalPagado);
                lblTotalDia.Text = $"Total del Día: ${granTotal:0.00}";
            }
        }

        private void BtnExportarWord_Click(object sender, RoutedEventArgs e)
        {
            if (_ventasDelDia == null || _ventasDelDia.Count == 0)
            {
                MessageBox.Show("No hay ventas para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Archivo PDF (*.pdf)|*.pdf";
            saveFile.FileName = $"Reporte_Ventas_{dpFecha.SelectedDate.Value:yyyyMMdd}.pdf";

            if (saveFile.ShowDialog() == true)
            {
                try
                {
                    using (PdfWriter writer = new PdfWriter(saveFile.FileName))
                    {
                        using (PdfDocument pdf = new PdfDocument(writer))
                        {
                            // 1. Usamos la ruta EXPLÍCITA de iText para que no choque con WPF
                            iText.Layout.Document doc = new iText.Layout.Document(pdf);

                            doc.Add(new iText.Layout.Element.Paragraph("BOARD GAME CAFE")
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                .SetFontSize(20));

                            doc.Add(new iText.Layout.Element.Paragraph($"Reporte de Ventas - {dpFecha.SelectedDate.Value:dd/MM/yyyy}")
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                .SetFontSize(14));

                            doc.Add(new iText.Layout.Element.Paragraph("\n"));

                            // 2. Tabla explícita
                            iText.Layout.Element.Table tabla = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 2, 4, 2, 2 })).UseAllAvailableWidth();

                            tabla.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Ticket")).SetBackgroundColor(ColorConstants.GRAY).SetFontColor(ColorConstants.WHITE));
                            tabla.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Fecha/Hora")).SetBackgroundColor(ColorConstants.GRAY).SetFontColor(ColorConstants.WHITE));
                            tabla.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Mesa")).SetBackgroundColor(ColorConstants.GRAY).SetFontColor(ColorConstants.WHITE));
                            tabla.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Total")).SetBackgroundColor(ColorConstants.GRAY).SetFontColor(ColorConstants.WHITE));

                            foreach (var venta in _ventasDelDia)
                            {
                                tabla.AddCell(venta.IdVenta.ToString());
                                tabla.AddCell(venta.FechaVenta);
                                tabla.AddCell(venta.IdMesa.ToString());
                                tabla.AddCell($"${venta.TotalPagado:F2}");
                            }

                            doc.Add(tabla);

                            doc.Add(new iText.Layout.Element.Paragraph("\n"));
                            doc.Add(new iText.Layout.Element.Paragraph(lblTotalDia.Text)
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                                .SetFontSize(16));

                            doc.Close();
                        }
                    }

                    MessageBox.Show("¡PDF generado correctamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al generar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
 
    }
}