using System.Windows;
using WpfApp1.Views; // Asegúrate de tener esto para que reconozca tu POSView

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Opcional: Cargar el Punto de Venta automáticamente al abrir el sistema
            AreaPrincipal.Content = new POSView();
        }

        // Este es el evento que conectaste en el XAML
        private void btnPOS_Click(object sender, RoutedEventArgs e)
        {
            // Borra lo que haya en la pantalla principal y carga la vista del POS
            AreaPrincipal.Content = new POSView();
        }
    }
}