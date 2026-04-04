namespace WpfApp1.Models
{
    public class AlertaStock
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public string EstadoProducto { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;

        public string MensajeAlerta
        {
            get
            {
                if (EstadoProducto == "Dañado") return $"⚠️ {Nombre} está DAÑADO";
                if (EstadoProducto == "Suspendido") return $"🚫 {Nombre} está SUSPENDIDO";
                if (StockActual == 0) return $"🔴 {Nombre} — SIN STOCK";
                if (StockActual <= StockMinimo) return $"🟡 {Nombre} — Stock bajo ({StockActual} uds)";
                return $"✅ {Nombre} — OK";
            }
        }

        public string ColorAlerta
        {
            get
            {
                if (EstadoProducto == "Dañado" || EstadoProducto == "Suspendido") return "#C0392B";
                if (StockActual == 0) return "#C0392B";
                if (StockActual <= StockMinimo) return "#F39C12";
                return "#27AE60";
            }
        }
    }
}
