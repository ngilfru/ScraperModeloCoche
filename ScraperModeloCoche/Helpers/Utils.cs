using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ScraperModeloCoche.Models;
using System.Windows.Forms;
using System.Drawing;
using C1.Win.C1TrueDBGrid;
using C1.Win.C1TrueDBGrid.BaseGrid;

namespace ScraperModeloCoche.Helpers
{
    public static class Utils
    {
        public const string NOMBRE_BBDD = "Flotas_dev";
#if DEBUG
        public const string NOMBRE_SERVIDOR = "SRVAPP\\SQL2017";
#else
        public const string NOMBRE_SERVIDOR = "SRVAPP\\SQL2017";        
#endif
        #region Constantes
        public const string CACHE_KEY_COMBO_DATA = "combosData";
        #endregion

        public static string ObtenerCadenaConexion(string usuario, string password)
        {            
            var builder = new SqlConnectionStringBuilder()
            {
                DataSource = NOMBRE_SERVIDOR,
                InitialCatalog = NOMBRE_BBDD,
                UserID = usuario,
                Password = password,
                CurrentLanguage = "English",
                PersistSecurityInfo = false
            };

            return builder.ConnectionString;
        }

        public static string RutaGrid()
        {
            var ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempGrid");
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
            return ruta;
        }        

        public static void MostrarAlerta(string mensaje, TipoMensaje tipo = TipoMensaje.ERROR, string titulo = Entorno.NombreAplicacion)
        {
            switch (tipo)
            {
                case TipoMensaje.ERROR:
                    MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case TipoMensaje.INFO:
                    MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case TipoMensaje.WARNING:
                    MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }

        public static DialogResult MostrarAlertaPregunta(string mensaje, string titulo = Entorno.NombreAplicacion)
        {
            return MessageBox.Show(mensaje, titulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        public static void ForzarConfiguracionRegional()
        {
            var culture = new CultureInfo("es-ES");
            var numberFormat = culture.NumberFormat;
            numberFormat.CurrencyGroupSeparator = ".";
            numberFormat.CurrencyDecimalSeparator = ",";
            numberFormat.NumberGroupSeparator = ".";
            numberFormat.NumberDecimalSeparator = ",";
            Thread.CurrentThread.CurrentCulture = culture;
        }

        public static async Task<bool> ValidarArgs(string[] args)
        {
            // Validar que nos pasen 3 argumentos: Usuario, Password y nombre del formulario
            if (args.Length != 3) { throw new ArgumentException("No se ha pasado toda la información necesaria para iniciar la aplicación"); }

            // Validar que el 3r argumento no esté vacío (El nombre del formulario a abrir)
            if (string.IsNullOrWhiteSpace(args[2])) throw new ArgumentException("Debes pasar el nombre correcto del formulario");

            // Crear cadena de conexión y validarla, aunque no haría falta porque si viene del GestFlota el usuario ya está validado
            var cadenaCon = ObtenerCadenaConexion(args[0], args[1]);
            bool cadenaConOk = await ValidarCadenaConexion(cadenaCon);

            if (cadenaConOk) Entorno.CadenaConexion = cadenaCon;            
            return true;
        }

        public static async Task<bool> ValidarCadenaConexion(string cadenaConn)
        {
            using(var cn = new SqlConnection(cadenaConn))
            {
                try
                {
                    if (cn.State == System.Data.ConnectionState.Closed) await cn.OpenAsync();
                    cn.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);                     
                }                
            }
        }        

        public static string ObtenerTrozoCadena(string cadena, int startIdx, char lastChar)
        {
            return cadena.Substring(startIdx, cadena.IndexOf(lastChar, 0)).TrimEnd();
        }

        public static string ObtenerTrozoCadena(string initialChar, string lastChar, string cadena)
        {
            return cadena.Substring((cadena.IndexOf(initialChar) + initialChar.Length), (cadena.IndexOf(lastChar) - cadena.IndexOf(initialChar) - initialChar.Length));
        }

        public static List<Operador> ListaOperadores(TipoFiltro tipoFiltro)
        {
            switch (tipoFiltro)
            {
                case TipoFiltro.Alphanumeric:
                    return new List<Operador>()
                    {
                        new Operador("Contiene", "LIKE '%@%'"),
                        new Operador("Empieza por", "LIKE '@%'"),
                        new Operador("Acaba en", "LIKE '%@'"),
                        new Operador("Igual que", "=@"),
                    };
                case TipoFiltro.Numeric:
                    return new List<Operador>()
                    {
                        new Operador("Igual que", "=@"),
                        new Operador("Mayor que", ">@"),
                        new Operador("Menor que", "<@"),
                    };
                case TipoFiltro.Date:
                    return new List<Operador>()
                    {
                        new Operador("Igual que", "='@'"),
                        new Operador("Después de", ">='@'"),
                        new Operador("Antes de", "<='@'"),
                        new Operador("Entre fecha", "BETWEEN '@' AND '@'"),
                    };
                case TipoFiltro.Multiline:
                    return new List<Operador>()
                    {
                        new Operador("Pertenece al conjunto", "IN (@)"),
                        new Operador("No pertenece al conjunto", "NOT IN (@)"),
                        new Operador("Contiene", "LIKE '%@%'"),
                        new Operador("Empieza por", "LIKE '@%'"),
                        new Operador("Acaba en", "LIKE '%@'"),
                        new Operador("Igual que", "=@"),
                    };
                case TipoFiltro.Bool:
                    return new List<Operador>()
                    {
                        new Operador("Es", "=1"),
                        new Operador("No es", "=0"),
                    };
                default:
                    return new List<Operador>()
                    {
                        new Operador("Contiene", "LIKE '%@%'"),
                        new Operador("Empieza por", "LIKE '@%'"),
                        new Operador("Igual que", "=@"),
                    };
            }
        }

        public static int ConvertBoolToInt(bool value) => value ? 1 : 0;        

        public static string GetFormName(string formToOpen)
        {
            List<Type> classes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && (!string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("ModulosGestionFlotas.UI")) && t.IsSubclassOf(typeof(Form))).ToList();

            foreach(Type t in classes)
            {
                if(t.Name.ToLower() == formToOpen.ToLower())
                    return t.FullName;
            }

            return null;                    
        }

        public static DataTable ConvertToDataTable<T>(IList<T> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }

        public static Dictionary<int, RowTypeEnum> ObtenerEstadoFilasQueSonCabecerasDeAgrupaciones(C1TrueDBGrid grid)
        {
            var res = new Dictionary<int, RowTypeEnum>();
            int idx = 0;
            foreach (ViewRow row in grid.Splits[0].Rows)
            {
                if (row is GroupRow grow)
                {
                    res.Add(idx, grow.RowType);
                    idx++;
                }

            }
            return res;
        }

        public static string CapturarPantalla()
        {
            var screen = Screen.PrimaryScreen.Bounds;
            var bmp = new Bitmap(screen.Width, screen.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(screen.Left, screen.Top, 0, 0, screen.Size);
            }

            // Guardar la imagen en un archivo
            string screenshotPath = Path.Combine(Path.GetTempPath(), "screenshot.png");
            bmp.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);

            return screenshotPath;
        }

    }
}
