using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CoreGraphics;
using KeyboardLib;
using System;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace iosKeyboardTest.iOS {
    public partial class MockKeyboardViewController : UIViewController, IKeyboardInputConnection_ios, ITriggerTouchEvents, IStoragePathHelper {
        //iosKeyboardTest.iOS.KeyboardExt.KeyboardView KeyboardView { get; set; }
        UITextView InputTextBox { get; set; }
        public override void ViewDidLoad() {
            base.ViewDidLoad();

            InputTextBox = new UITextView();
            //InputTextBox.InputView = new UIView();
            //InputTextBox.InputAccessoryView = new UIView();
            InputTextBox.Editable = true;
            InputTextBox.Text = "Whats up yo";
            double w1 = 300;
            double h1 = 300;
            double x1 = (UIScreen.MainScreen.Bounds.Width / 2) - (w1/2);
            double y1 = h1 / 2;
            InputTextBox.Frame = new CGRect(x1, y1, w1, h1);
            InputTextBox.SelectionChanged += InputTextBox_SelectionChanged;
            View.AddSubview(InputTextBox);



            var tbb = new UIButton(UIButtonType.System);
            tbb.SetTitle("Test", UIControlState.Normal);
            tbb.SizeToFit();
            tbb.BackgroundColor = UIColor.Purple;
            tbb.Layer.CornerRadius = 10;
            tbb.TranslatesAutoresizingMaskIntoConstraints = false;
            double w2 = 100;
            double h2 = 40;
            double x2 = (UIScreen.MainScreen.Bounds.Width / 2) - (w2 / 2);
            double y2 = y1 + h1 + 10;
            tbb.Frame = new CGRect(x2, y2, w2, h2);
            View.AddSubview(tbb);
            tbb.TouchUpInside += (s, e) => {
                //if(KeyboardView == null) {
                //    return;
                //}
                //KeyboardView.Render(true);
                OnPointerChanged?.Invoke(this, new(new(), TouchEventType.None));
            };
            OnFlagsChanged?.Invoke(this, EventArgs.Empty);

            
            //KeyboardView = new iosKeyboardTest.iOS.KeyboardExt.KeyboardView(this);

            //View.AddSubview(KeyboardView);

            //KeyboardView.OnTouchEvent += (s, e) => {
            //    OnPointerChanged?.Invoke(this, e);
            //};
        }

        private void InputTextBox_SelectionChanged(object sender, EventArgs e) {
            OnCursorChanged?.Invoke(this, new());
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator) {
            base.ViewWillTransitionToSize(toSize, coordinator);
            //InputTextBox.Text += Environment.NewLine + (UIScreen.MainScreen.Bounds.Width < UIScreen.MainScreen.Bounds.Height ? "PORTRAIT" : "LANDSCAPE");
        }

        public override void ViewDidUnload() {
            //base.ViewDidUnload();
            OnDismissed?.Invoke(this, EventArgs.Empty);
        }

        public bool NeedsInputModeSwitchKey { get; }

        public void OnInputModeSwitched() {
            //thrownew NotImplementedException();
        }

        public event EventHandler OnFlagsChanged;
        public event EventHandler OnDismissed;

        public string GetLeadingText(int offset, int len) {
            if (InputTextBox == null) {
                return string.Empty;
            }
            string pre_text = InputTextBox.Text.Substring(0, (int)InputTextBox.GetOffsetFromPosition(InputTextBox.BeginningOfDocument, InputTextBox.SelectedTextRange.Start));
            if (offset < 0) {
                offset = pre_text.Length;
            }
            var sb = new StringBuilder();
            for (int i = 0; i < offset; i++) {
                int pre_text_idx = pre_text.Length - 1 - i;
                if (pre_text_idx < 0) {
                    break;
                }
                sb.Insert(0, pre_text[pre_text_idx]);
            }
            return sb.ToString();
        }

        public void OnText(string text) {
            if (InputTextBox == null) {
                return;
            }
            InputTextBox.ReplaceText(InputTextBox.SelectedTextRange, text);
        }

        public void OnBackspace(int count) {
            if (InputTextBox == null) {
                return;
            }
            var rng = InputTextBox.SelectedTextRange;
            
            int sidx = Math.Max(0, (int)InputTextBox.GetOffsetFromPosition(InputTextBox.BeginningOfDocument, rng.Start));
            int eidx = Math.Max(0, (int)InputTextBox.GetOffsetFromPosition(InputTextBox.BeginningOfDocument, rng.End));
            int len = Math.Max(0, eidx - sidx);
            if (len == 0) {
                if (sidx == 0) {
                    return;
                }
                int old_idx = sidx;
                InputTextBox.Text = InputTextBox.Text.Substring(0, sidx - 1) + InputTextBox.Text.Substring(eidx);
                //InputTextBox.CaretIndex = Math.Max(0, old_idx - 1);
                var new_pos = InputTextBox.GetPosition(rng.Start, -1);
                if(new_pos != null) {
                    InputTextBox.SelectedTextRange = InputTextBox.GetTextRange(new_pos, new_pos);
                }
            } else {
                InputTextBox.ReplaceText(InputTextBox.SelectedTextRange, string.Empty);
            }
        }

        public void OnDone() {
            //thrownew NotImplementedException();
        }

        public void OnNavigate(int dx, int dy) {
            if (InputTextBox == null) {
                return;
            }

            int? x_dir = dx == 0 ? null : dx > 0 ? 1 : -1;
            int? y_dir = dy == 0 ? null : dy > 0 ? 1 : -1;

            //if (x_dir.HasValue) {
            //    Key kc = x_dir > 0 ? Key.Right : Key.Left;
            //    for (int i = 0; i < (int)Math.Abs(dx); i++) {
            //        InputTextBox.SelectedRan
            //        InputTextBox.RaiseEvent(new KeyEventArgs {
            //            Key = kc,
            //            RoutedEvent = TextBox.KeyDownEvent
            //        });
            //        InputTextBox.RaiseEvent(new KeyEventArgs {
            //            Key = kc,
            //            RoutedEvent = TextBox.KeyUpEvent
            //        });
            //    }
            //}
            //if (y_dir.HasValue) {
            //    Key kc = y_dir > 0 ? Key.Down : Key.Up;
            //    for (int i = 0; i < (int)Math.Abs(dy); i++) {
            //        InputTextBox.RaiseEvent(new KeyEventArgs {
            //            Key = kc,
            //            RoutedEvent = TextBox.KeyDownEvent
            //        });
            //        InputTextBox.RaiseEvent(new KeyEventArgs {
            //            Key = kc,
            //            RoutedEvent = TextBox.KeyUpEvent
            //        });
            //    }
            //}
        }

        public void OnFeedback(KeyboardFeedbackFlags flags) {
            //
        }

        public KeyboardFlags Flags { 
            get {
                var kbf = KeyboardFlags.None;
                kbf |= KeyboardFlags.Mobile;
                kbf |= KeyboardFlags.PlatformView;
                kbf |= KeyboardFlags.Normal;
                kbf |= KeyboardFlags.iOS;
                return kbf;
            }
        }

        public event EventHandler<TouchEventArgs> OnPointerChanged;

        public event EventHandler<SelectableTextRange> OnCursorChanged;

        public SelectableTextRange OnTextRangeInfoRequest() {

            return new SelectableTextRange(string.Empty, 0, 0);
        }

        public void OnReplaceText(int sidx, int eidx, string text) {
            
        }

        public void OnBackspaceRepeatStart(int repeatMs) {
            
        }

        public void OnBackspaceRepeatEnd() {
            
        }

        public void OnShowPreferences(object args) {
            
        }

        public ITextTools TextMeasurer { get; }
        public ISharedPrefService SharedPrefService { get; }
        public IAssetLoader AssetLoader { get; }
        public IMainThread MainThread { get; }


        public void OnSelectText(int sidx, int eidx) {
            throw new NotImplementedException();
        }

        public void OnSelectAll() {
            throw new NotImplementedException();
        }

        public void OnText(string text, bool forceInput = false) {
            throw new NotImplementedException();
        }

        public void OnShiftChanged(ShiftStateType newShiftState) {
            throw new NotImplementedException();
        }

        public ITextTools TextTools { get; }
        public IWordUpdater WordUpdater { get; }
        public ISpeechToTextConnection SpeechToTextService { get; }

        public string GetLocalStorageDir() {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        public IStoragePathHelper StoragePathHelper => this;

        public IAnimationTimer AnimationTimer { get; }
        public IThisAppVersionInfo VersionInfo { get; }


        public void OnDismiss() {
            throw new NotImplementedException();
        }
    }
}