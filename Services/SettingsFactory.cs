using OverlayIconWatcher.Interfaces;
using System.Text.Json;

namespace OverlayIconWatcher.Services;

public static class SettingsFactory
{
	public static ISettings Load(string registryPath, string filePath)
	{
		string json = File.ReadAllText(filePath);

		List<string> keepTheseKeysInFront = JsonSerializer.Deserialize<List<string>>(json)
			?? throw new Exception($"No settings found in file: {filePath}");

		return new Settings(registryPath, keepTheseKeysInFront, filePath);
	}
}
