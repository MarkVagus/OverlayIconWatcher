using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OverlayIconWatcher;

internal class Worker(ILogger logger) : BackgroundService
{
	readonly string RegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ShellIconOverlayIdentifiers";

	ILogger Logger { get; } = logger;

	RegistryWatcher? Watcher { get; set; }

	string? SettingsFilePath { get; set; }

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Logger.LogInformation("Service starting...");

		Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("No entry assembly found");
		string entryAssemblyDirectoryPath = Path.GetDirectoryName(entryAssembly.Location) ?? throw new Exception("No directory pth for entry assembly found");
		SettingsFilePath = Path.Combine(entryAssemblyDirectoryPath, "settings.json");

		Logger.LogInformation($"{entryAssembly.GetName().Name} {entryAssembly.GetName().Version}");

		if (!File.Exists(SettingsFilePath))
			throw new Exception($"settings.json not found at: {SettingsFilePath}");

		Logger.LogInformation($"Settings file: {SettingsFilePath}");

		Logger.LogInformation($"Start watching registry key: {RegistryKey}");
		Watcher = new(RegistryKey);

		Watcher.RegistryChanged += OnRegistryChanged;

		Logger.LogInformation("Service started.");
		return Task.CompletedTask;
	}

	private void OnRegistryChanged()
	{
		Watcher.RegistryChanged -= OnRegistryChanged;

		Task.Delay(1000);

		OverlayIconManager m = new(Logger, SettingsFilePath, RegistryKey);
		m.Execute();

		Watcher.RegistryChanged += OnRegistryChanged;
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Stopping service...");
		Logger.LogInformation($"Stop watching registry key: {RegistryKey}");

		if (Watcher is not null)
			Watcher.RegistryChanged -= OnRegistryChanged;

		Logger.LogInformation("Service stopped.");

		return base.StopAsync(cancellationToken);
	}
}
