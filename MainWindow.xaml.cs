using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Dapper;
using WpfApp1.Data;
using WpfApp1.Views;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private Button _botonActivo;

        public MainWindow()
        {
            InitializeComponent();

            // Reloj en tiempo real
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            ActualizarReloj();

            // Mostrar info del usuario logueado
            ConfigurarSesion();

            // Configurar visibilidad de botones según rol
            ConfigurarPermisos();
        }

        private void ConfigurarSesion()
        {
            lblNombreUsuario.Text = SesionActual.NombreUsuario;
        }

        private void ConfigurarPermisos()
        {
            if (SesionActual.EsAdmin)
            {
                // Admin ve todo
                btnDashboard.Visibility = Visibility.Visible;
                btnPOS.Visibility = Visibility.Visible;
                btnInventario.Visibility = Visibility.Visible;
                btnReportes.Visibility = Visibility.Visible;
                btnUsuarios.Visibility = Visibility.Visible;

                // Cargar Dashboard por defecto
                _botonActivo = btnDashboard;
                AreaPrincipal.Content = new DashboardView();
                ActualizarBadgeAlertas();
            }
            else if (SesionActual.EsVendedor)
            {
                // Vendedor solo ve POS
                btnDashboard.Visibility = Visibility.Collapsed;
                btnPOS.Visibility = Visibility.Visible;
                btnInventario.Visibility = Visibility.Collapsed;
                btnReportes.Visibility = Visibility.Collapsed;
                btnUsuarios.Visibility = Visibility.Collapsed;

                // Cargar POS por defecto
                btnPOS.Style = (Style)FindResource("SidebarButtonActive");
                _botonActivo = btnPOS;
                AreaPrincipal.Content = new POSView();
                lblTituloSeccion.Text = "Punto de Venta";
            }
            else if (SesionActual.EsSuper)
            {
                // Super ve Dashboard, Inventario (lectura), Reportes
                btnDashboard.Visibility = Visibility.Visible;
                btnPOS.Visibility = Visibility.Collapsed;
                btnInventario.Visibility = Visibility.Visible;
                btnReportes.Visibility = Visibility.Visible;
                btnUsuarios.Visibility = Visibility.Collapsed;

                // Cargar Dashboard por defecto
                _botonActivo = btnDashboard;
                AreaPrincipal.Content = new DashboardView();
                ActualizarBadgeAlertas();
            }
            else
            {
                // Fallback: solo POS
                btnDashboard.Visibility = Visibility.Collapsed;
                btnPOS.Visibility = Visibility.Visible;
                btnInventario.Visibility = Visibility.Collapsed;
                btnReportes.Visibility = Visibility.Collapsed;
                btnUsuarios.Visibility = Visibility.Collapsed;

                btnPOS.Style = (Style)FindResource("SidebarButtonActive");
                _botonActivo = btnPOS;
                AreaPrincipal.Content = new POSView();
                lblTituloSeccion.Text = "Punto de Venta";
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            ActualizarReloj();
            ActualizarBadgeAlertas();
        }

        private void ActualizarReloj()
        {
            lblFecha.Text = DateTime.Now.ToString("dd MMM yyyy");
            lblHora.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy  •  HH:mm");
        }

        private void ActualizarBadgeAlertas()
        {
            try
            {
                using (var conexion = new SqliteConnection(ConexionDB.CadenaConexion))
                {
                    int alertas = conexion.ExecuteScalar<int>(@"
                        SELECT COUNT(*) FROM Productos 
                        WHERE Activo = 1 AND (
                            (EsInventariable = 1 AND StockActual <= StockMinimo)
                            OR EstadoProducto IN ('Agotado', 'Dañado', 'Suspendido')
                        )");

                    if (alertas > 0)
                    {
                        badgeAlerta.Visibility = Visibility.Visible;
                        lblBadgeCount.Text = alertas.ToString();
                    }
                    else
                    {
                        badgeAlerta.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch { /* La BD puede no existir aún */ }
        }

        private void SetBotonActivo(Button boton, string tituloSeccion)
        {
            // Restaurar estilo anterior
            if (_botonActivo != null)
                _botonActivo.Style = (Style)FindResource("SidebarButton");

            // Activar nuevo
            boton.Style = (Style)FindResource("SidebarButtonActive");
            _botonActivo = boton;
            lblTituloSeccion.Text = tituloSeccion;
        }

        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            SetBotonActivo(btnDashboard, "Dashboard");
            AreaPrincipal.Content = new DashboardView();
            ActualizarBadgeAlertas();
        }

        private void btnPOS_Click(object sender, RoutedEventArgs e)
        {
            SetBotonActivo(btnPOS, "Punto de Venta");
            AreaPrincipal.Content = new POSView();
        }

        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            SetBotonActivo(btnInventario, "Inventario");
            AreaPrincipal.Content = new InventarioView();
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            SetBotonActivo(btnReportes, "Reportes de Ventas");
            AreaPrincipal.Content = new ReportesView();
        }

        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            SetBotonActivo(btnUsuarios, "Gestión de Usuarios");
            AreaPrincipal.Content = new UsuariosView();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Estás seguro de que deseas cerrar sesión?",
                "Cerrar Sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                _timer.Stop();
                SesionActual.CerrarSesion();
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }
    }
}