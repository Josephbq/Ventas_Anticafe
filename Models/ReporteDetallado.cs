namespace WpfApp1.Models
{
    public class ReporteDetallado
    {
        public string NombreProducto { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public double PrecioPromedioVenta { get; set; }
        public double CostoPromedio { get; set; }
        public double TotalVentas { get; set; }
        public double TotalCosto { get; set; }
        public double Ganancia { get; set; }

        // Propiedades formateadas para la UI
        public string PrecioPromedioVentaStr => $"{PrecioPromedioVenta:F2} Bs";
        public string CostoPromedioStr => $"{CostoPromedio:F2} Bs";
        public string TotalVentasStr => $"{TotalVentas:F2} Bs";
        public string TotalCostoStr => $"{TotalCosto:F2} Bs";
        public string GananciaStr => $"{Ganancia:F2} Bs";
    }
}
