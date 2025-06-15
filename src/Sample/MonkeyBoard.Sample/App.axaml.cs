using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MonkeyPaste.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

#if ENABLE_XAML_HOT_RELOAD
using HotAvalonia;
#endif

namespace MonkeyBoard.Sample {
    public partial class App : Application {
        public const string WAIT_FOR_DEBUG_ARG = "--wait-for-attach";
        public const string BREAK_ON_ATTACH_ARG = "--break-on-attach";
        public override void Initialize() {
#if ENABLE_XAML_HOT_RELOAD
            this.EnableHotReload(); 
#endif
            AvaloniaXamlLoader.Load(this);
        }

        StringBuilder LogSb { get; set; } = new();

public override void OnFrameworkInitializationCompleted() {
            if(OperatingSystem.IsIOS()) {
                MpConsole.ConsoleLineAdded += (s, e) => {
                    Dispatcher.UIThread.Post(() => {
                        LogSb.AppendLine(e);
                        if (MainView.Instance is { } mv) {
                            mv.TestTextBox.Text = LogSb.ToString();
                            if (mv.TestTextBox.Parent is ScrollViewer sv) {
                                sv.ScrollToEnd();
                            }
                        }

                    });
                };
            }
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow {
                    DataContext = new MainViewModel()
                };
            } else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
                
                singleViewPlatform.MainView = new MainView {
                    DataContext = new MainViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
        public static void WaitForDebug(object[] args) {
            if (!args.Contains(WAIT_FOR_DEBUG_ARG)) {
                return;
            }
            MpConsole.WriteLine("Attach debugger and use 'Set next statement'");
            while (true) {
                Thread.Sleep(100);
                if (Debugger.IsAttached) {
                    if (args.Contains(BREAK_ON_ATTACH_ARG)) {
                        Debugger.Break();
                    }
                    break;
                }
            }
        }
    }
}

