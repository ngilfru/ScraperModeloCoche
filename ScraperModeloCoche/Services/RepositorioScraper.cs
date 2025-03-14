using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperModeloCoche.Services
{
    public class RepositorioScraper
    {
        private readonly ServicioDB _servicioDB;
        public RepositorioScraper()
        {
            _servicioDB = ServicioDB.Instancia;
        }
    }
}
