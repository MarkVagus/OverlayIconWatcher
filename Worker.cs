using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OverlayIconWatcher;

internal class Worker : BackgroundService
{
	public Worker(ILogger<Worker> logger, OverlayIconManager manager, RegistryWatcher watcher)
	{
		Logger = logger;

		Manager = manager;

		Watcher = watcher;

		Logger.LogInformation(Program.ProgramInfo);

		//Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("No entry assembly found");
		//string entryAssemblyDirectoryPath = Path.GetDirectoryName(entryAssembly.Location) ?? throw new Exception("No directory pth for entry assembly found");
		//SettingsFilePath = Path.Combine(entryAssemblyDirectoryPath, "settings.json");

		//if (!File.Exists(SettingsFilePath))
		//	throw new Exception($"settings.json not found at: {SettingsFilePath}");

		//Logger.LogInformation("Settings file: {p}", SettingsFilePath);
	}


	ILogger Logger { get; }

	OverlayIconManager Manager { get; }

	RegistryWatcher Watcher { get; }


	CancellationToken _stoppingToken;

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_stoppingToken = cancellationToken;

		await base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Logger.LogInformation("Service starting...");

		await Manager.ExecuteAsync(stoppingToken);

		Logger.LogInformation("Start watching registry key: {key}", Watcher.RegistryKeyPath);
		Watcher.Changed += OnRegistryChangedAsync;

		Logger.LogInformation("Service started.");
	}

	async Task OnRegistryChangedAsync()
	{
		Watcher.Changed -= OnRegistryChangedAsync;

		Logger.LogInformation("Registry change detected.");

		await Task.Delay(1000);

		await Manager.ExecuteAsync(_stoppingToken);

		Watcher.Changed += OnRegistryChangedAsync;
	}


	public override Task StopAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Stopping service...");
		Logger.LogInformation("Stop watching registry key: {reg}", Watcher.RegistryKeyPath);

		Watcher.Changed -= OnRegistryChangedAsync;

		Logger.LogInformation("Service stopped.");

		return base.StopAsync(cancellationToken);
	}
}
