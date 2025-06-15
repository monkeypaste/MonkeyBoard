
using MonkeyPaste.Common;
using System;
using System.IO;

namespace MonkeyBoard.Common {
    public static class KbStorageHelpers {
        public const string IOS_SHARED_GROUP_ID = "group.com.monkey.monkeypaste";
        static IStoragePathHelper StoragePathHelper { get; set; }
        public static void Init(IStoragePathHelper storagePathHelper) {
            StoragePathHelper = storagePathHelper;
            if(OperatingSystem.IsIOS()) {
                //StoragePathHelper = null;
            }
        }
        static string _localStorageDir = default;
        public static string LocalStorageDir {
            get {
                if(_localStorageDir == default) {
                    string root_dir = default;
                    if(StoragePathHelper is { } sph) {
                        // IOS notes
                        // main dir:
                        // /Users/tkefauver/Library/Developer/CoreSimulator/Devices/{DEVICE_GUID}/data/Containers/Data/Application/{APP_GUID}/Documents
                        // plugin dir:
                        // /Users/tkefauver/Library/Developer/CoreSimulator/Devices/{DEVICE_GUID}/data/Containers/Data/PluginKitPlugin/{PLUGIN_GUID}/Documents
                        // shared dir:
                        // ???
                        root_dir = StoragePathHelper.GetLocalStorageBaseDir();
                    } else {
                        root_dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    }
                    root_dir = MpPlatformHelpers.GetStorageDir(forcedRootDir: root_dir);

                    _localStorageDir = Path.Combine(root_dir, "kb");
                    if(!_localStorageDir.IsDirectory()) {
                        MpFileIo.CreateDirectory(_localStorageDir);
                    }
                }
                return _localStorageDir;

            }
        }
        public static string AvAssetsBaseUri => $"avares://MonkeyBoard.Common/Assets";
        public static string ImgRootDir => Path.Combine(LocalStorageDir, "img");
    }
}
