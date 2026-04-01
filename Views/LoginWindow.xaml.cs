using System.Windows;
using Microsoft.Data.Sqlite;
using Dapper; // ¡Asegúrate de tener este using para la magia!
using WpfApp1.Data;   // Para poder acceder a tu ConexionDB
using WpfApp1.Models; // Para poder usar la clase Usuario que acabamos de crear

namespace WpfApp1.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            // 1. Obtenemos los textos y quitamos espacios en blanco accidentales
            string user = txtUser.Text.Trim();
            string pass = txtPass.Password.Trim();

            // Validamos que no hayan dejado los campos vacíos
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                lblError.Text = "Por favor ingresa usuario y contraseña.";
                return;
            }

            // 2. Abrimos la conexión a tu archivo SQLite
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // 3. Escribimos la consulta SQL (usamos @u y @p por seguridad contra hackeos)
                string sql = "SELECT * FROM Usuarios WHERE Username = @u AND Password = @p AND Activo = 1";

                // 4. DAPPER HACE LA MAGIA: Ejecuta el SQL y guarda el resultado en tu clase Usuario
                var usuarioAutenticado = conexion.QueryFirstOrDefault<Usuario>(sql, new { u = user, p = pass });

                // 5. Verificamos si encontró a alguien en la base de datos
                if (usuarioAutenticado != null)
                {
                    // ¡Login Exitoso con la Base de Datos!
                    MainWindow principal = new MainWindow();
                    principal.Show();
                    this.Close(); // Cerramos la ventana de Login
                }
                else
                {
                    // No coincidieron los datos en SQLite
                    lblError.Text = "Usuario o contraseña incorrectos.";
                }
            }
        }
    }
}