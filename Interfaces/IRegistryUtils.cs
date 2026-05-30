using Microsoft.Win32;

namespace OverlayIconWatcher.Interfaces;

public interface IRegistryUtils
{
	/// <summary>
	/// Umbenennung eines Registry-Schlüssels (rekursiv). Kopiert <paramref name="subKeyName"/> nach <paramref name="newSubKeyName"/>.
	/// Anschließend wird <paramref name="subKeyName"/> gelöscht.
	/// </summary>
	/// <param name="parentKey">Parent-Key</param>
	/// <param name="subKeyName">Umzubenennender Schlüssel in <paramref name="parentKey"/></param>
	/// <param name="newSubKeyName">Name des neuen Schlüssels</param>
	/// <returns>True if succeeds</returns>
	void RenameSubKey(RegistryKey parentKey, string subKeyName, string newSubKeyName);
}
