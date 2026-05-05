using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OverlayIconWatcher;

internal class Worker(ILogger<Worker> logger, OverlayIconManager manager, RegistryWatcher watcher) : BackgroundService
{
	readonly ILogger _logger = logger;

	readonly OverlayIconManager _manager = manager;

	readonly RegistryWatcher _watcher = watcher;

	CancellationToken _stoppingToken;

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_stoppingToken = cancellationToken;

		await base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Service starting...");

		await _manager.ExecuteAsync(stoppingToken);

		_logger.LogInformation("Start watching registry key: {key}", _watcher.RegistryKeyPath);
		_watcher.Changed += OnRegistryChangedAsync;

		_logger.LogInformation("Service started.");
	}

	async Task OnRegistryChangedAsync()
	{
		_watcher.Changed -= OnRegistryChangedAsync;

		_logger.LogInformation("Registry change detected.");

		await Task.Delay(1000);

		await _manager.ExecuteAsync(_stoppingToken);

		_watcher.Changed += OnRegistryChangedAsync;
	}


	public override Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Stopping service...");
		_logger.LogInformation("Stop watching registry key: {reg}", _watcher.RegistryKeyPath);

		_watcher.Changed -= OnRegistryChangedAsync;

		_logger.LogInformation("Service stopped.");

		return base.StopAsync(cancellationToken);
	}
}
