using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.InputMethodServices;
using Android.Media;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Avalonia.Threading;
//using Microsoft.Maui.ApplicationModel.DataTransfer;
//using Microsoft.Maui.Devices.Sensors;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using MonkeyPaste.Common.Plugin;
using MonkeyBoard.Common;
using MonkeyBoard.Bridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static Android.Views.View;
using Environment = System.Environment;
using Exception = System.Exception;
using GStream = Android.Media.Stream;
using Keycode = Android.Views.Keycode;
using Math = System.Math;
using Orientation = Android.Content.Res.Orientation;
using Point = Avalonia.Point;
using Stream = System.IO.Stream;
using TouchEventArgs = MonkeyBoard.Common.TouchEventArgs;
using View = Android.Views.View;
namespace MonkeyBoard.Android {
    [Service(
        Exported = true,
        Name = "com.Monkey.AdInputMethodService")]
    public class AdInputMethodService :
        InputMethodService,
        IKeyboardInputConnection,
        IPreviewFeedback,
        IOnTouchListener,
        IOnPopupTouchListener,
        IKbClipboardHelper,
        IStoragePathHelper,
        IAssetLoader,
        IKbWordUpdater,
        IMainThread,
        ITriggerTouchEvents {
        #region Private Variables
        // from https://learn.microsoft.com/en-us/answers/questions/252318/creating-a-custom-android-keyboard
        int _lastVibrateLevel = -1;
        VibrationEffect _vibrateEffect;

        private KeyboardFlags? _lastFlags;
        AudioManager _audioManager;
        Vibrator _vibrator;
        Handler mainHandler;

        #endregion

        #region Constants

        public const string PREF_BUNDLE_KEY = "PrefManager";

        #endregion

        #region Statics
        public static event EventHandler<AdInputMethodService> OnKeyboardCreated;
        public static bool IsDismissed { get; private set; }

        static bool IS_IDLE_CHECKER_ENABLED = false;
        static bool IS_WORD_UPDATE_ENABLED = true;

        #endregion

        #region Interfaces

        #region IClipboardHelper Implementation
        async Task<string> IKbClipboardHelper.GetTextAsync() {
            //return await Clipboard.Default.GetTextAsync();
            return await Clipboard.GetTextAsync();
        }
        Task IKbClipboardHelper.SetTextAsync(string text) =>
            //Clipboard.Default.SetTextAsync(text);
            Clipboard.SetTextAsync(text);

        #endregion

        #region IStoragePathHelper Implementation
        string IStoragePathHelper.GetLocalStorageBaseDir() {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        #endregion

        #region IWordUpdater Implementation
        void IKbWordUpdater.AddWords(Dictionary<string, int> words, bool allowInsert) {
            if(IS_WORD_UPDATE_ENABLED) {
                WordUpdateScheduler.AddWords(this, words, allowInsert);
            }
        }

        #endregion

        #region IAssetLoader Implementation
        public Stream LoadStream(string path) {
            Stream stream = null;
            if(path.StartsWith("avares")) {
                return KbAssetMover.LoadAvAssetStream(path);
            } else {
                AssetManager assets = ApplicationContext.Assets;
                stream = assets.Open(path, Access.Buffer);
            }
            return stream;

        }
        #endregion

        #region IMainThread Implementation
        public void Post(Action action) {
            if(mainHandler == null) {
                mainHandler = new Handler(Looper.MainLooper);
            }
            mainHandler.Post(() => {
                try {
                    action.Invoke();
                }
                catch(Exception ex) {
                    ex.Dump();
                }
            });
        }

        #endregion

        #region TouchListener Implementation
        Dictionary<int, (Point, Point)> Touches { get; set; } = [];

        #region IOnPopupTouchListener Implementation
        bool IOnPopupTouchListener.OnTouch(View v, MotionEvent e) {
            return OnTouch_internal(v, e, true);
        }
        #endregion

        bool OnTouch_internal(View v, MotionEvent e, bool fromPopup) {
            if(v.IsWindowDead()) {
                //ResetService();
                return true;
            }
            if(v is AdCustomView cv && cv.TagObj.ToStringOrEmpty() == "CHUNKIES") {

            }
            bool handled = true;
            var changed_touches = e.GetMotions(Touches);
            Point offset = new();
            if(v is ITranslatePoint itp) {
                // from float container
                offset = itp.TranslatePoint(new());
            }
            if(fromPopup) {
                // touch from pop up comes in relative to popup not root view
                offset = AdEmojiSearchPopupWindow.TranslatePoint(new());
                handled = false;
            }
            foreach(var ct in changed_touches) {
                var scaled_loc = (ct.p.loc + offset) / AdDeviceInfo.Scaling;
                var scaled_raw_loc = (ct.p.raw_loc + offset) / AdDeviceInfo.Scaling;
                var touch_e = new TouchEventArgs(scaled_loc, scaled_raw_loc, ct.eventType, ct.id.ToString());
                OnPointerChanged?.Invoke(this, touch_e);
            }
            return handled;
        }


        #region IOnTouchListener Implementation
        bool IOnTouchListener.OnTouch(View v, MotionEvent e) {
            return OnTouch_internal(v, e, false);
        }
        #endregion

        #endregion

        #region IHandleFeeback Implementation

        void IPreviewFeedback.PlaySound(double normalizedLevel) {
            PlaySound(SoundEffect.Invalid, normalizedLevel * 100);
        }
        void IPreviewFeedback.Vibrate(double normalizedLevel) {
            Vibrate((normalizedLevel * 4) + 1);
        }
        #endregion

        #region IKeyboardInputConnection Implementation
        string IKeyboardInputConnection.SourceId =>
            HostId;
        AdPermissionHelper PermHelper { get; set; }
        void IKeyboardInputConnection.OnInputModeSwitched() {
            if(PermHelper == null) {
                PermHelper = new AdPermissionHelper(this);
            }
            PermHelper.ShowKeyboardSelector();
        }
        bool IKeyboardInputConnection.NeedsInputModeSwitchKey =>
            this.ShouldOfferSwitchingToNextInputMethod();
        AdAnimationHelper AnimationHelper = new();
        IAnimationTimer IKeyboardInputConnection.AnimationTimer =>
            AnimationHelper;

        IKbClipboardHelper IKeyboardInputConnection.ClipboardHelper =>
            this;
        void IKeyboardInputConnection.OnAlert(string title, string detail) {
            Post(() => AdHelpers.Alert(AdSettingsActivity.CurrentContext ?? this, title, detail));
        }
        async Task<bool> IKeyboardInputConnection.OnConfirmAlertAsync(string title, string detail) {
            bool? result = null;
            Post(async () => {
                result = await AdHelpers.AlertYesNoAsync(AdSettingsActivity.CurrentContext ?? this, title, detail);
            });
            while(result is null) {
                await Task.Delay(100);
            }
            return result.Value;
        }

        bool IKeyboardInputConnection.IsReceivingCursorUpdates =>
            IsReceivingCursorUpdates;
        Size IKeyboardInputConnection.ScaledScreenSize =>
            AdDeviceInfo.ScaledSize;
        Size IKeyboardInputConnection.MaxScaledSize =>
            AdDeviceInfo.ScaledSize;
        Rect IKeyboardInputConnection.ScaledWorkAreaRect =>
            AdDeviceInfo.UnscaledWorkAreaRect.ToAvRect();

        IThisAppVersionInfo _versionInfo;
        IThisAppVersionInfo IKeyboardInputConnection.VersionInfo {
            get {
                if(_versionInfo == null) {
                    _versionInfo = new AdThisAppVersionInfo(this);
                }
                return _versionInfo;
            }
        }
        IStoragePathHelper IKeyboardInputConnection.StoragePathHelper =>
            this;
        public void OnToggleFloatingWindow() {
            IsWindowed = !IsWindowed;
            if(KeyboardView.DC.IsFloatingLayout == IsWindowed) {
                bool is_vm_valid = KeyboardView.DC.IsFloatingLayout == KeyboardView.IsFloating;
                bool is_service_valid = IsWindowed == KeyboardView.IsFloating;

                MpConsole.WriteLine($"Error! Float mismatch detected. Ignoring transition. Vm valid: {is_vm_valid} IMS valid: {is_service_valid}");
                // flip here so vm request works
                IsWindowed = !IsWindowed;
            }
            ResetState();
        }
        public void OnLog(string text, bool freeze = false) {
            MpConsole.WriteLine(text);
        }
        public void OnCollapse(bool isHold) {

        }
        public void OnShowPreferences(object args) {
            //ToggleFloatTest();
            StartPrefActivity();
        }
        private SpeechToText _speechToText;
        public ISpeechToTextConnection SpeechToTextService {
            get {
                if(_speechToText == null) {
                    _speechToText = new SpeechToText(this);
                    _speechToText.Init();
                }
                return _speechToText;
            }
        }

        IPreviewFeedback IKeyboardInputConnection.FeedbackHandler =>
            this;
        IMainThread IKeyboardInputConnection.MainThread =>
            this;
        IKbWordUpdater IKeyboardInputConnection.WordUpdater =>
            this;
        public IAssetLoader AssetLoader =>
            this;
        public ISharedPrefService SharedPrefService =>
            PrefManager;
        public ITextTools TextTools =>
            KeyboardView;

        public SharedPrefWrapper PrefManager { get; private set; }

        public KeyboardFlags Flags =>
            GetFlags(this.CurrentInputEditorInfo);
        public SelectableTextRange OnTextRangeInfoRequest() =>
            _CurTextInfo ?? GetTextInfoFromConnection();

        public void OnBackspace(int count) {
            bool forward = count < 0;
            count = (int)Math.Abs(count);
            while(count > 0) {
                // send actual backspace key event when nothing to delete from conn,
                // conn method doesn't auto remove Google Keep checkboxes like default keyboard does
                // NOTE not sure if below does fix it...
                TextRangeTools.DoBackspace(CurTextInfo, forward);
                if(HasInnerInput) {
                    InnerEditText.DoBackspace();
                } else {
                    if(forward) {
                        OnNavigate(1, 0);
                    }
                    this.CurrentInputConnection.SendKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.Del));
                }
                count--;
            }
        }
        public void OnSelectAll() {
            OnSelectText(0, this.CurTextInfo.Text.Length);
            this.CurrentInputConnection.PerformContextMenuAction(global::Android.Resource.Id.SelectAll);
        }
        public void OnSelectText(int sidx, int eidx) {
            CurTextInfo.Select(sidx, eidx);
            if(HasInnerInput) {
                InnerEditText.SetSelection(sidx, eidx);
            } else {
                this.CurrentInputConnection.SetSelection(sidx, eidx);
                //if (IsReceivingCursorUpdates) {
                //    this.CurrentInputConnection.SetSelection(sidx, eidx);
                //} else {
                //    this.CurrentInputConnection.SetComposingRegion(sidx, eidx);
                //    this.UpdateInputViewShown();
                //}

            }
        }
        public void OnReplaceText(int sidx, int eidx, string text) {
            // from https://stackoverflow.com/a/67071610/105028
            CurTextInfo.Select(sidx, eidx - sidx);
            TextRangeTools.DoText(CurTextInfo, text);

            if(HasInnerInput) {
                InnerEditText.SetSelection(sidx, eidx);
                InnerEditText.SelectedText = text;
                int after_idx = sidx + text.Length;
                InnerEditText.SetSelection(after_idx, after_idx);
                return;
            }

            this.CurrentInputConnection.SetComposingRegion(sidx, eidx);
            this.CurrentInputConnection.SetComposingText(text, 1);
            this.CurrentInputConnection.FinishComposingText();

            // when replace is from autocorrect the CurTextInfo selection maybe get off
            // track since autocorrect can happen asynchronously so just reset it on replace
            ResetTextInfo();
        }
        public void OnNavigate(int dx, int dy) {
            if(this.CurrentInputConnection is not { } ic) {
                return;
            }

            // prevent vert arrow nav (i guess)
            int min_y_extent = 20;
            dy = HasInnerInput ? 0 : dy;
            int x_step = dx == 0 ? 0 : dx > 0 ? 1 : -1;
            int y_step = dy == 0 ? 0 : dy > 0 ? 1 : -1;
            int count = Math.Max(Math.Abs(dx), Math.Abs(dy));
            for(int i = 0; i < count; i++) {
                x_step = i > (int)Math.Abs(dx) ? 0 : x_step;
                y_step = i > (int)Math.Abs(dy) ? 0 : y_step;

                // don't get horizontally forward/back beyond left/right
                int next_x = CurTextInfo.SelectionStartIdx + x_step;
                x_step = next_x < 0 || next_x > CurTextInfo.Text.Length ? 0 : x_step;

                // don't go vertical up/down when there's less than 20 characters in that direction
                // (uses min_y_extent as crude estimate of how many characters 1 up/down will displace insert)
                int next_y = CurTextInfo.SelectionStartIdx + (min_y_extent * y_step);
                int y_extent = y_step < 0 ?
                         next_y :
                        CurTextInfo.Text.Length - next_y;
                if(y_extent < min_y_extent) {
                    y_step = 0;
                }
                TextRangeTools.DoNavigate(CurTextInfo, x_step, y_step);
                if(HasInnerInput) {
                    InnerEditText.SetSelection(CurTextInfo.SelectionStartIdx, CurTextInfo.SelectionEndIdx);
                } else {
                    if(IsReceivingCursorUpdates && false) {
                        ic.SetSelection(CurTextInfo.SelectionStartIdx, CurTextInfo.SelectionEndIdx);
                    } else {
                        // fallback for funky input connections
                        if(x_step != 0) {
                            this.SendDownUpKeyEvents(x_step > 0 ? Keycode.DpadRight : Keycode.DpadLeft);
                        }
                        if(y_step != 0) {
                            this.SendDownUpKeyEvents(y_step > 0 ? Keycode.DpadDown : Keycode.DpadUp);
                        }

                    }
                }
            }
        }
        public void OnText(string text, bool forceInput = false) {
            if(this.CurrentInputConnection == null ||
                string.IsNullOrEmpty(text)) {
                return;
            }
            if(!HasInnerInput || !forceInput) {
                // don't update info when doing text from emoji search
                TextRangeTools.DoText(CurTextInfo, text);
            }

            if(HasInnerInput && !forceInput) {
                InnerEditText.SelectedText = text;
                return;
            }

            try {
                if(text == "\t") {
                    this.SendDownUpKeyEvents(Keycode.Tab);
                    return;
                }
                //if (text.ToLower() == "t") {
                //    KeyboardContainerView.ShowInWindow();
                //    return;

                //}

                this.CurrentInputConnection.CommitText(text, 1);
            }
            catch(Exception ex) {
                MpConsole.WriteLine(ex.ToString());
            }
        }
        ImeAction GetImeAction(EditorInfo ei) {
            if((int)(ei.ImeOptions & ImeFlags.NoEnterAction) != 0) {
                return ImeAction.None;
            }
            if(ei.ActionLabel != null) {
                return (ImeAction)ei.ActionId;
            }
            return (ImeAction)((int)ei.ImeOptions & (int)ImeAction.ImeMaskAction);
        }
        public void OnShiftChanged(ShiftStateType newShiftState) {
            if(this.CurrentInputConnection is not { } ic ||
               HasInnerInput) {
                return;
            }
            CapitalizationMode newMode = default;
            switch(newShiftState) {
                default:
                    newMode = 0;
                    break;
                case ShiftStateType.Shift:
                    newMode = CapitalizationMode.Characters;
                    break;
                case ShiftStateType.ShiftLock:
                    newMode = CapitalizationMode.Characters | CapitalizationMode.Words | CapitalizationMode.Sentences;
                    break;
            }
            this.CurrentInputEditorInfo.InitialCapsMode = newMode;
        }
        public void OnDone() {
            this.CurrentInputConnection.PerformEditorAction(GetImeAction(this.CurrentInputEditorInfo));
        }
        public void OnFeedback(KeyboardFeedbackFlags flags) {
            if(flags.HasFlag(KeyboardFeedbackFlags.Vibrate)) {
                Vibrate();
            }
            if(flags.HasFlag(KeyboardFeedbackFlags.Click)) {
                PlaySound(SoundEffect.KeyClick);
            }
            if(flags.HasFlag(KeyboardFeedbackFlags.Return)) {
                PlaySound(SoundEffect.Return);
            }
            if(flags.HasFlag(KeyboardFeedbackFlags.Delete)) {
                PlaySound(SoundEffect.Delete);
            }
            if(flags.HasFlag(KeyboardFeedbackFlags.Space)) {
                PlaySound(SoundEffect.Spacebar);
            }
            if(flags.HasFlag(KeyboardFeedbackFlags.Invalid)) {
                PlaySound(SoundEffect.Invalid);
            }
        }


        async Task<MpAvKbBridgeMessageBase> IKeyboardInputConnection.GetMessageResponseAsync(MpAvKbBridgeMessageBase request) {
            await Task.Delay(1);
            //if(request is MpAvKbClipboardRequestMessage) {
            //    return PopPendingClipboardItems();
            //}
            return null;
        }

        public event EventHandler<TouchEventArgs> OnPointerChanged;
        public event EventHandler<SelectableTextRange> OnCursorChanged;
        public event EventHandler OnDismissed;
        #endregion

        #endregion

        #region Properties
        string LastHostId { get; set; }
        public string HostId =>
            this.CurrentInputEditorInfo == null ? string.Empty : this.CurrentInputEditorInfo.PackageName;
        bool IsCheckingAccelerometer { get; set; }
        bool IsWindowed { get; set; }
        bool IsIdleCheckerRunning { get; set; }
        CancellationTokenSource IdleCheckerCt { get; set; }
        DateTime? LastCursorChangeDt { get; set; }
        SpeechToText SpeechToText { get; set; }
        AdEmojiSearchEditText InnerEditText { get; set; }
        bool IS_PLATFORM_MODE => true;
        SelectableTextRange _CurTextInfo;
        public SelectableTextRange CurTextInfo {
            get {
                if(_CurTextInfo == null) {
                    _CurTextInfo = GetTextInfoFromConnection();
                    if(string.IsNullOrEmpty(_CurTextInfo.Text)) {
                        if(HasInnerInput) {
                            // no big concern for inner input and is expected to be empty
                        } else {
                            _CurTextInfo = null;
                        }
                    }
                }
                return _CurTextInfo ?? new();
            }
        }

        public AdInputContainerView KeyboardContainerView { get; set; }
        public AdKeyboardView KeyboardView =>
            KeyboardContainerView == null ? null : KeyboardContainerView.KeyboardView;
        AvaloniaView AvView { get; set; }
        bool IsReceivingCursorUpdates { get; set; }
        bool HasInnerInput =>
            InnerEditText != null;

        bool? _isHwAccelEnabled;
        public bool IsHwAccelEnabled {
            get {
                if(_isHwAccelEnabled == null && PrefManager is { } pm) {
                    _isHwAccelEnabled = pm.GetPrefValue<bool>(PrefKeys.DO_HW_ACCEL);
                }
                return _isHwAccelEnabled.Value;
            }
        }

        public MpAdKbClipboardListener CbListener { get; private set; }
        public MpAdKbProcessWatcher ProcWatcher { get; private set; }

        #endregion

        #region Events
        public event EventHandler<(double roll, double pitch, double yaw)> OnDeviceMove;
        public event EventHandler<string> OnHostIdChanged;
        #endregion

        #region Constructors
        public AdInputMethodService() {
            OnKeyboardCreated?.Invoke(this, this);
            //Init();
        }

        #endregion

        #region Public Methods

        #region Overrides
        /* call order
         OnStartInput
         OnCreateInputView
         OnCreateCandidatesView
         OnWindowShown
        */

        public override View? OnCreateInputView() { // NOTE this is called BEFORE onCreateCandidates
            Init();
            if(IS_PLATFORM_MODE) {
                CreateAdKeyboard();
                return KeyboardContainerView;
            }
            return CreateAvKeyboard();
        }

        public override void OnViewClicked(bool focusChanged) {
            // anytime view is clicked reset text info because:
            // 1. if in-app undo clicked backspaces/inserts not shadowed in CurTextInfo
            if(HasInnerInput) {
                ClearInnerEditText();
            } else {
                ResetTextInfo();
            }
        }
        public override void OnUpdateCursorAnchorInfo(CursorAnchorInfo cursorAnchorInfo) {
            //MpConsole.WriteLine($"INSERT TOP: {cursorAnchorInfo.InsertionMarkerTop} BOTTOM: {cursorAnchorInfo.InsertionMarkerBottom} HORIZONTAL: {cursorAnchorInfo.InsertionMarkerHorizontal}");
            UpdateSelection(cursorAnchorInfo.SelectionStart, cursorAnchorInfo.SelectionEnd);
        }
        public override void OnUpdateSelection(int oldSelStart, int oldSelEnd, int newSelStart, int newSelEnd, int candidatesStart, int candidatesEnd) {
            if(IsReceivingCursorUpdates) {
                return;
            }
            UpdateSelection(newSelStart, newSelEnd);
        }

        public override void OnConfigurationChanged(Configuration newConfig) {
            //base.OnConfigurationChanged(newConfig);
            ResetState();
        }

        public override void OnStartInput(EditorInfo attribute, bool restarting) {
            if(HostId != LastHostId) {
                LastHostId = HostId;
                OnHostIdChanged?.Invoke(this, HostId);
            }
            if(attribute.InputType == InputTypes.Null ||
                (int)attribute.ImeOptions == 0 ||
                this.CurrentInputConnection == null) {
                // BUG this is what causes the keyboard to redraw after dismiss!!!
                // from (comment by j__m): https://stackoverflow.com/q/19961618/105028
                return;
            }
            if(!restarting &&
                KeyboardView != null &&
                KeyboardView.IsWindowDead()) {
                // dead window, reset
                //ResetService();
                return;
            }

            // NOTE triggering dismiss here is disabled to improve initial startup time per app, 
            // I think it was an attempt to deal with float mode starting but not sure
            //if(KeyboardView != null &&
            //    !restarting) {
            //    OnDismissed?.Invoke(this, EventArgs.Empty);
            //}

            IsReceivingCursorUpdates = this.CurrentInputConnection.RequestCursorUpdates((int)(CursorUpdate.Monitor | CursorUpdate.Immediate));
            ResetState();


            if(AvView == null) {
                return;
            }
            Dispatcher.UIThread.Post(async () => {
                if(AvView == null ||
                    AvView.Content is not Control c ||
                    c.DataContext is not KeyboardViewModel kbvm) {
                    return;
                }
                while(kbvm.IsBusy) {
                    await Task.Delay(100);
                }

                AvView.Invalidate();
            });
        }
        public override void OnWindowShown() {
            base.OnWindowShown();
            //this.SetCandidatesViewShown(IsWindowed);
            mainHandler = null;
            IsDismissed = false;
            if(KeyboardView != null &&
                KeyboardView.DC != null) {
                // NOTE this won't happen during initial create

                KeyboardContainerView.ShowKeyboard();
            }

            if(IS_IDLE_CHECKER_ENABLED) {
                StartIdleChecker();
            }
            //if(IsDismissed || AdKeyboardView == null) {
            //    return;
            //}
            //Post(async () => {
            //    // BUG on initial show some secondary keys font size is too big
            //    // i don't know why, this is to try to refresh it cause
            //    // redrawing the keys corrects it
            //    //await Task.Delay(500); 
            //    ResetTextInfo();
            //    await Task.Delay(1_000);
            //    if (!_hasWindowShown) {
            //        AdKeyboardView.AdKeyGridView.RedrawKeys();
            //    }
            //    _hasWindowShown = true;
            //});

            //StartIdleChecker();
        }
        public override void OnWindowHidden() {
            base.OnWindowHidden();
            IsDismissed = true;
            if(IS_IDLE_CHECKER_ENABLED) {
                StopIdleChecker();
            }
            OnDismissed?.Invoke(this, EventArgs.Empty);
        }

        public override void OnDestroy() {
            if(KeyboardView != null) {
                KeyboardView.Unload();
            }
            if(KeyboardContainerView != null) {
                KeyboardContainerView.RemoveAllViews();
                KeyboardContainerView = null;
            }

            base.OnDestroy();

        }
        #endregion

        public void ResetService() {
            Post(() => {
                MpConsole.WriteLine($"Reseting service...");
                if(KeyboardView != null) {
                    KeyboardView.SetOnTouchListener(null);
                }

                mainHandler = null;
                //if (IsWindowed) {
                //    this.SetCandidatesView(OnCreateCandidatesView());
                //    //this.SetInputView(null);
                //} else {
                //    this.SetInputView(OnCreateInputView());
                //    // this.SetCandidatesView(null);
                //}
                this.SetInputView(OnCreateInputView());

            });
        }
        public void SetInnerEditText(AdEmojiSearchEditText et) {
            InnerEditText = et;
            ResetTextInfo();
            if(InnerEditText is AdEmojiSearchEditText cet) {
                cet.OnSelChanged += Cet_OnSelChanged;
            }
        }

        private void Cet_OnSelChanged(object sender, (int selectionStart, int selectionEnd) e) {
            UpdateSelection(e.selectionStart, e.selectionEnd);
        }

        public void ClearInnerEditText() {
            if(InnerEditText is not AdEmojiSearchEditText cet) {
                return;
            }
            cet.OnSelChanged -= Cet_OnSelChanged;
            InnerEditText = null;
            ResetTextInfo();
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        #region Idle Checker

        private void MyInputMethodService_OnInputIdle(object sender, EventArgs e) {
            ResetTextInfo();
        }

        void StartIdleChecker() {
            if(IdleCheckerCt != null && IsIdleCheckerRunning) {
                return;
            }
            IdleCheckerCt?.Cancel();
            IdleCheckerCt?.Dispose();
            IdleCheckerCt = new CancellationTokenSource();
            RunIdleChecker(IdleCheckerCt.Token);
        }
        void StopIdleChecker() {
            IdleCheckerCt?.Cancel();
            IdleCheckerCt?.Dispose();
            IdleCheckerCt = null;
        }
        void RunIdleChecker(CancellationToken ct) {
            if(IsIdleCheckerRunning) {
                return;
            }
            IsIdleCheckerRunning = true;
            DateTime? last_idled_cursor_change_dt = null;
            Task.Run(async () => {
                while(true) {
                    if(ct.IsCancellationRequested) {
                        IsIdleCheckerRunning = false;
                        return;
                    }
                    if(this.Window is { } w &&
                        w.IsShowing &&
                        LastCursorChangeDt is { } lcc &&
                        lcc != last_idled_cursor_change_dt &&
                        (lcc == null || DateTime.Now - lcc >= TimeSpan.FromSeconds(5))) {
                        // NOTE cursor reset will trigger itself but its better to keep CurTextInfo in sync than not?
                        last_idled_cursor_change_dt = lcc;
                        Post(() => {
                            MpConsole.WriteLine($"Idle input detected", level: MpLogLevel.Verbose);
                            ResetTextInfo();
                        });
                    }
                    await Task.Delay(300);
                }
            });
        }
        #endregion

        #region Life Cycle

        void ResetState() {
            // NOTE hardReset is after orientation change so if floating popup window
            // is dismissed/re-shown cause it leaves a fake one (probably a bug I'm doing)
            bool is_reset = false;
            if(KeyboardView != null) {
                KeyboardView.ContainerOffsetY = 0;
                is_reset = KeyboardView.DC != null;
            }
            _isHwAccelEnabled = null;
            ResetTextInfo();
            Init();
            if(!is_reset) {
                return;
            }
            // ntf flags probably changed
            KeyboardView.DC.Init(Flags);
            KeyboardContainerView.ShowKeyboard();
            InitShadowChecker();
        }
        void InitShadowChecker() {
            if(KeyboardView == null || KeyboardView.DC == null) {
                return;
            }
            if(KeyboardView.DC.IsDynamicShadowsEnabled && !IsCheckingAccelerometer) {
                StartAccelerometerChecker();
            } else if(!KeyboardView.DC.IsDynamicShadowsEnabled && IsCheckingAccelerometer) {
                StopAccelerometerChecker();
            }
        }
        View CreateAdKeyboard() {
            if(KeyboardView is { } existing_kbv &&
                existing_kbv.DC is { } kbvm) {
                // clear current event handlers
                kbvm.SetInputConnection(null);
            }
            KeyboardContainerView = new AdInputContainerView(this);

            Post(() => {
                KeyboardContainerView.Init(this);
                KeyboardContainerView.ShowKeyboard();
                InitShadowChecker();
            });

            //return KeyboardContainerView.RootInputView;
            return KeyboardContainerView;
            //return KeyboardContainerView;
            //var test = new AdCustomView(this, new Paint()).SetDefaultProps();
            //test.Frame = new RectF(0, 300, 1080, 1500);
            //test.SetBackgroundColor(Color.Blue);
            //return test;
        }

        View CreateAvKeyboard() {
            //try {
            //    var kb_size = KeyboardViewModel.GetTotalSizeByScreenSize(AdDeviceInfo.ScaledSize, Flags.HasFlag(KeyboardFlags.Portrait));
            //    AvView = new AvaloniaView(this) {
            //        Focusable = false,
            //        //Content = KeyboardFactory.CreateKeyboardView(this, kb_size, AdDeviceInfo.Scaling, out var unscaledSize)
            //    };
            //    Size unscaledSize = default;
            //    var cntr2 = (LinearLayout)LayoutInflater.Inflate(Resource.Layout.keyboard_layout_view, null);
            //    cntr2.AddView(AvView);
            //    cntr2.Focusable = false;
            //    var cntr = new KeyboardLinearLayout(this, (int)unscaledSize.Height);
            //    cntr.AddView(cntr2);
            //    return cntr;
            //}
            //catch(Exception ex) {
            //    MpConsole.WriteLine(ex.ToString());
            //}
            return null;
        }
        bool? _isTablet;
        bool IsTablet() {
            if(_isTablet is not { } isTablet) {
                if(PrefManager is { } pm) {
                    isTablet = pm.GetPrefValue<bool>(PrefKeys.IS_TABLET);
                    _isTablet = isTablet;
                } else {
                    isTablet = false;
                }
            }
            return isTablet;
        }
        void Init() {
            if(PrefManager == null) {
                PrefManager = new SharedPrefWrapper();
                PrefManager.Init(new AdPrefService(this, PrefManager), this, IsTablet());//Resources.GetBoolean(Resource.Boolean.isTablet));
            }
            // if(CbListener == null) {
            //     CbListener = new MpAdKbClipboardListener(this);
            // }
            // if(ProcWatcher == null) {
            //     ProcWatcher = new MpAdKbProcessWatcher(this);
            // }

            AdDeviceInfo.Init(this);
        }
        #endregion

        #region Text Info
        void ClearTextInfo() {
            _CurTextInfo = null;
        }
        void ResetTextInfo() {
            Post(() => {
                SelectableTextRange lastTextInfo = _CurTextInfo == null ? null : _CurTextInfo.Clone();
                _CurTextInfo = null;
                _ = CurTextInfo;
                //if(CurTextInfo.IsValueEqual(lastTextInfo)) {
                // MpConsole.WriteLine($"Text info UNCHANGED");
                //}
                //MpConsole.WriteLine($"Text info reset");
                UpdateSelection(CurTextInfo.SelectionStartIdx, CurTextInfo.SelectionEndIdx);
            });

        }
        void UpdateSelection(int sidx, int eidx) {
            //Post(() => {
            var info = CurTextInfo;
            int newSelStart = sidx;
            int newSelEnd = eidx;
            info.Select(newSelStart, newSelEnd - newSelStart);

            LastCursorChangeDt = DateTime.Now;

            OnCursorChanged?.Invoke(this, info);
            //});
        }

        SelectableTextRange GetTextInfoFromConnection() {
            if(HasInnerInput) {
                return new SelectableTextRange(InnerEditText.Text, InnerEditText.SelectionStart, InnerEditText.SelectionEnd);
            }

            if(this.CurrentInputConnection == null) {
                // need to wait for cursor change to get actual text location (i think)
                return new();
            }
            var tup = GetAllText2();
            var result = new SelectableTextRange(tup.Item1, tup.sidx, tup.len);
            return result;
        }

        (string, int sidx, int len) GetAllText2() {
            var req = new ExtractedTextRequest();
            ExtractedText test = this.CurrentInputConnection.GetExtractedText(req, GetTextFlags.WithStyles);
            if(test == null) {
                return GetAllText();
            }
            var sidx = Math.Min(test.SelectionStart, test.SelectionEnd);
            var eidx = Math.Max(test.SelectionStart, test.SelectionEnd);
            return (test.Text.ToString(), sidx, eidx);
        }
        (string, int sidx, int len) GetAllText() {
            string leading = string.Empty;
            string sel = this.CurrentInputConnection.GetSelectedText(0) ?? string.Empty;
            string trailing = string.Empty;
            string Read(bool isBefore) {
                string last_text = null;
                int step = 10;
                int cur_count = 10;
                int dir = 1;
                while(true) {
                    string cur_text = isBefore ?
                        CurrentInputConnection.GetTextBeforeCursor(cur_count, 0) :
                        CurrentInputConnection.GetTextAfterCursor(cur_count, 0);
                    if(cur_text == null ||
                        cur_text == last_text) {
                        if(dir == 1) {
                            dir = -1;
                        } else {
                            // NOTE when n is before beginning of text it'll just return as far as it can go
                            break;
                        }
                    } else {

                        last_text = cur_text;
                    }
                    if(dir == 1) {
                        cur_count *= step;
                    } else {
                        cur_count--;
                    }
                }
                return last_text ?? string.Empty;
            }
            leading = Read(true);
            trailing = Read(false);
            return (leading + sel + trailing, leading.Length, sel.Length);
        }
        #endregion

        #region Accelerometer
        void StartAccelerometerChecker() {
            IsCheckingAccelerometer = true;
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            Accelerometer.Start(SensorSpeed.UI);
        }
        void StopAccelerometerChecker() {
            Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            Accelerometer.Stop();
            IsCheckingAccelerometer = false;
        }

        void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e) {
            var data = e.Reading;

            var roll = Math.Atan(data.Acceleration.Y / Math.Sqrt(Math.Pow(data.Acceleration.X, 2.0) + Math.Pow(data.Acceleration.Z, 2.0)));
            var pitch = Math.Atan(data.Acceleration.X / Math.Sqrt(Math.Pow(data.Acceleration.Y, 2.0) + Math.Pow(data.Acceleration.Z, 2.0)));
            var yaw = Math.Atan(Math.Sqrt(Math.Pow(data.Acceleration.X, 2.0) + Math.Pow(data.Acceleration.Z, 2.0)) / data.Acceleration.Z);

            OnDeviceMove?.Invoke(this, (roll, pitch, yaw));
            //OnDeviceMove?.Invoke(this, (data.Acceleration.X,data.Acceleration.Y,data.Acceleration.Z));
            //MpConsole.WriteLine("roll: " + roll.ToString() + ", pitch: " + pitch.ToString() + "yaw: " + yaw.ToString());
        }
        #endregion

        #region Helpers

        KeyboardFlags GetFlags(EditorInfo info) {
            var kbf = KeyboardFlags.Android;
            if(IS_PLATFORM_MODE) {
                kbf |= KeyboardFlags.PlatformView;
            }

            kbf |= IsPortrait() ? KeyboardFlags.Portrait : KeyboardFlags.Landscape;

            if(info != null) {
                int test34 = (int)info.InputType;
                var class_type = info.InputType & InputTypes.MaskClass;

                var var_type = info.InputType & InputTypes.MaskVariation;
                var flag_types = info.InputType & InputTypes.MaskFlags;
                switch(class_type) {
                    case InputTypes.Null:
                    case InputTypes.ClassText:
                        switch(var_type) {
                            case InputTypes.TextVariationUri:
                                kbf |= KeyboardFlags.Url;
                                break;
                            case InputTypes.TextVariationEmailAddress:
                            case InputTypes.TextVariationWebEmailAddress:
                                kbf |= KeyboardFlags.Email;
                                break;
                            default:
                                kbf |= KeyboardFlags.Normal;
                                break;
                        }
                        break;
                    case InputTypes.ClassNumber:
                        switch(var_type) {
                            case InputTypes.NumberVariationPassword:
                                kbf |= KeyboardFlags.Pin;
                                break;
                            default:
                                if(flag_types.HasFlag(InputTypes.NumberFlagDecimal)) {
                                    kbf |= KeyboardFlags.Numbers;
                                } else {
                                    kbf |= KeyboardFlags.Digits;
                                }
                                break;
                        }
                        break;
                    case InputTypes.ClassPhone:
                        kbf |= KeyboardFlags.Digits;
                        break;
                    case InputTypes.ClassDatetime:
                        kbf |= KeyboardFlags.Numbers;
                        break;
                    default:
                        kbf |= KeyboardFlags.Normal;
                        break;
                }

                if(
                    var_type.HasFlag(InputTypes.TextVariationPassword) ||
                    var_type.HasFlag(InputTypes.NumberVariationPassword) ||
                    var_type.HasFlag(InputTypes.TextVariationVisiblePassword) ||
                    var_type.HasFlag(InputTypes.TextVariationWebPassword)) {
                    kbf |= KeyboardFlags.Password;
                }
                if(flag_types.HasFlag(InputTypes.TextFlagImeMultiLine) ||
                    flag_types.HasFlag(InputTypes.TextFlagMultiLine)) {
                    kbf |= KeyboardFlags.MultiLine;
                }

                ImeAction doneAction = GetDoneAction(info);
                switch(doneAction) {
                    case ImeAction.Done:
                        kbf |= KeyboardFlags.Done;
                        break;
                    case ImeAction.Go:
                        kbf |= KeyboardFlags.Go;
                        break;
                    case ImeAction.Previous:
                        kbf |= KeyboardFlags.Previous;
                        break;
                    case ImeAction.Next:
                        kbf |= KeyboardFlags.Next;
                        break;
                    case ImeAction.Search:
                        kbf |= KeyboardFlags.Search;
                        break;
                    case ImeAction.Send:
                        kbf |= KeyboardFlags.Send;
                        break;
                    case ImeAction.None:
                        // this is the case in chrome url 
                        if(kbf.HasFlag(KeyboardFlags.Url)) {
                            kbf |= KeyboardFlags.Go;
                        } else if(kbf.HasFlag(KeyboardFlags.Email)) {
                            kbf |= KeyboardFlags.Send;
                        }
                        break;
                }
            }

            if(GetSystemService(Context.UiModeService) is UiModeManager uimm) {
                kbf |= uimm.NightMode == UiNightMode.Yes ? KeyboardFlags.Dark : KeyboardFlags.Light;
            }

            kbf |= IsTablet() ?
                 KeyboardFlags.Tablet :
                 KeyboardFlags.Mobile;

            // does android have floating keyboard? 
            kbf |= KeyboardFlags.FullLayout;

            if(IsWindowed) {
                kbf |= KeyboardFlags.FloatLayout;
            }

            if(PrefManager == null) {
                Init();
            }

            kbf = PrefManager.UpdateFlags(kbf);
            _lastFlags = kbf;
            return kbf;
        }
        void Vibrate(double? forceLevel = null) {
            int level = (int)(forceLevel.HasValue ? forceLevel.Value : PrefManager.VibrateDurMs);
            level = ((level - 2) * 10) + 1;
            if(level <= 0) {
                return;
            }
            if(_vibrator == null) {
                if((int)Build.VERSION.SdkInt >= 31) {
#pragma warning disable CA1416 // Validate platform compatibility
                    var vm = this.GetSystemService(VibratorManagerService) as VibratorManager;
                    _vibrator = vm.DefaultVibrator;
                } else {
#pragma warning disable CA1422 // Validate platform compatibility
                    _vibrator = this.GetSystemService(VibratorService) as Vibrator;
#pragma warning restore CA1422 // Validate platform compatibility
                }
            }
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            if(_lastVibrateLevel != level) {
                _lastVibrateLevel = level;
                _vibrateEffect = VibrationEffect.CreateOneShot((int)level, VibrationEffect.DefaultAmplitude);
            }
            if(level <= 0) {
                return;
            }
            _vibrator.Vibrate(_vibrateEffect);
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning restore IDE0059 // Unnecessary assignment of a value

        }
        void PlaySound(SoundEffect sound, double? forceLevel = null) {
            double level = forceLevel.HasValue ? forceLevel.Value : PrefManager.SoundVol;
            if(level <= 0) {
                return;
            }
            if(_audioManager == null &&
                GetSystemService(AudioService) is AudioManager am) {
                _audioManager = am;
            }
            int min = _audioManager.GetStreamMinVolume(GStream.Notification);
            int max = _audioManager.GetStreamMaxVolume(GStream.Notification);
            float volume = (float)(((max - min) * (level / 100)) + min);
            _audioManager.PlaySoundEffect(sound, volume);
            //MpConsole.WriteLine($"Volume: {volume} Min: {min} Max: {max}");
        }
        ImeAction GetDoneAction(EditorInfo info) {
            if(((int)info.ImeOptions & (int)ImeFlags.NoEnterAction) != 0) {
                return ImeAction.None;
            }
            return (ImeAction)((int)info.ImeOptions & (int)ImeAction.ImeMaskAction);
        }

        public void StartPrefActivity() {
            Intent prefIntent = new Intent(this, typeof(AdSettingsActivity));
            prefIntent.AddFlags(ActivityFlags.NewTask);

            // from https://stackoverflow.com/a/37774966/105028
            var b = new Bundle();
            b.PutBinder(PREF_BUNDLE_KEY, new FragmentDataBinder<SharedPrefWrapper>(PrefManager));
            prefIntent.PutExtras(b);

            StartActivity(prefIntent);
        }

        bool IsPortrait() {
            return Resources.Configuration.Orientation == Orientation.Portrait;
        }

        #endregion

        #endregion

    }
}
