using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using MonkeyPaste.Keyboard.Common;
using System;
using System.Linq;

namespace MonkeyPaste.Keyboard {
    public partial class KeyboardView : UserControl {
        public KeyboardViewModel BindingContext =>
            DataContext as KeyboardViewModel;
        public KeyboardView() {
            InitializeComponent();
        }
    }
}