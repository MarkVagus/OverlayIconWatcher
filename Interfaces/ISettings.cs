namespace OverlayIconWatcher.Interfaces;

public interface ISettings
{
	public string RegistryPath { get; }
	public List<string> KeepTheseKeysInFront { get; }
	public string Path { get; }
}
