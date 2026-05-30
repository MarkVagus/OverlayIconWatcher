using Microsoft.Win32;
using OverlayIconWatcher.Interfaces;

namespace OverlayIconWatcher.Services;

public class RegistryRenamer : IRegistryRenamer
{
	/// <summary>
	/// Renames a subkey of the passed in registry key since 
	/// the Framework totally forgot to include such a handy feature.
	/// </summary>
	/// <param name="regKey">The RegistryKey that contains the subkey 
	/// you want to rename (must be writeable)</param>
	/// <param name="subKeyName">The name of the subkey that you want to rename
	/// </param>
	/// <param name="newSubKeyName">The new name of the RegistryKey</param>
	/// <returns>True if succeeds</returns>
	public bool RenameSubKey(RegistryKey parentKey, string subKeyName, string newSubKeyName)
	{
		CopyKey(parentKey, subKeyName, newSubKeyName);
		parentKey.DeleteSubKeyTree(subKeyName);
		return true;
	}

	/// <summary>
	/// Copy a registry key.  The parentKey must be writeable.
	/// </summary>
	/// <param name="parentKey"></param>
	/// <param name="keyNameToCopy"></param>
	/// <param name="newKeyName"></param>
	/// <returns></returns>
	static public bool CopyKey(RegistryKey parentKey, string keyNameToCopy, string newKeyName)
	{
		//Create new key
		RegistryKey destinationKey = parentKey.CreateSubKey(newKeyName);

		//Open the sourceKey we are copying from
		if (parentKey is not null)
		{
			RegistryKey? sourceKey = parentKey.OpenSubKey(keyNameToCopy);
			if (sourceKey is not null)
				RecurseCopyKey(sourceKey, destinationKey);
		}

		return true;
	}

	static void RecurseCopyKey(RegistryKey sourceKey, RegistryKey destinationKey)
	{
		//copy all the values
		foreach (string valueName in sourceKey.GetValueNames())
		{
			object? objValue = sourceKey.GetValue(valueName);
			if (objValue is null)
				continue;
			RegistryValueKind valKind = sourceKey.GetValueKind(valueName);
			destinationKey.SetValue(valueName, objValue, valKind);
		}

		//For Each subKey 
		//Create a new subKey in destinationKey 
		//Call myself 
		foreach (string sourceSubKeyName in sourceKey.GetSubKeyNames())
		{
			RegistryKey? sourceSubKey = sourceKey.OpenSubKey(sourceSubKeyName);
			if (sourceSubKey is null)
				continue;
			RegistryKey destSubKey = destinationKey.CreateSubKey(sourceSubKeyName);
			RecurseCopyKey(sourceSubKey, destSubKey);
		}
	}
}
