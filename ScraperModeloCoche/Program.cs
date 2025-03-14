
namespace ScraperModeloCoche
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Entorno.CadenaConexion = Helpers.Utils.ObtenerCadenaConexion(args[0], args[1]);
        }
    }
}
