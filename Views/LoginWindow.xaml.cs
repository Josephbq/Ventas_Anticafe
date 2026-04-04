using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using Dapper;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUser.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login_Click(sender, new RoutedEventArgs());
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPass.Password.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                lblError.Text = "Por favor ingresa usuario y contraseña.";
                return;
            }

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                string sql = "SELECT * FROM Usuarios WHERE Username = @u AND Password = @p AND Activo = 1";
                var usuarioAutenticado = conexion.QueryFirstOrDefault<Usuario>(sql, new { u = user, p = pass });

                if (usuarioAutenticado != null)
                {
                    // Guardar sesión activa
                    SesionActual.UsuarioLogueado = usuarioAutenticado;

                    MainWindow principal = new MainWindow();
                    principal.Show();
                    this.Close();
                }
                else
                {
                    lblError.Text = "Usuario o contraseña incorrectos.";
                }
            }
        }

        // Permite mover la ventana arrastrando
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}