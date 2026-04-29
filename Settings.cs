namespace OverlayIconWatcher;

public class Settings(string registryPath, List<string> keepTheseKeysInFront, string path)
{
	public string RegistryPath { get; } = registryPath;

	public List<string> KeepTheseKeysInFront { get; } = keepTheseKeysInFront;

	public string Path { get; } = path;
}
