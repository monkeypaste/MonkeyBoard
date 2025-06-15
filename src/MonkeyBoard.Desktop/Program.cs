using Avalonia;
using Avalonia.ReactiveUI;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard;
using System;
using System.Linq;
using System.Text;

namespace MonkeyBoard.Desktop {
    sealed class Program {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI()
            .With(new Win32PlatformOptions {
                RenderingMode = [Win32RenderingMode.AngleEgl]
            })
            .AfterSetup(_ => { 
                    MpPlatformKeyboardServices.KeyboardPermissionHelper = new DesktopKbPermissionHelper();
                    MainView.ForceInputConn(new PortableInputConnection());
            });
    }
}

