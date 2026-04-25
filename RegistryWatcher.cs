using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace OverlayIconWatcher;

public class RegistryWatcher : IDisposable
{
	readonly Timer _timer;
	readonly string _path;
	HashSet<string> _lastSnapshot = [];

	readonly ILogger _logger;

	public event Action? Changed;

	public RegistryWatcher(ILogger logger, string path, int intervalMs = 2000)
	{
		_path = path;

		_timer = new Timer(_ => Check(), null, 0, intervalMs);

		_logger = logger;
	}

	void Check()
	{
		try
		{
			using var key = Registry.LocalMachine.OpenSubKey(_path);
			if (key is null)
				return;

			HashSet<string> current = [.. key.GetSubKeyNames()];

			if (!current.SetEquals(_lastSnapshot))
			{
				_lastSnapshot = current;
				Changed?.Invoke();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"Error in {nameof(RegistryWatcher)}: {ex.Message}");
		}
	}

	public void Dispose()
	{
		_timer.Dispose();
	}
}