

using Foundation;
using MonkeyPaste.Keyboard.Common;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosThisAppVersionInfo : IThisAppVersionInfo {
        string IThisAppVersionInfo.Version {
            get {
                //NSObject ver = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"];
                //return ver.ToString();
                return string.Empty;
            }
        }
    }
}
