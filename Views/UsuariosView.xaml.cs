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
    public partial class UsuariosView : UserControl
    {
        public UsuariosView()
        {
            InitializeComponent();
            CargarTablaUsuarios();
        }

        // ═══════════════════════════════════════════
        // CARGAR TABLA
        // ═══════════════════════════════════════════
        private void CargarTablaUsuarios()
        {
            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                var lista = conexion.Query<Usuario>(
                    "SELECT * FROM Usuarios ORDER BY Rol, Nombre").AsList();
                GridUsuarios.ItemsSource = lista;
                lblConteoUsuarios.Text = $"{lista.Count} usuarios";
            }

            // Colorear filas según estado
            GridUsuarios.LoadingRow += (s, e) =>
            {
                var usr = e.Row.Item as Usuario;
                if (usr == null) return;

                if (usr.Activo == 0)
                {
                    e.Row.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#FFF0E0"));
                    e.Row.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#999"));
                }
            };
        }

        // ═══════════════════════════════════════════
        // GUARDAR NUEVO USUARIO
        // ═══════════════════════════════════════════
        private void BtnGuardarUsuario_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreUsuario.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string rol = (cmbRol.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Vendedor";

            if (string.IsNullOrEmpty(nombre))
            {
                MostrarMensaje("El nombre es obligatorio.", true);
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                MostrarMensaje("El nombre de usuario es obligatorio.", true);
                return;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 4)
            {
                MostrarMensaje("La contraseña debe tener al menos 4 caracteres.", true);
                return;
            }

            using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
            {
                // Verificar que el username no exista
                int existe = conexion.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM Usuarios WHERE Username = @u",
                    new { u = username });

                if (existe > 0)
                {
                    MostrarMensaje("Ese nombre de usuario ya está en uso.", true);
                    return;
                }

                // Insertar usuario
                conexion.Execute(@"
                    INSERT INTO Usuarios (Nombre, Username, Password, Rol, Activo) 
                    VALUES (@Nombre, @Username, @Password, @Rol, 1)",
                    new
                    {
                        Nombre = nombre,
                        Username = username,
                        Password = password,
                        Rol = rol
                    });
            }

            // Limpiar formulario
            txtNombreUsuario.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
            MostrarMensaje($"✅ Usuario '{username}' creado con éxito como {rol}.", false);
            CargarTablaUsuarios();
        }

        // ═══════════════════════════════════════════
        // SELECCIÓN DE USUARIO EN TABLA
        // ═══════════════════════════════════════════
        private void GridUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var usuario = GridUsuarios.SelectedItem as Usuario;
            if (usuario == null)
            {
                panelAccionesUsuario.Visibility = Visibility.Collapsed;
                return;
            }

            panelAccionesUsuario.Visibility = Visibility.Visible;
            lblUsuarioSeleccionado.Text = $"{usuario.Nombre} (@{usuario.Username})";
            lblRolSeleccionado.Text = $"Rol: {usuario.Rol}";

            // No permitir desactivar al propio usuario
            if (usuario.IdUsuario == SesionActual.UsuarioLogueado?.IdUsuario)
            {
                btnToggleActivo.IsEnabled = false;
                btnToggleActivo.Content = "🔒 No disponible";
            }
            else
            {
                btnToggleActivo.IsEnabled = true;
                btnToggleActivo.Content = usuario.Activo == 1 ? "🔒 Desactivar" : "🔓 Activar";
            }
        }

        // ═══════════════════════════════════════════
        // ACTIVAR/DESACTIVAR USUARIO
        // ═══════════════════════════════════════════
        private void BtnToggleActivo_Click(object sender, RoutedEventArgs e)
        {
            var usuario = GridUsuarios.SelectedItem as Usuario;
            if (usuario == null) return;

            // No permitir desactivar al propio usuario
            if (usuario.IdUsuario == SesionActual.UsuarioLogueado?.IdUsuario)
            {
                MessageBox.Show("No puedes desactivar tu propia cuenta.",
                    "⚠️ Acción no permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int nuevoEstado = usuario.Activo == 1 ? 0 : 1;
            string accion = nuevoEstado == 1 ? "activar" : "desactivar";

            var resultado = MessageBox.Show(
                $"¿Estás seguro de {accion} al usuario '{usuario.Nombre}'?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
                {
                    conexion.Execute(
                        "UPDATE Usuarios SET Activo = @Estado WHERE IdUsuario = @Id",
                        new { Estado = nuevoEstado, Id = usuario.IdUsuario });
                }

                MessageBox.Show(
                    $"Usuario '{usuario.Nombre}' ha sido {(nuevoEstado == 1 ? "activado" : "desactivado")}.",
                    "✅ Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);

                CargarTablaUsuarios();
            }
        }

        // ═══════════════════════════════════════════
        // RESET PASSWORD
        // ═══════════════════════════════════════════
        private void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            var usuario = GridUsuarios.SelectedItem as Usuario;
            if (usuario == null) return;

            string nuevaPass = "1234"; // Password por defecto al resetear

            var resultado = MessageBox.Show(
                $"¿Resetear la contraseña de '{usuario.Nombre}'?\n\nLa nueva contraseña será: {nuevaPass}",
                "Resetear Contraseña", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
                {
                    conexion.Execute(
                        "UPDATE Usuarios SET Password = @Pass WHERE IdUsuario = @Id",
                        new { Pass = nuevaPass, Id = usuario.IdUsuario });
                }

                MessageBox.Show(
                    $"Contraseña de '{usuario.Nombre}' reseteada a: {nuevaPass}",
                    "✅ Contraseña Reseteada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
    }
}
