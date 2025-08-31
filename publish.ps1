Remove-Item -Recurse -Force publish/*

dotnet publish OverlayIconWatcher.csproj -c Release -r win-x64 --self-contained true -o ./publish