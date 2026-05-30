using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OverlayIconWatcher.Interfaces;

namespace OverlayIconWatcher.Services;

public class OverlayIconManager(ILogger<OverlayIconManager> logger, ISettings settings, IRegistryRenamer mover) : IOverlayIconManager
{
	readonly ILogger _logger = logger;
	readonly ISettings _settings = settings;
	readonly IRegistryRenamer _mover = mover;

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

	public async Task ReorderKeysAsync(CancellationToken cancellationToken = default)
	{
		using RegistryKey shellIconOverlayIdentifiersKey = Registry.LocalMachine.OpenSubKey(_settings.RegistryPath, true) ??
				throw new Exception($"Could not open registry key: {_settings.RegistryPath}");

		Dictionary<string, string> entries = await ReadEntriesAsync(shellIconOverlayIdentifiersKey, cancellationToken);

		int keysRemoved = 0, keysRenamed = 0;

		if (entries is not null && entries.Count > 0)
		{
			keysRemoved = await RemoveDuplicateKeysAsync(shellIconOverlayIdentifiersKey, entries, cancellationToken);
			keysRenamed = await RenameKeysAsync(shellIconOverlayIdentifiersKey, entries, cancellationToken);
		}

		if (keysRemoved + keysRenamed == 0)
			_logger.LogInformation("Reordering finished. No changes.");
		else
			_logger.LogInformation("Reordering finished. Deleted / Renamed = {removed} / {renamed}", keysRemoved, keysRenamed);
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

				_logger.LogInformation($"Removing duplicate entry: {e2.Value} ({e2.Key})");
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
			if (_settings.KeepTheseKeysInFront.Contains(normalizedKey))
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
		_logger.LogInformation("Removing duplicate Key: {keyName}", keyName);
		regKey.DeleteSubKeyTree(keyName);
	}

	void RenameKey(RegistryKey regKey, string oldKeyName, string newKeyName)
	{
		_logger.LogInformation("Renaming key (old => new): '{oldKeyName}' => '{newKeyName}'", oldKeyName, newKeyName);
		_mover.RenameSubKey(regKey, oldKeyName, newKeyName);
	}

}
