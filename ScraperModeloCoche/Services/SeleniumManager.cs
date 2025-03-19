using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ScraperModeloCoche.Services
{
    public class SeleniumManager : IDisposable
    {
        public IWebDriver _driver;
        private ChromeDriverService _service;
        private readonly object _lock = new object();
        private bool _disposed = false;
        private string binaryPath;
        private string chromeDriverPath;
        private string path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Services", "chrome-win64");

        // Importación de funciones de la API de Windows
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int SW_SHOW = 5;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        public SeleniumManager()
        {
            binaryPath = System.IO.Path.Combine(path, "chrome.exe");
            chromeDriverPath = System.IO.Path.Combine(path, "chromedriver.exe");
            InitializeDriver();
        }

        private void InitializeDriver()
        {
            lock (_lock)
            {
                if (_driver == null)
                {
                    // Configuración avanzada del servicio para ocultar la ventana del CMD
                    _service = ChromeDriverService.CreateDefaultService(path);
                    _service.HideCommandPromptWindow = true;
                    _service.SuppressInitialDiagnosticInformation = true;

                    // Configuración de ChromeOptions para mayor stealth
                    var options = new ChromeOptions();
                    options.AddArgument("--disable-blink-features=AutomationControlled");
                    options.AddExcludedArgument("enable-automation");
                    options.AddArgument("--disable-extensions");
                    options.AddArgument("--disable-infobars");
                    options.AddArgument("--no-sandbox");
                    options.AddArgument("--disable-dev-shm-usage");
                    options.AddArgument("--disable-gpu");

                    // OPCIÓN 1: Utilizar modo headless (nueva versión) para ocultar la ventana del navegador
                    //options.AddArgument("--headless=new");

                    // OPCIÓN 2 (Alternativa): Si no se desea utilizar headless, mover la ventana fuera de la pantalla
                    // options.AddArgument("--window-position=-32000,-32000");

                    // (Opcional) Establecer un user agent personalizado
                    // options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) ...");

                    // Inicializamos el driver con el servicio y las opciones configuradas
                    _driver = new ChromeDriver(_service, options);

                    // Inyectamos scripts para evitar que se detecte la automatización
                    ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                        Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                        Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
                    ");
                }
            }
        }

        /// <summary>
        /// Oculta la ventana del navegador de la barra de tareas utilizando una técnica
        /// que enumera las ventanas del proceso y modifica sus estilos.
        /// </summary>
        public void HideBrowser()
        {
            try
            {
                Process proc = Process.GetProcessById(_service.ProcessId);
                // Utilizamos la clase auxiliar para obtener el handle de la ventana principal
                IntPtr hWnd = WindowHelper.GetMainWindowHandle(proc.Id);
                if (hWnd != IntPtr.Zero)
                {
                    // Obtenemos los estilos extendidos actuales de la ventana
                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    // Removemos la bandera que permite que la ventana se muestre en la barra de tareas
                    exStyle &= ~WS_EX_APPWINDOW;
                    // Agregamos la bandera de ventana tipo "tool" para que no aparezca en la barra de tareas
                    exStyle |= WS_EX_TOOLWINDOW;
                    SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);
                    // Se vuelve a mostrar la ventana para aplicar los cambios de estilo
                    ShowWindow(hWnd, SW_SHOW);
                }
                else
                {
                    Console.WriteLine("No se encontró la ventana principal del proceso.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al ocultar la ventana: " + ex.Message);
            }
        }

        // Resto de métodos y propiedades de la clase...
        public IWebDriver Driver
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(SeleniumManager));
                return _driver;
            }
        }

        public string GetHtmlWithoutJsLoad(string url, int timeToWait = 0, int timeoutInSeconds = 3)
        {
            try
            {
                Driver.Navigate().GoToUrl(url);
                if (timeToWait > 0)
                    Thread.Sleep(timeToWait * 1000);
                if (DetectCaptcha() && !BypassCaptcha())
                    throw new Exception("Failed to bypass captcha.");
                return Driver.PageSource;
            }
            catch (WebDriverTimeoutException)
            {
                throw new TimeoutException("The page load timeout was exceeded.");
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while retrieving HTML: {ex.Message}", ex);
            }
        }

        public string GetHtmlWithJsLoad(string url, int timeoutInSeconds = 10)
        {
            try
            {
                Driver.Navigate().GoToUrl(url);
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutInSeconds));
                wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").ToString() == "complete");

                bool bypassCaptcha = DetectCaptcha() && !BypassCaptcha();

                return Driver.PageSource;
            }
            catch (WebDriverTimeoutException)
            {
                throw new TimeoutException("The page load timeout was exceeded.");
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while retrieving HTML: {ex.Message}", ex);
            }
        }

        public bool DetectCaptcha()
        {
            /*try
            {
                return Driver.FindElement(By.ClassName("g-recaptcha")) != null;
            }
            catch (NoSuchElementException)
            {
                return false;
            }*/
            try
            {
                // Busca un iframe cuyo src contenga la URL de los desafíos de Cloudflare
                return Driver.FindElement(By.CssSelector("div.cf-turnstile")) != null;
            }
            catch (NoSuchElementException)
            {
                return false;
            }

        }

        public bool BypassCaptcha()
        {
            try
            {
                var captchaElement = Driver.FindElement(By.ClassName("g-recaptcha"));
                if (captchaElement != null)
                {
                    new OpenQA.Selenium.Interactions.Actions(Driver)
                        .MoveToElement(captchaElement)
                        .Click()
                        .Perform();
                    Thread.Sleep(30000); // Ajusta el tiempo de espera según sea necesario
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to bypass captcha: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    try
                    {
                        _driver?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while disposing SeleniumManager: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            if (_service != null)
                            {
                                Process proc = Process.GetProcessById(_service.ProcessId);
                                if (proc != null && !proc.HasExited)
                                {
                                    proc.Kill();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al forzar el cierre del proceso Chrome: {ex.Message}");
                        }
                        finally
                        {
                            _driver = null;
                            _disposed = true;
                            _service?.Dispose();
                            _service = null;
                        }
                    }
                }
            }
            GC.SuppressFinalize(this);
        }

        ~SeleniumManager()
        {
            Dispose();
        }

    }

    /// <summary>
    /// Clase auxiliar para enumerar las ventanas abiertas y obtener el handle de la ventana principal
    /// de un proceso determinado.
    /// </summary>
    public static class WindowHelper
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        private const uint GW_OWNER = 4;

        public static IntPtr GetMainWindowHandle(int processId)
        {
            IntPtr mainWindowHandle = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out int windowProcessId);
                if (windowProcessId == processId && IsMainWindow(hWnd))
                {
                    mainWindowHandle = hWnd;
                    return false; // Detener la enumeración
                }
                return true; // Continuar con la enumeración
            }, IntPtr.Zero);
            return mainWindowHandle;
        }

        private static bool IsMainWindow(IntPtr hWnd)
        {
            // Una ventana principal es visible y no tiene ventana propietaria.
            return IsWindowVisible(hWnd) && GetWindow(hWnd, GW_OWNER) == IntPtr.Zero;
        }


    }


}