using System.Configuration;
using System.Data;
using System.Windows;
using WpfApp1.Data; // Para poder acceder a tu ConexionDB

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Crea el archivo .db y las tablas si no existen
            WpfApp1.Data.ConexionDB.InicializarBaseDeDatos();
        }
    }

}
