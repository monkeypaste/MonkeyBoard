using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyPaste.Keyboard;

public partial class MainView : UserControl {
    public static bool IS_LOG_TO_INPUT_ENABLED = true;
    public static MainView Instance { get; private set; }
    public TextBox TestTextBoxRef =>
        TestTextBox;
    public static bool show_windowless_kb = false;
    static IKeyboardInputConnection _conn;
    public static void ForceInputConn(IKeyboardInputConnection conn) {
        _conn = conn;
    }
    public Canvas OuterCanvas =>
        ContainerCanvas;

    public MainView() {
        Instance = this;
        InitializeComponent();

        this.GetObservable(BoundsProperty).Subscribe(value => OnBoundsChanged());
        this.EffectiveViewportChanged += (s, e) => OnBoundsChanged();

    }
    void SetupLogHandler() {
        if(this.GetVisualDescendants().OfType<TextBox>() is not { } tbl ||
            !tbl.Any()) {
            return;
        }
        foreach(var tb in tbl) {
            tb.TextChanged += Tb_TextChanged;
        }
    }

    private void Tb_TextChanged(object sender, TextChangedEventArgs e) {
        if(sender is not TextBox tb ||
            tb.Text is not { } cur_text ||
                cur_text.SplitByLineBreak() is not { } cur_lines ||
                cur_lines.ToList() is not { } output_lines ||
                cur_lines.Where(x => x.StartsWith(KeyConstants.LOG_LINE_PREFIX)) is not { } new_log_lines ||
                !new_log_lines.Any()) {
            return;
        }

        new_log_lines.ToList().ForEach(x => output_lines.Remove(x));
        tb.Text = string.Join(Environment.NewLine, output_lines);

        TestTextBox.Text += Environment.NewLine + string.Join(Environment.NewLine, new_log_lines.Select(x => x.Replace(KeyConstants.LOG_LINE_PREFIX, string.Empty)));
        if(TestTextBox.Parent is ScrollViewer sv) {
            sv.ScrollToEnd();
        }
    }

    void OnBoundsChanged() {
        if(this.GetVisualDescendants().OfType<KeyboardView>().FirstOrDefault() is not { } kbv ||
            kbv.DataContext is not KeyboardViewModel kbmvm) {
            return;
        }

        kbmvm.SetDesiredSize(KeyboardViewModel.GetTotalSizeByScreenSize(this.Bounds.Size, kbmvm.KeyboardFlags.HasFlag(KeyboardFlags.Portrait)));
        kbv.Width = kbmvm.TotalWidth;
        kbv.Height = kbmvm.TotalHeight;
    }


    public void RefreshButtonEnabled(bool wait = true) {
        if(MpPlatformKeyboardServices.KeyboardPermissionHelper is not { } kph) {
            return;
        }
        Dispatcher.UIThread.Post(async () => {
            await Task.Delay(3_000);
            bool is_enabled = kph.IsKeyboardEnabled();
            bool is_active = kph.IsKeyboardActive();
            EnableButton.IsEnabled = !is_enabled;
            ActivateButton.IsEnabled = !is_active && is_enabled;
        });
    }
    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        if(OperatingSystem.IsIOS() && IS_LOG_TO_INPUT_ENABLED) {
            SetupLogHandler();
        }

        ClearLogButton.Click += (s, e) => {
            TestTextBox.Text = string.Empty;
        };

        KeyboardViewModel kbvm = null;
        TextInputOptions.SetMultiline(TestTextBox, true);
        OrientationButton.Click += (s, e) => {
            if(TopLevel.GetTopLevel(this) is not Window w) {
                return;
            }
            if(w.Width > w.Height) {
                kbvm.KeyboardFlags &= ~KeyboardFlags.Landscape;
                kbvm.KeyboardFlags |= KeyboardFlags.Portrait;
            } else {
                kbvm.KeyboardFlags &= ~KeyboardFlags.Portrait;
                kbvm.KeyboardFlags |= KeyboardFlags.Landscape;
            }
            double temp = w.Width;
            w.Width = w.Height;
            w.Height = temp;
            w.InvalidateArrange();
            w.InvalidateMeasure();
            w.InvalidateVisual();
            OnBoundsChanged();
            kbvm.Init(kbvm.KeyboardFlags);
        };


        RefreshButtonEnabled(false);
        EnableButton.Click += (s, e) => {
            if(MpPlatformKeyboardServices.KeyboardPermissionHelper is not { } kph) {
                return;
            }
            kph.ShowKeyboardActivator();
            RefreshButtonEnabled();
        };


        ActivateButton.Click += (s, e) => {
            if(MpPlatformKeyboardServices.KeyboardPermissionHelper is not { } kph) {
                return;
            }
            kph.ShowKeyboardSelector();
            RefreshButtonEnabled();
        };
        TestButton.Click += async (s, e) => {
            FilePickerFileType ImageAll = new("All Images") {
                Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" },
                AppleUniformTypeIdentifiers = new[] { "public.image" },
                MimeTypes = new[] { "image/*" }
            };
            if(this.GetVisualRoot() is TopLevel vr) {
                var files = await vr.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions() {
                    Title = "Pick it",
                    //You can add either custom or from the built-in file types. See "Defining custom file types" on how to create a custom one.
                    FileTypeFilter = new[] { ImageAll }
                });
            }

            //TestTextBox.Text ="An optional reference to a string pointer. If the function returns anything other than SQLITE_OK, and error message will be passed back. If no error is encountered, the pointer will be set to NULL. The reference may be NULL to ignore error messages. Error messages must be freed with sqlite3_free().";
            //KeyboardPalette.P[PaletteColorType.PrintPalette();

            //if (kbvm != null) {
            //    kbvm.RenderAll();
            //    if(kbvm.IsNumPadLayout) {
            //        kbvm.KeyboardFlags &= ~KeyboardFlags.Numbers;
            //        kbvm.KeyboardFlags |= KeyboardFlags.Normal;
            //    } else {
            //        kbvm.KeyboardFlags &= ~KeyboardFlags.Normal;
            //        kbvm.KeyboardFlags |= KeyboardFlags.Numbers;
            //    }

            //    kbvm.Init(kbvm.KeyboardFlags);
            //}
            //Touches.Clear();
            //if(!show_windowless_kb) {
            //    return;
            //}

            //var rect = new Rect(0, 0, 1000, 300);         
            //var test = KeyboardBuilder.Build(null, new Size(1000, 300), 2.25, out _);
            //test.Measure(rect.Size);
            //test.Arrange(rect);
            //test.UpdateLayout();
            //test.InvalidateVisual();
            //RenderHelpers.RenderToFile(test, @"C:\Users\tkefauver\Desktop\test1.png");
        };

        if(MainViewModel.IsMockKeyboardVisible) {
            double scale = 1;
            if(OperatingSystem.IsWindows() &&
            TopLevel.GetTopLevel(this) is Window w) {
                scale = w.DesktopScaling;
            }
            Control ctrl_to_add = null;
            Control kbv = null;
            //show_windowless_kb = false;
            if(show_windowless_kb) {
                //kbv = KeyboardBuilder.Build(_conn, KeyboardViewModel.GetTotalSizeByScreenSize(this.Bounds.Size, kbvm.KeyboardFlags.HasFlag(KeyboardFlags.Portrait)), scale, out _);
                //kbvm = kbv.DataContext as KeyboardViewModel;
                //if(_conn is IKeyboardInputConnection_desktop) {
                //    var hidden_window = new Window() {
                //        SizeToContent = SizeToContent.WidthAndHeight,
                //        ShowInTaskbar = false,
                //        WindowState = WindowState.Minimized,
                //        SystemDecorations = SystemDecorations.None,
                //        Content = kbv
                //    };
                //    hidden_window.Show();
                //} else {

                //}

                var HeadlessKeyboardImage = new Image();

                var bg_border = new Viewbox() {
                    Width = kbvm.TotalWidth,
                    Height = kbvm.TotalHeight,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Child = new Border {
                        Background = Brushes.MidnightBlue,
                        Child = HeadlessKeyboardImage,
                    }
                };
                ctrl_to_add = bg_border;
            } else {
                //kbv = KeyboardViewModelFactory.CreateKeyboardView(_conn, KeyboardViewModel.GetTotalSizeByScreenSize(this.Bounds.Size, true), scale, out _);
                //kbvm = kbv.DataContext as KeyboardViewModel;
                //ctrl_to_add = kbv;
            }

            ctrl_to_add.VerticalAlignment = VerticalAlignment.Bottom;

            OuterPanel.Children.Add(ctrl_to_add);
            Grid.SetRow(ctrl_to_add, 4);

            if(_conn is ISetInputConnectionSource icd) {
                icd.SetKeyboardInputSource(this.TestTextBox);
            }

            if(_conn is IHeadLessRender_desktop hrd && show_windowless_kb) {
                hrd.SetRenderSource(kbv);
                hrd.SetPointerInputSource(ctrl_to_add);
                //scale = 1;
                var render_timer = new DispatcherTimer() {
                    Interval = TimeSpan.FromMilliseconds(1000d / 120d),
                    IsEnabled = true
                };

                void RenderKeyboard() {
                    //if(!show_windowless_kb) {
                    //    return;
                    //}
                    //if (KeyboardRenderer.GetKeyboardImageBytes(scale) is not { } bytes) {
                    //    return;
                    //}
                    //if (ctrl_to_add.GetVisualDescendants().OfType<Image>().FirstOrDefault() is not { } img) {
                    //    return;
                    //}
                    //img.Source = RenderHelpers.RenderToBitmap(bytes);
                }

                render_timer.Tick += (s, e) => {
                    RenderKeyboard();
                };
                RenderKeyboard();

                hrd.OnPointerChanged += (s, e) => {
                    kbvm.HandleTouch(e);
                    //kbv.BindingContext.Test1Command.Execute(null);
                    //HeadlessKeyboardImage.Source = hrd.RenderToBitmap(scale);
                    //RenderHelpers.RenderToFile(kbv, @"C:\Users\tkefauver\Desktop\test2.png");
                    if(e == null) {

                    }
                };


            }
        } else {
            // no mock 
        }
        if(!OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS()) {
            TestTextBox.Focus();
        }

    }

}