using System.Text.Json;

namespace OverlayIconWatcher;

public static class SettingsFactory
{
	public static Settings Load(string registryPath, string filePath)
	{
		string json = File.ReadAllText(filePath);

		List<string> keepTheseKeysInFront = JsonSerializer.Deserialize<List<string>>(json)
			?? throw new Exception($"No settings found in file: {filePath}");

		return new Settings(registryPath, keepTheseKeysInFront, filePath);
	}
}
