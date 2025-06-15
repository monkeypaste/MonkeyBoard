rem  -p:RemoveSections=true (bad for windows)
rem  -p: AssemblyName=MonkeyPaste.Keyboard.Android
dotnet publish -r linux-bionic-arm64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true