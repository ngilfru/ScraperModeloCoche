using ScraperModeloCoche.Models;
using System;
using System.Net.Http;
using HtmlAgilityPack;
using System.Globalization;
using System.Collections.Generic;

namespace ScraperModeloCoche.Services
{
    public class RepositorioScraper
    {
        private readonly ServicioDB _servicioDB;
        private readonly HttpClient _httpClient; // Campo privado para el cliente HTTP
        public RepositorioScraper()
        {
            _servicioDB = ServicioDB.Instancia; // Inicializa el servicio BBDD
            _httpClient = new HttpClient(); // Inicializa el cliente HTTP
        }

        ///<summary>
        ///Método que extrae los enlaces para extraer la informacion del vehiculo.
        ///</summary>
        ///<param name="url">URL de los modelos del coche</param>"
        ///<returns>Lista de enlaces</returns>
        public List<Vehiculo> ScrapingSegundaPagina(string url)
        {
            // Si la URL de la página principal es relativa, complétala
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://www.ultimatespecs.com" + url;
            }

            List<Vehiculo> listaVehiculos = new List<Vehiculo>();
            // Solicita el HTML de la URL proporcionada
            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Selecciona todos los divs con la clase "home_models_line gene"
            var divs = doc.DocumentNode.SelectNodes("//div[contains(@class,'home_models_line gene')]");
            if (divs != null)
            {
                foreach (var div in divs)
                {
                    // Selecciona únicamente los <a> que tienen el atributo href
                    var aTag = div.SelectSingleNode(".//a[@href]");
                    if (aTag != null)
                    {
                        // Extrae el valor del atributo href
                        string urlVehiculo = aTag.GetAttributeValue("href", string.Empty);

                        // Si la URL es relativa, conviértela a absoluta
                        if (!urlVehiculo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            if (urlVehiculo.StartsWith("//"))
                            {
                                urlVehiculo = "https:" + urlVehiculo;
                            }
                            else
                            {
                                urlVehiculo = "https://www.ultimatespecs.com" + urlVehiculo;
                            }
                        }

                        Console.WriteLine("URL extraída: " + urlVehiculo);

                        // Si la URL no está vacía, llama al método para extraer la información del vehículo
                        if (!string.IsNullOrEmpty(urlVehiculo))
                        {
                            List<Vehiculo> vehiculosDetalle = ExtraerInformacionVehiculo(urlVehiculo);
                            listaVehiculos.AddRange(vehiculosDetalle);
                        }
                    }
                }
            }
            return listaVehiculos;
        }

        ///<summary>
        ///Método que extrae la información de un vehículo de la web
        ///</summary>
        ///<param name="url">URL de las especificaciones del vehiculo</param>
        ///<returns>Objeto con la información del vehículo</returns>
        public List<Vehiculo> ExtraerInformacionVehiculo(string url)
            {

            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            // Carga el contenido HTML en el documento
            doc.LoadHtml(html);
            // Obtener el texto del título que contiene marca y modelo
            var tituloNodo = doc.DocumentNode.SelectSingleNode("//div[@class='page_title_text']");
                string tituloModelo = tituloNodo != null ? tituloNodo.InnerText.Trim() : string.Empty;
                //Quitar de la cadena el texto "Ficha técnica" que aparece al final
                tituloModelo = tituloModelo.Replace("Ficha Tecnica", "").Trim();

                //Seleccionar el nodo que contiene el largo en cm
                var largoNodo = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Largo:')]/following-sibling::span[1]");
                string largoTexto = largoNodo != null ? largoNodo.InnerText.Trim() : string.Empty;
                //Solo extraer el número de la cadena
                largoTexto = System.Text.RegularExpressions.Regex.Match(largoTexto, @"[\d\.]+").Value;
                //Convertir finalmente a decimal
                decimal largo = 0;
                if (!string.IsNullOrEmpty(largoTexto) && decimal.TryParse(largoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                {
                    largo = result;
                }

                //Seleccionar el nodo que contiene el ancho en cm
                var anchoNodo = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Ancho:')]/following-sibling::span[1]");
                string anchoTexto = anchoNodo != null ? anchoNodo.InnerText.Trim() : string.Empty;
                anchoTexto = System.Text.RegularExpressions.Regex.Match(anchoTexto, @"[\d\.]+").Value;
                decimal ancho = 0;
                if (!string.IsNullOrEmpty(anchoTexto) && decimal.TryParse(anchoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultadoAncho))
                {
                    ancho = resultadoAncho;
                }

                //Seleccionar el nodo que contiene el alto en cm
                var altoNodo = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Ancho:')]/following-sibling::span[1]");
                string altoTexto = altoNodo != null ? altoNodo.InnerText.Trim() : string.Empty;
                altoTexto = System.Text.RegularExpressions.Regex.Match(altoTexto, @"[\d\.]+").Value;
                decimal alto = 0;
                if (!string.IsNullOrEmpty(altoTexto) && decimal.TryParse(altoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultadoAlto))
                {
                    alto = resultadoAlto;
                }

                //Seleccionar el nodo que contiene el peso en cm
                var pesoNodo = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Ancho:')]/following-sibling::span[1]");
                string pesoTexto = pesoNodo != null ? pesoNodo.InnerText.Trim() : string.Empty;
                pesoTexto = System.Text.RegularExpressions.Regex.Match(pesoTexto, @"[\d\.]+").Value;
                decimal peso = 0;
                if (!string.IsNullOrEmpty(pesoTexto) && decimal.TryParse(pesoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultadoPeso))
                {
                    peso = resultadoPeso;
                }

                //Seleccionar las tablas que contienen el tipo de motor y extraer los combustibles para pasarlos a char.
                List<char> combustibles = new List<char>();
                var tablaNodos = doc.DocumentNode.SelectNodes("//table[contains(@class,'table_versions')]");
                if (tablaNodos != null)
                {
                    //por cada tabla buscará h2 para ver de qué tipo de combustible se trata
                    foreach (var tabla in tablaNodos) {
                        var header = tabla.SelectSingleNode(".//h2");
                        if (header != null)
                        {
                            string headerText = header.InnerText.Trim();

                            if (headerText.IndexOf("Gasolina", StringComparison.OrdinalIgnoreCase) >= 0)
                                combustibles.Add('G');
                            if (headerText.IndexOf("Diesel", StringComparison.OrdinalIgnoreCase) >= 0)
                                combustibles.Add('D');
                            if (headerText.IndexOf("Eléctrico", StringComparison.OrdinalIgnoreCase) >= 0)
                                combustibles.Add('E');
                            if (headerText.IndexOf("Híbrido", StringComparison.OrdinalIgnoreCase) >= 0)
                                combustibles.Add('H');
                        }
                    }
                }

                //lista de vehículos en caso de que haya más de un tipo de combustible
                List<Vehiculo> vehiculos = new List<Vehiculo>();
                if (combustibles.Count > 0)
                {
                    foreach (var combustible in combustibles) {
                        Vehiculo vehiculo = new Vehiculo
                        {
                            MarcaModelo = tituloModelo,
                            Largo = largo.ToString(),
                            Ancho = ancho.ToString(),
                            Alto = alto.ToString(),
                            Peso = peso.ToString(),
                            TipoMotor = combustible
                        };
                        vehiculos.Add(vehiculo);
                    }
                }

                return vehiculos;
            }
    }
}