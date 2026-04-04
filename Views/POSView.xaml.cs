using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Data.Sqlite;
using Dapper;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.Views
{
    public class ItemTicket
    {
        public int IdDetalle { get; set; }
        public string NombreProducto { get; set; } = "";
        public int Cantidad { get; set; }
        public double PrecioUnitario { get; set; }
        public double Subtotal { get; set; }

        public override string ToString() => $"{Cantidad}x {NombreProducto} — {Subtotal:F2} Bs";
    }

    public partial class POSView : UserControl
    {
        private Mesa? _mesaActual;
        private string _filtroCategoria = "TODOS";

        public POSView()
        {
            InitializeComponent();
            CargarMesas();
            CargarProductos();
        }

        // ═══════════════════════════════════════════
        // MESAS
        // ═══════════════════════════════════════════
        private void CargarMesas()
        {
            PanelMesas.Children.Clear();

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                var mesas = conexion.Query<Mesa>("SELECT * FROM Mesas").AsList();

                foreach (var mesa in mesas)
                {
                    var btnMesa = new Button();
                    btnMesa.Width = 130;
                    btnMesa.Height = 90;
                    btnMesa.Margin = new Thickness(0, 0, 10, 10);
                    btnMesa.Cursor = System.Windows.Input.Cursors.Hand;
                    btnMesa.Tag = mesa;
                    btnMesa.Click += BtnMesa_Click;

                    // Contenido del botón
                    var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                    sp.Children.Add(new TextBlock
                    {
                        Text = mesa.Nombre,
                        FontSize = 15,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text = mesa.Estado == "Libre" ? "● Libre" : "● Ocupada",
                        FontSize = 11,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 0),
                        Foreground = mesa.Estado == "Libre"
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                            : new SolidColorBrush(Colors.White)
                    });

                    btnMesa.Content = sp;

                    // Template para bordes redondeados
                    var template = new ControlTemplate(typeof(Button));
                    var borderFactory = new FrameworkElementFactory(typeof(Border));
                    borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
                    borderFactory.SetValue(Border.PaddingProperty, new Thickness(10));
                    borderFactory.Name = "border";

                    if (mesa.Estado == "Libre")
                    {
                        borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEBE4")));
                        borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9CBB8")));
                        borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(2));
                        btnMesa.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1514"));
                    }
                    else
                    {
                        borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D2B1F")));
                        borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8C5A35")));
                        borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(2));
                        btnMesa.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEBE4"));
                    }

                    var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                    contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                    borderFactory.AppendChild(contentPresenter);
                    template.VisualTree = borderFactory;
                    btnMesa.Template = template;

                    PanelMesas.Children.Add(btnMesa);
                }
            }
        }

        // ═══════════════════════════════════════════
        // PRODUCTOS
        // ═══════════════════════════════════════════
        private void CargarProductos()
        {
            PanelProductos.Children.Clear();

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                string sql = "SELECT * FROM Productos WHERE Activo = 1";
                if (_filtroCategoria != "TODOS")
                    sql += " AND Tipo = @tipo";

                var productos = conexion.Query<Producto>(sql, new { tipo = _filtroCategoria }).AsList();

                foreach (var prod in productos)
                {
                    var btnProd = new Button();
                    btnProd.Width = 140;
                    btnProd.Height = 100;
                    btnProd.Margin = new Thickness(0, 0, 10, 10);
                    btnProd.Cursor = System.Windows.Input.Cursors.Hand;
                    btnProd.Tag = prod;
                    btnProd.Click += BtnProducto_Click;

                    bool disponible = prod.EstadoProducto == "Disponible" &&
                                      (prod.EsInventariable == 0 || prod.StockActual > 0);
                    btnProd.IsEnabled = disponible;

                    // Contenido
                    var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

                    sp.Children.Add(new TextBlock
                    {
                        Text = prod.Tipo == "JUEGOS" ? "🎲" : "☕",
                        FontSize = 18,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text = prod.Nombre,
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 120
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"{prod.PrecioVentaBase:F2} Bs",
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 3, 0, 0)
                    });

                    // Indicador de stock
                    if (prod.EsInventariable == 1)
                    {
                        string stockLabel = prod.StockActual == 0 ? "Sin stock" : $"Stock: {prod.StockActual}";
                        sp.Children.Add(new TextBlock
                        {
                            Text = stockLabel,
                            FontSize = 9,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = prod.StockActual <= prod.StockMinimo
                                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"))
                                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                        });
                    }
                    else if (prod.EstadoProducto != "Disponible")
                    {
                        sp.Children.Add(new TextBlock
                        {
                            Text = prod.EstadoProducto,
                            FontSize = 9,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"))
                        });
                    }

                    btnProd.Content = sp;

                    // Template con bordes redondeados
                    var template = new ControlTemplate(typeof(Button));
                    var borderFactory = new FrameworkElementFactory(typeof(Border));
                    borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
                    borderFactory.SetValue(Border.PaddingProperty, new Thickness(8));
                    borderFactory.Name = "border";

                    Color bgColor, fgColor;
                    if (!disponible)
                    {
                        bgColor = (Color)ColorConverter.ConvertFromString("#E0D5C8");
                        fgColor = (Color)ColorConverter.ConvertFromString("#999");
                    }
                    else if (prod.Tipo == "JUEGOS")
                    {
                        bgColor = (Color)ColorConverter.ConvertFromString("#1A1514");
                        fgColor = (Color)ColorConverter.ConvertFromString("#EFEBE4");
                    }
                    else
                    {
                        bgColor = (Color)ColorConverter.ConvertFromString("#6B4226");
                        fgColor = (Color)ColorConverter.ConvertFromString("#EFEBE4");
                    }

                    borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(bgColor));
                    btnProd.Foreground = new SolidColorBrush(fgColor);

                    var cp = new FrameworkElementFactory(typeof(ContentPresenter));
                    cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                    borderFactory.AppendChild(cp);
                    template.VisualTree = borderFactory;
                    btnProd.Template = template;

                    PanelProductos.Children.Add(btnProd);
                }
            }
        }

        // ═══════════════════════════════════════════
        // TABS DE CATEGORÍA
        // ═══════════════════════════════════════════
        private void BtnTab_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            _filtroCategoria = boton?.Tag?.ToString() ?? "TODOS";

            // Actualizar estilos de tabs
            btnTabTodos.Style = (Style)FindResource("ToggleBtn");
            btnTabCafe.Style = (Style)FindResource("ToggleBtn");
            btnTabJuegos.Style = (Style)FindResource("ToggleBtn");

            if (boton != null)
                boton.Style = (Style)FindResource("ToggleBtnActive");

            CargarProductos();
        }

        // ═══════════════════════════════════════════
        // CLICK EN PRODUCTO
        // ═══════════════════════════════════════════
        private void BtnProducto_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            var prod = boton?.Tag as Producto;
            if (prod == null) return;

            if (_mesaActual == null || _mesaActual.Estado == "Libre")
            {
                MessageBox.Show("Primero debes seleccionar y abrir una mesa.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                int idVentaAbierta = conexion.ExecuteScalar<int>(
                    "SELECT IdVenta FROM VentasCabecera WHERE IdMesa = @IdMesa AND Estado = 'Abierta'",
                    new { IdMesa = _mesaActual.IdMesa });

                if (idVentaAbierta > 0)
                {
                    // Obtener costo del último lote (para cafetería)
                    double costoAplicado = 0;
                    if (prod.EsInventariable == 1)
                    {
                        costoAplicado = conexion.ExecuteScalar<double>(
                            "SELECT COALESCE(CostoUnitario, 0) FROM LotesInventario WHERE IdProducto = @Id ORDER BY IdLote DESC LIMIT 1",
                            new { Id = prod.IdProducto });
                    }

                    conexion.Execute(@"
                        INSERT INTO VentasDetalle (IdVenta, IdProducto, Cantidad, PrecioVendido, CostoAplicado, Subtotal, TipoOrigen) 
                        VALUES (@IdVenta, @IdProd, 1, @Precio, @Costo, @Precio, @TipoOrigen)",
                        new
                        {
                            IdVenta = idVentaAbierta,
                            IdProd = prod.IdProducto,
                            Precio = prod.PrecioVentaBase,
                            Costo = costoAplicado,
                            TipoOrigen = prod.Tipo
                        });

                    // Descontar stock para inventariables
                    if (prod.EsInventariable == 1)
                    {
                        conexion.Execute(
                            "UPDATE Productos SET StockActual = MAX(0, StockActual - 1) WHERE IdProducto = @Id",
                            new { Id = prod.IdProducto });

                        // Actualizar estado si se llega a 0
                        var stockRestante = conexion.ExecuteScalar<int>(
                            "SELECT StockActual FROM Productos WHERE IdProducto = @Id",
                            new { Id = prod.IdProducto });
                        if (stockRestante == 0)
                        {
                            conexion.Execute(
                                "UPDATE Productos SET EstadoProducto = 'Agotado' WHERE IdProducto = @Id",
                                new { Id = prod.IdProducto });
                        }
                    }
                }
            }

            CargarTicketMesa(_mesaActual);
            CargarProductos(); // Refrescar stock visual
        }

        // ═══════════════════════════════════════════
        // CLICK EN MESA
        // ═══════════════════════════════════════════
        private void BtnMesa_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            _mesaActual = boton?.Tag as Mesa;
            if (_mesaActual == null) return;

            lblMesaSeleccionada.Text = $"🪑 {_mesaActual.Nombre} — {_mesaActual.Estado}";

            if (_mesaActual.Estado == "Libre")
            {
                var resp = MessageBox.Show(
                    $"La {_mesaActual.Nombre} está libre.\n¿Deseas abrir una cuenta para esta mesa?",
                    "Abrir Mesa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resp == MessageBoxResult.Yes)
                    AbrirMesa(_mesaActual);
            }
            else
            {
                CargarTicketMesa(_mesaActual);
            }
        }

        private void AbrirMesa(Mesa mesa)
        {
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                conexion.Execute("UPDATE Mesas SET Estado = 'Ocupada' WHERE IdMesa = @Id",
                    new { Id = mesa.IdMesa });

                conexion.Execute(@"
                    INSERT INTO VentasCabecera (IdUsuario, IdMesa, FechaVenta, Subtotal, TotalPagado, Estado) 
                    VALUES (@Usu, @Mesa, @Fecha, 0, 0, 'Abierta')",
                    new
                    {
                        Usu = 1,
                        Mesa = mesa.IdMesa,
                        Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
            }

            CargarMesas();
            ListaTicket.Items.Clear();
            lblMesaSeleccionada.Text = $"🪑 {mesa.Nombre} — Ocupada";
        }

        // ═══════════════════════════════════════════
        // CARGAR TICKET
        // ═══════════════════════════════════════════
        private void CargarTicketMesa(Mesa mesa)
        {
            ListaTicket.Items.Clear();
            lblTotal.Text = "0.00 Bs";

            if (mesa == null || mesa.Estado == "Libre") return;

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                int idVenta = conexion.ExecuteScalar<int>(
                    "SELECT IdVenta FROM VentasCabecera WHERE IdMesa = @IdMesa AND Estado = 'Abierta'",
                    new { IdMesa = mesa.IdMesa });

                if (idVenta > 0)
                {
                    var detalles = conexion.Query(@"
                        SELECT d.IdDetalle, p.Nombre, d.Cantidad, d.PrecioVendido, d.Subtotal 
                        FROM VentasDetalle d
                        INNER JOIN Productos p ON d.IdProducto = p.IdProducto
                        WHERE d.IdVenta = @IdVenta",
                        new { IdVenta = idVenta });

                    double totalSuma = 0;

                    foreach (var item in detalles)
                    {
                        var ticketItem = new ItemTicket
                        {
                            IdDetalle = (int)item.IdDetalle,
                            NombreProducto = (string)item.Nombre,
                            Cantidad = (int)(long)item.Cantidad,
                            PrecioUnitario = (double)item.PrecioVendido,
                            Subtotal = (double)item.Subtotal
                        };

                        // Crear visual del item
                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                        var txtQty = new TextBlock
                        {
                            Text = ticketItem.Cantidad.ToString(),
                            FontSize = 13,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8C5A35")),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(txtQty, 0);

                        var txtNombre = new TextBlock
                        {
                            Text = ticketItem.NombreProducto,
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1514")),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(txtNombre, 1);

                        var txtPrecio = new TextBlock
                        {
                            Text = $"{ticketItem.PrecioUnitario:F2}",
                            FontSize = 12,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8C5A35")),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(txtPrecio, 2);

                        var txtSubtotal = new TextBlock
                        {
                            Text = $"{ticketItem.Subtotal:F2} Bs",
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1514")),
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Right
                        };
                        Grid.SetColumn(txtSubtotal, 3);

                        grid.Children.Add(txtQty);
                        grid.Children.Add(txtNombre);
                        grid.Children.Add(txtPrecio);
                        grid.Children.Add(txtSubtotal);

                        var listItem = new ListBoxItem
                        {
                            Content = grid,
                            Tag = ticketItem,
                            Padding = new Thickness(5, 8, 5, 8),
                            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0EBE4")),
                            BorderThickness = new Thickness(0, 0, 0, 1)
                        };

                        ListaTicket.Items.Add(listItem);
                        totalSuma += ticketItem.Subtotal;
                    }

                    lblTotal.Text = $"{totalSuma:F2} Bs";
                    conexion.Execute(
                        "UPDATE VentasCabecera SET Subtotal = @Total, TotalPagado = @Total WHERE IdVenta = @IdVenta",
                        new { Total = totalSuma, IdVenta = idVenta });
                }
            }
        }

        // ═══════════════════════════════════════════
        // QUITAR PRODUCTO
        // ═══════════════════════════════════════════
        private void BtnQuitarProducto_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ListaTicket.SelectedItem as ListBoxItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Selecciona un producto de la lista para quitarlo.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ticketItem = selectedItem.Tag as ItemTicket;
            if (ticketItem == null) return;

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Obtener el producto para restaurar stock
                var detalle = conexion.QueryFirstOrDefault(@"
                    SELECT IdProducto, Cantidad FROM VentasDetalle WHERE IdDetalle = @Id",
                    new { Id = ticketItem.IdDetalle });

                if (detalle != null)
                {
                    int idProd = (int)(long)detalle.IdProducto;
                    int cant = (int)(long)detalle.Cantidad;

                    // Restaurar stock
                    var producto = conexion.QueryFirstOrDefault<Producto>(
                        "SELECT * FROM Productos WHERE IdProducto = @Id", new { Id = idProd });

                    if (producto != null && producto.EsInventariable == 1)
                    {
                        conexion.Execute(
                            "UPDATE Productos SET StockActual = StockActual + @Cant, EstadoProducto = 'Disponible' WHERE IdProducto = @Id",
                            new { Cant = cant, Id = idProd });
                    }
                }

                conexion.Execute("DELETE FROM VentasDetalle WHERE IdDetalle = @Id",
                    new { Id = ticketItem.IdDetalle });
            }

            CargarTicketMesa(_mesaActual!);
            CargarProductos();
        }

        // ═══════════════════════════════════════════
        // COBRAR
        // ═══════════════════════════════════════════
        private void BtnCobrar_Click(object sender, RoutedEventArgs e)
        {
            if (_mesaActual == null || _mesaActual.Estado == "Libre")
            {
                MessageBox.Show("No hay ninguna cuenta abierta para cobrar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                int idVenta = conexion.ExecuteScalar<int>(
                    "SELECT IdVenta FROM VentasCabecera WHERE IdMesa = @IdMesa AND Estado = 'Abierta'",
                    new { IdMesa = _mesaActual.IdMesa });

                if (idVenta > 0)
                {
                    conexion.Execute("UPDATE VentasCabecera SET Estado = 'Pagada' WHERE IdVenta = @Id",
                        new { Id = idVenta });
                    conexion.Execute("UPDATE Mesas SET Estado = 'Libre' WHERE IdMesa = @IdMesa",
                        new { IdMesa = _mesaActual.IdMesa });

                    MessageBox.Show(
                        $"¡Cobro realizado con éxito!\nLa {_mesaActual.Nombre} vuelve a estar disponible.",
                        "✅ Caja Registrada", MessageBoxButton.OK, MessageBoxImage.Information);

                    _mesaActual = null;
                    lblMesaSeleccionada.Text = "Selecciona una mesa";
                    ListaTicket.Items.Clear();
                    lblTotal.Text = "0.00 Bs";
                    CargarMesas();
                }
            }
        }
    }
}