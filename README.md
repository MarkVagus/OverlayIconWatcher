OverlayIconWatcher 2.0.0.0

This program detects registry changes concerning OverlayIcons. Problem: Programs as OneDrive steal Overlay Icons by
changing the order in RegistryKey = HKLM\Software\Microsoft\Windows\CurrentVersion\Explorer\ShellOverlayIdentifiers

Whenever a change in this registry key is detected, this program will rechange order and set the OverlayIcons in
settings.json to front!

In settings.json, place your favorite Overlay Icons; example:

[
  "Tortoise1Normal",
  "Tortoise2Modified",
  "Tortoise3Conflict",
  "Tortoise4Locked",
  "Tortoise5ReadOnly",
  "Tortoise6Deleted",
  "Tortoise7Added",
  "Tortoise8Ignored",
  "Tortoise9Unversioned",
  "NextcloudError",
  "NextcloudOK",
  "NextcloudOKShared",
  "NextcloudSync",
  "NextcloudWarning"
]

Please change the settings.json to your concerns!

Installation as a windows service:

sc create OverlayIconWatcher binPath= "path-to-OverlayIconWatcher.exe" start= auto

Logs are written to directory: %programdata%\OverlayIconWatcher
