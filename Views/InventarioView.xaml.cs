using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using Dapper;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.Views
{
    public partial class InventarioView : UserControl
    {
        private DataGrid _ultimaGridActiva;

        public InventarioView()
        {
            InitializeComponent();
            CargarTablaProductos();

            // Si el usuario es Super, ocultar formularios (solo lectura)
            if (SesionActual.EsSuper)
            {
                panelFormularios.Visibility = Visibility.Collapsed;
            }
        }

        // ═══════════════════════════════════════════
        // MODO: PRODUCTO vs LOTE
        // ═══════════════════════════════════════════
        private void BtnModoProducto_Click(object sender, RoutedEventArgs e)
        {
            btnModoProducto.Style = (Style)FindResource("ToggleBtnActive");
            btnModoLote.Style = (Style)FindResource("ToggleBtn");
            panelNuevoProducto.Visibility = Visibility.Visible;
            panelNuevoLote.Visibility = Visibility.Collapsed;
        }

        private void BtnModoLote_Click(object sender, RoutedEventArgs e)
        {
            btnModoProducto.Style = (Style)FindResource("ToggleBtn");
            btnModoLote.Style = (Style)FindResource("ToggleBtnActive");
            panelNuevoProducto.Visibility = Visibility.Collapsed;
            panelNuevoLote.Visibility = Visibility.Visible;
            CargarComboProductos();
        }

        private void CargarComboProductos()
        {
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                var productos = conexion.Query<Producto>(
                    "SELECT * FROM Productos WHERE Activo = 1 AND EsInventariable = 1 ORDER BY Nombre").AsList();
                cmbProductoLote.ItemsSource = productos;
                if (productos.Count > 0)
                    cmbProductoLote.SelectedIndex = 0;
            }
        }

        // ═══════════════════════════════════════════
        // CATEGORÍA CAMBIO (mostrar/ocultar campos)
        // ═══════════════════════════════════════════
        private void CmbTipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelCamposCafe == null) return;

            var seleccion = (cmbTipo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            panelCamposCafe.Visibility = seleccion == "JUEGOS" ? Visibility.Collapsed : Visibility.Visible;
        }

        // ═══════════════════════════════════════════
        // CARGAR TABLAS SEPARADAS
        // ═══════════════════════════════════════════
        private void CargarTablaProductos()
        {
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Cafetería
                var listaCafe = conexion.Query<Producto>(
                    "SELECT * FROM Productos WHERE Activo = 1 AND Tipo = 'CAFETERIA' ORDER BY Nombre").AsList();
                GridCafeteria.ItemsSource = listaCafe;
                lblConteoCafe.Text = $"{listaCafe.Count} productos";

                // Juegos
                var listaJuegos = conexion.Query<Producto>(
                    "SELECT * FROM Productos WHERE Activo = 1 AND Tipo = 'JUEGOS' ORDER BY Nombre").AsList();
                GridJuegos.ItemsSource = listaJuegos;
                lblConteoJuegos.Text = $"{listaJuegos.Count} juegos";
            }

            // Colorear filas cafetería
            GridCafeteria.LoadingRow += ColorearFila;
            GridJuegos.LoadingRow += ColorearFila;
        }

        private void ColorearFila(object? sender, DataGridRowEventArgs e)
        {
            var prod = e.Row.Item as Producto;
            if (prod == null) return;

            if (prod.EstadoProducto == "Dañado" || prod.EstadoProducto == "Suspendido")
            {
                e.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0E0"));
                e.Row.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
            }
            else if (prod.EsInventariable == 1 && prod.StockActual == 0)
            {
                e.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                e.Row.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
            }
            else if (prod.EsInventariable == 1 && prod.StockActual <= prod.StockMinimo)
            {
                e.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));
                e.Row.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
            }
        }

        // ═══════════════════════════════════════════
        // GUARDAR NUEVO PRODUCTO
        // ═══════════════════════════════════════════
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            string tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "CAFETERIA";
            string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Disponible";

            if (string.IsNullOrEmpty(nombre))
            {
                MostrarMensaje("El nombre es obligatorio.", true);
                return;
            }

            if (!double.TryParse(txtPrecio.Text.Trim(), out double precio))
            {
                MostrarMensaje("El precio debe ser un número válido.", true);
                return;
            }

            bool esJuego = tipo == "JUEGOS";
            int esInventariable = esJuego ? 0 : 1;
            int stockInicial = 0;
            int stockMinimo = 0;
            double costoCompra = 0;

            if (!esJuego)
            {
                if (!int.TryParse(txtStock.Text.Trim(), out stockInicial) || stockInicial < 0)
                {
                    MostrarMensaje("El stock inicial debe ser un número entero.", true);
                    return;
                }
                int.TryParse(txtStockMin.Text.Trim(), out stockMinimo);
                double.TryParse(txtCosto.Text.Trim(), out costoCompra);
            }
            else
            {
                // Para juegos, stock = cantidad de copias
                stockInicial = 1; // al menos 1 copia
            }

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Insertar producto
                string sqlProducto = @"
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto, Activo) 
                    VALUES (@Nom, @Tip, @Pre, @Stock, @Min, @Inv, @Est, 1);
                    SELECT last_insert_rowid();";

                long idProducto = conexion.ExecuteScalar<long>(sqlProducto, new
                {
                    Nom = nombre,
                    Tip = tipo,
                    Pre = precio,
                    Stock = stockInicial,
                    Min = stockMinimo,
                    Inv = esInventariable,
                    Est = estado
                });

                // Crear lote inicial para productos de cafetería
                if (!esJuego && stockInicial > 0)
                {
                    conexion.Execute(@"
                        INSERT INTO LotesInventario (IdProducto, CostoUnitario, PrecioVentaLote, CantidadInicial, CantidadDisponible, FechaIngreso, Notas) 
                        VALUES (@Id, @Costo, @Precio, @Cant, @Cant, @Fecha, 'Lote inicial')",
                        new
                        {
                            Id = idProducto,
                            Costo = costoCompra,
                            Precio = precio,
                            Cant = stockInicial,
                            Fecha = DateTime.Now.ToString("yyyy-MM-dd")
                        });
                }
            }

            // Limpiar formulario
            txtNombre.Clear();
            txtPrecio.Clear();
            txtCosto.Clear();
            txtStock.Clear();
            txtStockMin.Clear();
            MostrarMensaje($"✅ ¡{nombre} guardado con éxito!", false);
            CargarTablaProductos();
        }

        // ═══════════════════════════════════════════
        // GUARDAR NUEVO LOTE
        // ═══════════════════════════════════════════
        private void BtnGuardarLote_Click(object sender, RoutedEventArgs e)
        {
            var prodSeleccionado = cmbProductoLote.SelectedItem as Producto;
            if (prodSeleccionado == null)
            {
                MostrarMensajeLote("Selecciona un producto.", true);
                return;
            }

            if (!double.TryParse(txtCostoLote.Text.Trim(), out double costo))
            {
                MostrarMensajeLote("El costo debe ser un número válido.", true);
                return;
            }

            if (!double.TryParse(txtPrecioVentaLote.Text.Trim(), out double precioVenta))
            {
                MostrarMensajeLote("El precio de venta debe ser un número válido.", true);
                return;
            }

            if (!int.TryParse(txtCantidadLote.Text.Trim(), out int cantidad) || cantidad <= 0)
            {
                MostrarMensajeLote("La cantidad debe ser un número entero mayor a 0.", true);
                return;
            }

            string notas = txtNotasLote.Text.Trim();

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Insertar lote
                conexion.Execute(@"
                    INSERT INTO LotesInventario (IdProducto, CostoUnitario, PrecioVentaLote, CantidadInicial, CantidadDisponible, FechaIngreso, Notas) 
                    VALUES (@Id, @Costo, @Precio, @Cant, @Cant, @Fecha, @Notas)",
                    new
                    {
                        Id = prodSeleccionado.IdProducto,
                        Costo = costo,
                        Precio = precioVenta,
                        Cant = cantidad,
                        Fecha = DateTime.Now.ToString("yyyy-MM-dd"),
                        Notas = string.IsNullOrEmpty(notas) ? (string?)null : notas
                    });

                // Actualizar stock y precio del producto
                conexion.Execute(@"
                    UPDATE Productos 
                    SET StockActual = StockActual + @Cantidad, 
                        PrecioVentaBase = @NuevoPrecio,
                        EstadoProducto = 'Disponible'
                    WHERE IdProducto = @Id",
                    new
                    {
                        Cantidad = cantidad,
                        NuevoPrecio = precioVenta,
                        Id = prodSeleccionado.IdProducto
                    });
            }

            txtCostoLote.Clear();
            txtPrecioVentaLote.Clear();
            txtCantidadLote.Clear();
            txtNotasLote.Clear();

            MostrarMensajeLote($"✅ Lote de {cantidad} uds agregado a {prodSeleccionado.Nombre}.\nPrecio venta actualizado: {precioVenta:F2} Bs", false);
            CargarTablaProductos();
            CargarComboProductos();
        }

        // ═══════════════════════════════════════════
        // SELECCIÓN DE PRODUCTO EN TABLA
        // ═══════════════════════════════════════════
        private void GridProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null) return;

            var prod = grid.SelectedItem as Producto;
            if (prod == null)
            {
                // Solo ocultar si no hay selección en ninguna tabla
                if (GridCafeteria.SelectedItem == null && GridJuegos.SelectedItem == null)
                {
                    panelLotes.Visibility = Visibility.Collapsed;
                    panelAcciones.Visibility = Visibility.Collapsed;
                }
                return;
            }

            // Deseleccionar la otra tabla
            if (grid == GridCafeteria && GridJuegos.SelectedItem != null)
                GridJuegos.SelectedItem = null;
            else if (grid == GridJuegos && GridCafeteria.SelectedItem != null)
                GridCafeteria.SelectedItem = null;

            _ultimaGridActiva = grid;

            // Mostrar historial de lotes (solo para cafetería)
            if (prod.Tipo == "CAFETERIA")
            {
                using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
                {
                    var lotes = conexion.Query<LoteInventario>(
                        "SELECT * FROM LotesInventario WHERE IdProducto = @Id ORDER BY FechaIngreso DESC",
                        new { Id = prod.IdProducto }).AsList();

                    if (lotes.Count > 0)
                    {
                        GridLotes.ItemsSource = lotes;
                        lblTituloLotes.Text = $"📋 Historial de Lotes — {prod.Nombre}";
                        panelLotes.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        panelLotes.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                panelLotes.Visibility = Visibility.Collapsed;
            }

            // Mostrar acciones (solo si no es Super en modo lectura)
            if (!SesionActual.EsSuper)
            {
                panelAcciones.Visibility = Visibility.Visible;
                lblProductoSel.Text = $"Producto: {prod.Nombre} ({prod.Tipo})";

                // Seleccionar el estado actual
                foreach (ComboBoxItem item in cmbCambiarEstado.Items)
                {
                    if (item.Content.ToString() == prod.EstadoProducto)
                    {
                        cmbCambiarEstado.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        // ═══════════════════════════════════════════
        // ACTUALIZAR ESTADO
        // ═══════════════════════════════════════════
        private void BtnActualizarEstado_Click(object sender, RoutedEventArgs e)
        {
            Producto? prod = null;
            if (_ultimaGridActiva != null)
                prod = _ultimaGridActiva.SelectedItem as Producto;

            if (prod == null) return;

            string nuevoEstado = (cmbCambiarEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Disponible";

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                conexion.Execute(
                    "UPDATE Productos SET EstadoProducto = @Estado WHERE IdProducto = @Id",
                    new { Estado = nuevoEstado, Id = prod.IdProducto });
            }

            MessageBox.Show($"Estado de '{prod.Nombre}' actualizado a: {nuevoEstado}",
                "✅ Estado Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);

            CargarTablaProductos();
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════
        private void MostrarMensaje(string msg, bool esError)
        {
            lblMensaje.Text = msg;
            lblMensaje.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(esError ? "#C0392B" : "#27AE60"));
        }

        private void MostrarMensajeLote(string msg, bool esError)
        {
            lblMensajeLote.Text = msg;
            lblMensajeLote.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(esError ? "#C0392B" : "#27AE60"));
        }
    }
}