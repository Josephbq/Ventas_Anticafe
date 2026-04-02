using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    public class ReporteVenta
    {
        public int IdVenta { get; set; }
        public string FechaVenta { get; set; }
        public int IdMesa { get; set; }
        public double TotalPagado { get; set; }
    }
}