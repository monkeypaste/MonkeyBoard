
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard.Common;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AvAssetLoader = Avalonia.Platform.AssetLoader;

namespace MonkeyPaste.Keyboard {

    public class PortableInputConnection : 
        IKeyboardInputConnection, 
        IMainThread, 
        ITextTools, 
        IAssetLoader, 
        IStoragePathHelper, 
        ISetInputConnectionSource {
        private IKbWordUpdater _wordUpdater;
        private IKbClipboardHelper _clipboardHelper;
        TextBox InputTextBox { get; set; }
        Control RenderSource { get; set; }
        Control PointerInputSource { get; set; }
        public void OnLog(string text, bool freeze = false) {
            MpConsole.WriteLine(text);
        }
        public void OnText(string text, bool forceInput = false) {
            if(InputTextBox == null) {
                return;
            }
            InputTextBox.SelectedText = text;
        }
        public void OnShiftChanged(ShiftStateType sst) { }
        public void OnBackspace(int count) {
            if(InputTextBox == null) {
                return;
            }
            int sidx = Math.Max(0, Math.Min(InputTextBox.SelectionStart, InputTextBox.SelectionEnd));
            int eidx = Math.Max(0, Math.Max(InputTextBox.SelectionStart, InputTextBox.SelectionEnd));
            int len = Math.Max(0, eidx - sidx);
            if(len == 0) {
                if(sidx == 0) {
                    return;
                }
                int old_idx = InputTextBox.CaretIndex;
                InputTextBox.Text = InputTextBox.Text.Substring(0, sidx - 1) + InputTextBox.Text.Substring(eidx);
                InputTextBox.CaretIndex = Math.Max(0, old_idx - 1);
            } else {
                InputTextBox.SelectedText = string.Empty;
            }
        }

        public string GetLeadingText(int offset, int len) {
            if(InputTextBox == null) {
                return string.Empty;
            }
            string pre_text = InputTextBox.Text.Substring(0, InputTextBox.SelectionStart);
            if(offset < 0) {
                offset = pre_text.Length;
            }
            var sb = new StringBuilder();
            for(int i = 0; i < offset; i++) {
                int pre_text_idx = pre_text.Length - 1 - i;
                if(pre_text_idx < 0) {
                    break;
                }
                sb.Insert(0, pre_text[pre_text_idx]);
            }
            return sb.ToString();
        }
        public KeyboardFlags Flags =>
            KeyboardFlags.Mobile |
            KeyboardFlags.Normal |
            KeyboardFlags.Dark;
        public void OnDone() {
            OnDismissed?.Invoke(this, EventArgs.Empty);
        }
        Size IKeyboardInputConnection.ScaledScreenSize =>
            new Size(1080, 2200);
        public void SetKeyboardInputSource(TextBox textBox) {
            InputTextBox = textBox;
            InputTextBox.GetObservable(TextBox.CaretIndexProperty)
                .Subscribe(value => { OnCursorChanged?.Invoke(this, OnTextRangeInfoRequest()); });
        }

        public void OnNavigate(int dx, int dy) {
            if(InputTextBox == null) {
                return;
            }

            //InputTextBox.CaretIndex += CursorControlHelper.FindCaretOffset(InputTextBox.Text, InputTextBox.CaretIndex, dx, dy);

            int? x_dir = dx == 0 ? null : dx > 0 ? 1 : -1;
            int? y_dir = dy == 0 ? null : dy > 0 ? 1 : -1;

            if(x_dir.HasValue) {
                Key kc = x_dir > 0 ? Key.Right : Key.Left;
                for(int i = 0; i < (int)Math.Abs(dx); i++) {
                    InputTextBox.RaiseEvent(new KeyEventArgs {
                        Key = kc,
                        RoutedEvent = TextBox.KeyDownEvent
                    });
                    InputTextBox.RaiseEvent(new KeyEventArgs {
                        Key = kc,
                        RoutedEvent = TextBox.KeyUpEvent
                    });
                }
            }
            if(y_dir.HasValue) {
                Key kc = y_dir > 0 ? Key.Down : Key.Up;
                for(int i = 0; i < (int)Math.Abs(dy); i++) {
                    InputTextBox.RaiseEvent(new KeyEventArgs {
                        Key = kc,
                        RoutedEvent = TextBox.KeyDownEvent
                    });
                    InputTextBox.RaiseEvent(new KeyEventArgs {
                        Key = kc,
                        RoutedEvent = TextBox.KeyUpEvent
                    });
                }
            }
        }

        #region IHeadlessRender Implementation

        public void SetPointerInputSource(Control sourceControl) {
            PointerInputSource = sourceControl;

            PointerInputSource.PointerPressed += (s, e) => {
                var loc = e.GetPosition(PointerInputSource);
                OnPointerChanged?.Invoke(this, new TouchEventArgs(loc, TouchEventType.Press));
            };
            PointerInputSource.PointerMoved += (s, e) => {
                if(OperatingSystem.IsWindows() &&
                    !e.GetCurrentPoint(s as Visual).Properties.IsLeftButtonPressed) {
                    // ignore mouse movement on desktop
                    //OnPointerChanged?.Invoke(this, null);
                    return;
                }
                var loc = e.GetPosition(PointerInputSource);
                OnPointerChanged?.Invoke(this, new TouchEventArgs(loc, TouchEventType.Move));
            };
            PointerInputSource.PointerReleased += (s, e) => {
                var loc = e.GetPosition(PointerInputSource);
                OnPointerChanged?.Invoke(this, new TouchEventArgs(loc, TouchEventType.Release));
            };
        }
        public void SetRenderSource(Control sourceControl) {
            RenderSource = sourceControl;
        }

        public void OnFeedback(KeyboardFeedbackFlags flags) {
        }

        public event EventHandler OnDismissed;
        public event EventHandler<TouchEventArgs> OnPointerChanged;
        public event EventHandler<SelectableTextRange> OnCursorChanged;

        public SelectableTextRange OnTextRangeInfoRequest() {
            if(InputTextBox == null) {
                return new(string.Empty, 0, 0);
            }
            return new SelectableTextRange(
                InputTextBox.Text,
                InputTextBox.SelectionStart,
                InputTextBox.SelectionEnd - InputTextBox.SelectionStart);
        }

        public void OnReplaceText(int sidx, int eidx, string text) {
            if(InputTextBox == null) {
                return;
            }
            InputTextBox.SelectionStart = sidx;
            InputTextBox.SelectionEnd = eidx;
            InputTextBox.SelectedText = text;
        }

        public void OnBackspaceRepeatStart(int repeatMs) {

        }

        public void OnBackspaceRepeatEnd() {

        }

        public void OnShowPreferences(object args) {

        }

        public ITextTools TextTools => this;
        public ISharedPrefService SharedPrefService { get; set; } = new DummyPrefService();
        public IAssetLoader AssetLoader => this;

        IKbWordUpdater IKeyboardInputConnection.WordUpdater => _wordUpdater;

        public IMainThread MainThread => this;

        public void Post(Action action) {
            Dispatcher.UIThread.Post(action);
        }
        bool ITextTools.CanRender(string text) => true;
        public Size MeasureText(string text, double scaledFontSize, out double ascent, out double descent) {
            var ft = new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(InputTextBox.FontFamily, InputTextBox.FontStyle, InputTextBox.FontWeight),
                    Math.Max(1, scaledFontSize),
                    Brushes.White);
            ascent = 0;
            descent = 0;
            return new Size(ft.Width, ft.Height);
        }

        public Stream LoadStream(string path) {
            Stream stream = null;
            if(path.StartsWith("avares")) {
                stream = AvAssetLoader.Open(new Uri(path));
            } else {
                stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public void OnSelectText(int sidx, int eidx) {
            InputTextBox.SelectionStart = sidx;
            InputTextBox.SelectionEnd = eidx;
        }


        public ISpeechToTextConnection SpeechToTextService { get; }
        public IKbWordUpdater WordUpdater => _wordUpdater;

        public void OnSelectAll() {
            InputTextBox.SelectAll();
        }

        public string GetLocalStorageDir() {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        public IStoragePathHelper StoragePathHelper =>
            this;

        IKbClipboardHelper IKeyboardInputConnection.ClipboardHelper => _clipboardHelper;

        public string SourceId { get; }
        public Task<MpAvKbBridgeMessageBase> GetMessageResponseAsync(MpAvKbBridgeMessageBase request) {
            throw new NotImplementedException();
        }

        public IThisAppVersionInfo VersionInfo { get; }
        public IPreviewFeedback FeedbackHandler { get; }

        public void OnCollapse(bool isHold) {
            OnDeviceMove?.Invoke(this, default);
            throw new NotImplementedException();
        }

        public event EventHandler<(double roll, double pitch, double yaw)> OnDeviceMove;

        public bool IsReceivingCursorUpdates { get; }

        public void OnToggleFloatingWindow() {
            throw new NotImplementedException();
        }

        public Rect ScaledWorkAreaRect { get; }

        public void OnAlert(string title, string detail) {
            throw new NotImplementedException();
        }

        public Task<bool> OnConfirmAlertAsync(string title, string detail) {
            throw new NotImplementedException();
        }

        public IKbClipboardHelper ClipboardHelper { get; }
        public bool NeedsInputModeSwitchKey => false;

        public void OnInputModeSwitched() {
            throw new NotImplementedException();
        }

        public IAnimationTimer AnimationTimer { get; }
        public Size MaxScaledSize { get; }


        #endregion

        public string GetLocalStorageBaseDir() {
            throw new NotImplementedException();
        }
    }
}

