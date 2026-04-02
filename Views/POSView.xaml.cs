using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using Dapper;
using WpfApp1.Data;    // Para ConexionDB
using WpfApp1.Models;  // Para tu clase Mesa

namespace WpfApp1.Views
{

    // Pon esto al final de tu archivo, justo antes de la última llave }
    public class ItemTicket
    {
        public int IdDetalle { get; set; } // El ID real en la base de datos
        public string TextoMostrar { get; set; } // Lo que verá el cajero

        // Este truco hace que WPF muestre el texto automáticamente en la lista
        public override string ToString()
        {
            return TextoMostrar;
        }
    }
    public partial class POSView : UserControl
    {
        public POSView()
        {
            InitializeComponent();
            CargarMesas();
            CargarProductos();// Llamamos a la función apenas carga la pantalla
        }

        private void CargarMesas()
        {
            // Limpiamos el panel por si acaso
            PanelMesas.Children.Clear();

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Dapper trae todas las mesas de la base de datos
                var mesas = conexion.Query<Mesa>("SELECT * FROM Mesas").AsList();

                // Recorremos cada mesa y le fabricamos un botón
                foreach (var mesa in mesas)
                {
                    Button btnMesa = new Button();
                    btnMesa.Content = $"{mesa.Nombre}\n({mesa.Estado})";
                    btnMesa.Width = 120;
                    btnMesa.Height = 100;
                    btnMesa.Margin = new Thickness(5);
                    btnMesa.FontSize = 16;
                    btnMesa.FontWeight = FontWeights.Bold;
                    btnMesa.BorderThickness = new Thickness(0);
                    btnMesa.Cursor = System.Windows.Input.Cursors.Hand;

                    // Guardamos el objeto Mesa entero dentro del botón para usarlo después
                    btnMesa.Tag = mesa;

                    // Asignamos los colores de tu paleta según el estado
                    if (mesa.Estado == "Libre")
                    {
                        btnMesa.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9CBB8")); // Beige Oscuro
                        btnMesa.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1514")); // Texto Negro
                    }
                    else
                    {
                        btnMesa.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A3525")); // Café Oscuro
                        btnMesa.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEBE4")); // Texto Claro
                    }

                    // Le agregamos el evento Click dinámicamente
                    btnMesa.Click += BtnMesa_Click;

                    // Lo agregamos a la pantalla
                    PanelMesas.Children.Add(btnMesa);
                }
            }
        }
        private void CargarProductos()
        {
            PanelProductos.Children.Clear();

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // TRUCO: Si no hay productos, creamos unos de prueba automáticamente
                var cantidad = conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM Productos");
                if (cantidad == 0)
                {
                    conexion.Execute("INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase) VALUES ('Catan', 'LUDOTECA', 5.00)");
                    conexion.Execute("INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase) VALUES ('Frappé Oreo', 'CAFETERIA', 7.50)");
                    conexion.Execute("INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase) VALUES ('Café Americano', 'CAFETERIA', 3.00)");
                    conexion.Execute("INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase) VALUES ('Papas Fritas', 'CAFETERIA', 4.50)");
                }

                // Traemos todos los productos activos
                var productos = conexion.Query<Producto>("SELECT * FROM Productos WHERE Activo = 1").AsList();

                foreach (var prod in productos)
                {
                    Button btnProd = new Button();
                    btnProd.Content = $"{prod.Nombre}\n${prod.PrecioVentaBase:0.00}";
                    btnProd.Width = 120;
                    btnProd.Height = 80;
                    btnProd.Margin = new Thickness(5);
                    btnProd.FontWeight = FontWeights.SemiBold;
                    btnProd.BorderThickness = new Thickness(0);
                    btnProd.Cursor = System.Windows.Input.Cursors.Hand;
                    btnProd.Tag = prod; // Escondemos los datos del producto en el botón

                    // Pintamos diferente si es Ludoteca (Negro) o Cafetería (Café Acento)
                    if (prod.Tipo == "CAFETERIA")
                    {
                        btnProd.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8C5A35"));
                        btnProd.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEBE4"));
                    }
                    else
                    {
                        btnProd.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1514"));
                        btnProd.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEBE4"));
                    }

                    btnProd.Click += BtnProducto_Click;
                    PanelProductos.Children.Add(btnProd);
                }
            }
        }

        // Este evento captura el clic en un producto
        private void BtnProducto_Click(object sender, RoutedEventArgs e)
        {
            Button botonPresionado = sender as Button;
            Producto prodSeleccionado = botonPresionado.Tag as Producto;

            if (_mesaActual == null || _mesaActual.Estado == "Libre")
            {
                MessageBox.Show("Primero debes seleccionar y abrir una mesa para agregarle productos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // 1. Buscamos cuál es el "Id" del ticket que está abierto para esta mesa
                string sqlBuscarTicket = "SELECT IdVenta FROM VentasCabecera WHERE IdMesa = @IdMesa AND Estado = 'Abierta'";
                int idVentaAbierta = conexion.ExecuteScalar<int>(sqlBuscarTicket, new { IdMesa = _mesaActual.IdMesa });

                if (idVentaAbierta > 0)
                {
                    // 2. Insertamos el producto en la cuenta (VentasDetalle)
                    string sqlInsertarDetalle = @"
                INSERT INTO VentasDetalle (IdVenta, IdProducto, Cantidad, PrecioVendido, CostoAplicado, Subtotal, TipoOrigen) 
                VALUES (@IdVenta, @IdProd, 1, @Precio, 0, @Precio, @TipoOrigen)"; // Costo 0 por ahora para el MVP

                    conexion.Execute(sqlInsertarDetalle, new
                    {
                        IdVenta = idVentaAbierta,
                        IdProd = prodSeleccionado.IdProducto,
                        Precio = prodSeleccionado.PrecioVentaBase,
                        TipoOrigen = prodSeleccionado.Tipo
                    });
                }
            }

            // 3. Volvemos a leer la base de datos para que el ticket se dibuje actualizado
            CargarTicketMesa(_mesaActual);
        }


        // Variable global para recordar qué mesa estamos atendiendo
        private Mesa _mesaActual;

        private void BtnMesa_Click(object sender, RoutedEventArgs e)
        {
            Button botonPresionado = sender as Button;
            _mesaActual = botonPresionado.Tag as Mesa; // Recuperamos la mesa

            // Actualizamos el título del ticket
            lblMesaSeleccionada.Text = $"Mesa Seleccionada: {_mesaActual.Nombre}";

            if (_mesaActual.Estado == "Libre")
            {
                // 1. Preguntamos si quieren abrirla
                var respuesta = MessageBox.Show($"La {_mesaActual.Nombre} está libre. ¿Deseas abrir una cuenta para esta mesa?", "Abrir Mesa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (respuesta == MessageBoxResult.Yes)
                {
                    AbrirMesa(_mesaActual);
                }
            }
            else
            {
                // 2. Si ya está Ocupada, cargamos lo que están consumiendo
                CargarTicketMesa(_mesaActual);
            }
        }

        private void AbrirMesa(Mesa mesa)
        {
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Paso A: Cambiar el estado de la mesa a "Ocupada"
                string sqlUpdateMesa = "UPDATE Mesas SET Estado = 'Ocupada' WHERE IdMesa = @Id";
                conexion.Execute(sqlUpdateMesa, new { Id = mesa.IdMesa });

                // Paso B: Crear la cabecera del ticket (La cuenta abierta)
                // Nota: Por ahora pondremos IdUsuario = 1 (el Admin). Luego lo enlazaremos al usuario logueado.
                string sqlCrearTicket = @"
            INSERT INTO VentasCabecera (IdUsuario, IdMesa, FechaVenta, Subtotal, TotalPagado, Estado) 
            VALUES (@Usu, @Mesa, @Fecha, 0, 0, 'Abierta')";

                conexion.Execute(sqlCrearTicket, new
                {
                    Usu = 1,
                    Mesa = mesa.IdMesa,
                    Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") // Guardamos la hora exacta
                });
            }

            // Paso C: Recargar los botones visuales para que la mesa se pinte de color Café
            CargarMesas();

            // Dejamos el ticket limpio y listo para empezar a pedir
            ListaTicket.Items.Clear();
        }

        private void CargarTicketMesa(Mesa mesa)
        {
            ListaTicket.Items.Clear();
            lblTotal.Text = "Total: $0.00";

            if (mesa == null || mesa.Estado == "Libre") return;

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                int idVentaAbierta = conexion.ExecuteScalar<int>("SELECT IdVenta FROM VentasCabecera WHERE IdMesa = @IdMesa AND Estado = 'Abierta'", new { IdMesa = mesa.IdMesa });

                if (idVentaAbierta > 0)
                {
                    // Agregamos d.IdDetalle a la consulta para saber qué borrar después
                    string sqlDetalles = @"
                SELECT d.IdDetalle, p.Nombre, d.Cantidad, d.Subtotal 
                FROM VentasDetalle d
                INNER JOIN Productos p ON d.IdProducto = p.IdProducto
                WHERE d.IdVenta = @IdVenta";

                    var productosEnTicket = conexion.Query(sqlDetalles, new { IdVenta = idVentaAbierta });
                    double totalSuma = 0;

                    foreach (var item in productosEnTicket)
                    {
                        // En lugar de texto, agregamos nuestro objeto con el ID oculto
                        ListaTicket.Items.Add(new ItemTicket
                        {
                            IdDetalle = (int)item.IdDetalle,
                            TextoMostrar = $"{item.Cantidad}x {item.Nombre} - ${(double)item.Subtotal:0.00}"
                        });
                        totalSuma += (double)item.Subtotal;
                    }

                    lblTotal.Text = $"Total: ${totalSuma:0.00}";
                    conexion.Execute("UPDATE VentasCabecera SET Subtotal = @Total, TotalPagado = @Total WHERE IdVenta = @IdVenta", new { Total = totalSuma, IdVenta = idVentaAbierta });
                }
            }
        }

        // === NUEVO: LÓGICA PARA ELIMINAR UN PRODUCTO ===
        private void BtnQuitarProducto_Click(object sender, RoutedEventArgs e)
        {
            // Validamos que haya seleccionado algo en la lista
            if (ListaTicket.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecciona un producto de la lista para quitarlo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Extraemos nuestro objeto con el ID oculto
            ItemTicket itemSeleccionado = ListaTicket.SelectedItem as ItemTicket;

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Lo borramos físicamente de la base de datos
                conexion.Execute("DELETE FROM VentasDetalle WHERE IdDetalle = @Id", new { Id = itemSeleccionado.IdDetalle });
            }

            // Recargamos el ticket para que actualice la lista y el Total
            CargarTicketMesa(_mesaActual);
        }

        // === NUEVO: LÓGICA PARA COBRAR ===
        private void BtnCobrar_Click(object sender, RoutedEventArgs e)
        {
            if (_mesaActual == null || _mesaActual.Estado == "Libre")
            {
                MessageBox.Show("No hay ninguna cuenta abierta para cobrar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // 1. Buscamos el ticket
                int idVentaAbierta = conexion.ExecuteScalar<int>("SELECT IdVenta FROM VentasCabecera WHERE IdMesa = @IdMesa AND Estado = 'Abierta'", new { IdMesa = _mesaActual.IdMesa });

                if (idVentaAbierta > 0)
                {
                    // 2. Cerramos el Ticket (Pagada)
                    conexion.Execute("UPDATE VentasCabecera SET Estado = 'Pagada' WHERE IdVenta = @Id", new { Id = idVentaAbierta });

                    // 3. Liberamos la Mesa
                    conexion.Execute("UPDATE Mesas SET Estado = 'Libre' WHERE IdMesa = @IdMesa", new { IdMesa = _mesaActual.IdMesa });

                    MessageBox.Show($"¡Cobro realizado con éxito!\nLa {_mesaActual.Nombre} vuelve a estar disponible.", "Caja", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 4. Limpiamos la pantalla
                    _mesaActual = null;
                    lblMesaSeleccionada.Text = "Mesa Seleccionada: Ninguna";
                    ListaTicket.Items.Clear();
                    lblTotal.Text = "Total: $0.00";

                    // 5. Redibujamos las mesas para que vuelva a estar Beige
                    CargarMesas();
                }
            }
        }
    }
}