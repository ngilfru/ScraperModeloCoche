using ScrapingAutopista.Models;
using System;
using System.Net.Http;
using HtmlAgilityPack;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace ScrapingAutopista.Services
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
            // Si la URL es relativa, la convierte a absoluta
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://www.autopista.es" + url;
            }

            // Descarga el HTML de la página
            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Selecciona todos los divs con la clase "marca"
            var marcaNodes = doc.DocumentNode.SelectNodes("//div[@class='marca']");
            if (marcaNodes != null)
            {
                foreach (var marca in marcaNodes)
                {
                    // Dentro de cada "marca", se busca el <a> dentro del div "texto"
                    var aTag = marca.SelectSingleNode(".//div[@class='texto']/a[@href]");
                    if (aTag != null)
                    {
                        string href = aTag.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(href))
                        {
                            // Convierte la URL relativa a absoluta
                            if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                if (href.StartsWith("//"))
                                {
                                    href = "https:" + href;
                                }
                                else
                                {
                                    href = "https://www.autopista.es" + href;
                                }
                            }
                            Console.WriteLine("URL de Marca extraída: " + href);

                            // Llama a la siguiente función para procesar la página de la marca
                            ScrapingSegundaPagina(href);
                        }
                    }
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
            // Convertir la URL relativa a absoluta si es necesario
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://www.autopista.es" + url;
            }

            // Descargar el HTML de la URL
            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Selecciona todos los <a> que se encuentran dentro de "div.modelo" > "div.texto" dentro del bloque de modelos
            var aTags = doc.DocumentNode.SelectNodes("//div[contains(@class,'modulo grid-modelos bloque')]//div[@class='modelo']//div[@class='texto']/a[@href]");
            if (aTags == null)
            {
                Console.WriteLine("No se encontraron enlaces de modelo.");
                return;
            }

            // Recorre cada enlace extraído
            foreach (var aTag in aTags)
            {
                string href = aTag.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrEmpty(href))
                    continue;

                // Si la URL es relativa, la convierte a absoluta
                if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    href = href.StartsWith("//") ? "https:" + href : "https://www.autopista.es" + href;
                }

                Console.WriteLine("URL de modelo extraída: " + href);

                // Llamada al método que procesa el detalle del modelo
                 ScrapingTerceraPagina(href);
            }
        }


        ///<summary>
        ///Método que extrae los diferentes modelos de coche
        ///</summary>
        ///<param name="url">URL de los modelos del vehiculo</param>
        ///<returns>Lista de objetos</returns>
        public void ScrapingTerceraPagina(string url)
        {
            // Completa la URL si es relativa.
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "https://www.autopista.es" + url;

            // Descarga y carga el HTML.
            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Selecciona los enlaces de cada versión (del primer <td> de cada fila del <tbody>)
            var aTags = doc.DocumentNode.SelectNodes(
                "//div[contains(@class, 'modulo') and contains(@class, 'tabla-modelos') and contains(@class, 'bloque')]//div[@class='inner']//table[contains(@class, 'tablesorter')]//tbody/tr/td[1]/a[@href]");

            if (aTags == null)
            {
                Console.WriteLine("No se encontraron enlaces en la tabla.");
                return;
            }

            // Lista para almacenar los datos de todas las versiones para este modelo.
            List<Vehiculo> versiones = new List<Vehiculo>();
            foreach (var aTag in aTags)
            {
                string href = aTag.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrEmpty(href))
                    continue;

                // Convierte la URL relativa a absoluta.
                if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    href = href.StartsWith("//") ? "https:" + href : "https://www.autopista.es" + href;

                Console.WriteLine("URL extraída: " + href);

                // Extrae la información de cada versión y agrega todos los objetos devueltos.
                List<Vehiculo> vList = ExtraerInformacionVehiculo(href);
                versiones.AddRange(vList);
            }

            string modeloTitulo = "";
            var modeloNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'modulo') and contains(@class, 'texto-marca')]");
            if (modeloNode != null)
            {
                var h1Model = modeloNode.SelectSingleNode(".//h1");
                if (h1Model != null)
                    modeloTitulo = h1Model.InnerText.Trim();
            }

            // Agrupar las versiones por tipo de motor y tomar el máximo de cada dimensión.
            var agrupado = versiones.GroupBy(v => v.TipoMotor);
            foreach (var grupo in agrupado)
            {
                decimal maxLargo = grupo.Max(v => decimal.Parse(v.Largo, CultureInfo.InvariantCulture));
                decimal maxAncho = grupo.Max(v => decimal.Parse(v.Ancho, CultureInfo.InvariantCulture));
                decimal maxAlto = grupo.Max(v => decimal.Parse(v.Alto, CultureInfo.InvariantCulture));
                decimal maxPeso = grupo.Max(v => decimal.Parse(v.Peso, CultureInfo.InvariantCulture));

                Vehiculo vehiculoFinal = new Vehiculo
                {
                    MarcaModelo = modeloTitulo,
                    Largo = maxLargo.ToString(CultureInfo.InvariantCulture),
                    Ancho = maxAncho.ToString(CultureInfo.InvariantCulture),
                    Alto = maxAlto.ToString(CultureInfo.InvariantCulture),
                    Peso = maxPeso.ToString(CultureInfo.InvariantCulture),
                    TipoMotor = grupo.Key
                };

                listaVehiculos.Add(vehiculoFinal);
                Console.WriteLine($"Vehículo agregado: {modeloTitulo} - {grupo.Key}");
            }
        }


        ///<summary>
        ///Método que extrae la información de un vehículo de la web
        ///</summary>
        ///<param name="url">URL de las especificaciones del vehiculo</param>
        ///<returns>Objeto con la información del vehículo</returns>
        public List<Vehiculo> ExtraerInformacionVehiculo(string url)
        {
            List<Vehiculo> resultado = new List<Vehiculo>();

            // Si la URL es relativa, se completa.
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://www.autopista.es" + url;
            }

            // Descarga y carga el HTML.
            var html = _httpClient.GetStringAsync(url).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 1. Extraer el título (marca y modelo) desde el <h1> dentro del div "inner".
            string tituloModelo = "";
            var innerDiv = doc.DocumentNode.SelectSingleNode("//div[@class='inner']");
            if (innerDiv != null)
            {
                var h1Node = innerDiv.SelectSingleNode(".//h1");
                if (h1Node != null)
                {
                    tituloModelo = h1Node.InnerText.Trim();
                }
            }

            // 2. Variables para almacenar dimensiones y combustible.
            decimal largo = 0, ancho = 0, alto = 0, peso = 0;
            // Utilizamos un HashSet para recolectar los combustibles únicos.
            HashSet<string> combustiblesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Recorrer todos los contenedores de datos oficiales (pueden tener diferentes ids).
            var dataOfficialNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'data-official')]");
            if (dataOfficialNodes != null)
            {
                foreach (var dataOfficial in dataOfficialNodes)
                {
                    var rows = dataOfficial.SelectNodes(".//table/tbody/tr");
                    if (rows == null)
                        continue;

                    foreach (var row in rows)
                    {
                        var tds = row.SelectNodes("./td");
                        if (tds == null || tds.Count < 2)
                            continue;

                        string label = tds[0].InnerText.Trim();
                        string value = tds[1].InnerText.Trim();
                        // Extraer solo la parte numérica (por ejemplo, "5331" de "5331 mm").
                        string numValue = System.Text.RegularExpressions.Regex.Match(value, @"[\d\.]+").Value;

                        if (label.Equals("Largo", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal.TryParse(numValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out largo);
                        }
                        else if (label.Equals("Ancho", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal.TryParse(numValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ancho);
                        }
                        else if (label.Equals("Alto", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal.TryParse(numValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out alto);
                        }
                        else if (label.Equals("Peso", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal.TryParse(numValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out peso);
                        }
                        else if (label.Equals("Combustible", StringComparison.OrdinalIgnoreCase))
                        {
                            // Si hay múltiples combustibles, los separamos por comas o punto y coma.
                            var partes = value.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var parte in partes)
                            {
                                string trimmed = parte.Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                {
                                    combustiblesSet.Add(trimmed);
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Título: " + tituloModelo);
            Console.WriteLine("Largo: " + largo);
            Console.WriteLine("Ancho: " + ancho);
            Console.WriteLine("Alto: " + alto);
            Console.WriteLine("Peso: " + peso);
            Console.WriteLine("Combustibles encontrados: " + string.Join(", ", combustiblesSet));

            // 3. Por cada tipo de combustible único, determinar el carácter y crear un objeto Vehiculo.
            foreach (var combustible in combustiblesSet)
            {
                char tipoMotor = ' ';
                if (combustible.IndexOf("Eléctrico", StringComparison.OrdinalIgnoreCase) >= 0)
                    tipoMotor = 'E';
                else if (combustible.IndexOf("Híbrido", StringComparison.OrdinalIgnoreCase) >= 0)
                    tipoMotor = 'H';
                else if (combustible.IndexOf("Gasolina", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         combustible.IndexOf("Gasóleo", StringComparison.OrdinalIgnoreCase) >= 0)
                    tipoMotor = 'G';

                // Creamos el objeto Vehiculo para cada combustible.
                Vehiculo vehiculo = new Vehiculo
                {
                    MarcaModelo = tituloModelo,
                    Largo = largo.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Ancho = ancho.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Alto = alto.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Peso = peso.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    TipoMotor = tipoMotor
                };

                resultado.Add(vehiculo);
                Console.WriteLine("Vehículo agregado: " + tituloModelo + " - " + tipoMotor);
            }

            return resultado;
        }

    }
}