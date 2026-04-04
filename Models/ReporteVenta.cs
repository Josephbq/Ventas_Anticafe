namespace WpfApp1.Models
{
    public class ReporteVenta
    {
        public int IdVenta { get; set; }
        public string FechaVenta { get; set; } = string.Empty;
        public int IdMesa { get; set; }
        public string? NombreMesa { get; set; }
        public double TotalPagado { get; set; }
        public int CantidadItems { get; set; }
    }
}