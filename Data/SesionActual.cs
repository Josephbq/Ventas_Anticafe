using WpfApp1.Models;

namespace WpfApp1.Data
{
    public static class SesionActual
    {
        public static Usuario? UsuarioLogueado { get; set; }
        public static string Rol => UsuarioLogueado?.Rol ?? "";
        public static string NombreUsuario => UsuarioLogueado?.Nombre ?? "Sin sesión";
        public static bool EsAdmin => Rol == "Admin";
        public static bool EsVendedor => Rol == "Vendedor";
        public static bool EsSuper => Rol == "Super";

        public static void CerrarSesion()
        {
            UsuarioLogueado = null;
        }
    }
}
