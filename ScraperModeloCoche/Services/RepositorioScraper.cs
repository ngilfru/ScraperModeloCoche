using ScraperModeloCoche.Models;
using System;
using System.Net.Http;
using HtmlAgilityPack;
using System.Globalization;
using System.Collections.Generic;
using OpenQA.Selenium;

namespace ScraperModeloCoche.Services
{
    public class RepositorioScraper
    {
        private readonly ServicioDB _servicioDB;
        private readonly HttpClient _httpClient; // Campo privado para el cliente HTTP
        public List<Vehiculo> listaVehiculos = new List<Vehiculo>();
        public RepositorioScraper()
        {
            _servicioDB = ServicioDB.Instancia; // Inicializa el servicio BBDD
            _httpClient = new HttpClient(); // Inicializa el cliente HTTP
        }

        ///<summary>
        ///Método que te lleva al tipo de modelo concreto del vehiculo
        ///</summary>
        ///<param name="url">URL con diferentes marcas de vehiculos</param>
        ///<returns>Lista de enlaces</returns>
        public void ScrapingPrimeraPagina(string url)
        {
           

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://www.ultimatespecs.com" + url;
            }

            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var aTags = doc.DocumentNode.SelectNodes("//div[@id='car_make']//div[contains(@class, 'makelinks')]//a[@href]");
            if (aTags != null)
            {
                foreach (var aTag in aTags)
                {
                    string href = aTag.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(href))
                    {
                        if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            if (href.StartsWith("//"))
                            {
                                href = "https:" + href;
                            }
                            else
                            {
                                href = "https://www.ultimatespecs.com" + href;
                            }
                            
                        }
                    }
                    if(aTag.InnerText != "Aston Martin")
                    {
                        continue;
                    }
                    Console.WriteLine("URL de Marca extraída: " + href);

                    ScrapingSegundaPagina(href);
                }
            }
        }

        ///<summary>
        ///Método que extrae los enlaces para extraer la informacion del vehiculo.
        ///</summary>
        ///<param name="url">URL de los modelos del coche</param>"
        ///<returns>Lista de enlaces</returns>
        public void ScrapingSegundaPagina(string url)
        {
            // Si la URL de la página es relativa, se completa
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://www.ultimatespecs.com" + url;
            }

            // Solicita el HTML de la URL proporcionada
            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Selecciona todos los divs que contengan la clase "home_models_line"
            var divs = doc.DocumentNode.SelectNodes("//div[contains(@class,'home_models_line')]");
            if (divs != null)
            {
                foreach (var div in divs)
                {
                    // Selecciona todos los <a> que tengan el atributo href dentro de cada div
                    var aTags = div.SelectNodes(".//a[@href]");
                    if (aTags != null)
                    {
                        foreach (HtmlNode aTag in aTags)
                        {
                            // Obtén el valor del atributo href
                            string urlVehiculo = aTag.Attributes["href"]?.Value ?? string.Empty;

                            // Si la URL es relativa, conviértela a absoluta
                            if (!string.IsNullOrEmpty(urlVehiculo) &&
                                !urlVehiculo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
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

                            // Si la URL no está vacía, llama al método para la siguiente etapa
                            if (!string.IsNullOrEmpty(urlVehiculo))
                            {
                                ScrapingTerceraPagina(urlVehiculo);
                            }
                        }
                    }
                }
            }
        }



        ///<summary>
        ///Método que extrae los diferentes modelos de coche
        ///</summary>
        ///<param name="url">URL de los modelos del vehiculo</param>
        ///<returns>Lista de objetos</returns>
        public void ScrapingTerceraPagina(string url)
        {
            // Si la URL es relativa, la completa
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://www.ultimatespecs.com" + url;
            }

            // Obtener el HTML de la URL
            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Seleccionar todos los divs con la clase "home_models_line gene"
            var divs = doc.DocumentNode.SelectNodes("//div[contains(@class, 'home_models_line gene')]");
            if (divs != null)
            {
                foreach (var div in divs)
                {
                    // Dentro de cada div, se seleccionan TODOS los <a> que tengan atributo href.
                    var aTags = div.SelectNodes(".//a[@href]");
                    if (aTags != null)
                    {
                        foreach (var aTag in aTags)
                        {
                            // Extraer el href
                            string href = aTag.GetAttributeValue("href", string.Empty);
                            if (!string.IsNullOrEmpty(href))
                            {
                                // Si es URL relativa, la convierte a absoluta.
                                if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (href.StartsWith("//"))
                                    {
                                        href = "https:" + href;
                                    }
                                    else
                                    {
                                        href = "https://www.ultimatespecs.com" + href;
                                    }
                                }

                                Console.WriteLine("URL extraída en Tercera Página: " + href);

                                 ExtraerInformacionVehiculo(href);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No se encontraron divs con la clase 'home_models_line gene'.");
            }
        }


        ///<summary>
        ///Método que extrae la información de un vehículo de la web
        ///</summary>
        ///<param name="url">URL de las especificaciones del vehiculo</param>
        ///<returns>Objeto con la información del vehículo</returns>
        public void ExtraerInformacionVehiculo(string url)
            {

            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            // Carga el contenido HTML en el documento




            doc.LoadHtml(html);
            // Obtener el texto del título que contiene marca y modelo
            var tituloNodo = doc.DocumentNode.SelectSingleNode("//div[@class='page_title']");
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
            var altoNodo = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Alto:')]/following-sibling::span[1]");
            string altoTexto = altoNodo != null ? altoNodo.InnerText.Trim() : string.Empty;
            altoTexto = System.Text.RegularExpressions.Regex.Match(altoTexto, @"[\d\.]+").Value;
            decimal alto = 0;
            if (!string.IsNullOrEmpty(altoTexto) && decimal.TryParse(altoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultadoAlto))
            {
                alto = resultadoAlto;
            }

            //Seleccionar el nodo que contiene el peso en cm
            var pesoNodo = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Peso:')]/following-sibling::span[1]");
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
                    listaVehiculos.Add(vehiculo);
                }
            }
            }
    }
}