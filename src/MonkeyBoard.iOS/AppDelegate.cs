using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.iOS;
using Avalonia.Platform;
using Avalonia.Threading;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Storage;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard;
using MonkeyPaste.Keyboard.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace MonkeyBoard.iOS {
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public partial class AppDelegate : AvaloniaAppDelegate<App>, IStoragePathHelper
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                //.UseReactiveUI()
                //.With(new iOSPlatformOptions { RenderingMode = [iOSRenderingMode.Metal] })
                .AfterSetup(_ => {
                    Init2();
                    //FinishedLaunching(UIApplication.SharedApplication, null);
                    //var ud = new NSUserDefaults(KbStorageHelpers.IOS_SHARED_GROUP_ID, NSUserDefaultsType.SuiteName);
                    //ud.Synchronize();
                    //ud.SetBool(true, "BUTT_TEST");
                    //var test = ud.BoolForKey("BUTT_TEST");
                    //MpConsole.WriteLine($"BUTT TEST result: {test}");
                });
            ;
        }
        void Init2() {
            //if (this is IAvaloniaAppDelegate aad) {
            //    aad.Activated += IAvaloniaAppDelegate_Activated;
            //}
            KbStorageHelpers.Init(this);

            MpConsoleFlags log_flags = MpConsoleFlags.Stampless;
            string log_path = null;
#if DEBUG
            log_path = Path.Combine(KbStorageHelpers.LocalStorageDir, $"{DateTime.Now.Ticks.ToString()}.log");
            log_flags |= MpConsoleFlags.File | MpConsoleFlags.Console;
#endif
            MpConsole.Init(log_path, log_flags);

            MpPlatformKeyboardServices.KeyboardPermissionHelper = new iosPermissionHelper_old();

            KbAssetMover.MoveAssets(true);

            var sb = new StringBuilder();
            sb.AppendLine();

            sb.AppendLine($"CURRENT LOCALE:");
            sb.AppendLine(CultureInfo.InstalledUICulture.ToString());
            sb.AppendLine($"SHARED STORAGE:");
            sb.AppendLine((this as IStoragePathHelper).GetLocalStorageBaseDir());

#if DEBUG
            string plugin_cache_dir = Path.Combine(
                (this as IStoragePathHelper).GetLocalStorageBaseDir(),
                "..",
                "..",
                "..",
                "Data",
                "PluginKitPlugin");
            string cur_plugin_dir = Directory.GetDirectories(plugin_cache_dir).Select(x => new DirectoryInfo(x)).OrderByDescending(x => x.LastWriteTime).FirstOrDefault().FullName;
            sb.AppendLine("PLUGIN STORAGE:");
            sb.AppendLine(cur_plugin_dir);
#endif
            MpConsole.WriteLine(sb.ToString());


            Dispatcher.UIThread.Post(async () => {
                await Task.Delay(5_000);
                ShowFilePickerAsync().FireAndForgetSafeAsync();
            });
        }

        async Task ShowFilePickerAsync() {
            await Task.Delay(5_000);
            //async Task<FileResult> PickAndShow(PickOptions options) {
            //    try {
            //        var result = await FilePicker.Default.PickAsync(options);
            //        return result;
            //    }
            //    catch (Exception ex) {
            //        // The user canceled or something went wrong
            //        ex.Dump();
            //    }
            //    return null;
            //}

            //var result = await PickAndShow(new PickOptions() { FileTypes = FilePickerFileType.Images });
            //if (result is not { } fr) {
            //    return;
            //}
            //FilePickerFileType filePickerFileType = new FilePickerFileType(
            //new Dictionary<DevicePlatform, IEnumerable<string>> {
            //            { DevicePlatform.iOS, new [] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" } },
            //});

            //PickOptions pickOptions = new PickOptions {
            //    PickerTitle = "Select all the files",
            //    FileTypes = filePickerFileType,
            //};
            //var results = await FilePicker.PickMultipleAsync(pickOptions);
        }

        //[Export("application:didFinishLaunchingWithOptions:")]
        //public new bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) {
        //    //this.Window = new UIWindow(UIScreen.MainScreen.Bounds);
        //    //var mkbvc = new MockKeyboardViewController();
        //    //Window.RootViewController = mkbvc;
        //    //Window.MakeKeyAndVisible();
        //    MpConsole.WriteLine($"YOOOOOO");
        //    KbStorageHelpers.Init(this);
        //    OpenPrefs();
        //    return true;
        //}

        //private void IAvaloniaAppDelegate_Activated(object sender, Avalonia.Controls.ApplicationLifetimes.ActivatedEventArgs e) {
        //    if (e is not ProtocolActivatedEventArgs paea ||
        //        paea.Uri is not { } uri) {
        //        return;
        //    }
        //    OpenPrefs();
        //}

        string IStoragePathHelper.GetLocalStorageBaseDir() {
            var url = NSFileManager.DefaultManager.GetContainerUrl(KbStorageHelpers.IOS_SHARED_GROUP_ID);

            if(url == null ||
                url.AbsoluteUrl is not { } aburl ||
                aburl.Path is not { } shared_storage_path) {
                MpConsole.WriteLine($"SHARED STORAGE NOT FOUND :(");
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            return shared_storage_path;
        }


    }
}

