using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OverlayIconWatcher.Interfaces;

namespace OverlayIconWatcher;

internal class Worker(ILogger<Worker> logger, IOverlayIconManager manager, ISettings settings, IConfiguration configuration) : BackgroundService
{
	readonly ILogger _logger = logger;
	readonly IOverlayIconManager _manager = manager;
	readonly string _registryKey = settings.RegistryPath;
	readonly int _intervalMs = Math.Max(1000, configuration.GetValue("Interval", 1000));

	HashSet<string> _lastSnapshot = [];

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Service starting...");

		await _manager.ReorderKeysAsync(stoppingToken);

		_lastSnapshot = LoadKeys();

		_logger.LogInformation("Start watching registry key: {key}", _registryKey);

		_logger.LogInformation("Interval (ms): {i}", _intervalMs);

		_logger.LogInformation("Service started.");

		try
		{

			while (!stoppingToken.IsCancellationRequested)
			{
				await CheckAsync(stoppingToken);

				await Task.Delay(_intervalMs, stoppingToken);
			}
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { } // No action required
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled exception");
			throw;
		}
	}

	HashSet<string> LoadKeys()
	{
		using var key = Registry.LocalMachine.OpenSubKey(_registryKey);
		if (key is null)
			return [];

		HashSet<string> current = [.. key.GetSubKeyNames()];

		return current;
	}

	async Task CheckAsync(CancellationToken cancellationToken)
	{
		HashSet<string> current = LoadKeys();

		if (!current.SetEquals(_lastSnapshot))
		{
			_lastSnapshot = current;

			_logger.LogInformation("Registry change detected.");

			await _manager.ReorderKeysAsync(cancellationToken);
		}
	}
}
