using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Reflection;
using System.Text.Json;

namespace OverlayIconWatcher;

public class OverlayIconManager
{
    public OverlayIconManager(ILogger logger, string settingsFilepath, string registryKey)
    {
        Logger = logger;

        try
        {
            logger.LogInformation("Opening registry key: {key}", registryKey);

            RegistryKey shellIconOverlayIdentifiersKey = Registry.LocalMachine.OpenSubKey(registryKey, true) ??
                throw new Exception($"Could not open registry key: {registryKey}");

            ShellIconOverlayIdentifiersKey = shellIconOverlayIdentifiersKey;

            Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("No entry assembly found");

            logger.LogInformation("Reading Settings.json: {s}", settingsFilepath);

            string json = File.ReadAllText(settingsFilepath);
            List<string> keepTheseKeysInFront = JsonSerializer.Deserialize<List<string>>(json) ??
                throw new Exception("Settings.json is empty!");

            KeepTheseKeysInFront = keepTheseKeysInFront;

            logger.LogInformation("{count} entries will be sorted at beginning:", keepTheseKeysInFront.Count);

            int i = 0;
            foreach (string k in KeepTheseKeysInFront)
            {
                i++;
                logger.LogInformation("{i}:{k}", i, k);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ex.Message);
            throw;
        }
    }

    ILogger Logger { get; }

    List<string> KeepTheseKeysInFront { get; }

    RegistryKey ShellIconOverlayIdentifiersKey { get; }

    private Dictionary<string, string> ReadEntries()
    {
        Dictionary<string, string> entries = [];

        foreach (var skn in ShellIconOverlayIdentifiersKey.GetSubKeyNames())
        {
            using var subkey = ShellIconOverlayIdentifiersKey.OpenSubKey(skn);
            string? value = subkey?.GetValue("") as string;
            if (!string.IsNullOrEmpty(value))
                entries.Add(skn, value);
        }

        return entries;
    }

    public void Execute()
    {
        Dictionary<string, string> entries = ReadEntries();

        int keysRemoved = 0, keysRenamed = 0;

        if (entries is not null && entries.Count > 0)
        {
            keysRemoved = RemoveDuplicateKeys(entries);
            keysRenamed = RenameKeys(entries);
        }

        if (keysRenamed + keysRenamed == 0)
            Logger.LogInformation("Reordering finished. No changes.");
        else
            Logger.LogInformation($"Reordering finished. Deleted / Renamed = {keysRemoved} / {keysRenamed}");
    }

    /// <summary>
    /// Entfernt doppelte Einträge (bezogen auf den Standardwert, der in der Form {9AAFF...} vorliegt)
    /// </summary>
    /// <param name="entries"></param>
    int RemoveDuplicateKeys(Dictionary<string, string> entries)
    {
        int keysDeleted = 0;

        HashSet<string> removedKeys = [];

        foreach (var e in entries.ToArray())
        {
            if (removedKeys.Contains(e.Key))
                continue;

            foreach (var e2 in entries.ToArray().Where(x => x.Value == e.Value))
            {
                if (e2.Key == e.Key)
                    continue;

                Logger.LogInformation($"Removing duplcate entry: {e2.Value} ({e2.Key})");
                RemoveKey(e2.Key);
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
    int RenameKeys(Dictionary<string, string> entries)
    {
        int keysRenamed = 0;

        foreach (var e in entries)
        {
            string normalizedKey = e.Key.TrimStart(' ');
            string newKeyName = normalizedKey;
            if (KeepTheseKeysInFront.Contains(normalizedKey))
                newKeyName = $" {normalizedKey}";

            if (newKeyName != e.Key)
            {
                RenameKey(e.Key, newKeyName);
                keysRenamed++;
            }
        }

        return keysRenamed;
    }

    void RemoveKey(string keyName)
    {
        Logger.LogInformation("Removing duplicate Key: {keyName}", keyName);
        ShellIconOverlayIdentifiersKey.DeleteSubKeyTree(keyName);
    }

    void RenameKey(string oldKeyName, string newKeyName)
    {
        Logger.LogInformation("Renaming key (old => new): '{oldKeyName}' => '{newKeyName}'", oldKeyName, newKeyName);
        RegistryUtils.RenameSubKey(ShellIconOverlayIdentifiersKey, oldKeyName, newKeyName);
    }

}
