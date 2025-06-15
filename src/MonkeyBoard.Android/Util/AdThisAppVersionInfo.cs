using Android.Content;

using MonkeyBoard.Common;

namespace MonkeyBoard.Android {
    public class AdThisAppVersionInfo : IThisAppVersionInfo {
        Context Context { get; set; }
        string IThisAppVersionInfo.Version {
            get {
                return Context.PackageManager.GetPackageInfo(Context.PackageName, 0).VersionName;
            }
        }
        public AdThisAppVersionInfo(Context context) {
            Context = context;
        }
    }
}
