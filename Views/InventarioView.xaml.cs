using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using Dapper;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.Views
{
    public partial class InventarioView : UserControl
    {
        public InventarioView()
        {
            InitializeComponent();
            CargarTablaProductos(); // Llenamos la tabla al abrir la pantalla
        }

        private void CargarTablaProductos()
        {
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Traemos todos los productos activos de la base de datos
                var listaProductos = conexion.Query<Producto>("SELECT * FROM Productos WHERE Activo = 1").AsList();

                // Con esta sola línea, WPF rellena toda la tabla visual
                GridProductos.ItemsSource = listaProductos;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Recolectamos los datos y validamos que no estén vacíos
            string nombre = txtNombre.Text.Trim();
            string tipo = (cmbTipo.SelectedItem as ComboBoxItem).Content.ToString();
            string precioTexto = txtPrecio.Text.Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(precioTexto))
            {
                MessageBox.Show("El nombre y el precio son obligatorios.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Intentamos convertir el texto del precio a un número decimal
            if (!double.TryParse(precioTexto, out double precioConvertido))
            {
                MessageBox.Show("El precio debe ser un número válido (ej. 15.50).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Guardamos en SQLite
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                string sqlInsertar = @"
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, Activo) 
                    VALUES (@Nom, @Tip, @Pre, 1)";

                conexion.Execute(sqlInsertar, new
                {
                    Nom = nombre,
                    Tip = tipo,
                    Pre = precioConvertido
                });
            }

            // 4. Limpiamos el formulario, avisamos de éxito y recargamos la tabla
            txtNombre.Clear();
            txtPrecio.Clear();
            lblMensaje.Text = "¡Producto guardado con éxito!";
            CargarTablaProductos();
        }
    }
}