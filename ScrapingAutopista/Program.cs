using ScrapingAutopista.Models;
using ScrapingAutopista.Services;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScrapingAutopista
{
    internal class Program
    {
        public static async Task InsertVehiculosAsync(List<Vehiculo> vehiculos)
        {
            foreach (var vehiculo in vehiculos)
            {
                string sql = "INSERT INTO TipusModelCotxe_Scraper(ModelCotxe, Marca, Largo, Ancho, Alto, Peso, CreatAuto, FactorCarga, TipoMotor) " +
                             "VALUES (@ModelCotxe, @Marca, @Largo, @Ancho, @Alto, @Peso, @CreatAuto, @FactorCarga, @TipoMotor)";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ModelCotxe", vehiculo.MarcaModelo);
                parameters.Add("@Marca", vehiculo.MarcaModelo);
                parameters.Add("@Largo", Convert.ToDecimal(vehiculo.Largo));
                parameters.Add("@Ancho", Convert.ToDecimal(vehiculo.Ancho));
                parameters.Add("@Alto", Convert.ToDecimal(vehiculo.Alto));
                parameters.Add("@Peso", Convert.ToDecimal(vehiculo.Peso));
                parameters.Add("@CreatAuto", 0);
                parameters.Add("@FactorCarga", 0);
                parameters.Add("@TipoMotor", vehiculo.TipoMotor);

                bool ok = await ServicioDB.Instancia.ExecuteCommandAsync(sql, parameters);
                if (ok)
                    Console.WriteLine($"Vehículo {vehiculo.MarcaModelo} insertado con éxito.");
                else
                    Console.WriteLine($"Error insertando vehículo {vehiculo.MarcaModelo}.");
            }
        }

        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("No se han pasado suficientes argumentos. Usar: <usuario> <password>");
                Console.ReadKey();
                return;
            }

            Entorno.CadenaConexion = Helpers.Utils.ObtenerCadenaConexion(args[0], args[1]);
            Console.WriteLine("Cadena de conexión establecida.");
            await extraerInformacion();
        }

        static async Task extraerInformacion()
        {
            RepositorioScraper repositorioScraper = new RepositorioScraper();
            repositorioScraper.ScrapingPrimeraPagina("https://www.autopista.es/coches/");
            Console.WriteLine($"Se han obtenido {repositorioScraper.listaVehiculos.Count} vehículos.");

            // Insertar en la BBDD
            await InsertVehiculosAsync(repositorioScraper.listaVehiculos);

            Console.WriteLine("Proceso completado. Presione cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}
