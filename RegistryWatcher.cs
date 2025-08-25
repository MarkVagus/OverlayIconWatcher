using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace OverlayIconWatcher;

public class RegistryWatcher : IDisposable
{
	const int KEY_NOTIFY = 0x10;
	const int REG_NOTIFY_CHANGE_NAME = 0x1;
	const int REG_NOTIFY_CHANGE_ATTRIBUTES = 0x2;
	const int REG_NOTIFY_CHANGE_LAST_SET = 0x4;
	const int REG_NOTIFY_CHANGE_SECURITY = 0x8; 
	const int ERROR_SUCCESS = 0;

	// P/Invoke-Deklarationen
	[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern IntPtr RegOpenKeyEx(IntPtr hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, uint dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern int RegCloseKey(IntPtr hKey);

	readonly IntPtr hKey;
	IntPtr HEvent { get; }
	Thread WatcherThread { get; }
	bool Running { get; set; }

	ILogger Logger { get; }

	public event Action? RegistryChanged;

	public RegistryWatcher(ILogger logger, string registryPath)
	{
		Logger = logger;

		IntPtr HKEY_LOCAL_MACHINE = new(unchecked((int)0x80000002));

		// Öffnen des Registry-Schlüssels
		int result = (int)RegOpenKeyEx(HKEY_LOCAL_MACHINE, registryPath, 0, KEY_NOTIFY, out hKey);
		if (result != ERROR_SUCCESS)
			throw new InvalidOperationException("Fehler beim Öffnen des Registry-Schlüssels.");

		// Event erstellen, das bei Änderungen ausgelöst wird
		HEvent = CreateEvent(IntPtr.Zero, true, false, null);
		if (HEvent == IntPtr.Zero)
			throw new InvalidOperationException("Fehler beim Erstellen des Ereignisses.");

		// Überwachungs-Thread starten
		WatcherThread = new Thread(WatcherLoop);
		Running = true;
		WatcherThread.IsBackground = true;
		WatcherThread.Start();
	}

	private void WatcherLoop()
	{
		try
		{
			while (Running)
			{
				// Warten auf eine Änderung
				uint notifyFilter = REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_CHANGE_LAST_SET;
				int result = RegNotifyChangeKeyValue(hKey, true, notifyFilter, HEvent, true);
				if (result != ERROR_SUCCESS)
					throw new InvalidOperationException("Fehler bei der Registrierung der Benachrichtigung.");

				// Auf Ereignis warten
				uint waitResult = WaitForSingleObject(HEvent, 1000);
				if (waitResult == 0) // Warten auf die Benachrichtigung
					OnRegistryChanged();
			}
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, $"Error in {nameof(RegistryWatcher)}: {ex.Message}");
		}
	}

	protected virtual void OnRegistryChanged()
	{
		RegistryChanged?.Invoke();
	}

	public void Stop()
	{
		Running = false;
		WatcherThread.Join();
		int result = RegCloseKey(hKey);
		if (result != 0)
			Logger.LogError($"RegCloseKey returned: {result}");
	}

	public void Dispose()
	{
		Stop();
		GC.SuppressFinalize(this);
	}
}
