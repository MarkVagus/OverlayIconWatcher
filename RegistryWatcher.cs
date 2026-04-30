using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace OverlayIconWatcher;

/// <summary>
/// Timerbasierter Registry-Watcher
/// </summary>
public class RegistryWatcher : IDisposable
{
	readonly Timer _timer;

	internal string RegistryKeyPath { get; }

	HashSet<string> _lastSnapshot = [];

	readonly ILogger _logger;

	public event Func<Task>? Changed;

	public RegistryWatcher(ILogger logger, string registryKeyPath, int intervalMs = 2000)
	{
		RegistryKeyPath = registryKeyPath;

		_timer = new Timer(_ => Check(), null, 0, intervalMs);

		_logger = logger;
	}

	int _isRunning = 0;

	void Check()
	{
		if (Interlocked.Exchange(ref _isRunning, 1) == 1)
			return;

		try
		{
			using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath);
			if (key is null) 
				return;

			HashSet<string> current = [.. key.GetSubKeyNames()];

			if (!current.SetEquals(_lastSnapshot))
			{
				_lastSnapshot = current;

				var handler = Changed;

				if (handler is not null)
				{
					_ = Task.Run(async () =>
					{
						try
						{
							await handler();
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error in Changed handler");
						}
					});
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"Error in {nameof(RegistryWatcher)}: {ex.Message}");
		}
		finally
		{
			Interlocked.Exchange(ref _isRunning, 0);
		}
	}

	public void Dispose()
	{
		_timer.Dispose();
	}
}