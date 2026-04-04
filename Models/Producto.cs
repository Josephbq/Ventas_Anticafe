namespace WpfApp1.Models
{
    public class Producto
    {
        public int IdProducto { get; set; }
        public string? CodigoBarras { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "CAFETERIA"; // 'CAFETERIA' o 'JUEGOS'
        public double PrecioVentaBase { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public int EsInventariable { get; set; } = 1; // 1 = sí (cafetería), 0 = no (juegos son servicios)
        public string EstadoProducto { get; set; } = "Disponible"; // Disponible, Agotado, Dañado, Suspendido
        public int Activo { get; set; } = 1;
    }
}