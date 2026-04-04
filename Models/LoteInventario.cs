namespace WpfApp1.Models
{
    public class LoteInventario
    {
        public int IdLote { get; set; }
        public int IdProducto { get; set; }
        public double CostoUnitario { get; set; }
        public double PrecioVentaLote { get; set; }
        public int CantidadInicial { get; set; }
        public int CantidadDisponible { get; set; }
        public string FechaIngreso { get; set; } = string.Empty;
        public string? Notas { get; set; }

        // Para mostrar en la UI
        public string? NombreProducto { get; set; }
    }
}
