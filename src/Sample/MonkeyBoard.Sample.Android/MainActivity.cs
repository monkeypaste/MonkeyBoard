using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using MonkeyBoard.Android;
using MonkeyBoard.Bridge;

namespace MonkeyBoard.Sample.Android;

[Activity(
    Label = "MonkeyBoard.Sample.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App> {
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .AfterSetup(_ => {
                MpPlatformKeyboardServices.KeyboardPermissionHelper = new AdPermissionHelper(this);
            });
    }
}
