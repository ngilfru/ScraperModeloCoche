
using ScraperModeloCoche.Services;
using System;

namespace ScraperModeloCoche
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("No se han pasado suficientes argumentos. Usar: <usuario> <password>");
                Console.ReadKey();
                return;
            }

            Entorno.CadenaConexion = Helpers.Utils.ObtenerCadenaConexion(args[0], args[1]);
            Console.WriteLine("Cadena de conexión establecida.");
            extraerInformacion();
        }
        static  void extraerInformacion()
        {
             RepositorioScraper repositorioScraper = new RepositorioScraper();
             repositorioScraper.ScrapingPrimeraPagina("https://www.ultimatespecs.com/es/car-specs");
            Console.WriteLine(repositorioScraper.listaVehiculos.Count);
            Console.ReadKey();
        }

    }
}
