using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OverlayIconWatcher.Logging;

namespace OverlayIconWatcher
{
	internal class Program
	{
		static void Main(string[] args)
		{
			ILogger logger = Global.LoggerFactory.CreateLogger<Program>();

			Host.CreateDefaultBuilder(args)
				.UseWindowsService() // ← sorgt dafür, dass als Windows-Dienst gearbeitet wird
				.ConfigureServices((hostContext, services) =>
				{
					services.AddHostedService<Worker>();
					services.AddSingleton(logger);
				})
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();
				})
				.Build()
				.Run();
		}
	}
}
