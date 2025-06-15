using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
//using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using MonkeyPaste.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Size = Avalonia.Size;

namespace MonkeyBoard.Common {
    public class KeyboardViewModel : FrameViewModelBase {
        #region Private Variables        
        #endregion

        #region Constants

        const double TOTAL_MOBILE_KEYBOARD_SCREEN_HEIGHT_RATIO_PORTRAIT = KeyConstants.PHI / 4;
        const double TOTAL_MOBILE_KEYBOARD_SCREEN_HEIGHT_RATIO_LANDSCAPE = KeyConstants.PHI / 3d;

        const double TOTAL_TABLET_KEYBOARD_SCREEN_HEIGHT_RATIO_PORTRAIT = KeyConstants.PHI / 5d;
        const double TOTAL_TABLET_KEYBOARD_SCREEN_HEIGHT_RATIO_LANDSCAPE = KeyConstants.PHI / 2.5d;

        #endregion

        #region Statics
        public static Size? ScaledScreenSize { get; private set; }
        public static Size GetTotalSizeByScreenSize(Size scaledScreenSize, bool isPortrait, bool isTablet = false, Size maxScaledSize = default) {
            if(ScaledScreenSize == null) {
                ScaledScreenSize = scaledScreenSize;
            }
            double ratio =
                isPortrait ?
                    isTablet ?
                        TOTAL_TABLET_KEYBOARD_SCREEN_HEIGHT_RATIO_PORTRAIT :
                        TOTAL_MOBILE_KEYBOARD_SCREEN_HEIGHT_RATIO_PORTRAIT :
                    isTablet ?
                        TOTAL_TABLET_KEYBOARD_SCREEN_HEIGHT_RATIO_LANDSCAPE :
                        TOTAL_MOBILE_KEYBOARD_SCREEN_HEIGHT_RATIO_LANDSCAPE;

            //if (maxScaledSize is { } mss && mss.Width != 0 && mss.Height != 0) {
            //    double w = Math.Min(scaledScreenSize.Width, mss.Width);
            //    double h = Math.Min(scaledScreenSize.Height, mss.Height);
            //    var clampedScreenSize = new Size(w, h);
            //    if(clampedScreenSize != maxScaledSize && clampedScreenSize.Width < scaledScreenSize.Width && clampedScreenSize.Height < scaledScreenSize.Height) {
            //        // when ios tablet is floating no ratio
            //        ratio = 1;
            //    }
            //    scaledScreenSize = clampedScreenSize;
            //}

            //MpConsole.WriteLine($"Tablet: {isTablet} Ratio: {ratio}");
            return new Size(scaledScreenSize.Width, scaledScreenSize.Height * ratio);
        }

        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            Keys.ForEach(x => x.Renderer.LayoutFrame(invalidate));
            this.RaisePropertyChanged(nameof(KeyboardWidth));
            this.RaisePropertyChanged(nameof(KeyGridHeight));
            this.RaisePropertyChanged(nameof(TotalWidth));
            this.RaisePropertyChanged(nameof(TotalHeight));
            this.RaisePropertyChanged(nameof(MenuHeight));
        }

        public override void MeasureFrame(bool invalidate) {
            Keys.ForEach(x => x.Renderer.MeasureFrame(invalidate));
            this.RaisePropertyChanged(nameof(NeedsNextKeyboardButton));
        }

        public override void PaintFrame(bool invalidate) {
            Keys.ForEach(x => x.Renderer.PaintFrame(invalidate));

            this.RaisePropertyChanged(nameof(ErrorText));
        }
        public override void RenderFrame(bool invalidate) {
            Keys.ForEach(x => x.Renderer.RenderFrame(invalidate));
            Renderer.LayoutFrame(false);
            Renderer.MeasureFrame(false);
            Renderer.PaintFrame(invalidate);
        }
        #endregion

        #endregion

        #region Properties

        #region Members
        public KeyboardFlags KeyboardFlags { get; set; }
        KeyboardFlags LastInitializedFlags { get; set; }
        public new IKeyboardInputConnection InputConnection { get; private set; }
        public override IFrameRenderer Renderer =>
            IsFloatingLayout ? FloatContainerViewModel.Renderer : _renderer ?? this;
        #endregion

        #region View Models
        public FrameViewModelBase VisiblePageViewModel =>
            MenuViewModel.SpeechPageViewModel.IsVisible ?
                MenuViewModel.SpeechPageViewModel :
                MenuViewModel.EmojiPagesViewModel.IsVisible ?
                    MenuViewModel.EmojiPagesViewModel :
                    this;
        public FloatContainerViewModel FloatContainerViewModel { get; private set; }
        public FooterViewModel FooterViewModel { get; private set; }
        public CursorControlViewModel CursorControlViewModel { get; private set; }
        public MenuViewModel MenuViewModel { get; private set; }
        public ObservableCollection<KeyViewModel> Keys { get; set; } = [];
        public List<List<KeyViewModel>> Rows { get; set; } = [];
        public IEnumerable<KeyViewModel> KeyboardKeys =>
            Keys.Where(x => !x.IsPopupKey);
        public IEnumerable<KeyViewModel> VisibleKeyboardKeys =>
            Keys.Where(x => x.IsVisibleKeyboardKey);
        public KeyViewModel[] PopupKeys { get; private set; }
        public List<KeyViewModel> VisiblePopupKeys { get; private set; } = [];
        public List<KeyViewModel> PressedKeys { get; set; } = [];
        public KeyViewModel PrimaryKey { get; private set; }
        public KeyViewModel EmojiKey { get; private set; }
        public KeyViewModel SpacebarKey { get; private set; }
        public KeyViewModel BackspaceKey { get; private set; }
        public KeyViewModel PeriodKey { get; private set; }
        public KeyViewModel DotComKey { get; private set; }
        public KeyViewModel LastPressedKey { get; set; }
        public KeyViewModel LastReleasedKey { get; set; }
        public List<KeyViewModel> ShiftKeys { get; set; } = [];
        public List<KeyViewModel> TabKeys { get; set; } = [];
        #endregion

        #region Layout

        public double FloatEmojiScale =>
             CanInitiateFloatLayout ? FloatFontScale : 0.33;
        public double FloatFontScale =>
            Math.Max(CanInitiateFloatLayout ? 0 : 1, (FloatContainerViewModel.FloatScale.X + FloatContainerViewModel.FloatScale.Y) / 2d);
        public Size TotalDockedSize { get; private set; }

        double PlatformPadRatio =>
            OperatingSystem.IsIOS() ? 1/*0.75*/ : 1;
        public double DefaultOuterPadX => KeyConstants.PHI * 3.5 * PlatformPadRatio;
        public double DefaultOuterPadY => KeyConstants.PHI * 3.5 * PlatformPadRatio;
        public override Rect Frame => TotalRect;

        Size _desiredSize;
        Size DesiredSize => _desiredSize;
        public Thickness KeyboardMargin { get; set; } = new Thickness(0);
        public Rect TotalRect { get; private set; } = new();
        public Rect MenuRect { get; private set; } = new();
        public Rect KeyGridRect { get; private set; } = new();
        public int MaxColCount { get; private set; } = 1;
        public int RowCount { get; private set; }
        public int VisibleRowCount {
            get {
                if(IsNumPadLayout || IsShowNumberRowEnabled) {
                    return RowCount;
                }
                return RowCount - 1;
            }
        }
        public double NumberRowHeightRatio => IsNumPadLayout ? 1 : 1 - MenuHeightRatio;

        public int MaxPopupColCountFixed =>
            4;
        public int MaxPopupRowCount { get; private set; }
        double MenuHeightRatio => KeyConstants.PHI / 11d;
        public double MenuHeight { get; private set; }
        public double FooterHeight { get; private set; }

        public double KeyboardWidth { get; private set; }
        public double KeyGridHeight { get; private set; }
        public double TotalWidth =>
            KeyboardMargin.Left + KeyboardMargin.Right +
            KeyboardWidth;
        public double TotalHeight =>
            KeyboardMargin.Top + KeyboardMargin.Bottom +
            MenuHeight +
            KeyGridHeight +
            FooterHeight;
        public double TotalDockedHeight { get; private set; }

        #endregion

        #region Appearance
        public string ShadowHex =>
            KeyboardPalette.P[PaletteColorType.KeyShadowBg];
        byte BgAlpha { get; set; } = 255;
        byte FgAlpha { get; set; } = 255;
        byte FgBgAlpha { get; set; } = 255;

        public bool IsKeyGridVisible =>
            //IsVisible &&
            //MenuViewModel.SelectedTabItemType == MenuTabItemType.None ||
            MenuViewModel != null &&
            MenuViewModel.EmojiPagesViewModel != null &&
            MenuViewModel.SpeechPageViewModel != null &&
            !MenuViewModel.EmojiPagesViewModel.IsVisible &&
            !MenuViewModel.SpeechPageViewModel.IsVisible //&&
                                                         //MenuViewModel.EmojiPagesViewModel.EmojiSearchViewModel.IsVisible
            ;

        public string KeyGridBgHexColor => KeyboardPalette.P[PaletteColorType.Bg];

        public CornerRadius CommonCornerRadius { get; private set; } = new CornerRadius(SharedPrefWrapper.DEF_CORNER_RADIUS);

        #endregion

        #region State
        public override bool IsVisible =>
            !FloatContainerViewModel.IsScaling;
        public bool CanInitiateFloatLayout =>
            OperatingSystem.IsAndroid();
        public List<(string, Point)> InputReleaseHistory { get; } = [];
        public string LastSpaceReplacedText { get; private set; }
        //public int TranslatedRow { get; set; }
        public bool IsDirty { get; set; } = true;

        bool CanAutoCap =>
            !IsPassword &&
            !IsNumPadLayout &&
            !IsUrlLayout &&
            !IsEmailLayout &&
            (IsMobile || ShiftKeys.All(x => !x.IsPressed));
        bool IsCursorChangeManuallySuppressed { get; set; }
        bool WasCursorChangedWhileSuppressed { get; set; }
        public bool IsDoubleTapEnabled { get; private set; } = true;
        public bool CanShowPopupWindows =>
            OperatingSystem.IsAndroid();
        public bool IsLettersCharSet =>
            CharSet == CharSetType.Letters;
        public bool IsSymbols1CharSet =>
            CharSet == CharSetType.Symbols1;
        public bool IsSymbols2CharSet =>
            CharSet == CharSetType.Symbols2;
        public bool IsAnySymbolsCharSet =>
            IsSymbols1CharSet || IsSymbols2CharSet;
        public bool IsNumbers1CharSet =>
            CharSet == CharSetType.Numbers1;
        public bool IsNumbers2CharSet =>
            CharSet == CharSetType.Numbers2;
        public bool IsAnyNumbersCharSet =>
            IsNumbers1CharSet || IsNumbers2CharSet;
        public bool IsBusy { get; set; }

        public bool IsInitialized =>
            InputConnection != null && InputConnection.Flags == LastInitializedFlags;
        public KeyboardFeedbackFlags FeedbackCursorChange { get; private set; }
        public KeyboardFeedbackFlags FeedbackClick { get; private set; }
        public KeyboardFeedbackFlags FeedbackReturn { get; private set; }
        public KeyboardFeedbackFlags FeedbackDelete { get; private set; }
        public KeyboardFeedbackFlags FeedbackSpace { get; private set; }
        public KeyboardFeedbackFlags FeedbackInvalid { get; private set; }
        string LastInput { get; set; } = string.Empty;
        public SelectableTextRange LastTextInfo { get; private set; }
        public SelectableTextRange CurTextInfo { get; private set; }
        string LastUpdateWordText { get; set; }
        public bool IsAnyShiftState =>
            ShiftState != ShiftStateType.None;
        public bool IsShiftOnTemp =>
            ShiftState == ShiftStateType.Shift;
        public bool IsShiftOnLock =>
            ShiftState == ShiftStateType.ShiftLock;
        public bool IsPullEnabled =>
            IsTablet;
        public bool IsSlideEnabled =>
            IsMobile;
        public bool? HasLeadingText { get; private set; }
        bool IsAutoCapEnabled { get; set; }
        bool IsHeadlessMode =>
            InputConnection is IKeyboardInputConnection;
        public double ScreenScaling { get; private set; }
        public double ActualScaling { get; private set; }
        public string ErrorText { get; private set; } = "NO ERRORS";
        public bool NeedsNextKeyboardButton =>
            (OperatingSystem.IsIOS() &&
            InputConnection != null &&
            (InputConnection as IKeyboardInputConnection).NeedsInputModeSwitchKey);

        bool IsAnyPopupMenuVisible =>
            VisiblePopupKeys.Any();
        bool IsHoldMenuVisible =>
            VisiblePopupKeys.Skip(1).Any();

        public bool CanAutoCorrect =>
            IsAutoCorrectEnabled &&
            !IsEmailLayout &&
            !IsUrlLayout &&
            !IsNumPadLayout;

        private CharSetType _charSet;
        public CharSetType CharSet => _charSet;
        public int CharSetIdx {
            get {
                switch(CharSet) {
                    case CharSetType.Letters:
                        return 0;
                    case CharSetType.Symbols1:
                        return 1;
                    case CharSetType.Symbols2:
                        return 2;
                    case CharSetType.Numbers1:
                        return 0;
                    case CharSetType.Numbers2:
                        return 1;
                }

                return 0;
            }
        }
        private ShiftStateType _shiftState;
        public ShiftStateType ShiftState => _shiftState;
        ITriggerTouchEvents HeadlessRender =>
            InputConnection as ITriggerTouchEvents;

        #region Hold/Tap Stuff
        int HoldDelayMs => 1;
        public int MinDelayForHoldPopupMs { get; set; } = 500;

        public int MaxDoubleTapSpaceForPeriodMs { get; set; } = 150;
        int MaxCursorChangeFromEventDelayForFeedbackMs => 50;

        #region Backspace
        public bool IsBackspaceRepeatEnabled { get; set; }
        public int BackspaceRepeatMs { get; set; }
        public int BackspaceHoldToRepeatMs { get; set; } = 800;

        public bool IsBackspacing =>
            BackspaceKey != null && BackspaceKey.IsPressed;
        #endregion

        #region Cursor Control

        #endregion

        #endregion

        #endregion

        #region Model
        public List<List<string>> LetterGroups { get; private set; }
        public bool IsTextLayout =>
            !IsNumPadLayout;
        public bool IsPin { get; private set; }
        public bool IsDigits { get; private set; }
        public bool IsNumbers { get; private set; }
        public bool IsPassword { get; private set; }
        public bool IsNumPadLayout { get; private set; }
        public bool IsEmailLayout { get; private set; }
        public bool IsUrlLayout { get; private set; }
        public bool IsFreeTextLayout { get; private set; }
        public bool IsSmartPunctuationEnabled { get; private set; }
        public bool IsShadowsEnabled { get; private set; }
        public bool IsDynamicShadowsEnabled { get; private set; }
        public Point ShadowOffset { get; private set; } = new();
        public bool IsThemeDark { get; private set; }
        public KeyboardThemeType ThemeType { get; private set; }
        public bool IsTablet { get; private set; }
        public bool IsPortrait { get; private set; }
        public bool IsFloatingLayout { get; private set; }
        public bool IsLandscape { get; private set; }
        public bool IsMobile { get; private set; }
        public bool IsShowNumberRowEnabled { get; private set; }
        public bool IsHorizontalOrientation { get; private set; }
        public bool IsVerticalOrientation =>
            !IsHorizontalOrientation;
        public bool IsKeyBordersVisible { get; private set; }
        public bool IsEmojiKeyVisible { get; private set; }
        public bool IsPressPopupsEnabled { get; private set; }
        public bool IsHoldPopupsEnabled { get; private set; }
        public bool IsTabsAsSpacesEnabled { get; private set; }
        public int TabSpaceCount { get; private set; }
        public bool IsSmartQuotesEnabled { get; private set; }
        public bool IsExtendedSmartQuotesEnabled { get; private set; }
        public bool IsAutoCapitalizationEnabled { get; private set; }
        public bool IsDoubleTapSpaceEnabled { get; private set; }
        public bool IsNextWordCompletionEnabled { get; private set; }
        public bool IsCompletionDictUpdateEnabled { get; private set; }
        public bool IsShowLetterGroupPopupsEnabled { get; private set; }
        public bool IsCompletionDictUpdateForPwdEnabled { get; private set; }
        public bool IsAutoCorrectEnabled { get; private set; }
        public bool IsBackspaceUndoLastAutoCorrectEnabled { get; private set; }
        public bool IsConfirmIgnoreAutoCorrectEnabled { get; private set; }
        public bool IsShowEmojiTextCompletionEnabled { get; private set; }
        public bool IsLoggingEnabled { get; private set; }
        public MpLogLevel CurrentLogLevel { get; private set; }
        public bool IsSwipeLeftDeleteEnabled { get; private set; }
        public int MaxTextCompletionResults { get; private set; }
        public int MaxEmojiCompletionResults { get; private set; }
        public bool IsMultiLineInput { get; private set; }

        public bool IsDoubleTapCursorControlEnabled { get; private set; }

        bool _canCursorControlBeEnabled;
        public bool CanCursorControlBeEnabled {
            get => MenuViewModel.EmojiPagesViewModel.EmojiSearchViewModel.IsVisible ? false : _canCursorControlBeEnabled;
            set => _canCursorControlBeEnabled = value;
        }
        public double CursorControlSensitivity { get; private set; }
        public EmojiSkinToneType DefaultSkinToneType { get; set; }
        public EmojiHairStyleType DefaultHairStyleType { get; set; }

        string CustomBgImagePath { get; set; }
        string CustomBgHexColor { get; set; }
        #endregion

        #endregion

        #region Events

        public event EventHandler<KeyViewModel> OnShowPopup;
        public event EventHandler<KeyViewModel> OnHidePopup;

        public event EventHandler<VectorDirection> OnSwipe;

        public event EventHandler OnInputConnectionChanged;
        public event EventHandler OnKeyLayoutChanged;
        public event EventHandler OnCharSetChanged;
        public event EventHandler OnInitialized;
        public event EventHandler OnIsFloatLayoutChanged;
        #endregion

        #region Constructors
        public KeyboardViewModel() : this(null, new Size(360, 740), 2.75) { }
        public KeyboardViewModel(IKeyboardInputConnection inputConn, Size scaledSize, double scale) : this(inputConn, scaledSize, scale, scale) { }

        public KeyboardViewModel(IKeyboardInputConnection inputConn, Size scaledSize, double scale, double actualScale) {
            MpConsole.WriteLine("kbvm ctor called");
            this.SetRootViewModel(this);
            SetInputConnection(inputConn);
            FooterViewModel = new FooterViewModel(this);
            FloatContainerViewModel = new FloatContainerViewModel(this);
            MenuViewModel = new MenuViewModel(this);
            CursorControlViewModel = new CursorControlViewModel(this);
            ScreenScaling = scale;
            ActualScaling = actualScale;
            _desiredSize = scaledSize;
            OnSwipe += KeyboardViewModel_OnSwipe;
            if(inputConn is { } ic) {
                Init(ic.Flags);
            } else {
                // desktop
                Init(KeyboardFlags.Mobile | KeyboardFlags.Normal);
            }
        }


        #endregion

        #region Public Methods

        public void Init(KeyboardFlags flags) {
            try {
                bool not_dirty = flags.HasFlag(KeyboardFlags.NotDirty);
                if(not_dirty) {
                    IsDirty = false;
                }
                if(IsDirty) {
                    CultureManager.Init(InputConnection);
                    ResourceStrings.Init(CultureManager.CurrentUiCulture, CultureManager.CurrentKbCulture);
                }

                KeyboardLayoutType last_layout_type = KeyboardFlags.ToKeyboardLayoutType();
                bool last_tablet = IsTablet;
                bool last_was_email = IsEmailLayout;
                bool last_was_url = IsUrlLayout;
                bool last_emoji_key = IsEmojiKeyVisible;
                bool last_show_nums = IsShowNumberRowEnabled;
                bool last_landscape = IsLandscape;
                bool last_portrait = IsPortrait;
                bool last_floating = IsFloatingLayout;

                SetFlags(flags);

                KeyboardLayoutType new_layout_type = KeyboardFlags.ToKeyboardLayoutType();
                bool new_tablet = IsTablet;
                bool new_is_email = IsEmailLayout;
                bool new_is_url = IsUrlLayout;
                bool new_emoji_key = IsEmojiKeyVisible;
                bool new_show_nums = IsShowNumberRowEnabled;
                bool new_landscape = IsLandscape;
                bool new_portrait = IsPortrait;
                bool new_floating = IsFloatingLayout;

                bool did_tablet_change = last_tablet != new_tablet;
                bool did_floating_change = last_floating != new_floating;
                bool did_orientation_change =
                    did_floating_change ||
                    last_portrait != new_portrait ||
                    last_landscape != new_landscape;


                bool is_new_layout =
                    IsDirty ||
                    did_tablet_change ||
                    did_orientation_change ||
                    last_layout_type == KeyboardLayoutType.None ||
                    new_layout_type.IsNumpadLayout() != last_layout_type.IsNumpadLayout();

                bool did_keys_change =
                    did_tablet_change ||
                    last_was_email != new_is_email ||
                    last_was_url != new_is_url ||
                    last_emoji_key != new_emoji_key ||
                    last_show_nums != new_show_nums ||
                    is_new_layout;

                KeyboardPalette.SetTheme(
                    CustomBgImagePath,
                    CustomBgHexColor,
                    IsThemeDark,
                    BgAlpha,
                    FgAlpha,
                    FgBgAlpha);

                if(did_orientation_change || did_floating_change) {
                    UpdateOrientation(IsPortrait, IsTablet, true, false);
                }

                if(IsNumPadLayout) {
                    SetCharSet(CharSetType.Numbers1, false);
                } else {
                    SetCharSet(CharSetType.Letters, false);
                }

                if(is_new_layout) {
                    InitKeys();
                } else {
                    KeyboardLayoutFactory.ResetPrimaryKey(PrimaryKey, KeyboardFlags);
                }

                ResetState(false);
                if(did_floating_change || did_orientation_change) {
                    FloatContainerViewModel.ResetFloatProperties();
                }
                SetDesiredSize(DesiredSize, false);
                LastInitializedFlags = KeyboardFlags;
                if(is_new_layout && !not_dirty) {
                    // NOTE not dirty makes sure menu state isn't changed when
                    // emoji search resets keyboard
                    MenuViewModel.Init();
                }

                this.Renderer.RenderFrame(true);
                //MpConsole.WriteLine($"Kb init. Keys Changed: {did_keys_change} Layout Changed: {is_new_layout} Dirty: {IsDirty}");
                IsDirty = false;
                if(is_new_layout) {
                    OnKeyLayoutChanged?.Invoke(this, EventArgs.Empty);
                }
                if(did_floating_change) {
                    OnIsFloatLayoutChanged?.Invoke(this, EventArgs.Empty);
                }
                OnInitialized?.Invoke(this, EventArgs.Empty);
            }
            catch(Exception ex) {
                ex.Dump();
            }

        }
        public void TriggerKeyLayoutChange() {
            OnKeyLayoutChanged?.Invoke(this, EventArgs.Empty);
        }
        public void SetInputConnection(IKeyboardInputConnection conn) {
            if(InputConnection is IKeyboardInputConnection old_conn) {
                old_conn.OnCursorChanged -= Ic_OnCursorChanged;
                old_conn.OnDismissed -= Ic_OnDismissed;
                old_conn.OnDeviceMove -= Ic_OnDeviceMove;

                if(old_conn is ITriggerTouchEvents tte) {
                    tte.OnPointerChanged -= Ic_OnPointerChanged;
                }

                if(old_conn.SharedPrefService is { } sps) {
                    sps.PreferencesChanged -= Ic_PreferencesChanged;
                }
            }
            if(conn is IKeyboardInputConnection new_conn) {
                InputConnection = new_conn;
                new_conn.OnCursorChanged += Ic_OnCursorChanged;
                new_conn.OnDismissed += Ic_OnDismissed;
                new_conn.OnDeviceMove += Ic_OnDeviceMove;

                if(new_conn is ITriggerTouchEvents tte) {
                    tte.OnPointerChanged += Ic_OnPointerChanged;
                }

                if(new_conn.SharedPrefService is { } sps) {
                    sps.PreferencesChanged += Ic_PreferencesChanged;
                }

                if(new_conn.StoragePathHelper is { } sph) {
                    KbStorageHelpers.Init(sph);
                }
            } else {
                InputConnection = null;
            }
            OnInputConnectionChanged?.Invoke(this, EventArgs.Empty);

        }

        private void Ic_OnDeviceMove(object sender, (double roll, double pitch, double yaw) e) {
            UpdateShadows(e);
        }
        void UpdateShadows((double roll, double pitch, double yaw)? props, bool invalidate = true) {
            if(!IsShadowsEnabled) {
                ShadowOffset = new();
            } else if(props is not { } e) {
                ShadowOffset = new Point(-DefaultOuterPadX / 12, DefaultOuterPadY / 8);
            } else {
                double max_y = DefaultOuterPadY * (IsThemeDark ? 0.5 : 0.9);
                double offset_y = max_y * -e.roll;
                if(e.yaw > 0) {
                    offset_y *= -1;
                }
                double max_x = DefaultOuterPadX * (IsThemeDark ? 0.5 : 0.9);
                double offset_x = max_x * e.pitch;
                var last_offset = ShadowOffset;
                ShadowOffset = new Point(offset_x, offset_y);
                if(invalidate && ShadowOffset.Distance(last_offset) < 0.1) {
                    invalidate = false;
                }
            }

            if(IsKeyGridVisible &&
                invalidate) {
                VisibleKeyboardKeys.ForEach(x => x.Renderer.PaintFrame(true));
            }
        }

        private void Ic_OnDismissed(object sender, EventArgs e) {
            DoUpdateWords();
            ResetState();
        }


        public void SuppressCursorChange() {
            IsCursorChangeManuallySuppressed = true;
        }
        public void UnsupressCursorChange() {
            IsCursorChangeManuallySuppressed = false;
        }
        public void DoSelectCaretWord() {
            if(InputConnection.OnTextRangeInfoRequest() is not { } ti ||
                    TextRangeTools.GetWordAtCaret(ti, out int sidx, out _, out _, false) is not { } word_text) {
                return;
            }
            InputConnection.OnSelectText(sidx, sidx + word_text.Length);
        }
        public void DoSelectAll() {
            if(InputConnection is not { } ic ||
                ic.OnTextRangeInfoRequest() is not { } curRange) {
                return;
            }
            //ic.OnSelectText(0, curRange.Text.Length);
            // redundant but if only 1 fails its still same 
            ic.OnSelectAll();
        }
        void ResetState(bool invalidate = true) {
            //SetShiftState(ShiftStateType.None, invalidate);
            if(IsNumPadLayout) {
                SetCharSet(CharSetType.Numbers1, invalidate);
            } else {
                SetCharSet(CharSetType.Letters, invalidate);
            }
            FooterViewModel.ResetState();
            FloatContainerViewModel.ResetState();
            CursorControlViewModel.ResetState();
            _lastDebouncedTouchEventArgs = null;
            LastTextInfo = null;
            LastUpdateWordText = null;
            LastSpaceReplacedText = null;
            LastInput = string.Empty;

            CurTextInfo = null;
            HasLeadingText = null;
            foreach(var pkvm in PressedKeys.ToList()) {
                pkvm.SetPressed(false, null, invalidate: invalidate);
            }
            PressedKeys.Clear();
            Touches.Clear();
            MenuViewModel.ResetState();
        }

        private TouchEventArgs _lastDebouncedTouchEventArgs;

        bool IsTouchBounced(TouchEventArgs e) {
            if(CursorControlViewModel.IsCursorControlActive) {
                return false;
            }
            if(e == null) {
                return true;
            }
            if(_lastDebouncedTouchEventArgs == null) {
                return false;
            }
            if(_lastDebouncedTouchEventArgs.TouchEventType != e.TouchEventType) {
                return false;
            }
            double dist = Touches.Dist(e.Location, _lastDebouncedTouchEventArgs.Location);
            return dist < 5;
        }

        public void HandleTouch(TouchEventArgs e) {
            if(Touches.Update(
                e.TouchId,
                e.Location,
                e.RawLocation,
                e.TouchEventType) is not { } touch) {
                return;
            }

            if(FooterViewModel.HandleTouch(touch, e.TouchEventType)) {
                return;
            }
            if(FloatContainerViewModel.HandleTouch(touch, e.TouchEventType)) {
                return;
            }
            if(MenuViewModel.HandleMenuTouch(e.TouchEventType, touch)) {
                return;
            }
            if(!IsKeyGridVisible) {
                return;
            }
            //if (IsTouchBounced(e)) {
            //    return;
            //}
            //_lastDebouncedTouchEventArgs = e;

            switch(e.TouchEventType) {
                case TouchEventType.Press:
                    PressKey(touch);
                    break;
                case TouchEventType.Move:
                    MoveKey(touch);
                    break;
                case TouchEventType.Release:
                    ReleaseKey(touch);
                    break;
            }
        }
        KeyViewModel GetPressedKeyForTouch(Touch touch) {
            return PressedKeys
                        .FirstOrDefault(x => x.TouchId == touch.Id);
        }
        public void SetError(string msg) {
            ErrorText = msg;
            Renderer.PaintFrame(true);
        }

        public void SetDesiredSize(Size scaledSize, bool invalidate = true) {
            _desiredSize = scaledSize;
            double w = scaledSize.Width;
            double h = scaledSize.Height;
            if(IsFloatingLayout) {
                w -= (FloatContainerViewModel.FloatPad * 2);
                h -= (FloatContainerViewModel.FloatPad * 2);
            }
            MenuHeight = h * MenuHeightRatio;
            FooterHeight = MenuHeight / KeyConstants.PHI;//IsFloatingLayout || NeedsNextKeyboardButton ? MenuHeight : 0;
            KeyGridHeight = h - MenuHeight - FooterHeight;
            KeyboardWidth = w;
            MenuRect = new Rect(0, 0, KeyboardWidth, MenuHeight);
            KeyGridRect = new Rect(0, MenuRect.Bottom, KeyboardWidth, KeyGridHeight);
            TotalRect = new Rect(0, 0, TotalWidth, TotalHeight);

            CursorControlViewModel.InitLayout();

            FloatContainerViewModel.RefreshFloatScale();

            PopupKeys.ForEach(x => x.SetPopupKeyRect(null));
            MeasureKeys(false);
            UpdateShadows(null, false);

            if(invalidate) {
                this.Renderer.RenderFrame(true);
            }
        }

        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        #region Event Handlers
        private void Ic_PreferencesChanged(object sender, PreferencesChangedEventArgs e) {
            IsDirty = true;
            if(e == null) {
                return;
            }
            // TODO 
            // 1. clear recent/omitted emojis
            // 2. clear compl items
            // 
            foreach(var change in e.ChangedPrefLookup) {
                switch(change.Key) {
                    case PrefKeys.DEFAULT_SKIN_TONE_TYPE:
                    case PrefKeys.DEFAULT_HAIR_STYLE_TYPE:
                        if(MenuViewModel != null && MenuViewModel.EmojiPagesViewModel != null) {
                            //MenuViewModel.EmojiPagesViewModel.IsEmojiSetChanged = true;
                            MenuViewModel.EmojiPagesViewModel.Init();
                        }

                        break;
                }
            }
        }
        private void KeyboardViewModel_OnSwipe(object sender, VectorDirection e) {
            switch(e) {
                case VectorDirection.Left:
                    if(!IsSwipeLeftDeleteEnabled) {
                        break;
                    }
                    DoSelectCaretWord();
                    InputConnection.OnBackspace(1);
                    break;
            }
        }
        void Ic_OnPointerChanged(object s, TouchEventArgs e) {
            HandleTouch(e);
        }


        private void Ic_OnCursorChanged(object sender, SelectableTextRange e) {
            HandleCursorChange(e);
        }

        #endregion
        void InitKeys() {
            var cfg = new KeyboardLayoutConfig {
                IsEmojiButtonVisible = IsEmojiKeyVisible,
                IsNumberRowVisible = IsShowNumberRowEnabled,
                IsReset = IsDirty,
                NeedsNextKeyboardKey = NeedsNextKeyboardButton
            };
            if(KeyboardLayoutFactory.Build(InputConnection, KeyboardFlags, cfg) is not { } klr ||
                klr.Rows is not { } rows ||
                klr.Groups is not { } groups) {
                if(KeyboardLayoutFactory.BuildFallback(KeyboardFlags, cfg) is not { } klr2 ||
                    klr2.Rows is not { } rows2 ||
                    klr2.Groups is not { } groups2) {
                    MpConsole.WriteLine($"Error building keyboard");
                    return;
                }
                rows = rows2;
                groups = groups2;
            }
            List<List<object>> keyRows = rows;
            LetterGroups = groups;
            Keys.Clear();
            Rows.Clear();
            MaxColCount = 0;
            RowCount = keyRows.Count;
            for(int r = 0; r < keyRows.Count; r++) {
                var row = new List<KeyViewModel>();
                for(int c = 0; c < keyRows[r].Count; c++) {
                    var keyObj = keyRows[r][c];
                    var kvm = CreateKeyViewModel(keyRows[r][c], r, c);
                    Keys.Add(kvm);
                    row.Add(kvm);

                    if(kvm.IsShiftKey) {
                        ShiftKeys.Add(kvm);
                    } else if(kvm.IsShiftKeyAndOnTemp) {
                        TabKeys.Add(kvm);
                    } else if(kvm.IsPrimarySpecial) {
                        PrimaryKey = kvm;
                    } else if(kvm.IsEmojiKey) {
                        EmojiKey = kvm;
                    } else if(kvm.IsSpaceBar) {
                        SpacebarKey = kvm;
                    } else if(kvm.IsPeriod) {
                        PeriodKey = kvm;
                    } else if(kvm.IsBackspace) {
                        BackspaceKey = kvm;
                    } else if(kvm.CurrentChar == ResourceStrings.U["SubDomainKeyValue"].value ||
                              kvm.CurrentChar == ResourceStrings.U["DomainKeyValue"].value) {
                        DotComKey = kvm;
                    }
                }
                Rows.Add(row);
                MaxColCount = Math.Max(MaxColCount, row.Where(x => x.IsVisible).Count());
            }

            // find max popups for current keyboard
            int max_popup_keys = Keys.Max(x => x.GetPopupCharacters().Count());
            // calculate max possible rows using a fixed max col count
            MaxPopupRowCount = max_popup_keys < MaxPopupColCountFixed ? 1 : (int)Math.Floor((double)max_popup_keys / (double)(MaxPopupColCountFixed));
            //TotalTopPad = MaxPopupRowCount * DefaultKeyHeight;


            for(int i = 0; i < 2; i++) {
                // create 2x Max possible popup keys for multi touch
                int idx = 0;
                for(int r = 0; r < MaxPopupRowCount; r++) {
                    for(int c = 0; c < MaxPopupColCountFixed; c++) {
                        var pukvm = CreatePopUpKeyViewModel(idx++, r, c);
                        Keys.Add(pukvm);
                    }
                }
            }
            PopupKeys = Keys.Where(x => x.IsPopupKey).ToArray();

            if(!CanShowPopupWindows) {
                double max_key_height = Keys.Max(x => x.Height);
                double menu_height = MenuHeight;
                double safe_pad = 5;
                double inner_top_margin = max_key_height - menu_height + safe_pad;
                if(inner_top_margin > 0) {
                    // needs this much inner margin to show popups without overflowing
                    // top of keyboard
                    KeyboardMargin = new Thickness(
                        KeyboardMargin.Left,
                        inner_top_margin,
                        KeyboardMargin.Right,
                        KeyboardMargin.Bottom);
                }
            }
        }

        bool HandleBridgeMessage(SelectableTextRange textInfo) {
            return false;
            // if(InputConnection.SourceId != MpAvKbBridgeTextBox.SOURCE_ID ||
            //     !textInfo.Text.StartsWith(MpAvKbBridgeTextBox.BRIDGE_ID_TEXT) ||
            //     textInfo.Text.Substring(MpAvKbBridgeTextBox.BRIDGE_ID_TEXT.Length) is not { } msg) {
            //     return false;
            // }
            // Task.Run(async () => {
            //     if(msg.DeserializeBase64Object<MpAvKbBridgeMessageBase>() is not { } msgBaseObj ||
            //         msgBaseObj.MessageId is not { } msgId) {
            //         return;
            //     }
            //     MpAvKbBridgeMessageBase resp = null;
            //     switch(msgId) {
            //         case nameof(MpAvKbWelcomeMessage): {
            //                 if(msg.DeserializeBase64Object<MpAvKbWelcomeMessage>() is not { } wm) {
            //                     break;
            //                 }
            //                 InputConnection.SharedPrefService.SetPrefValue(PrefKeys.IS_TABLET, wm.isTablet);
            //                 break;
            //             }
            //         case nameof(MpAvKbClipboardRequestMessage): {
            //                 if(msg.DeserializeBase64Object<MpAvKbClipboardRequestMessage>() is not { } crm) {
            //                     break;
            //                 }
            //                 resp = await InputConnection.GetMessageResponseAsync(crm);
            //                 break;
            //             }
            //         case nameof(MpAvKbDismissRequest): {
            //                 InputConnection.OnDone();
            //                 break;
            //             }
            //
            //         default:
            //             throw new Exception($"Unhandled kb bridge msg type '{msgId}'");
            //     }
            //     if(resp == null) {
            //         return;
            //     }
            //     SuppressCursorChange();
            //     DoSelectAll();
            //     InputConnection.OnText(resp.SerializeObjectToBase64());
            //
            //     UnsupressCursorChange();
            // });
            //
            //
            // return true;
        }


        void HandleCursorChange(SelectableTextRange textInfo) {
            CurTextInfo = textInfo.Clone();

            DoCursorFeedback();

            if(!CanHandleCursorChange()) {
                WasCursorChangedWhileSuppressed = true;
                return;
            }
            WasCursorChangedWhileSuppressed = false;

            if(HandleBridgeMessage(CurTextInfo)) {
                return;
            }

            //MenuViewModel.AutoCompleteViewModel.ClearCompletions();
            bool? had_leading_text = HasLeadingText;
            HasLeadingText = textInfo.LeadingText.Length > 0;
            CheckAutoCap(textInfo);
            if(IsUrlLayout &&
                DotComKey is { } dot_com_kvm &&
                HasLeadingText != had_leading_text) {
                // show 'www.' when no leading newText, otherwise .com
                string key = HasLeadingText.HasValue && HasLeadingText.Value ? "DomainKeyValue" : "SubDomainKeyValue";
                dot_com_kvm.ForceCurrentChar(ResourceStrings.U[key].value);
            }

            MenuViewModel.HandleCursorChange(textInfo);
            LastTextInfo = CurTextInfo;
        }
        void DoCursorFeedback() {
            //DateTime this_cursor_change_dt = DateTime.Now;

            bool do_feedback = false;
            if(CursorControlViewModel.IsCursorControlActive) {
                do_feedback = true;
            }
            //else if (IsBackspacing) {
            //    do_feedback = true;
            //}
            if(do_feedback) {
                InputConnection.OnFeedback(FeedbackCursorChange);
            }
        }
        bool CanHandleCursorChange() {
            if(IsCursorChangeManuallySuppressed) {
                return false;
            }
            if(CursorControlViewModel.IsCursorControlActive) {
                return false;
            }
            if(PressedKeys.Any(x => x.IsBackspace)) {
                return false;
            }
            if(MenuViewModel.EmojiPagesViewModel.EmojiFooterMenuViewModel.IsBackspacePressed) {
                return false;
            }
            return true;
        }
        void CheckAutoCap(SelectableTextRange textInfo) {
            if(textInfo == null ||
                !CanAutoCap ||
                !IsAutoCapitalizationEnabled ||
                InputConnection is not { } ic ||
                ShiftState == ShiftStateType.ShiftLock) {
                return;
            }

            bool needs_shift = false;
            string leading_text = textInfo.LeadingText;

            if(string.IsNullOrEmpty(leading_text)) {
                // auto cap if insert is leading
                needs_shift = true;
            } else {

                for(int i = 0; i < leading_text.Length; i++) {
                    char cur_char = leading_text[leading_text.Length - i - 1];
                    if(cur_char == KeyConstants.SPACE_CHAR) {
                        continue;
                    }
                    bool is_eos = TextRangeTools.IsEndOfSentenceChar(cur_char, true, out bool is_line_break);
                    // only allow shift if prev char is newline or
                    // space(s) after any end-of-sentence char 
                    needs_shift =
                        is_eos &&
                        ((i == 0 && is_line_break) || i > 0);
                    break;
                }
            }
            if(needs_shift && ShiftState == ShiftStateType.None) {
                IsAutoCapEnabled = true;
            } else {
                IsAutoCapEnabled = false;
            }
            SetShiftState(needs_shift ? ShiftStateType.Shift : ShiftStateType.None);

        }

        void SetFlags(KeyboardFlags kbFlags) {
            if(InputConnection.SharedPrefService is not ISharedPrefService prefService) {
                return;
            }
            KeyboardFlags = kbFlags;

            IsNumbers = KeyboardFlags.HasFlag(KeyboardFlags.Numbers);
            IsDigits = KeyboardFlags.HasFlag(KeyboardFlags.Digits);
            IsPin = KeyboardFlags.HasFlag(KeyboardFlags.Pin);
            IsNumPadLayout = IsNumbers || IsDigits || IsPin;
            IsFreeTextLayout = KeyboardFlags.HasFlag(KeyboardFlags.Normal);
            IsEmailLayout = KeyboardFlags.HasFlag(KeyboardFlags.Email);
            IsUrlLayout = KeyboardFlags.HasFlag(KeyboardFlags.Url);
            IsMobile = KeyboardFlags.HasFlag(KeyboardFlags.Mobile);
            IsTablet = KeyboardFlags.HasFlag(KeyboardFlags.Tablet);
            IsPassword = KeyboardFlags.HasFlag(KeyboardFlags.Password);
            IsHorizontalOrientation = KeyboardFlags.HasFlag(KeyboardFlags.Landscape);
            IsMultiLineInput = KeyboardFlags.HasFlag(KeyboardFlags.MultiLine);
            IsLandscape = KeyboardFlags.HasFlag(KeyboardFlags.Landscape);
            IsPortrait = KeyboardFlags.HasFlag(KeyboardFlags.Portrait);
            IsFloatingLayout = /*prefService.GetPrefValue<bool>(PrefKeys.DO_FLOAT_LAYOUT) || */KeyboardFlags.HasFlag(KeyboardFlags.FloatLayout);
            prefService.SetPrefValue(PrefKeys.DO_FLOAT_LAYOUT, IsFloatingLayout);

            IsEmojiKeyVisible = !IsEmailLayout && !IsUrlLayout && prefService.GetPrefValue<bool>(PrefKeys.DO_EMOJI_KEY);

            ThemeType = prefService.GetPrefValue<string>(PrefKeys.THEME_TYPE).ToEnum<KeyboardThemeType>();
            if(ThemeType == KeyboardThemeType.Default) {
                // use flag value
                IsThemeDark = KeyboardFlags.HasFlag(KeyboardFlags.Dark);
            } else {
                IsThemeDark = ThemeType == KeyboardThemeType.Dark;
            }


            double cr_factor = (double)prefService.GetPrefValue<int>(PrefKeys.DEFAULT_CORNER_RADIUS_FACTOR);
            if(OperatingSystem.IsIOS()) {
                // ios does corners weird
                cr_factor *= 0.45;
            }
            CommonCornerRadius = new CornerRadius(Math.Max(0.1, KeyConstants.PHI * cr_factor));

            if(prefService.GetPrefValue<bool>(PrefKeys.DO_CUSTOM_BG_PATH)) {
                CustomBgImagePath = prefService.GetPrefValue<string>(PrefKeys.CUSTOM_BG_PATH);
                if(string.IsNullOrWhiteSpace(CustomBgImagePath)) {
                    CustomBgImagePath = null;
                }
            } else {
                CustomBgImagePath = null;
            }

            if(prefService.GetPrefValue<bool>(PrefKeys.DO_CUSTOM_BG_COLOR)) {
                CustomBgHexColor = prefService.GetPrefValue<string>(PrefKeys.CUSTOM_BG_COLOR);
                if(string.IsNullOrWhiteSpace(CustomBgHexColor)) {
                    CustomBgHexColor = null;
                }
            } else {
                CustomBgHexColor = null;
            }


            IsSmartPunctuationEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_SMART_PUNCTUATION);

            IsKeyBordersVisible = prefService.GetPrefValue<bool>(PrefKeys.DO_KEY_BORDERS);
            IsShadowsEnabled = IsKeyBordersVisible && prefService.GetPrefValue<bool>(PrefKeys.DO_SHADOWS);
            IsDynamicShadowsEnabled = IsShadowsEnabled && prefService.GetPrefValue<bool>(PrefKeys.DO_DYNAMIC_SHADOWS);

            IsDoubleTapEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_DOUBLE_TAP_INPUT);

            IsShowNumberRowEnabled = IsMobile && prefService.GetPrefValue<bool>(PrefKeys.DO_NUM_ROW);
            IsPressPopupsEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_POPUP);
            IsHoldPopupsEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_LONG_POPUP);
            IsShowLetterGroupPopupsEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_SHOW_LETTER_GROUPS);

            IsAutoCapitalizationEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_AUTO_CAPITALIZATION);

            IsTabsAsSpacesEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_TABS_AS_SPACES);
            TabSpaceCount = prefService.GetPrefValue<int>(PrefKeys.TAB_SP_COUNT);

            IsSmartQuotesEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_SMART_QUOTES);
            IsExtendedSmartQuotesEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_EXTENDED_SMART_QUOTES);

            IsSwipeLeftDeleteEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_SWIPE_LEFT_DELETE_WORD);

            IsDoubleTapSpaceEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_DOUBLE_SPACE_PERIOD);
            MaxDoubleTapSpaceForPeriodMs = prefService.GetPrefValue<int>(PrefKeys.DOUBLE_TAP_SPACE_DELAY_MS);

            CanCursorControlBeEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_CURSOR_CONTROL);
            CursorControlSensitivity = (double)prefService.GetPrefValue<int>(PrefKeys.CURSOR_CONTROL_SENSITIVITY);
            IsDoubleTapCursorControlEnabled = CanCursorControlBeEnabled && prefService.GetPrefValue<bool>(PrefKeys.DO_DOUBLE_TAP_CURSOR_CONTROL);

            IsCompletionDictUpdateEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_ADD_NEW_WORDS);
            IsCompletionDictUpdateForPwdEnabled = IsCompletionDictUpdateEnabled && prefService.GetPrefValue<bool>(PrefKeys.DO_ADD_NEW_PASSWORDS);
            IsNextWordCompletionEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_NEXT_WORD_COMPLETION);

            IsAutoCorrectEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_AUTO_CORRECT);
            IsBackspaceUndoLastAutoCorrectEnabled = IsAutoCorrectEnabled && prefService.GetPrefValue<bool>(PrefKeys.DO_BACKSPACE_UNDOS_LAST_AUTO_CORRECT);
            IsConfirmIgnoreAutoCorrectEnabled = IsAutoCorrectEnabled && prefService.GetPrefValue<bool>(PrefKeys.DO_SHOW_IGNORE_AUTO_CORRECT);

            MinDelayForHoldPopupMs = prefService.GetPrefValue<int>(PrefKeys.LONG_POPUP_DELAY);

            IsBackspaceRepeatEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_BACKSPACE_REPEAT);
            BackspaceHoldToRepeatMs = prefService.GetPrefValue<int>(PrefKeys.BACKSPACE_REPEAT_DELAY_MS);
            BackspaceRepeatMs = prefService.GetPrefValue<int>(PrefKeys.BACKSPACE_REPEAT_SPEED_MS);

            IsShowEmojiTextCompletionEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_SHOW_EMOJI_IN_TEXT_COMPLETIONS);

            IsLoggingEnabled = prefService.GetPrefValue<bool>(PrefKeys.DO_LOGGING);
            CurrentLogLevel = IsLoggingEnabled ?
                prefService.GetPrefValue<string>(PrefKeys.LOG_LEVEL).ToEnum<MpLogLevel>() :
                MpLogLevel.None;

            MaxTextCompletionResults = prefService.GetPrefValue<int>(PrefKeys.MAX_TEXT_COMPLETION_COUNT);
            MaxEmojiCompletionResults = prefService.GetPrefValue<int>(PrefKeys.MAX_EMOJI_COMPLETION_COUNT);

            DefaultSkinToneType = prefService.GetPrefValue<string>(PrefKeys.DEFAULT_SKIN_TONE_TYPE).ToEnum<EmojiSkinToneType>();
            DefaultHairStyleType = prefService.GetPrefValue<string>(PrefKeys.DEFAULT_HAIR_STYLE_TYPE).ToEnum<EmojiHairStyleType>();


            BgAlpha = (byte)prefService.GetPrefValue<int>(PrefKeys.BG_OPACITY);
            FgAlpha = (byte)prefService.GetPrefValue<int>(PrefKeys.FG_OPACITY);
            FgBgAlpha = (byte)prefService.GetPrefValue<int>(PrefKeys.FG_BG_OPACITY);

            var def_feeback = KeyboardFeedbackFlags.None;
            if(prefService.GetPrefValue<bool>(PrefKeys.DO_SOUND)) {
                def_feeback |= KeyboardFeedbackFlags.Click;
            }
            if(prefService.GetPrefValue<bool>(PrefKeys.DO_VIBRATE)) {
                def_feeback |= KeyboardFeedbackFlags.Vibrate;
            }

            var ret_feedback = def_feeback;
            if(ret_feedback.HasFlag(KeyboardFeedbackFlags.Click)) {
                ret_feedback &= ~KeyboardFeedbackFlags.Click;
                ret_feedback |= KeyboardFeedbackFlags.Return;
            }
            var del_feedback = def_feeback;

            if(del_feedback.HasFlag(KeyboardFeedbackFlags.Click)) {
                del_feedback &= ~KeyboardFeedbackFlags.Click;
                del_feedback |= KeyboardFeedbackFlags.Delete;
            }
            var sp_feedback = def_feeback;
            if(sp_feedback.HasFlag(KeyboardFeedbackFlags.Click)) {
                sp_feedback &= ~KeyboardFeedbackFlags.Click;
                sp_feedback |= KeyboardFeedbackFlags.Space;
            }
            var inv_feedback = def_feeback;
            if(inv_feedback.HasFlag(KeyboardFeedbackFlags.Click)) {
                inv_feedback &= ~KeyboardFeedbackFlags.Click;
                inv_feedback |= KeyboardFeedbackFlags.Invalid;
            }
            FeedbackCursorChange = def_feeback;
            FeedbackClick = def_feeback;
            FeedbackReturn = ret_feedback;
            FeedbackDelete = del_feedback;
            FeedbackSpace = sp_feedback;
            FeedbackInvalid = inv_feedback;

            if(prefService is SharedPrefWrapper spw) {
                spw.Logger.InitLogger();
            }
        }

        public KeyViewModel GetKeyUnderPoint(Point scaledPoint) {
            var p = scaledPoint;
            var test = VisibleKeyboardKeys.Where(x => x.TotalHitRect.Contains(p)).ToList();
            var result = VisibleKeyboardKeys
                .FirstOrDefault(x => x.TotalHitRect.Contains(p));
            return result;
        }

        KeyViewModel CreatePopUpKeyViewModel(int idx, int r, int c) {
            var pu_kvm = CreateKeyViewModel(idx, r, c);
            return pu_kvm;
        }

        KeyViewModel CreateKeyViewModel(object keyObj, int r, int c) {
            var kvm = new KeyViewModel(this, keyObj, r, c);
            return kvm;
        }
        void ToggleSymbolSet() {
            CharSetType to_select = CharSet;
            switch(CharSet) {
                case CharSetType.Numbers1:
                    to_select = CharSetType.Numbers2;
                    break;
                case CharSetType.Numbers2:
                    to_select = CharSetType.Numbers1;
                    break;
                case CharSetType.Letters:
                    to_select = CharSetType.Symbols1;
                    break;
                default:
                    to_select = CharSetType.Letters;
                    break;

            }
            if(to_select == CharSet) {
                // missing state?
                Debugger.Break();
            }
            SetCharSet(to_select);
        }
        void ToggleCapsLock() {
            if(ShiftState == ShiftStateType.ShiftLock) {
                SetShiftState(ShiftStateType.None);
            } else {
                SetShiftState(ShiftStateType.ShiftLock);
            }
        }
        void SetShiftState(ShiftStateType sst, bool invalidate = true) {
            if(_shiftState == sst) {
                return;
            }
            _shiftState = sst;
            if(_shiftState == ShiftStateType.None) {

            }
            foreach(var kvm in VisibleKeyboardKeys) {
                kvm.UpdateCharacters();
                if(invalidate) {
                    kvm.Renderer.RenderFrame(true);
                }
            }
            if(invalidate) {
                MenuViewModel.Renderer.LayoutFrame(true);
            }
            this.RaisePropertyChanged(nameof(ShiftState));
            if(InputConnection is { } ic) {
                ic.OnShiftChanged(sst);
            }
        }
        void SetCharSet(CharSetType cst, bool invalidate = true) {
            if(_charSet == cst) {
                return;
            }
            _charSet = cst;
            foreach(var kvm in KeyboardKeys) {
                kvm.UpdateCharacters();
            }
            MeasureKeys(false);
            if(invalidate) {
                // NOTE don't be tempted to use full render here!
                // it adds too much delay while typing (only do for debug)
                //this.Renderer.RenderFrame(true);
                foreach(var kvm in KeyboardKeys) {
                    kvm.Renderer.RenderFrame(true);
                }
            }
            this.OnCharSetChanged?.Invoke(this, EventArgs.Empty);
            this.RaisePropertyChanged(nameof(CharSet));
        }

        void HandleShift() {
            if(CharSet == CharSetType.Letters) {
                if(ShiftState == ShiftStateType.ShiftLock) {
                    SetShiftState(ShiftStateType.None);
                } else {
                    SetShiftState((ShiftStateType)((int)ShiftState + 1));
                }
            } else {
                if(CharSet == CharSetType.Symbols1) {
                    SetCharSet(CharSetType.Symbols2);
                } else {
                    SetCharSet(CharSetType.Symbols1);
                }
            }
        }
        void ShowEmojiPages() {
            MenuViewModel.SetMenuPage(MenuPageType.TabSelector, MenuTabItemType.Emoji);
            //this.Renderer.Render(true);            
        }

        #region Popups
        void ShowPressPopup(KeyViewModel kvm, Touch touch) {
            if(!IsPressPopupsEnabled || !kvm.CanShowPressPopup) {
                return;
            }
            kvm.ClearPopups();
            kvm.AddPopupAnchor(0, 0, kvm.CurrentChar);
            kvm.PressPopupShowDt = DateTime.Now;
            kvm.FitPopupInFrame(touch);
            InputConnection.MainThread.Post(() => OnShowPopup?.Invoke(this, kvm));
        }
        public void HidePopup(KeyViewModel kvm) {
            InputConnection.MainThread.Post(() => OnHidePopup?.Invoke(this, kvm));
        }
        void ShowHoldPopup(KeyViewModel kvm, Touch touch) {
            if(IsHoldMenuVisible ||
                !IsHoldPopupsEnabled ||
                !kvm.CanShowHoldPopup ||
                !IsLettersCharSet ||
                kvm.IsPulling ||
                PressedKeys.Any(x => x != kvm)) {
                return;
            }

            kvm.ClearPopups();
            var popup_chars = kvm.PopupCharacters.ToList();
            if(kvm.IsRightSideKey) {
                popup_chars.Reverse();
            }
            int idx = 0;
            for(int r = 0; r < MaxPopupRowCount; r++) {
                for(int c = 0; c < MaxPopupColCountFixed; c++) {
                    string pv = string.Empty;
                    if(idx < popup_chars.Count) {
                        // visible popup
                        pv = popup_chars[idx];
                    } else if(r == 0) {
                        break;
                    }
                    kvm.AddPopupAnchor(r, c, pv);
                    idx++;
                }
                if(idx >= popup_chars.Count) {
                    break;
                }
            }
            kvm.FitPopupInFrame(touch);
            if(InputConnection is { } ic &&
                ic.MainThread is { } mt) {
                mt.Post(() => OnShowPopup?.Invoke(this, kvm));
            }

        }

        #endregion

        #region Cursor Control

        #endregion

        #region Key Pull
        public void UpdatePull(Touch touch) {
            var pkvm = GetPressedKeyForTouch(touch);
            if(pkvm == null ||
                !pkvm.CanPullKey) {
                return;
            }
            //if (!pkvm.TotalHitRect.Contains(touch.Location) &&
            //    !pkvm.IsPulling) {
            //    // reset pull
            //    //pkvm.PullTranslateY = 0;
            //    return;
            //}
            bool was_pulling = pkvm.IsPulling;
            double last_pull_trans_y = pkvm.PullTranslateY;
            double y_diff = touch.Location.Y - touch.PressLocation.Y;
            pkvm.PullTranslateY = Math.Clamp(y_diff, 0, pkvm.MaxPullTranslateY);
            if(pkvm.IsPulling && !was_pulling) {
                pkvm.ClearPopups();
            }
            if(Math.Abs(pkvm.PullTranslateY - last_pull_trans_y) >= 1) {
                MpConsole.WriteLine($"Pull: {y_diff}");
                pkvm.Renderer.PaintFrame(true);
            }
        }
        #endregion

        #region Float



        #endregion

        #region Logging


        #endregion

        #region Key Press (Life Cycle)
        void PressKey(Touch touch, bool isSoft = false) {
            if(GetKeyUnderPoint(touch.Location) is not { } kvm) {
                return;
            }
            kvm.SetPressed(true, touch, isSoft);

            if(kvm.IsBackspace) {
                RepeatBackspace(() => {
                    return IsBackspaceRepeatEnabled && kvm.IsPressed && kvm.TouchId == touch.Id;
                });
                return;
            }
            InputConnection.OnFeedback(FeedbackClick);

            if(CanPerformAction(touch, kvm, true)) {
                PerformKeyAction(kvm);
            }

            if(kvm.HasPressPopup) {
                ShowPressPopup(kvm, touch);
                if(kvm.HasHoldPopup) {
                    InputConnection.MainThread.Post(async () => {
                        await Task.Delay(MinDelayForHoldPopupMs);
                        if(kvm.IsPressed && kvm.TouchId == touch.Id) {
                            ShowHoldPopup(kvm, touch);
                        }
                    });
                }
                return;
            }
            if(CanCursorControlBeEnabled &&
                kvm.IsSpaceBar &&
                CursorControlViewModel is { } ccvm) {
                InputConnection.MainThread.Post(async () => {
                    await Task.Delay(MinDelayForHoldPopupMs);
                    if(kvm.IsPressed && ccvm.CheckCanCursorControlBeEnabled(touch, kvm, true)) {
                        ccvm.StartCursorControl(touch);
                    }
                });
                return;
            }
        }
        void MoveKey(Touch touch) {
            if(CursorControlViewModel is not { } ccvm) {
                return;
            }

            if(ccvm.IsCursorControlActive) {
                return;
            }
            if(IsPullEnabled) {
                UpdatePull(touch);
            }

            var pressed_kvm = GetPressedKeyForTouch(touch);
            if(pressed_kvm != null && pressed_kvm.IsHoldMenuOpen) {
                pressed_kvm.UpdateActivePopup(touch);
                return;
            }

            if(ccvm.CheckCanCursorControlBeEnabled(touch, pressed_kvm, false)) {
                ccvm.StartCursorControl(touch);
                return;
            }

            if(IsSlideEnabled &&
                pressed_kvm != null &&
                GetKeyUnderPoint(touch.Location) is { } touch_kvm &&
                pressed_kvm != touch_kvm) {
                // when key is pressed and this is its
                // associated touch but the touch isn't over the key

                // soft release it
                SoftReleaseKey(pressed_kvm);
                //PressKeyAsync(touch,true).FireAndForgetSafeAsync();
                PressKey(touch, true);
            }
        }
        void SoftReleaseKey(KeyViewModel kvm) {
            if(kvm == null) {
                return;
            }
            kvm.SetPressed(false, null);
        }
        void ReleaseKey(Touch touch) {
            if(CursorControlViewModel.IsCursorControlActive) {
                CursorControlViewModel.StopCursorControl(touch);
                return;
            }
            if(GetPressedKeyForTouch(touch) is not { } pressed_kvm) {
                return;
            }

            if(CanPerformAction(touch, pressed_kvm, false)) {
                string result = PerformKeyAction(pressed_kvm);
                UpdateReleaseHistory(touch, result);
            }
            pressed_kvm.SetPressed(false, null);

            if(WasCursorChangedWhileSuppressed) {
                HandleCursorChange(InputConnection.OnTextRangeInfoRequest());
            }
        }
        void UpdateReleaseHistory(Touch touch, string input) {
            if(!IsAutoCorrectEnabled ||
                string.IsNullOrWhiteSpace(input)) {
                return;
            }
            InputReleaseHistory.Add((input, touch.Location));
        }
        #endregion

        #region Key Action
        public string PerformKeyAction(KeyViewModel pressed_kvm) {
            string pv = string.Empty;
            var active_kvm = pressed_kvm.ActivePopupKey;
            if(active_kvm == null) {
                active_kvm = pressed_kvm;
            }
            if(active_kvm == null) {
                return pv;
            }

            switch(pressed_kvm.SpecialKeyType) {
                case SpecialKeyType.Collapse:
                    InputConnection.OnCollapse(false);
                    break;
                case SpecialKeyType.NextKeyboard:
                    if(InputConnection is IKeyboardInputConnection ims_ios) {
                        ims_ios.OnInputModeSwitched();
                    }
                    break;
                case SpecialKeyType.Tab:
                    if(IsAnyShiftState) {
                        DoShiftTab();
                    } else {
                        DoTab(active_kvm);
                        pv = "\t";
                    }
                    break;
                case SpecialKeyType.Shift:
                    HandleShift();
                    break;
                case SpecialKeyType.SymbolToggle:
                case SpecialKeyType.NumberSymbolsToggle:
                    ToggleSymbolSet();
                    break;
                case SpecialKeyType.Backspace:
                    // handled in press
                    break;
                case SpecialKeyType.Enter:
                    pv = Environment.NewLine;
                    DoText(pressed_kvm, pv);
                    //DoUpdateWords();
                    break;
                case SpecialKeyType.Done:
                case SpecialKeyType.Go:
                case SpecialKeyType.Search:
                case SpecialKeyType.Next:
                    DoDone();
                    break;
                case SpecialKeyType.CapsLock:
                    ToggleCapsLock();
                    break;
                case SpecialKeyType.Emoji:
                    ShowEmojiPages();
                    break;
                default:
                    pv = active_kvm.PrimaryValue;
                    if(IsPullEnabled &&
                        active_kvm.IsPulled) {
                        // release comes from active not pressed
                        // when pulled don't care whats active just use secondary
                        pv = active_kvm.SecondaryValue;
                        active_kvm.PullTranslateY = 0;
                    }
                    if(CheckAndDoDoubleTap(active_kvm) is { } dbl_tap_str) {
                        pv = dbl_tap_str;
                        break;
                    }
                    if(CheckAndDoSmartQuote(active_kvm) is { } quote_str) {
                        pv = quote_str;
                        SetCharSet(CharSetType.Letters);
                        break;
                    }
                    pv = DoText(active_kvm, pv);
                    break;
            }
            return pv;
        }
        bool CanPerformAction(Touch touch, KeyViewModel kvm, bool isPress) {
            if(kvm.IsActionOnPress && kvm.IsActionOnRelease) {
                return true;
            }
            if(isPress) {
                return kvm.IsActionOnPress;
            }
            if(HandleSwipe(touch)) {
                return false;
            }
            if(kvm.IsActionOnPress || !kvm.IsActionOnRelease) {
                return false;
            }
            if(kvm.IsPulled) {
                return true;
            }

            if(kvm.IsHoldMenuOpen) {
                return true;
            }
            return kvm.TotalHitRect.Contains(touch.Location);
        }
        void DoDone() {
            if(MenuViewModel.EmojiPagesViewModel.EmojiFooterMenuViewModel.SearchBoxViewModel.IsVisible) {
                MenuViewModel.CloseEmojiSearch();
                return;
            }
            InputConnection?.OnDone();
        }
        string DoText(KeyViewModel active_kvm, string pv) {
            if(active_kvm.IsSpaceBar) {
                LastSpaceReplacedText = CurTextInfo == null ? string.Empty : CurTextInfo.SelectedText;
            }
            if(CheckAndDoSmartPunctuation(active_kvm, pv, string.Empty, false) is { } punc_text) {
                // if succeeds do text is called again w/ empty
                return punc_text;
            }
            InputConnection?.OnText(pv);
            FinishInsert(active_kvm, pv);
            return pv;
        }
        #endregion

        #region Swipe

        public bool HandleSwipe(Touch touch) {
            if(!IsSwipeLeftDeleteEnabled ||
                IsHoldMenuVisible ||
                MenuViewModel.EmojiPagesViewModel.IsAnyPopupVisible ||
                touch.LocationHistory is not { } lh ||
                !lh.Any()) {
                return false;
            }
            var down = lh.FirstOrDefault();
            var up = lh.LastOrDefault();
            if(!KeyGridRect.Contains(down.Value) || !KeyGridRect.Contains(up.Value)) {
                return false;
            }
            if(up.Key - down.Key > TimeSpan.FromMilliseconds(300)) {
                return false;
            }
            if(up.Value.Distance(down.Value) < KeyboardWidth / 6d) {
                return false;
            }
            var dir = up.Value.GetDirection(down.Value);
            if(dir != VectorDirection.Left) {
                return false;
            }
            OnSwipe?.Invoke(this, (dir));
            return true;
        }
        #endregion

        #region Backspace

        public void RepeatBackspace(Func<bool> canRepeat) {
            //if (InputConnection is IKeyboardInputConnection ios_ic &&
            //    ios_ic.ActionTimer is { } anim_timer) {
            //    // BUG async repeat doesn't work all a sudden on ios
            //    anim_timer.Repeat(ios_ic.MainThread, () => DoBackspace(), canRepeat, BackspaceHoldToRepeatMs, BackspaceRepeatMs);
            //    return;
            //}
            if(InputConnection.MainThread is not { } mt) {
                return;
            }
            mt.Repeat(() => DoBackspace(), canRepeat, BackspaceHoldToRepeatMs, BackspaceRepeatMs);
        }
        public void DoBackspace(uint count = 1) {
            if(InputConnection.MainThread is not { } mt) {
                return;
            }
            int shift = PressedKeys.Any(x => x.IsShiftKey) ? -1 : 1;
            bool do_feedback = false;
            if(CurTextInfo is { } cti) {
                if(cti.SelectedText.Length > 0) {
                    do_feedback = true;
                } else if(shift < 0 && cti.TrailingText.Length > 0) {
                    // forward delete
                    do_feedback = true;
                } else if(shift > 0 && cti.LeadingText.Length > 0) {
                    do_feedback = true;
                }
            }
            if(do_feedback) {
                // only do feedback if there's something to delete
                InputConnection.OnFeedback(FeedbackDelete);
            }

            InputConnection.OnBackspace((int)count);

        }

        #endregion
        void DoUpdateWords() {
            if(InputConnection is { } ic &&
                ic.WordUpdater is { } wu &&
                CurTextInfo is { } lti &&
                lti.Text is { } match_text &&
                match_text.Trim() != LastUpdateWordText &&
                (!IsPassword || (IsCompletionDictUpdateForPwdEnabled && IsPassword))) {

                LastUpdateWordText = match_text.Trim();
                if(match_text.GetWordCounts() is { } word_counts &&
                    word_counts.Any()) {
                    wu.AddWords(word_counts, IsCompletionDictUpdateEnabled);
                }

            }
        }
        void FinishInsert(KeyViewModel kvm, string pv) {
            LastInput = pv;
            if(kvm != null) {
                if(ShiftState == ShiftStateType.Shift && (IsMobile || ShiftKeys.All(x => !x.IsPressed))) {
                    SetShiftState(ShiftStateType.None);
                }
                if(kvm.IsSpaceBar && !IsNumPadLayout) {
                    // after typing space reset to default keyboard                            
                    SetCharSet(CharSetType.Letters);
                }
            }
        }

        public void UndoLeadingWithText(SelectableTextRange cur_info, string newText, string oldText) {
            int old_idx = cur_info.LeadingText.ToLower().LastIndexOf(oldText.ToLower());
            if(old_idx < 0) {
                return;
            }
            int offset = cur_info.LeadingText.Length - old_idx;
            //KeyboardViewModel.SuppressCursorChange();
            InputConnection.OnReplaceText(cur_info.SelectionStartIdx - offset, cur_info.SelectionEndIdx, newText);
            //KeyboardViewModel.UnsupressCursorChange();
        }

        #region Tab
        void DoTab(KeyViewModel tab_kvm) {
            string pv = "\t";
            if(IsTabsAsSpacesEnabled) {
                pv = string.Join(string.Empty, Enumerable.Range(0, TabSpaceCount).Select(x => " "));
            }
            DoText(tab_kvm, pv);
        }
        void DoShiftTab() {
            if(CurTextInfo is not { } cti ||
                cti.SelectionStartIdx == 0 ||
                cti.LeadingText.EndsWith('\n')) {
                return;
            }
            uint del_count = 0;
            for(int i = 0; i < TabSpaceCount; i++) {
                if(i >= cti.LeadingText.Length) {
                    break;
                }
                var cur_char = cti.LeadingText[cti.LeadingText.Length - i - 1];
                if(cur_char == '\t') {
                    if(i == 0) {
                        del_count = 1;
                    }
                    break;
                } else if(cur_char == ' ') {
                    del_count++;
                } else {
                    break;
                }
            }
            DoBackspace(del_count);
        }
        #endregion

        #region Smart Punctuation
        string CheckAndDoSmartPunctuation(KeyViewModel kvm, string new_text, string old_text, bool fromDoubleTap) {
            if(!IsSmartPunctuationEnabled) {
                return null;
            }
            bool is_new_punc = false;
            bool is_lead_space = false;
            bool old_needs_space_prefix = true;
            bool new_needs_space_sufffix = true;

            if(fromDoubleTap && new_text == ". ") {
                is_new_punc = true;
                new_needs_space_sufffix = false;
                old_needs_space_prefix = false;
            } else if(new_text.Length == 1 && TextRangeTools.IsSmartPunctuationChar(new_text[0])) {
                is_new_punc = true;
                new_needs_space_sufffix = new_text[0] != '\'';
            }
            if(!is_new_punc ||
                InputConnection.OnTextRangeInfoRequest() is not { } cti) {
                return null;
            }

            if(fromDoubleTap) {
                if(cti.LeadingText.TakeLast(2).FirstOrDefault() == ' ') {
                    is_lead_space = true;
                }
            } else if(cti.LeadingText.EndsWith(' ')) {
                is_lead_space = true;
            }
            if(!is_lead_space) {
                return null;
            }
            if(old_needs_space_prefix) {
                old_text = " " + old_text;
            }
            if(new_needs_space_sufffix) {
                new_text = new_text + " ";
            }
            UndoLeadingWithText(cti, new_text, old_text);
            FinishInsert(kvm, new_text);
            return new_text;
        }

        #endregion

        #region Double Tap

        string CheckAndDoDoubleTap(KeyViewModel kvm) {
            if(!kvm.CanDoubleTap ||
                !kvm.IsPressed ||
                kvm.LastReleaseDt is not { } lrdt ||
                DateTime.Now - lrdt > TimeSpan.FromMilliseconds(MaxDoubleTapSpaceForPeriodMs)) {
                return null;
            }
            if(InputConnection.OnTextRangeInfoRequest() is not { } cti) {
                return null;
            }
            if(cti.LeadingText.Length > 1 && cti.LeadingText.TakeLast(2).FirstOrDefault() is { } prev_leading_char) {

                if(kvm.IsSpaceBar) {
                    if(prev_leading_char == ' ') {
                        // prevent double tap if typing a bunch of spaces
                        return null;
                    }
                } else if(!char.IsWhiteSpace(prev_leading_char)) {
                    // don't allow double tap in the middle of word
                    return null;
                }
            }

            string new_text = kvm.IsSpaceBar ? ". " : kvm.SecondaryValue;
            string old_text = kvm.PrimaryValue;
            if(CheckAndDoSmartPunctuation(kvm, new_text, old_text, true) is { } punc_text) {
                return punc_text;
            }
            UndoLeadingWithText(cti, new_text, old_text);
            FinishInsert(kvm, new_text);
            return new_text;
        }

        #endregion

        #region Smart Quotes

        Dictionary<string, string> QuoteLookup { get; } = new Dictionary<string, string>() {
            {"\"","\"" },
            {"'","'" },
        };
        Dictionary<string, string> ExtendedQuoteLookup { get; } = new Dictionary<string, string>() {
            {"{","}" },
            {"[","]" },
            {"(",")" },
            {"<",">" },
        };

        string CheckAndDoSmartQuote(KeyViewModel kvm) {
            string quote_str = kvm.PrimaryValue;
            bool is_quote = IsSmartQuotesEnabled && QuoteLookup.ContainsKey(quote_str);
            bool is_ext_quote = IsExtendedSmartQuotesEnabled && ExtendedQuoteLookup.ContainsKey(quote_str);
            if(!is_quote && !is_ext_quote) {
                return null;
            }

            if(CurTextInfo is not { } cti ||
                TextRangeTools.GetWordAtCaret(cti, out _, out _, out _, false) is not { } caret_word ||
                (!string.IsNullOrWhiteSpace(caret_word) && CurTextInfo.SelectionLength == 0)) {
                // only check when there's no leading word (unless selection is range
                return null;
            }

            string quote_compliment = is_ext_quote ? ExtendedQuoteLookup[quote_str] : QuoteLookup[quote_str];
            string output_text = quote_str + cti.SelectedText + quote_compliment;
            InputConnection.OnReplaceText(cti.SelectionStartIdx, cti.SelectionEndIdx, output_text);
            InputConnection.OnNavigate(-1, 0);
            return output_text;
        }

        #endregion

        #region Measuring

        void UpdateOrientation(bool isPortrait, bool isTablet, bool isInit, bool invalidate) {
            var screen_size = InputConnection.ScaledScreenSize;
            double min = Math.Min(screen_size.Width, screen_size.Height);
            double max = Math.Max(screen_size.Width, screen_size.Height);
            double w = isPortrait ? min : max;
            double h = isPortrait ? max : min;
            ScaledScreenSize = new Size(w, h);
            var desired_size = GetTotalSizeByScreenSize(screen_size, isPortrait, isTablet);
            TotalDockedSize = desired_size;
            if(IsFloatingLayout) {
                desired_size = FloatContainerViewModel.InitFloatLayout(desired_size, isInit);
            }
            SetDesiredSize(desired_size, invalidate);
        }

        double GetSpecialKeyWidthRatio(SpecialKeyType skt, int rowIdx, bool isOnLeft) {
            // tablet ratios based on ipad pro https://discussions.apple.com/content/attachment/7e71442a-3222-47bf-8a08-58a8e92492fc
            double mobile_special_ratio = 1.5;
            double def_key_ratio = 1;
            double max_ratio = KeyConstants.PHI;//2;
            if(IsNumPadLayout) {
                return def_key_ratio;
            }
            switch(skt) {
                case SpecialKeyType.Tab:
                    // TABLET ONLY
                    return def_key_ratio;

                case SpecialKeyType.Backspace:
                    return IsTablet ? def_key_ratio : mobile_special_ratio;

                case SpecialKeyType.CapsLock:
                    // TABLET ONLY
                    return KeyConstants.PHI + 0.2;

                case SpecialKeyType.Enter:
                    return IsTablet ? max_ratio : mobile_special_ratio;

                case SpecialKeyType.Shift:
                    return IsTablet ? isOnLeft ? max_ratio : KeyConstants.PHI : mobile_special_ratio;

                case SpecialKeyType.NextKeyboard:
                    // TABLET ONLY
                    return def_key_ratio;

                case SpecialKeyType.SymbolToggle:
                    if(IsMobile) {
                        return mobile_special_ratio;

                    }
                    return isOnLeft ? max_ratio : def_key_ratio;//def_key_ratio : max_ratio;

                case SpecialKeyType.VoiceInput:
                    // TABLET ONLY
                    return def_key_ratio;

                case SpecialKeyType.Collapse:
                    // TABLET ONLY
                    return max_ratio;

                case SpecialKeyType.Emoji:
                    return def_key_ratio;

                default:
                    return IsMobile ? mobile_special_ratio : def_key_ratio;

            }
        }
        double GetRowDefaultKeyWidth(int rowIdx, KeyRowLayoutType layoutType) {
            if(rowIdx < 0 ||
                rowIdx >= Rows.Count ||
                Rows[rowIdx] is not { } row) {
                return 0;
            }
            int vis_cols =
                    //layoutType == KeyRowLayoutType.Stretch && row.All(x=>!x.IsSpaceBar) ?
                    //    row.Where(x => x.IsVisible).Count() :
                    MaxColCount;

            double w = (KeyboardWidth/* - KeyboardMargin.Left - KeyboardMargin.Right*/) / vis_cols;
            return w;
        }
        double GetRowDefaultKeyHeight(int rowIdx) {
            double def_height = (KeyGridHeight/* - KeyboardMargin.Top - KeyboardMargin.Bottom*/) / VisibleRowCount;
            if(IsNumPadLayout) {
                return def_height;
            }
            if(rowIdx == 0) {
                // num row
                if(!IsShowNumberRowEnabled) {
                    return 0;
                }
                return def_height * NumberRowHeightRatio;
            }

            double num_row_height = GetRowDefaultKeyHeight(0);
            if(num_row_height == 0) {
                return def_height;
            }
            double num_diff = def_height - num_row_height;
            double diff_per_row = num_diff / (VisibleRowCount - 1);
            return def_height + diff_per_row;
        }

        void MeasureKeys(bool invalidate) {
            KeyRowLayoutType layoutType = IsTablet ? KeyRowLayoutType.Stretch : KeyRowLayoutType.Center;
            double y = KeyboardMargin.Top;
            int visible_row_idx = 0;
            int visible_row_count = VisibleRowCount;
            foreach(var (row, rowIdx) in Rows.WithIndex()) {
                // KEY SIZE
                double def_width_for_row = GetRowDefaultKeyWidth(rowIdx, layoutType);
                double row_height = GetRowDefaultKeyHeight(rowIdx);
                foreach(var (kvm, colIdx) in row.WithIndex()) {
                    double w = 0;
                    double h = 0;
                    if(kvm.IsVisible) {
                        if(kvm.IsInput) {
                            w = def_width_for_row;
                            h = row_height;
                        } else {
                            w = def_width_for_row * GetSpecialKeyWidthRatio(kvm.SpecialKeyType, rowIdx, colIdx <= row.Count / 2d);
                            h = row_height;
                        }
                    }
                    kvm.SetKeySize(w, h, false);
                }

                if(row.All(x => !x.IsVisible)) {
                    // hidden row
                    row.ForEach(x => x.UpdateHitRect(-1, -1, -1, -1));
                    continue;
                }

                // ROW WIDTH
                double raw_row_width = row.Sum(x => x.Width);
                double row_overflow = KeyboardWidth - KeyboardMargin.Left - KeyboardMargin.Right - raw_row_width;

                if(row.FirstOrDefault(x => x.IsSpaceBar) is { } spacebar_kvm) {
                    if(row_overflow > 0) {
                        // give spacebar extra space
                        spacebar_kvm.SetKeySize(spacebar_kvm.Width + row_overflow, spacebar_kvm.Height, invalidate);
                        row_overflow = 0;
                    } else {
                        MpConsole.WriteLine($"Layout error, spacebar row should shorter than kb width");
                    }
                }
                IEnumerable<KeyViewModel> keys_to_resize = null;
                if(row_overflow < 0 || layoutType == KeyRowLayoutType.Stretch) {
                    // row is too big or needs stretch, distribute overflow evenly over the rows visible keys
                    keys_to_resize = row.Where(x => x.IsVisible);

                    if(layoutType == KeyRowLayoutType.Stretch && row.Where(x => x.IsVisible && x.IsSpecial) is { } row_specials &&
                        row_specials.Any()) {
                        // only stretch specials
                        keys_to_resize = row_specials;
                    }


                } else if(row_overflow > 0) {
                    // row is too small
                    if(row.All(x => x.IsVisible) || rowIdx == Rows.Count - 1) {
                        // row is NOT a centered keyboard row, 
                        keys_to_resize = row.Where(x => x.IsVisible);
                    }
                }
                if(keys_to_resize != null && keys_to_resize.Any()) {
                    double per_key_overflow_fix = row_overflow / keys_to_resize.Count();
                    keys_to_resize.ForEach(x => x.SetKeySize(x.Width + per_key_overflow_fix, x.Height, invalidate));

                    row_overflow = 0;
                }

                double x = KeyboardMargin.Left;
                switch(layoutType) {
                    case KeyRowLayoutType.Center:
                        x = row_overflow / 2;
                        break;
                    case KeyRowLayoutType.Right:
                        x = row_overflow;
                        break;
                }

                // KEY RECT
                int visible_col_idx = 0;
                int visible_col_count = row.Where(x => x.IsVisible).Count();
                foreach(var (kvm, colIdx) in row.WithIndex()) {
                    kvm.SetKeyPosition(x, y, invalidate);
                    x += kvm.Width;
                    if(kvm.IsVisible) {
                        kvm.UpdateHitRect(visible_row_idx, visible_col_idx++, visible_row_count, visible_col_count);
                    } else {
                        kvm.UpdateHitRect(-1, -1, visible_row_count, visible_col_count);
                    }
                }
                y += row_height;
                visible_row_idx++;
            }
        }
        #endregion

        #endregion

        #region Commands
        public ICommand NextKeyboardCommand => new MpCommand(() => {
            if(InputConnection is not IKeyboardInputConnection ic_ios
            ) {
                return;
            }
            ic_ios.OnInputModeSwitched();
        });

        public ICommand Test1Command => new MpCommand<object>(
            (args) => {

                //var test = new Border() {
                //    Background = Brushes.Purple,
                //    Width = 1000,
                //    Height = 1000,
                //    //Child = new Ellipse() {
                //    //    Width = 100,
                //    //    Height = 100,
                //    //    Fill = Brushes.Orange
                //    //}
                //    Child = new TestView() {
                //        Width = 100,
                //        Height = 100
                //    }
                //};
                //var rect = new Rect(0, 0, 500, 500);
                //var test = new TestView() {
                //    Width = rect.Width,
                //    Height = rect.Height
                //};
                //test.InitializeComponent();
                //test.Measure(rect.Size);
                //test.Arrange(rect);
                //test.UpdateLayout();
                //test.InvalidateVisual();

                //var test = MonkeyBoard.Builder.Build(InputConnection, new Size(KeyboardWidth, KeyGridHeight), ScreenScaling, out _);

                //RenderHelpers.RenderToFile(test, @"C:\Users\tkefauver\Desktop\test1.png");


            });
        #endregion
    }
}
