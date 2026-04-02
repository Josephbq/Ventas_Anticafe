using System.Windows;
using WpfApp1.Views;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Este es el evento que conectaste en el XAML
        private void btnPOS_Click(object sender, RoutedEventArgs e)
        {
            // Borra lo que haya en la pantalla principal y carga la vista del POS
            AreaPrincipal.Content = new POSView();
        }

        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            // Cambiamos el contenido del área blanca por nuestra nueva pantalla de Inventario
            AreaPrincipal.Content = new InventarioView();
        }
        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            AreaPrincipal.Content = new ReportesView();
        }
    }
}