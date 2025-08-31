using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace OverlayIconWatcher;

internal class Program
{
    internal static string ProgramInfo => $"{Assembly.GetEntryAssembly()?.GetName().Name} {Assembly.GetEntryAssembly()?.GetName().Version}";

    static void Main(string[] args)
    {
        try
        {
            Host.CreateDefaultBuilder(args)
                .UseWindowsService() // ← sorgt dafür, dass als Windows-Dienst gearbeitet wird
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddLog4Net("log4net.config");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                })
                .Build()
                .Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}
