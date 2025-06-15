using AudioToolbox;
using Avalonia;
using Avalonia.Controls.Shapes;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard.Common;
using ObjCRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Path = System.IO.Path;
namespace MonkeyBoard.iOS.KeyboardExt {
#pragma warning disable CA1010
    public partial class iosKeyboardViewController :
        UIInputViewController,
        IKeyboardInputConnection,
        IKbClipboardHelper,
        IPreviewFeedback,
        ITriggerTouchEvents,
        IKbWordUpdater,
        IMainThread,
        IAssetLoader,
        IStoragePathHelper {
#pragma warning restore CA1010


        #region Private Variables

        #endregion

        #region Constants
        #endregion

        #region Statics

        static bool FakeNoFullAccess { get; set; } = false;

        // NOTE!! changes to logging have to be here AND in AppDelegate
        static bool IS_LOG_TO_INPUT_ENABLED =>
#if DEBUG
            false;
#else
            false;
#endif

        static string error = string.Empty;
        static UIButton showErrorButton;
        static UILabel showErrorLabel;
        static iosKeyboardViewController _instance;
        public static iosKeyboardViewController Instance =>
            _instance;

        #endregion

        #region Interfaces


        #region IClipboardHelper Implementation
        async Task<string> IKbClipboardHelper.GetTextAsync() {
            await Task.Delay(1);
            if(UIPasteboard.General.String is { } str) {
                return str;
            }
            return string.Empty;
            //return await Clipboard.Default.GetTextAsync();
        }
        async Task IKbClipboardHelper.SetTextAsync(string text) {
            //Clipboard.Default.SetTextAsync(text);
            await Task.Delay(1);
            UIPasteboard.General.String = text;
        }


        #endregion

        #region IWordUpdater Implementation
        void IKbWordUpdater.AddWords(Dictionary<string, int> words, bool allowInsert) {
            WordDb.UpdateWordUseAsync(words, allowInsert).FireAndForgetSafeAsync();
        }
        #endregion

        #region IHandleFeeback Implementation

        void IPreviewFeedback.PlaySound(double normalizedLevel) {
            PlaySound(KeyboardFeedbackFlags.Invalid, normalizedLevel * 100);
        }
        void IPreviewFeedback.Vibrate(double normalizedLevel) {
            Vibrate((normalizedLevel * 4) + 1);
        }
        #endregion

        #region IStoragePathHelper Implementation

        string IStoragePathHelper.GetLocalStorageBaseDir() {
            var url = NSFileManager.DefaultManager.GetContainerUrl(KbStorageHelpers.IOS_SHARED_GROUP_ID);
            if(!HasFullAccessWrapper ||
                url == null ||
                url.AbsoluteUrl is not { } aburl ||
                aburl.Path is not { } shared_storage_path) {
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            return shared_storage_path;
        }

        #endregion

        #region IKeyboardInputConnection Implementation

        public event EventHandler<SelectableTextRange> OnCursorChanged;
        public event EventHandler OnDismissed;
        public event EventHandler<TouchEventArgs> OnPointerChanged;
        IKbClipboardHelper IKeyboardInputConnection.ClipboardHelper =>
            this;

        void IKeyboardInputConnection.OnAlert(string title, string detail) =>
            iosHelpers.Alert(this, title, detail, KeyboardView.DC.IsThemeDark);
        Task<bool> IKeyboardInputConnection.OnConfirmAlertAsync(string title, string detail) =>
            iosHelpers.AlertYesNoAsync(this, title, detail, KeyboardView.DC.IsThemeDark);
        bool IKeyboardInputConnection.IsReceivingCursorUpdates =>
            true;
        public void Post(Action action) {
            DispatchQueue.MainQueue.DispatchAsync(() => {
                try {
                    action.Invoke();
                }
                catch(Exception ex) {
                    ex.Dump();
                    SetError(ex.ToStringOrEmpty());
                }
            });
        }

        public event EventHandler<(double roll, double pitch, double yaw)> OnDeviceMove;
        IPreviewFeedback IKeyboardInputConnection.FeedbackHandler =>
            this;
        public void OnBackspace(int count) {
            bool forward = count < 0;
            count = (int)Math.Abs(count);
            for(int i = 0; i < count; i++) {
                TextRangeTools.DoBackspace(CurTextInfo, forward);
                if(HasInnerInput) {
                    //InnerTextView.DoBackspace(1);
                    InnerTextView.Text = CurTextInfo.Text;
                } else {
                    if(forward) {
                        OnNavigate(1, 0);
                    }
                    this.TextDocumentProxy?.DeleteBackward();

                }
            }
        }
        Stream IAssetLoader.LoadStream(string path) =>
            KbAssetMover.LoadAvAssetStream(path);
        bool IKeyboardInputConnection.NeedsInputModeSwitchKey =>
            true;
        public Size ScaledScreenSize { get; private set; } = new();
        public Size MaxScaledSize { get; private set; } = new();

        public void OnInputModeSwitched() {
            iosFooterView.SetLabel($"Input mode switch called");
            //iosKeyboardContainerView.AdjustHeight(-150);
            this.AdvanceToNextInputMode();
        }
        public void OnLog(string text, bool freeze = false) {
            iosFooterView.SetLabel(text, freeze);
        }
        public void OnDone() {
            this.DismissKeyboard();
        }

        public void OnCollapse(bool isHold) {
            if(isHold) {
                this.DismissKeyboard();
                return;
            }
            KeyboardContainerView.ToggleExpanded();
        }
        public void OnNavigate(int dx, int dy) {
            if(HasInnerInput) {
            } else {
                this.TextDocumentProxy?.AdjustTextPositionByCharacterOffset(dx);
            }
        }

        public void OnFeedback(KeyboardFeedbackFlags flags) {
            Post(() => {
                if(flags.HasFlag(KeyboardFeedbackFlags.Vibrate)) {
                    Vibrate();
                }
                if(flags.HasFlag(KeyboardFeedbackFlags.Click)) {
                    PlaySound(KeyboardFeedbackFlags.Click);
                }
                if(flags.HasFlag(KeyboardFeedbackFlags.Return)) {
                    PlaySound(KeyboardFeedbackFlags.Return);
                }
                if(flags.HasFlag(KeyboardFeedbackFlags.Delete)) {
                    PlaySound(KeyboardFeedbackFlags.Delete);
                }
                if(flags.HasFlag(KeyboardFeedbackFlags.Space)) {
                    PlaySound(KeyboardFeedbackFlags.Space);
                }
                if(flags.HasFlag(KeyboardFeedbackFlags.Invalid)) {
                    PlaySound(KeyboardFeedbackFlags.Invalid);
                }
            });
        }
        void Vibrate(double? forceLevel = null) {
            double level = forceLevel.HasValue ? forceLevel.Value : SharedPrefService.VibrateDurMs;
            //iosFooterView.SetLabel($"Vibrate lvl: {level}");
            if(level <= 0) {
                return;
            }
            if(Vibrator == null) {
                // from https://stackoverflow.com/a/78032485/105028
                if(iosDeviceInfo.SdkVersion >= Version.Parse("13.0") &&
                    iosDeviceInfo.SdkVersion < Version.Parse("17.5")) {
                    Vibrator = new UIImpactFeedbackGenerator(UIImpactFeedbackStyle.Light);
                } else if(iosDeviceInfo.SdkVersion >= Version.Parse("17.5")) {
                    Vibrator = UIImpactFeedbackGenerator.GetFeedbackGenerator(this.RootView);
                }
            }
            nfloat intensity = (nfloat)Math.Max(0d, (level - 2) / 4d);
            if(intensity <= 0) {
                return;
            }
            Post(() => Vibrator.ImpactOccurred(intensity));
        }
        void PlaySound(KeyboardFeedbackFlags soundFlag, double? forceLevel = null) {
            double level = forceLevel.HasValue ? forceLevel.Value : SharedPrefService.SoundVol;
            //iosFooterView.SetLabel($"Sound lvl: {(level / 100f)}");
            if(level <= 0 || ObjCRuntime.Runtime.Arch != Arch.DEVICE) {
                return;
            }
            string sound_file_name = null;
            switch(soundFlag) {
                // flags from https://github.com/klaas/SwiftySystemSounds/blob/master/README.md
                // real proj https://github.com/TUNER88/iOSSystemSoundsLibrary
                case KeyboardFeedbackFlags.Click:
                    sound_file_name = "key_press_click.caf";
                    break;
                case KeyboardFeedbackFlags.Return:
                    sound_file_name = "New/Typewriters.caf";
                    break;
                case KeyboardFeedbackFlags.Delete:
                    sound_file_name = "key_press_delete.caf";
                    break;
                case KeyboardFeedbackFlags.Space:
                    sound_file_name = "keyboard_press_normal.caf";
                    break;
                case KeyboardFeedbackFlags.Invalid:
                    sound_file_name = "ct-error.caf";
                    break;
            }
            Post(() => {
                try {
                    //var player = new AVAudioPlayer(NSUrl.FromFilename($"/System/Library/Audio/UISounds/{sound_file_name}"), null, out NSError error);
                    //if (error != null) {
                    //    iosFooterView.SetLabel(error.ToString());
                    //}
                    //iosFooterView.SetLabel($"Sound lvl: {(level / 100f)}");
                    //player.Volume = 1;// (float)(level / 100d);
                    //player.Play();
                    var sound = new SystemSound(NSUrl.FromFilename($"/System/Library/Audio/UISounds/{sound_file_name}"));
                    sound.PlaySystemSound();
                }
                catch(Exception ex) {
                    ex.Dump();
                    SetError(ex.ToString());
                }
            });


        }
        KeyboardFlags? _flags;
        public KeyboardFlags Flags {
            get {
                if(_flags == null) {
                    _flags = GetFlags(null);
                }
                return _flags.Value;
            }
        }

        public void OnReplaceText(int sidx, int eidx, string text) {
            CurTextInfo.Select(sidx, eidx - sidx);
            TextRangeTools.DoText(CurTextInfo, text);

            if(HasInnerInput) {
                //InnerTextView.SetSelection(sidx, eidx);
                //InnerTextView.SelectedText = text;
                //int after_idx = sidx + text.Length;
                //InnerTextView.SetSelection(after_idx, after_idx);
                InnerTextView.Text = CurTextInfo.Text;
                return;
            }

            if(this.TextDocumentProxy is not { } tdp) {
                return;
            }

            int cur_idx = tdp.DocumentContextBeforeInput.ToStringOrEmpty().Length;
            int offset = eidx - cur_idx;
            tdp.AdjustTextPositionByCharacterOffset(offset);
            OnBackspace(eidx - sidx/* + 1*/);

            //tdp.SetMarkedText(text, new NSRange(sidx, eidx - sidx));
            OnText(text);

            // when replace is from autocorrect the CurTextInfo selection maybe get off
            // track since autocorrect can happen asynchronously so just reset it on replace
            ResetTextInfo();
        }

        void ResetTextInfo() {
            Post(async () => {
                _CurTextInfo = null;
                await Task.Delay(150);
                _ = CurTextInfo;
                OnSelectionChange(CurTextInfo.SelectionStartIdx, CurTextInfo.SelectionEndIdx);
            });
        }
        void OnSelectionChange(int sidx, int eidx) {
            Post(() => {
                var info = CurTextInfo;
                if(info.SelectionStartIdx != sidx || info.SelectionEndIdx != eidx) {
                    int newSelStart = sidx;
                    int newSelEnd = eidx;
                    info.Select(newSelStart, newSelEnd - newSelStart);

                    LastCursorChangeDt = DateTime.Now;
                }

                OnCursorChanged?.Invoke(this, info);

            });
        }
        public bool IsPreferencesVisible { get; private set; }
        public void OnShowPreferences(object args) {
            Post(async () => {
                var prefView = new PrefView();
                var dvc = await prefView.CreateDialogAsync(SharedPrefService, KeyboardView.DC.IsThemeDark);

                void OnViewDisappeared(object sender, EventArgs e) {
                    IsPreferencesVisible = false;
                    ReinitializeKeyboard();
                }
                dvc.ViewDisappearing += OnViewDisappeared;

                this.PresentViewController(dvc, true, null);
                IsPreferencesVisible = true;
                KeyboardContainerView.Redraw(true);
            });
        }
        void ReinitializeKeyboard() {
            if(KeyboardView is not { } kbv ||
                        KeyboardView.DC is not { } kbvm) {
                return;
            }
            kbvm.IsDirty = true;
            kbvm.Init(GetFlags(null));
            kbv.RenderFrame(true);
            KeyboardContainerView.Frame = kbvm.TotalRect.ToCGRect();
            KeyboardContainerView.ActivateConstraints();

            // NOTE calling this in add because first call doesn't work
            //iosKeyboardContainerView.AdjustHeight(0);
            //AddKeyboard();
        }

        IThisAppVersionInfo _versionInfo;
        IThisAppVersionInfo IKeyboardInputConnection.VersionInfo {
            get {
                if(_versionInfo == null) {
                    _versionInfo = new iosThisAppVersionInfo();
                }
                return _versionInfo;
            }
        }
        IStoragePathHelper IKeyboardInputConnection.StoragePathHelper =>
            this;

        ISharedPrefService IKeyboardInputConnection.SharedPrefService =>
            SharedPrefService;

        SharedPrefWrapper _sharedPrefService;
        public SharedPrefWrapper SharedPrefService {
            get {
                if(_sharedPrefService == null) {
                    _sharedPrefService = new SharedPrefWrapper();
                    _sharedPrefService.Init(new iosPrefService(this, _sharedPrefService), this, IsTablet());
                }
                return _sharedPrefService;
            }
        }
        public IAssetLoader AssetLoader => this;
        public IMainThread MainThread =>
            this;

        public SelectableTextRange OnTextRangeInfoRequest() {
            return GetCurTextInfo();
        }

        public void OnSelectText(int sidx, int eidx) {
            CurTextInfo.Select(sidx, eidx);
            if(HasInnerInput) {
                //InnerTextView.SetSelection(sidx, eidx);
            } else {
                this.TextDocumentProxy.SetMarkedText(CurTextInfo.SelectedText, new NSRange(CurTextInfo.SelectionStartIdx, CurTextInfo.SelectionLength));
            }
        }

        public void OnSelectAll() {
            CurTextInfo.Select(0, CurTextInfo.Text.Length);
            if(HasInnerInput) {
                //InnerTextView.SetSelection(0, CurTextInfo.Text.Length);
                return;
            }
            // get all the text
            string text = CurTextInfo.Text;
            // move caret to end
            OnNavigate(CurTextInfo.Text.Length - CurTextInfo.SelectionEndIdx, 0);
            // delete everything
            OnBackspace(text.Length);
            // put back as 'marked'
            this.TextDocumentProxy.SetMarkedText(text, new NSRange(0, text.Length));
        }

        public void OnText(string text, bool forceInput = false) {
            if(TextDocumentProxy == null ||
                string.IsNullOrEmpty(text)) {
                return;
            }
            var last_cursor_changed_dt = LastCursorChangeDt;
            if(HasInnerInput || !forceInput) {
                // don't update info when doing text from emoji search
                TextRangeTools.DoText(CurTextInfo, text);
            }

            if(HasInnerInput && !forceInput) {
                //iosKeyboardContainerView.Subviews.OfType<iosEmojiSearchTextView>().FirstOrDefault().Enabled = false;
                //InnerTextView.SelectedText = text;
                InnerTextView.Text = CurTextInfo.Text;
                InnerTextView.Redraw();
                //iosKeyboardContainerView.Subviews.OfType<iosEmojiSearchTextView>().FirstOrDefault().Enabled = true;
            } else {
                if(text.Length == 1 && char.IsNumber(text[0]) && int.TryParse(text[0].ToString(), out int count)) {
                    KeyboardView.EmojiPagesView.SetShowCount(count);
                }
                this.TextDocumentProxy?.InsertText(text);
            }
            // BUG calling reset info because not getting events 
            ResetTextInfo();
            if(last_cursor_changed_dt == LastCursorChangeDt) {
                LastCursorChangeDt = DateTime.Now;
                OnCursorChanged?.Invoke(this, CurTextInfo);
            }
            //iosFooterView.SetLabel($"OnText called: '{TextRangeTools.GetWordAtCaret(CurTextInfo, out _, out _, out _)}'");
        }

        public void OnShiftChanged(ShiftStateType newShiftState) {
            if(TextDocumentProxy is not IUITextInputTraits tit ||
                HasInnerInput) {
                return;
            }
            UITextAutocapitalizationType new_cap_type = tit.AutocapitalizationType;
            switch(newShiftState) {
                case ShiftStateType.None:
                    new_cap_type = UITextAutocapitalizationType.None;
                    break;
                case ShiftStateType.Shift:
                    new_cap_type = UITextAutocapitalizationType.Words;
                    break;
                case ShiftStateType.ShiftLock:
                    new_cap_type = UITextAutocapitalizationType.AllCharacters;
                    break;
            }
            //tit.AutocapitalizationType = new_cap_type;
        }
        iosDisplayLinkHelper _displayLinkHelper;
        IAnimationTimer IKeyboardInputConnection.AnimationTimer {
            get {
                if(_displayLinkHelper == null) {
                    _displayLinkHelper = new();
                }
                return _displayLinkHelper;
            }
        }

        ITextTools IKeyboardInputConnection.TextTools =>
            KeyboardView;
        public IKbWordUpdater WordUpdater { get; }
        public ISpeechToTextConnection SpeechToTextService { get; }

        #endregion

        #endregion

        #region Properties
        DateTime? LastCursorChangeDt { get; set; }
        SelectableTextRange _CurTextInfo;
        public SelectableTextRange CurTextInfo {
            get {
                if(_CurTextInfo == null) {
                    _CurTextInfo = GetCurTextInfo();
                    if(string.IsNullOrEmpty(_CurTextInfo.Text)) {
                        if(HasInnerInput) {
                            // no big concern for inner input and is expected to be empty
                        } else {
                            //_CurTextInfo = null;
                        }
                    }
                }
                return _CurTextInfo ?? new();
            }
        }
        public bool HasFullAccessWrapper =>
            FakeNoFullAccess ? false : this.HasFullAccess;
        UIImpactFeedbackGenerator Vibrator { get; set; }
        public iosKeyboardContainerView KeyboardContainerView { get; private set; }
        public iosKeyboardView KeyboardView =>
            KeyboardContainerView == null ? null : KeyboardContainerView.KeyboardView;

        iosEmojiSearchTextView InnerTextView { get; set; }
        bool HasInnerInput =>
            InnerTextView != null;

        bool IsLoadDone { get; set; }
        #endregion

        #region Events
        #endregion

        #region Constructors
        #endregion

        #region Public Methods

        #region Overrides
        public override void ViewDidLoad() {
            base.ViewDidLoad();

            try {
                _instance = this;
                _flags = GetFlags(null);
                InitKb();
                AddKeyboard();
                IsLoadDone = true;
                //RootView.AddSubview(new UIView(new CGRect(0, 0, 1000, 1000)) { BackgroundColor = UIColor.Blue });
            }
            catch(Exception ex) {
                SetError(ex.ToString());
            }
        }
        public override void ViewDidDisappear(bool animated) {
            base.ViewDidDisappear(animated);
            OnDismissed?.Invoke(this, EventArgs.Empty);
        }
        public override void ViewDidLayoutSubviews() {
            base.ViewDidLayoutSubviews();
            if(iosDeviceInfo.UpdateOrientation(UIScreen.MainScreen.Bounds.Size)) {
                _flags = GetFlags(null);
                MpConsole.WriteLine($"Orientation changed. IsPortrait: {iosDeviceInfo.IsPortrait}");
                ReinitializeKeyboard();
            }
        }

        public override void ViewWillAppear(bool animated) {
            base.ViewWillAppear(animated);
            var new_flags = GetFlags(null);
            if(new_flags != _flags) {
                bool needs_reset = _flags != KeyboardFlags.None;
                _flags = new_flags;
                if(needs_reset) {
                    ReinitializeKeyboard();
                }
            }
        }
        bool IsFloatDetected { get; set; }
        public override void ViewDidAppear(bool animated) {
            base.ViewDidAppear(animated);
            bool was_float_detected = IsFloatDetected;
            IsFloatDetected = this.RootView.Frame.Width != 0 && this.RootView.Frame.Width != KeyboardContainerView.Frame.Width;
            //IsFloatDetected = false;
            //iosFooterView.SetLabel($"F {this.RootView.Frame} _____________________________________________________________________________________________________");
            if(IsFloatDetected != was_float_detected && IsFloatDetected) {
                //iosFooterView.SetLabel($"F {IsFloatDetected}");
                ReinitializeKeyboard();
            }
            //if(IsFloatDetected) {
            //    MaxScaledSize = this.RootView.Frame.ToScaledRect().Size;
            //} else {
            //    MaxScaledSize = iosDeviceInfo.ScaledSize;
            //}
        }

        public override void TextDidChange(IUITextInput textInput) {
            //if (iosKeyboardView != null && iosKeyboardView.Focused) {
            //    return;
            //}
            if(CurTextInfo.IsValueEqual(GetCurTextInfo())) {
                return;
            }

            //iosFooterView.SetLabel($"TextDidChange called");
            ResetTextInfo();
        }
        public override void DidReceiveMemoryWarning() {
            try {
                // get cur stats
                long heap_size = GC.GetGCMemoryInfo().HeapSizeBytes;

                // force cleanup
                //GC.Collect(2, GCCollectionMode.Aggressive);
                iosHelpers.DoGC();
                //GC.WaitForFullGCComplete();

                // since apple doesn't tell us constraints use stats to limit further usage
                //int heap_mb = (int)(heap_size / (double)Math.Pow(1024, (Int64)2));
                //int capped_heap_mb = (int)(heap_mb * 0.85);
                //AppContext.SetData("GCHeapHardLimit", (ulong)capped_heap_mb << 20);
                //GC.RefreshMemoryLimit();
                iosFooterView.SetLabel($"Mem warning! Heap was {(int)(heap_size / (double)Math.Pow(1024, (Int64)2))}mb");
            }
            catch(Exception ex) {
                ex.Dump();
                SetError(ex.ToString());
            }
        }

        #endregion

        public void SetInnerTextField(iosEmojiSearchTextView tf) {
            if(InnerTextView != null) {
                InnerTextView.SelectionChanged -= InnerTextField_SelectionChanged;
            }
            InnerTextView = tf;
            ResetTextInfo();
            if(InnerTextView == null) {
                return;
            }
            InnerTextView.SelectionChanged += InnerTextField_SelectionChanged;
        }

        private void InnerTextField_SelectionChanged(object sender, EventArgs e) {
            if(sender is not iosEmojiSearchTextView tv ||
                tv.Range is not { } str) {
                return;
            }
            OnSelectionChange(str.SelectionStartIdx, str.SelectionEndIdx);
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        SelectableTextRange GetCurTextInfo() {
            if(HasInnerInput) {
                return new(InnerTextView.Text, InnerTextView.Text.Length, 0);
            }
            if(TextDocumentProxy is not { } tdp) {
                return new();
            }
            try {
                string pre = tdp.DocumentContextBeforeInput.ToStringOrEmpty();
                string sel = tdp.SelectedText.ToStringOrEmpty();
                string post = tdp.DocumentContextAfterInput.ToStringOrEmpty();
                string text = pre + sel + post;
                return new(text, pre.Length, sel.Length);
            }
            catch(Exception ex) {
                ex.Dump();
                SetError(ex.ToStringOrEmpty());
            }
            return new();
        }

        private void KeyboardView_OnTouchEvent(object sender, TouchEventArgs e) {
            try {
                OnPointerChanged?.Invoke(this, e);
            }
            catch(Exception ex) {
                SetError(ex.ToStringOrEmpty());
            }
        }

        void OnTouchInternal(TouchEventArgs e, bool fromPopup) {

        }

        void InitKb() {
            if(HasFullAccessWrapper) {
                KbStorageHelpers.Init(this);
            } else {
                KbStorageHelpers.Init(null);
            }


            if(HasFullAccessWrapper) {
                KbAssetMover.MoveAssets(false);
            } else {
                string locale_zip_src_path = NSBundle.MainBundle.PathForResource("en-US", "zip");
                string local_dst_dir = Path.Combine(
                    KbStorageHelpers.LocalStorageDir,
                    "locale",
                    "en-US");
                if(locale_zip_src_path.IsFile() && !local_dst_dir.IsDirectory()) {
                    ZipFile.ExtractToDirectory(locale_zip_src_path, local_dst_dir, true);
                }

                string img_zip_src_path = NSBundle.MainBundle.PathForResource("img", "zip");
                string img_dst_dir = Path.Combine(
                    KbStorageHelpers.LocalStorageDir,
                    "img");
                if(img_zip_src_path.IsFile() && !img_dst_dir.IsDirectory()) {
                    ZipFile.ExtractToDirectory(img_zip_src_path, img_dst_dir, true);
                }
            }

            if(IS_LOG_TO_INPUT_ENABLED) {
                InitLogger();
            }
        }
        void InitLogger() {
            string log_path = Path.Combine(KbStorageHelpers.LocalStorageDir, $"{DateTime.Now.Ticks}_ext.log");
            MpConsoleFlags log_flags = MpConsoleFlags.Console | MpConsoleFlags.File | MpConsoleFlags.Stampless;
            MpConsole.Init(log_path, log_flags);
            MpConsole.ConsoleLineAdded += MpConsole_ConsoleLineAdded;
        }
        [Export("handleInputList:fromView:withEvent:")]
        public override void HandleInputModeList(UIView fromView, UIEvent withEvent) {
            base.HandleInputModeList(fromView, withEvent);
        }
        Selector _inputListSelector;
        public Selector InputListSelector {
            get {
                if(_inputListSelector == null) {
                    _inputListSelector = new Selector("handleInputList:fromView:withEvent:");
                }
                return _inputListSelector;
            }
        }
        //public new UIView View { get; set; }
        public UIView RootView =>
            View;
        void AddKeyboard() {
            ScaledScreenSize = iosDeviceInfo.ScaledSize;
            IsFloatDetected = this.RootView.Frame.Width != 0;
            MaxScaledSize = IsFloatDetected ? this.RootView.Frame.ToScaledRect().Size : ScaledScreenSize;
            if(KeyboardContainerView != null) {
                KeyboardView.OnTouchEvent -= KeyboardView_OnTouchEvent;
                KeyboardContainerView.Unload();
            }
            KeyboardContainerView = new iosKeyboardContainerView();
            KeyboardContainerView.Init(this);

            KeyboardView.OnTouchEvent += KeyboardView_OnTouchEvent;
            RootView.AddSubview(KeyboardContainerView);
            RootView.BackgroundColor = UIColor.Clear;
            KeyboardContainerView.ActivateConstraints();

            // NOTE calling this in add because first call doesn't work
            //KeyboardContainerView.AdjustHeight(0);
        }

        private void MpConsole_ConsoleLineAdded(object sender, string e) {
            if(e.SplitByLineBreak() is not { } lines) {
                return;
            }
            string prefixed_lines = string.Join(Environment.NewLine, lines.Select(x => KeyConstants.LOG_LINE_PREFIX + x));
            Post(() => OnText(prefixed_lines));
        }

        public static void SetError(string er) {
            error = er;

            if(showErrorButton == null) {
                AddDebugButton();
            }
            showErrorButton.SetTitle(error, UIControlState.Normal);
            showErrorButton.SizeToFit();

            setErrorLabel(0, error);

            _instance.OnText(string.Join(Environment.NewLine, er.SplitByLineBreak().Select(x => KeyConstants.LOG_LINE_PREFIX + x)));
        }
        static int step = 0;
        static int stride = 500;

        static void setErrorLabel(int new_step, string err) {
            step = new_step;

            if(step + stride >= err.Length) {
                step = 0;
            }
            int adj_len = step + stride;
            if(adj_len >= err.Length) {
                adj_len = err.Length - step;
            }
            showErrorLabel.Text = err.Substring(step, adj_len);
            showErrorLabel.SizeToFit();
        }
        static void AddDebugButton() {
            showErrorButton = new UIButton(UIButtonType.System);
            showErrorButton.TranslatesAutoresizingMaskIntoConstraints = false;
            showErrorButton.TouchUpInside += (s, e) => {
                //(_instance as IKeyboardInputConnection).OnText(error);
                //iosHelpers.Alert(_instance, string.Empty, error);
                setErrorLabel(step + stride, error);

            };
            _instance.RootView.AddSubview(showErrorButton);
            NSLayoutConstraint.ActivateConstraints([
                showErrorButton.LeftAnchor.ConstraintEqualTo(_instance.RootView.LeftAnchor),
                showErrorButton.RightAnchor.ConstraintEqualTo(_instance.RootView.RightAnchor),
                showErrorButton.TopAnchor.ConstraintEqualTo(_instance.RootView.TopAnchor),
                ]);


            showErrorLabel = new UILabel();
            showErrorLabel.BackgroundColor = UIColor.Black;
            showErrorLabel.TextColor = UIColor.White;
            showErrorLabel.LineBreakMode = UILineBreakMode.CharacterWrap;
            showErrorLabel.Lines = 1000;
            showErrorLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _instance.RootView.AddSubview(showErrorLabel);
            NSLayoutConstraint.ActivateConstraints([
                showErrorLabel.LeftAnchor.ConstraintEqualTo(_instance.RootView.LeftAnchor),
                showErrorLabel.RightAnchor.ConstraintEqualTo(_instance.RootView.RightAnchor),
                showErrorLabel.BottomAnchor.ConstraintEqualTo(_instance.RootView.BottomAnchor),
                ]);

            SetError(error);
        }

        public bool IsTablet() {
            return UIDevice.CurrentDevice.Model.ToLower().StartsWith("ipad");
        }
        KeyboardFlags GetFlags(IUITextInputTraits? tit) {
            tit = tit == null ? TextDocumentProxy : tit;
            var kbf = KeyboardFlags.PlatformView | KeyboardFlags.iOS;

            bool is_portrait = iosDeviceInfo.IsPortrait;
            if(is_portrait) {
                kbf |= KeyboardFlags.Portrait;
            } else {
                kbf |= KeyboardFlags.Landscape;
            }


            if(UIScreen.MainScreen.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark) {
                kbf |= KeyboardFlags.Dark;
            } else {
                kbf |= KeyboardFlags.Light;
            }
            if(tit != null) {
                switch(tit.KeyboardType) {
                    case UIKeyboardType.NumberPad:
                    case UIKeyboardType.PhonePad:
                    //kbf |= KeyboardFlags.Pin;
                    //break;
                    case UIKeyboardType.NamePhonePad:
                    case UIKeyboardType.NumbersAndPunctuation:
                    case UIKeyboardType.DecimalPad:
                    case UIKeyboardType.AsciiCapableNumberPad:
                        kbf |= KeyboardFlags.Numbers;
                        break;
                    case UIKeyboardType.EmailAddress:
                        kbf |= KeyboardFlags.Email;
                        break;
                    case UIKeyboardType.WebSearch:
                        kbf |= KeyboardFlags.Search;
                        break;
                    case UIKeyboardType.Url:
                        kbf |= KeyboardFlags.Url;
                        break;
                    case UIKeyboardType.Twitter:
                        kbf |= KeyboardFlags.Normal | KeyboardFlags.Tweet;
                        break;
                    default:
                        kbf |= KeyboardFlags.Normal;
                        break;
                }

                switch(tit.ReturnKeyType) {
                    case UIReturnKeyType.Go:
                        kbf |= KeyboardFlags.Go;
                        break;
                    case UIReturnKeyType.Google:
                    case UIReturnKeyType.Search:
                    case UIReturnKeyType.Yahoo:
                        kbf |= KeyboardFlags.Search;
                        break;
                    case UIReturnKeyType.Join:
                        kbf |= KeyboardFlags.Join;
                        break;
                    case UIReturnKeyType.Next:
                    case UIReturnKeyType.Continue:
                        kbf |= KeyboardFlags.Next;
                        break;
                    case UIReturnKeyType.Send:
                        kbf |= KeyboardFlags.Send;
                        break;
                    case UIReturnKeyType.Done:
                        kbf |= KeyboardFlags.Done;
                        break;
                    case UIReturnKeyType.EmergencyCall:
                        kbf |= KeyboardFlags.Call;
                        break;
                }
                if(tit.SecureTextEntry) {
                    kbf |= KeyboardFlags.Password;
                }
            }


            bool is_floating = IsFloatDetected;
            if(is_floating) {
                kbf |= KeyboardFlags.FloatLayout;
                var w = this.RootView.Frame.ToScaledRect().Size.Width;
                MaxScaledSize = new Size(w, w / 2);
                iosFooterView.SetLabel($"F {MaxScaledSize} ____________________________________________________________________________________________________________");
            } else {
                kbf |= KeyboardFlags.FullLayout;
                MaxScaledSize = iosDeviceInfo.ScaledSize;
                iosFooterView.SetLabel($"No float");
            }

            kbf |= IsTablet() && !is_floating ?
                KeyboardFlags.Tablet :
                KeyboardFlags.Mobile;
            ScaledScreenSize = iosDeviceInfo.ScaledSize;

            //iosFooterView.SetLabel($"KeyboardFlags: {kbf}");

            return kbf;
        }
        bool IsFloating() {
            return this.RootView.Frame.Width < iosDeviceInfo.UnscaledSize.Width / 2;
        }

        public void OnToggleFloatingWindow() {
            throw new NotImplementedException();
        }

        public Rect ScaledWorkAreaRect { get; }
        #endregion
    }
}