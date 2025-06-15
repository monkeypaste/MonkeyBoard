using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MonkeyBoard.Bridge;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public interface MpIKbHostTools {
        bool ShowKeyboard();
        bool HideKeyboard();
    }
    public interface IThisAppVersionInfo {
        string Version { get; }
    }
    public interface IAnimationTimer {
        void Start(object key, Action action);
        void Stop(object key);
    }
    public interface IPlatformSharedPref {
        bool CanWrite { get; }
        object GetPlatformPrefValue(PrefKeys prefKey);
        void SetPlatformPrefValue(PrefKeys prefKey, object newValObj);
    }
    public interface IMainThread {
        void Post(Action action);
    }
    public interface IAssetLoader {
        Stream LoadStream(string path);
    }
    public interface ISharedPrefService {
        //Task RestoreDefaults();
        void RestoreDefaults();
        T GetPrefValue<T>(PrefKeys prefKey);
        void SetPrefValue<T>(PrefKeys prefKey, T newValue);
        event EventHandler<PreferencesChangedEventArgs> PreferencesChanged;
    }
    public interface IInertiaScroll {
        IFrameRenderer Renderer { get; }
        bool CanScroll { get; }
    }

    public interface IKeyboardMenuTabItem {
        object IconSourceObj { get; }
        bool IsSelected { get; }
        bool IsPressed { get; set; }
        Rect TabItemRect { get; }
        Rect IconRect { get; }
        MenuTabItemType TabItemType { get; }
    }
    public interface ITextTools {
        bool CanRender(string text);
        Size MeasureText(string text, double scaledFontSize, out double ascent, out double descent);
    }
    public interface ISpeechToTextConnection {
        void StartListening();
        void StopListening();
        event EventHandler OnReady;
        event EventHandler OnSpeechBegin;
        event EventHandler<string> OnPartialText;
        event EventHandler<string> OnText;
        event EventHandler OnSpeechEnd;
        event EventHandler<double> OnVolumeChanged;
        event EventHandler<string> OnError;

    }
    public interface ITranslatePoint {
        Point TranslatePoint(Point p);
    }
    public interface IPreviewFeedback {
        void Vibrate(double normalizedLevel);
        void PlaySound(double normalizedLevel);
    }

    public interface ISetInputConnectionSource {
        void SetKeyboardInputSource(TextBox sourceControl);
    }
    public interface IKeyboardInputConnection {
        event EventHandler<(double roll, double pitch, double yaw)> OnDeviceMove;
        event EventHandler<SelectableTextRange> OnCursorChanged;
        event EventHandler OnDismissed;
        SelectableTextRange OnTextRangeInfoRequest();
        void OnReplaceText(int sidx, int eidx, string text);
        void OnSelectText(int sidx, int eidx);
        void OnSelectAll();
        void OnText(string text, bool forceInput = false);
        void OnBackspace(int count);
        void OnDone();
        void OnNavigate(int dx, int dy);
        void OnFeedback(KeyboardFeedbackFlags flags);
        void OnShowPreferences(object args);
        void OnShiftChanged(ShiftStateType newShiftState);
        void OnCollapse(bool isHold);
        void OnToggleFloatingWindow();
        void OnLog(string text, bool freeze = false);
        void OnAlert(string title, string detail);
        Task<bool> OnConfirmAlertAsync(string title, string detail);
        Size ScaledScreenSize { get; }
        Size MaxScaledSize { get; }
        Rect ScaledWorkAreaRect { get; }
        bool IsReceivingCursorUpdates { get; }
        bool NeedsInputModeSwitchKey { get; }
        void OnInputModeSwitched();
        IAnimationTimer AnimationTimer { get; }
        KeyboardFlags Flags { get; }
        IPreviewFeedback FeedbackHandler { get; }
        IThisAppVersionInfo VersionInfo { get; }
        ITextTools TextTools { get; }
        ISharedPrefService SharedPrefService { get; }
        IAssetLoader AssetLoader { get; }
        IKbWordUpdater WordUpdater { get; }
        IMainThread MainThread { get; }
        ISpeechToTextConnection SpeechToTextService { get; }
        IStoragePathHelper StoragePathHelper { get; }
        IKbClipboardHelper ClipboardHelper { get; }
        string SourceId { get; }
        Task<MpAvKbBridgeMessageBase> GetMessageResponseAsync(MpAvKbBridgeMessageBase request);
    }
    public interface IKbClipboardHelper {
        Task SetTextAsync(string text);
        Task<string> GetTextAsync();

    }
    public interface IKbWordUpdater {
        void AddWords(Dictionary<string, int> words, bool allowInsert);
    }
    public interface IFrameRenderer {
        bool IsDisposed { get; }
        void LayoutFrame(bool invalidate);
        void MeasureFrame(bool invalidate);
        void PaintFrame(bool invalidate);
        void RenderFrame(bool invalidate);
    }
    public interface IFrameRenderContext {
        void SetRenderContext(IFrameRenderer context);
    }
    //public interface IKeyboardInputConnection : IKeyboardInputConnection {

    //}
    public interface ITriggerTouchEvents {
        event EventHandler<TouchEventArgs> OnPointerChanged;
    }
    public interface IHeadLessRender_desktop : ITriggerTouchEvents {
        void SetRenderSource(Control sourceControl);
        void SetPointerInputSource(Control sourceControl);
    }
}
