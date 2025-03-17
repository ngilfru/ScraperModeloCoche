using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperModeloCoche.Models
{
    public class Vehiculo
    {
        public string MarcaModelo { get; set; }
        public string Largo { get; set; }
        public string Ancho { get; set; }
        public string Alto { get; set; }
        public string Peso { get; set; }
        // 'G' Gasolina, 'D' Diesel, 'H' Hibrido, 'E' Electrico
        public char TipoMotor { get; set; }

    }
}
