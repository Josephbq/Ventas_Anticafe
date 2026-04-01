using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    public class Mesa
    {
        public int IdMesa { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; } // 'Libre' u 'Ocupada'
    }
}
