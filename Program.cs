using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OverlayIconWatcher.Interfaces;
using OverlayIconWatcher.Services;
using Serilog;
using System.Reflection;

namespace OverlayIconWatcher;

internal class Program
{
	internal static string ProgramInfo => $"{Assembly.GetEntryAssembly()?.GetName().Name} {Assembly.GetEntryAssembly()?.GetName().Version}";

	static readonly string RegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ShellIconOverlayIdentifiers";

	static void Main(string[] args)
	{
		try
		{
			var host = Host.CreateDefaultBuilder(args)
				.UseWindowsService() // ← sorgt dafür, dass als Windows-Dienst gearbeitet wird
				.UseSerilog((context, services, configuration) =>
				{
					configuration.ReadFrom.Configuration(context.Configuration);
				})
				.ConfigureServices((hostContext, services) =>
				{
					// Settings
					services.AddSingleton(sp =>
					{
						Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("No entry assembly found");
						string entryAssemblyDirectoryPath = Path.GetDirectoryName(entryAssembly.Location) ?? throw new Exception("No directory pth for entry assembly found");
						string settingsFilePath = Path.Combine(entryAssemblyDirectoryPath, "settings.json");

						return SettingsFactory.Load(RegistryKey, settingsFilePath);
					});

					services.AddSingleton<IRegistryRenamer, RegistryRenamer>();
					services.AddSingleton<IOverlayIconManager, OverlayIconManager>();
					services.AddHostedService<Worker>();
				})
				.Build();

			ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
			logger.LogInformation("=== APPLICATION START ===");
			logger.LogInformation("{p}", ProgramInfo);

			ISettings settings = host.Services.GetRequiredService<ISettings>();
			logger.LogInformation("Settings loaded from: {s}", settings.Path);

			logger.LogInformation("{n} keys sorted at beginning:", settings.KeepTheseKeysInFront.Count);
			int i = 0;
			foreach (string key in settings.KeepTheseKeysInFront)
			{
				logger.LogInformation("{i} {key}", $"#{++i}", key);
			}

			host.Run();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			throw;
		}
	}
}
