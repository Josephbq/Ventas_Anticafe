using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using Dapper;
using WpfApp1.Data;
using WpfApp1.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace WpfApp1.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            CargarTodo();
        }

        private void CargarTodo()
        {
            try
            {
                CargarResumen();
                CargarTopProductos();
                CargarVentasPorHora();
                CargarDistribucionCategoria();
                CargarAlertas();
                CargarVentasPorDia();
            }
            catch (Exception ex)
            {
                // Silenciar errores si la BD está vacía o tablas no existen aún
                System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════
        // CARDS DE RESUMEN
        // ═══════════════════════════════════════════
        private void CargarResumen()
        {
            string hoy = DateTime.Now.ToString("yyyy-MM-dd");

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Ventas del día
                double ventasDia = conexion.ExecuteScalar<double>(
                    "SELECT COALESCE(SUM(TotalPagado), 0) FROM VentasCabecera WHERE Estado = 'Pagada' AND FechaVenta LIKE @f || '%'",
                    new { f = hoy });
                lblVentasDia.Text = $"{ventasDia:F2} Bs";

                // Ganancia del día
                double gananciaDia = conexion.ExecuteScalar<double>(@"
                    SELECT COALESCE(SUM(d.PrecioVendido * d.Cantidad - d.CostoAplicado * d.Cantidad), 0)
                    FROM VentasDetalle d
                    INNER JOIN VentasCabecera v ON d.IdVenta = v.IdVenta
                    WHERE v.Estado = 'Pagada' AND v.FechaVenta LIKE @f || '%'",
                    new { f = hoy });
                lblGananciaDia.Text = $"{gananciaDia:F2} Bs";

                // Alertas de stock
                int alertas = conexion.ExecuteScalar<int>(@"
                    SELECT COUNT(*) FROM Productos 
                    WHERE Activo = 1 AND (
                        (EsInventariable = 1 AND StockActual <= StockMinimo)
                        OR EstadoProducto IN ('Agotado', 'Dañado', 'Suspendido')
                    )");
                lblAlertasStock.Text = alertas.ToString();
                if (alertas > 0)
                    lblAlertasStock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));

                // Mesas activas
                int mesasOcupadas = conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM Mesas WHERE Estado = 'Ocupada'");
                int mesasTotal = conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM Mesas");
                lblMesasActivas.Text = mesasOcupadas.ToString();
                lblMesasTotal.Text = $" / {mesasTotal}";
            }
        }

        // ═══════════════════════════════════════════
        // TOP 5 PRODUCTOS MÁS VENDIDOS
        // ═══════════════════════════════════════════
        private void CargarTopProductos()
        {
            string hace30 = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                var top = conexion.Query<ProductoPopular>(@"
                    SELECT p.Nombre, CAST(SUM(d.Cantidad) AS INTEGER) as TotalVendido
                    FROM VentasDetalle d
                    INNER JOIN VentasCabecera v ON d.IdVenta = v.IdVenta
                    INNER JOIN Productos p ON d.IdProducto = p.IdProducto
                    WHERE v.Estado = 'Pagada' AND v.FechaVenta >= @f
                    GROUP BY p.Nombre
                    ORDER BY TotalVendido DESC LIMIT 5",
                    new { f = hace30 }).AsList();

                if (top.Count == 0)
                {
                    chartTopProductos.Series = Array.Empty<ISeries>();
                    return;
                }

                chartTopProductos.Series = new ISeries[]
                {
                    new ColumnSeries<int>
                    {
                        Values = top.Select(t => t.TotalVendido).ToArray(),
                        Fill = new SolidColorPaint(SKColor.Parse("#6B4226")),
                        MaxBarWidth = 40,
                        Padding = 8,
                        Rx = 4,
                        Ry = 4
                    }
                };

                chartTopProductos.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = top.Select(t => t.Nombre.Length > 12 ? t.Nombre.Substring(0, 12) + "…" : t.Nombre).ToArray(),
                        LabelsRotation = 0,
                        TextSize = 11,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#1A1514"))
                    }
                };

                chartTopProductos.YAxes = new Axis[]
                {
                    new Axis
                    {
                        TextSize = 11,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#8C5A35")),
                        MinLimit = 0
                    }
                };
            }
        }

        // ═══════════════════════════════════════════
        // VENTAS POR HORA
        // ═══════════════════════════════════════════
        private void CargarVentasPorHora()
        {
            string hoy = DateTime.Now.ToString("yyyy-MM-dd");

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                var porHora = conexion.Query<VentaPorHora>(@"
                    SELECT CAST(SUBSTR(FechaVenta, 12, 2) AS INTEGER) as Hora, 
                           COALESCE(SUM(TotalPagado), 0) as Total
                    FROM VentasCabecera
                    WHERE Estado = 'Pagada' AND FechaVenta LIKE @f || '%'
                    GROUP BY Hora ORDER BY Hora",
                    new { f = hoy }).AsList();

                // Llenar todas las horas del día (8-22)
                var horasCompletas = Enumerable.Range(8, 15)
                    .Select(h => new VentaPorHora
                    {
                        Hora = h,
                        Total = porHora.FirstOrDefault(p => p.Hora == h)?.Total ?? 0
                    }).ToList();

                chartVentasHora.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = horasCompletas.Select(h => h.Total).ToArray(),
                        Fill = new SolidColorPaint(SKColor.Parse("#8C5A35").WithAlpha(40)),
                        Stroke = new SolidColorPaint(SKColor.Parse("#8C5A35"), 3),
                        GeometryFill = new SolidColorPaint(SKColor.Parse("#6B4226")),
                        GeometryStroke = new SolidColorPaint(SKColor.Parse("#EFEBE4"), 2),
                        GeometrySize = 8,
                        LineSmoothness = 0.3
                    }
                };

                chartVentasHora.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = horasCompletas.Select(h => $"{h.Hora:00}:00").ToArray(),
                        LabelsRotation = -45,
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#1A1514"))
                    }
                };

                chartVentasHora.YAxes = new Axis[]
                {
                    new Axis
                    {
                        TextSize = 11,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#8C5A35")),
                        MinLimit = 0,
                        Labeler = v => $"{v:F0} Bs"
                    }
                };
            }
        }

        // ═══════════════════════════════════════════
        // DISTRIBUCIÓN JUEGOS VS CAFETERÍA
        // ═══════════════════════════════════════════
        private void CargarDistribucionCategoria()
        {
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                var categorias = conexion.Query<ResumenCategoria>(@"
                    SELECT d.TipoOrigen, COALESCE(SUM(d.Subtotal), 0) as Total
                    FROM VentasDetalle d
                    INNER JOIN VentasCabecera v ON d.IdVenta = v.IdVenta
                    WHERE v.Estado = 'Pagada'
                    GROUP BY d.TipoOrigen").AsList();

                if (categorias.Count == 0)
                {
                    chartCategoria.Series = Array.Empty<ISeries>();
                    return;
                }

                var colores = new[] { SKColor.Parse("#6B4226"), SKColor.Parse("#D9CBB8") };
                var series = new ISeries[categorias.Count];

                panelLeyenda.Children.Clear();

                for (int i = 0; i < categorias.Count; i++)
                {
                    var cat = categorias[i];
                    series[i] = new PieSeries<double>
                    {
                        Values = new double[] { cat.Total },
                        Name = cat.Etiqueta,
                        Fill = new SolidColorPaint(colores[i % colores.Length]),
                        Pushout = i == 0 ? 5 : 0
                    };

                    // Leyenda manual
                    var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 10, 0) };
                    sp.Children.Add(new Border
                    {
                        Width = 12, Height = 12,
                        CornerRadius = new CornerRadius(3),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(i == 0 ? "#6B4226" : "#D9CBB8")),
                        Margin = new Thickness(0, 0, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"{cat.Etiqueta}: {cat.Total:F2} Bs",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1514")),
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    panelLeyenda.Children.Add(sp);
                }

                chartCategoria.Series = series;
            }
        }

        // ═══════════════════════════════════════════
        // ALERTAS DE INVENTARIO
        // ═══════════════════════════════════════════
        private void CargarAlertas()
        {
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                var alertas = conexion.Query<AlertaStock>(@"
                    SELECT IdProducto, Nombre, StockActual, StockMinimo, EstadoProducto, Tipo
                    FROM Productos 
                    WHERE Activo = 1 AND (
                        (EsInventariable = 1 AND StockActual <= StockMinimo)
                        OR EstadoProducto IN ('Agotado', 'Dañado', 'Suspendido')
                    )
                    ORDER BY StockActual ASC").AsList();

                panelAlertas.Children.Clear();

                if (alertas.Count == 0)
                {
                    panelAlertas.Children.Add(new TextBlock
                    {
                        Text = "✅ Todo en orden — no hay alertas de inventario",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                        FontSize = 14,
                        Margin = new Thickness(0, 20, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                    return;
                }

                foreach (var alerta in alertas)
                {
                    var border = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8F0")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(alerta.ColorAlerta)),
                        BorderThickness = new Thickness(3, 0, 0, 0),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 10, 12, 10),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    var sp = new StackPanel();
                    sp.Children.Add(new TextBlock
                    {
                        Text = alerta.MensajeAlerta,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 13,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1514"))
                    });

                    if (alerta.Tipo == "CAFETERIA" && alerta.EstadoProducto == "Disponible")
                    {
                        sp.Children.Add(new TextBlock
                        {
                            Text = $"Stock: {alerta.StockActual} | Mínimo: {alerta.StockMinimo}",
                            FontSize = 11,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8C5A35")),
                            Margin = new Thickness(0, 3, 0, 0)
                        });
                    }

                    border.Child = sp;
                    panelAlertas.Children.Add(border);
                }
            }
        }

        // ═══════════════════════════════════════════
        // VENTAS POR DÍA DE SEMANA
        // ═══════════════════════════════════════════
        private void CargarVentasPorDia()
        {
            string hace28 = DateTime.Now.AddDays(-28).ToString("yyyy-MM-dd");
            string[] diasNombre = { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                var porDia = conexion.Query<VentaPorDia>(@"
                    SELECT CAST(strftime('%w', SUBSTR(FechaVenta, 1, 10)) AS INTEGER) as DiaNum,
                           COALESCE(SUM(TotalPagado), 0) as Total
                    FROM VentasCabecera
                    WHERE Estado = 'Pagada' AND FechaVenta >= @f
                    GROUP BY DiaNum ORDER BY DiaNum",
                    new { f = hace28 }).AsList();

                // Llenar todos los días
                var diasCompletos = Enumerable.Range(0, 7)
                    .Select(d => new VentaPorDia
                    {
                        DiaNum = d,
                        DiaSemana = diasNombre[d],
                        Total = porDia.FirstOrDefault(p => p.DiaNum == d)?.Total ?? 0
                    }).ToList();

                // Reordenar: Lun-Dom
                var ordenado = diasCompletos.Skip(1).Concat(diasCompletos.Take(1)).ToList();

                chartVentasDia.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = ordenado.Select(d => d.Total).ToArray(),
                        Fill = new SolidColorPaint(SKColor.Parse("#8C5A35")),
                        MaxBarWidth = 50,
                        Padding = 10,
                        Rx = 4,
                        Ry = 4
                    }
                };

                chartVentasDia.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = ordenado.Select(d => d.DiaSemana).ToArray(),
                        TextSize = 12,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#1A1514"))
                    }
                };

                chartVentasDia.YAxes = new Axis[]
                {
                    new Axis
                    {
                        TextSize = 11,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#8C5A35")),
                        MinLimit = 0,
                        Labeler = v => $"{v:F0} Bs"
                    }
                };
            }
        }
    }
}
