using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperAutopista.Models
{
    public class Operador
    {
        private string _descripcion;
        private string _valorSql;

        public string Descripcion { get => _descripcion; set => _descripcion = value; }
        public string ValorSql { get => _valorSql; set => _valorSql = value; }

        public Operador(string desc, string valorSql)
        {
            _descripcion = desc;
            _valorSql = valorSql;
        }
    }
}
