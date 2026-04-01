using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using Dapper;
using WpfApp1.Data;    // Para ConexionDB
using WpfApp1.Models;  // Para tu clase Mesa

namespace WpfApp1.Views
{
    public partial class POSView : UserControl
    {
        public POSView()
        {
            InitializeComponent();
            CargarMesas(); // Llamamos a la función apenas carga la pantalla
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

        // Este método se ejecutará cuando hagas clic en CUALQUIER botón de mesa
        private void BtnMesa_Click(object sender, RoutedEventArgs e)
        {
            Button botonPresionado = sender as Button;
            Mesa mesaSeleccionada = botonPresionado.Tag as Mesa; // Recuperamos los datos de la mesa

            MessageBox.Show($"Acabas de hacer clic en la {mesaSeleccionada.Nombre}. Está {mesaSeleccionada.Estado}.", "Mesa Seleccionada");

            // Aquí es donde actualizaremos el título del Ticket a la derecha
            // lblMesaSeleccionada.Text = $"Mesa Seleccionada: {mesaSeleccionada.Nombre}";
        }
    }
}