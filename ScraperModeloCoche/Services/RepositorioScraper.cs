using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Globalization;
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
