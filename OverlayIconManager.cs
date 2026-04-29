using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Reflection;
using System.Text.Json;

namespace OverlayIconWatcher;

public class OverlayIconManager
{
	public OverlayIconManager(ILogger<OverlayIconManager> logger, Settings settings)
	{
		Logger = logger;

		Settings = settings;
	}

	Settings Settings { get; }

	//public OverlayIconManager(ILogger logger, string settingsFilepath, string registryKey)
	//{
	//	Logger = logger;

	//	try
	//	{
	//		logger.LogInformation("Opening registry key: {key}", registryKey);
			  
	//		RegistryKey shellIconOverlayIdentifiersKey = Registry.LocalMachine.OpenSubKey(registryKey, true) ??
	//			throw new Exception($"Could not open registry key: {registryKey}");

	//		ShellIconOverlayIdentifiersKey = shellIconOverlayIdentifiersKey;

	//		Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("No entry assembly found");

	//		logger.LogInformation("Reading Settings.json: {s}", settingsFilepath);

	//		string json = File.ReadAllText(settingsFilepath);
	//		List<string> keepTheseKeysInFront = JsonSerializer.Deserialize<List<string>>(json) ??
	//			throw new Exception("Settings.json is empty!");

	//		KeepTheseKeysInFront = keepTheseKeysInFront;

	//		logger.LogInformation("{count} entries will be sorted at beginning:", keepTheseKeysInFront.Count);

	//		int i = 0;
	//		foreach (string k in KeepTheseKeysInFront)
	//		{
	//			i++;
	//			logger.LogInformation("{i}:{k}", i, k);
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		Logger.LogError(ex, ex.Message);
	//		throw;
	//	}
	//}

	//public async Task InitializeAsync(CancellationToken stoppingToken)
	//{
	//	try
	//	{
	//		Logger.LogInformation("Opening registry key: {key}", Settings.RegistryPath);

	//		RegistryKey shellIconOverlayIdentifiersKey = Registry.LocalMachine.OpenSubKey(Settings.RegistryPath, true) ??
	//			throw new Exception($"Could not open registry key: {Settings.RegistryPath}");

	//		ShellIconOverlayIdentifiersKey = shellIconOverlayIdentifiersKey;

	//		Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("No entry assembly found");

	//		string settingsFilePath = entryAssembly.Location

	//		logger.LogInformation("Reading Settings.json: {s}", settingsFilepath);

	//		string json = File.ReadAllText(settingsFilepath);
	//		List<string> keepTheseKeysInFront = JsonSerializer.Deserialize<List<string>>(json) ??
	//			throw new Exception("Settings.json is empty!");

	//		KeepTheseKeysInFront = keepTheseKeysInFront;

	//		logger.LogInformation("{count} entries will be sorted at beginning:", keepTheseKeysInFront.Count);

	//		int i = 0;
	//		foreach (string k in KeepTheseKeysInFront)
	//		{
	//			i++;
	//			logger.LogInformation("{i}:{k}", i, k);
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		Logger.LogError(ex, ex.Message);
	//		throw;
	//	}
	//}

	ILogger Logger { get; }

	//List<string> KeepTheseKeysInFront { get; }

	//RegistryKey ShellIconOverlayIdentifiersKey { get; set; }

	Task<Dictionary<string, string>> ReadEntriesAsync(RegistryKey regKey, CancellationToken cancellationToken = default)
	{
		Dictionary<string, string> entries = [];


		foreach (var skn in regKey.GetSubKeyNames())
		{
			cancellationToken.ThrowIfCancellationRequested();

			using var subkey = regKey.OpenSubKey(skn);
			string? value = subkey?.GetValue("") as string;
			if (!string.IsNullOrEmpty(value))
				entries.Add(skn, value);
		}

		return Task.FromResult(entries);
	}

	public async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		using RegistryKey shellIconOverlayIdentifiersKey = Registry.LocalMachine.OpenSubKey(Settings.RegistryPath, true) ??
				throw new Exception($"Could not open registry key: {Settings.RegistryPath}");

		Dictionary<string, string> entries = await ReadEntriesAsync(shellIconOverlayIdentifiersKey, cancellationToken);

		int keysRemoved = 0, keysRenamed = 0;

		if (entries is not null && entries.Count > 0)
		{
			keysRemoved = await RemoveDuplicateKeysAsync(shellIconOverlayIdentifiersKey, entries, cancellationToken);
			keysRenamed = await RenameKeysAsync(shellIconOverlayIdentifiersKey, entries, cancellationToken);
		}

		if (keysRenamed + keysRenamed == 0)
			Logger.LogInformation("Reordering finished. No changes.");
		else
			Logger.LogInformation("Reordering finished. Deleted / Renamed = {removed} / {renamed}", keysRemoved, keysRenamed);
	}

	/// <summary>
	/// Entfernt doppelte Einträge (bezogen auf den Standardwert, der in der Form {9AAFF...} vorliegt)
	/// </summary>
	/// <param name="entries"></param>
	async Task<int> RemoveDuplicateKeysAsync(RegistryKey regKey, Dictionary<string, string> entries, CancellationToken cancellationToken = default)
	{
		int keysDeleted = 0;

		HashSet<string> removedKeys = [];

		foreach (var e in entries.ToArray())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (removedKeys.Contains(e.Key))
				continue;

			foreach (var e2 in entries.ToArray().Where(x => x.Value == e.Value))
			{
				if (e2.Key == e.Key)
					continue;

				Logger.LogInformation($"Removing duplicate entry: {e2.Value} ({e2.Key})");
				RemoveKey(regKey, e2.Key);
				entries.Remove(e2.Key);
				removedKeys.Add(e2.Key);

				keysDeleted++;
			}
		}

		return keysDeleted;
	}

	/// <summary>
	/// Entfernt aus allen Einträgen führende Leerzeichen und fügt bei den priorisierten
	/// Einträgen genau ein Leerzeichen am Anfang hinzu (damit diese prioritär behandelt werden)
	/// </summary>
	/// <param name="entries"></param>
	async Task<int> RenameKeysAsync(RegistryKey regKey, Dictionary<string, string> entries, CancellationToken cancellationToken = default)
	{
		int keysRenamed = 0;

		foreach (var e in entries)
		{
			cancellationToken.ThrowIfCancellationRequested();

			string normalizedKey = e.Key.TrimStart(' ');
			string newKeyName = normalizedKey;
			if (Settings.KeepTheseKeysInFront.Contains(normalizedKey))
				newKeyName = $" {normalizedKey}";

			if (newKeyName != e.Key)
			{
				RenameKey(regKey, e.Key, newKeyName);
				keysRenamed++;
			}
		}

		return keysRenamed;
	}

	void RemoveKey(RegistryKey regKey, string keyName)
	{
		Logger.LogInformation("Removing duplicate Key: {keyName}", keyName);
		regKey.DeleteSubKeyTree(keyName);
	}

	void RenameKey(RegistryKey regKey, string oldKeyName, string newKeyName)
	{
		Logger.LogInformation("Renaming key (old => new): '{oldKeyName}' => '{newKeyName}'", oldKeyName, newKeyName);
		RegistryUtils.RenameSubKey(regKey, oldKeyName, newKeyName);
	}

}
