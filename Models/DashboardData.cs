namespace WpfApp1.Models
{
    public class ProductoPopular
    {
        public string Nombre { get; set; } = string.Empty;
        public int TotalVendido { get; set; }
    }

    public class VentaPorHora
    {
        public int Hora { get; set; }
        public double Total { get; set; }
        public string HoraStr => $"{Hora:00}:00";
    }

    public class VentaPorDia
    {
        public string DiaSemana { get; set; } = string.Empty;
        public int DiaNum { get; set; }
        public double Total { get; set; }
    }

    public class ResumenCategoria
    {
        public string TipoOrigen { get; set; } = string.Empty;
        public double Total { get; set; }
        public string Etiqueta => TipoOrigen == "JUEGOS" ? "🎲 Juegos" : "☕ Cafetería";
    }
}
