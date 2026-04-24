using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
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
				.UseSerilog((context, services, configuration) =>
				{
					configuration.ReadFrom.Configuration(context.Configuration);
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
