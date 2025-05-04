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
	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern IntPtr RegOpenKeyEx(IntPtr hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, uint dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

	[DllImport("kernel32.dll")]
	private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern int RegCloseKey(IntPtr hKey);

	private readonly IntPtr hKey;
	private readonly IntPtr hEvent;
	private readonly string registryPath;
	private readonly Thread watcherThread;
	private bool running;

	public event Action RegistryChanged;

	public RegistryWatcher(string registryPath)
	{
		this.registryPath = registryPath;
		const uint HKEY_LOCAL_MACHINE = 0x80000002;

		// Öffnen des Registry-Schlüssels
		int result = (int)RegOpenKeyEx((IntPtr)HKEY_LOCAL_MACHINE, registryPath, 0, KEY_NOTIFY, out hKey);
		if (result != ERROR_SUCCESS)
		{
			throw new InvalidOperationException("Fehler beim Öffnen des Registry-Schlüssels.");
		}

		// Event erstellen, das bei Änderungen ausgelöst wird
		hEvent = CreateEvent(IntPtr.Zero, true, false, null);
		if (hEvent == IntPtr.Zero)
		{
			throw new InvalidOperationException("Fehler beim Erstellen des Ereignisses.");
		}

		// Überwachungs-Thread starten
		watcherThread = new Thread(WatcherLoop);
		running = true;
		watcherThread.IsBackground = true;
		watcherThread.Start();
	}

	private void WatcherLoop()
	{
		try
		{
			while (running)
			{
				// Warten auf eine Änderung
				uint notifyFilter = REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_CHANGE_LAST_SET;
				int result = RegNotifyChangeKeyValue(hKey, true, notifyFilter, hEvent, true);
				if (result != ERROR_SUCCESS)
				{
					throw new InvalidOperationException("Fehler bei der Registrierung der Benachrichtigung.");
				}

				// Auf Ereignis warten
				uint waitResult = WaitForSingleObject(hEvent, 1000);
				if (waitResult == 0) // Warten auf die Benachrichtigung
				{
					OnRegistryChanged();
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in {nameof(RegistryWatcher)}: {ex.Message}");
		}
	}

	protected virtual void OnRegistryChanged()
	{
		RegistryChanged?.Invoke();
	}

	public void Stop()
	{
		running = false;
		watcherThread.Join();
		RegCloseKey(hKey);
	}

	public void Dispose()
	{
		Stop();
	}
}
