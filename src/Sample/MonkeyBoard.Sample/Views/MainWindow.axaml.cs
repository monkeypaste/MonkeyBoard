using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using MonkeyBoard.Common;
using MonkeyBoard.Sample;
using System;
using System.Linq;


namespace MonkeyBoard.Sample;

public partial class MainWindow : Window {
    public static MainWindow Instance { get; private set; }
    public MainWindow() {
        Instance = this;

        InitializeComponent();
        if(OperatingSystem.IsWindows()) {
            this.Width = 360;
            this.Height = 740;

            //this.Width = 810;
            //this.Height = 1080;
        }
        this.GetObservable(BoundsProperty).Subscribe(value => OnBoundsChanged());
    }

    void OnBoundsChanged() {
        if(this.GetVisualDescendants().OfType<KeyboardView>().FirstOrDefault() is not { } kbv ||
            kbv.DataContext is not KeyboardViewModel kbvm) {
            return;
        }
        kbvm.SetDesiredSize(
            KeyboardViewModel.GetTotalSizeByScreenSize(
                this.Bounds.Size, kbvm.KeyboardFlags.HasFlag(KeyboardFlags.Portrait),
                kbvm.KeyboardFlags.HasFlag(KeyboardFlags.Tablet)));
    }
}