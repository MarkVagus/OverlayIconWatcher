using Microsoft.Win32;

namespace OverlayIconWatcher.Interfaces;

public interface IRegistryRenamer
{
	bool RenameSubKey(RegistryKey parentKey, string subKeyName, string newSubKeyName);
}
