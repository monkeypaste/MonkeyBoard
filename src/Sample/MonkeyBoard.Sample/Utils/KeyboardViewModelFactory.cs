using Avalonia.Controls;
using Avalonia.Data;
using MonkeyBoard.Common;
using MonkeyBoard.Sample;
using MonkeyPaste.Common;
using System;
using System.Diagnostics;
using Size = Avalonia.Size;

namespace iosKeyboardTest {
    public static class KeyboardViewModelFactory {
        public static KeyboardView CreateKeyboardView(IKeyboardInputConnection inputConn, Size scaledSize, double scale, out Size unscaledSize) {
            var kbvm = new KeyboardViewModel(inputConn, scaledSize, scale);
            var kbv = new KeyboardView() {
                DataContext = kbvm,
                [!Control.WidthProperty] = new Binding { Source = kbvm, Path = nameof(kbvm.TotalWidth) },
                [!Control.HeightProperty] = new Binding { Source = kbvm, Path = nameof(kbvm.TotalHeight) },
            };
            DateTime press_time = default;
            kbv.PointerPressed += (s, e) => {
                if (kbvm == null) {
                    kbvm = new KeyboardViewModel(inputConn, scaledSize, scale);
                    kbv.DataContext = kbvm;
                    kbvm.Renderer.RenderFrame(true);
                    //kbvm.RenderAll();
                }
                int mb = (int)(GC.GetTotalMemory(false) / (1024 * 1024));
                MpConsole.WriteLine($"Mem: {mb}mb");
                press_time = DateTime.Now;
                //kbvm.SetPointerLocation(new TouchEventArgs(e.GetPosition(kbv), TouchEventType.Press));
            };
            kbv.PointerMoved += (s, e) => {
                if (OperatingSystem.IsWindows() && !e.GetCurrentPoint(kbv).Properties.IsLeftButtonPressed) {
                    return;
                }
                //kbvm.SetPointerLocation(new TouchEventArgs(e.GetPosition(kbv), TouchEventType.Move));
            };
            kbv.PointerReleased += (s, e) => {
                MpConsole.WriteLine($"Actual Touch time: {(DateTime.Now - press_time).Milliseconds}ms");
                //kbvm.SetPointerLocation(new TouchEventArgs(e.GetPosition(kbv), TouchEventType.Release));
            };


            unscaledSize = kbvm == null ? new(scaledSize.Width * scale, scaledSize.Height * scale) : new Size(kbvm.TotalWidth * scale, kbvm.TotalHeight * scale);
            return kbv;
        }
    }
}
