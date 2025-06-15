using Avalonia;
using Avalonia.Layout;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Point = Avalonia.Point;

namespace MonkeyBoard.Common {
    public class KeyViewModel : FrameViewModelBase {
        #region Private Variables
        #endregion

        #region Constants 

        public const string BACKSPACE_IMG_FILE_NAME = "backspace.png";//"⌫";
        public const string EMOJI_SELECT_BTN_IMG_FILE_NAME = "emoji.png";
        public const string COLLAPSE_KB_IMG_FILE_NAME = "kb_down_arrow.png";
        //public const string ENTER_IMG_FILE_NAME = "enter.png";//"⏎";
        public const string NEXT_KEYBOARD_IMG_FILE_NAME = "globe.png";
        public const string SEARCH_IMG_FILE_NAME = "search.png";//"🔍";
        public const string SHIFT_IMG_FILE_NAME = "shift.png";//"⇧";
        public const string SHIFT_LOCK_IMG_FILE_NAME = "shift_lock.png";
        public const string SHIFT_ON_IMG_FILE_NAME = "shift_on.png";


        #endregion

        #region Statics 
        public static string[] IMG_FILE_NAMES => [
            SHIFT_IMG_FILE_NAME,
            SHIFT_ON_IMG_FILE_NAME,
            SHIFT_LOCK_IMG_FILE_NAME,
            SEARCH_IMG_FILE_NAME,
            //ENTER_IMG_FILE_NAME,
            BACKSPACE_IMG_FILE_NAME,
            EMOJI_SELECT_BTN_IMG_FILE_NAME,
            COLLAPSE_KB_IMG_FILE_NAME,
            NEXT_KEYBOARD_IMG_FILE_NAME
        ];

        static string BACKSPACE_TEXT => ResourceStrings.K["BACKSPACE_TEXT"].value;
        static string SHIFT_TEXT_0 => ResourceStrings.K["SHIFT_TEXT_0"].value;
        static string SHIFT_TEXT_1 => ResourceStrings.K["SHIFT_TEXT_1"].value;
        static string SHIFT_TEXT_2 => ResourceStrings.K["SHIFT_TEXT_2"].value;
        static string TAB_TEXT => ResourceStrings.K["TAB_TEXT"].value;
        static string CAPS_LOCK_TEXT => ResourceStrings.K["CAPS_LOCK_TEXT"].value;
        static string GO_TEXT => ResourceStrings.K["GO_TEXT"].value;
        static string ENTER_TEXT => ResourceStrings.K["ENTER_TEXT"].value;
        static string PREVIOUS_TEXT => ResourceStrings.K["PREVIOUS_TEXT"].value;
        static string NEXT_TEXT => ResourceStrings.K["NEXT_TEXT"].value;
        static string SEND_TEXT => ResourceStrings.K["SEND_TEXT"].value;
        static string DONE_TEXT => ResourceStrings.K["DONE_TEXT"].value;
        static string SYMBOLS1_TEXT_TABLET => ResourceStrings.K["SYMBOLS1_TEXT_TABLET"].value;
        static string SYMBOLS1_TEXT => ResourceStrings.K["SYMBOLS1_TEXT"].value;
        static string SYMBOLS2_TEXT => ResourceStrings.K["SYMBOLS2_TEXT"].value;
        static string NUM_SYMBOLS1_TEXT => ResourceStrings.K["NUM_SYMBOLS1_TEXT"].value;
        static string NUM_SYMBOLS2_TEXT => ResourceStrings.K["NUM_SYMBOLS2_TEXT"].value;

        static string[] _SPECIAL_KEY_TEXTS;
        static string[] SPECIAL_KEY_TEXTS {
            get {
                if(_SPECIAL_KEY_TEXTS == null) {
                    _SPECIAL_KEY_TEXTS = [
                        ENTER_TEXT,
                        BACKSPACE_TEXT,
                        SHIFT_TEXT_0,
                        SYMBOLS1_TEXT_TABLET,
                        SHIFT_TEXT_1,
                        SHIFT_TEXT_2,
                        TAB_TEXT,
                        CAPS_LOCK_TEXT,
                        GO_TEXT,
                        PREVIOUS_TEXT,
                        NEXT_TEXT,
                        SEND_TEXT,
                        DONE_TEXT,
                        SYMBOLS1_TEXT,
                        SYMBOLS2_TEXT,
                        NUM_SYMBOLS1_TEXT,
                        NUM_SYMBOLS2_TEXT,
                    ];
                }
                return _SPECIAL_KEY_TEXTS;
            }
        }

        static bool IsPrimarySpecialKey(SpecialKeyType skt) {
            switch(skt) {
                case SpecialKeyType.Send:
                case SpecialKeyType.Done:
                case SpecialKeyType.Search:
                case SpecialKeyType.Go:
                case SpecialKeyType.Enter:
                case SpecialKeyType.Next:
                    return true;
                default:
                    return false;
            }
        }

        static string GetAlphasForNumeric(string num) {
            switch(num) {
                default:
                    return string.Empty;
                case "2":
                    return "ABC";
                case "3":
                    return "DEF";
                case "4":
                    return "GHI";
                case "5":
                    return "JKL";
                case "6":
                    return "MNO";
                case "7":
                    return "PQRS";
                case "8":
                    return "TUV";
                case "9":
                    return "WXYZ";
                case "0":
                    return "+";
            }
        }
        public static IEnumerable<string> GetSpecialKeyCharsOrResourceKeys(SpecialKeyType skt, bool isTablet) {
            switch(skt) {
                case SpecialKeyType.None:
                    yield break;
                case SpecialKeyType.Emoji:
                    yield return EMOJI_SELECT_BTN_IMG_FILE_NAME;
                    break;
                case SpecialKeyType.Shift:
                    if(isTablet) {
                        yield return SHIFT_TEXT_0;
                    } else {
                        yield return SHIFT_IMG_FILE_NAME;
                    }
                    yield return SHIFT_TEXT_1;
                    yield return SHIFT_TEXT_2;
                    break;
                case SpecialKeyType.Tab:
                    yield return TAB_TEXT;
                    break;
                case SpecialKeyType.CapsLock:
                    yield return CAPS_LOCK_TEXT;
                    break;
                case SpecialKeyType.Go:
                    yield return GO_TEXT;
                    break;
                case SpecialKeyType.Previous:
                    yield return PREVIOUS_TEXT;
                    break;
                case SpecialKeyType.Next:
                    yield return NEXT_TEXT;
                    break;
                case SpecialKeyType.Send:
                    yield return SEND_TEXT;
                    break;
                case SpecialKeyType.Done:
                    yield return DONE_TEXT;
                    break;
                case SpecialKeyType.Backspace:
                    if(isTablet) {
                        yield return BACKSPACE_TEXT;
                        break;
                    }
                    yield return BACKSPACE_IMG_FILE_NAME;
                    break;
                case SpecialKeyType.SymbolToggle:
                    if(isTablet) {
                        yield return SYMBOLS1_TEXT_TABLET;
                        yield return SYMBOLS2_TEXT;
                        break;
                    }
                    yield return SYMBOLS1_TEXT;
                    yield return SYMBOLS2_TEXT;
                    break;
                case SpecialKeyType.NumberSymbolsToggle:
                    yield return NUM_SYMBOLS1_TEXT;
                    yield return NUM_SYMBOLS2_TEXT;
                    break;
                case SpecialKeyType.Enter:
                    yield return ENTER_TEXT;
                    break;
                case SpecialKeyType.Search:
                    yield return SEARCH_IMG_FILE_NAME;
                    break;
                case SpecialKeyType.NextKeyboard:
                    yield return NEXT_KEYBOARD_IMG_FILE_NAME;
                    break;
                case SpecialKeyType.Collapse:
                    yield return COLLAPSE_KB_IMG_FILE_NAME;
                    break;
            }
        }
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void MeasureFrame(bool invalidate) {
            if(this is KeyViewModel kvm) {

                //kvm.RaisePropertyChanged(nameof(NeedsOuterTranslate));
                kvm.RaisePropertyChanged(nameof(IsSpecial));
                kvm.RaisePropertyChanged(nameof(PrimaryFontSize));
                kvm.RaisePropertyChanged(nameof(SecondaryFontSize));
                kvm.RaisePropertyChanged(nameof(X));
                kvm.RaisePropertyChanged(nameof(Y));
                kvm.RaisePropertyChanged(nameof(PullTranslateY));
            }

        }

        public override void PaintFrame(bool invalidate) {
            if(this is KeyViewModel kvm) {
                kvm.RaisePropertyChanged(nameof(IsSecondaryVisible));
                kvm.RaisePropertyChanged(nameof(IsPressed));
                kvm.RaisePropertyChanged(nameof(KeyOpacity));
                kvm.RaisePropertyChanged(nameof(PrimaryValue));
                kvm.RaisePropertyChanged(nameof(SecondaryValue));
                kvm.RaisePropertyChanged(nameof(IsShiftKeyAndOnTemp));
                kvm.RaisePropertyChanged(nameof(IsShiftKeyAndOnLock));
            }
        }

        public override void LayoutFrame(bool invalidate) {
            if(this is KeyViewModel kvm) {
                kvm.RaisePropertyChanged(nameof(ZIndex));
                kvm.RaisePropertyChanged(nameof(IsActiveKey));

                kvm.RaisePropertyChanged(nameof(Width));
                kvm.RaisePropertyChanged(nameof(Height));
                kvm.RaisePropertyChanged(nameof(InnerWidth));
                kvm.RaisePropertyChanged(nameof(InnerHeight));
                kvm.RaisePropertyChanged(nameof(CornerRadius));
            }
        }
        #endregion

        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new KeyboardViewModel Parent { get; set; }
        public IEnumerable<KeyNeighbor> Neighbors { get; set; } = [];
        public KeyViewModel PopupAnchorKey { get; private set; }

        public IEnumerable<KeyViewModel> PopupKeys =>
            Parent.VisiblePopupKeys
            .Where(x => x.PopupAnchorKey == this)
            .OrderBy(x => x.Row)
            .ThenBy(x => x.Column);
        public KeyViewModel DefaultPopupKey =>
            PopupKeys
            .FirstOrDefault(x => x.IsDefaultPopupKey);
        private KeyViewModel _activePopupKey;
        public KeyViewModel ActivePopupKey {
            get => _activePopupKey ?? DefaultPopupKey;
            private set {
                if(_activePopupKey != value) {
                    _activePopupKey = value;
                }
            }
        }

        #endregion

        #region Appearance
        public string BgHex { get; private set; }

        string _primaryHex;
        public string PopupBg =>
            KeyboardPalette.P[PaletteColorType.HoldBg];
        public string PopupFg =>
            KeyboardPalette.P[PaletteColorType.HoldFg];
        public string PopupSelectedBg =>
            KeyboardPalette.P[PaletteColorType.HoldFocusBg];
        public string PrimaryHex {
            get {
                if(!IsPulling) {
                    return _primaryHex;
                }
                double alpha = 255 - ((PullTranslateY / MaxPullTranslateY) * 255);
                return _primaryHex.AdjustAlpha(alpha / 255);//.AdjustAlpha((byte)alpha);
            }
            private set => _primaryHex = value;
        }
        string _secondaryHex;
        public string SecondaryHex {
            get {
                if(!IsPulling) {
                    return _secondaryHex;
                }
                return _secondaryHex.Lerp(_primaryHex, PullTranslateY / MaxPullTranslateY);
            }
            private set => _secondaryHex = value;
        }
        public double PrimaryFontSize =>
            Math.Max(5, Math.Min(InnerWidth, InnerHeight) * PrimaryFontSizeRatio * Parent.FloatFontScale);
        public double SecondaryFontSize {
            get {
                double fs = Math.Max(1, Math.Min(InnerWidth, InnerHeight) * SecondaryFontSizeRatio * Parent.FloatFontScale);
                if(!IsPulling) {
                    return fs;
                }
                double pull_percent = PullTranslateY / MaxPullTranslateY;
                fs = PrimaryFontSize * pull_percent;
                return fs;
            }
        }

        public bool IsSecondaryVisible {
            get {
                if(string.IsNullOrEmpty(SecondaryValue) || !IsVisible || PrimaryValue == SecondaryValue) {
                    return false;
                }
                if(Parent.IsNumPadLayout) {
                    if(SpecialKeyType == SpecialKeyType.NumberSymbolsToggle) {
                        return false;
                    }
                    if((Parent.IsDigits || Parent.IsPin) &&
                        (PrimaryValue == "*" || PrimaryValue == "0" || PrimaryValue == "#")) {
                        return false;
                    }
                    return true;
                }
                if(Parent.IsTextLayout && IsInput) {
                    if(!Parent.IsShowNumberRowEnabled) {
                        if(/*VisibleRow > 0 || */!Parent.IsLettersCharSet) {
                            return false;
                        }
                    } else {
                        if(!Parent.IsLettersCharSet) {
                            return false;
                        }
                    }

                    return true;
                }
                return false;
            }
        }
        public double KeyOpacity =>
            IsVisible ? 1 : 0;

        public override bool IsVisible {
            get {

                if(IsPopupKey) {
                    if(IsFakePopupKey) {
                        return true;
                    }
                    return PopupAnchorKey != null;
                }
                if(IsDotComKey) {
                    return Parent.IsUrlLayout || Parent.IsEmailLayout;
                }
                if(IsNumberRowKey) {
                    return Parent.IsShowNumberRowEnabled;
                }
                if(IsExtraUrlKey) {
                    return Parent.IsUrlLayout;
                }
                if(IsExtraEmailKey) {
                    return Parent.IsEmailLayout;
                }
                if(IsEmojiKey) {
                    return Parent.IsEmojiKeyVisible;
                }
                if(Row == Parent.Rows.Count - 1 &&
                    Parent.IsTablet &&
                    Parent.IsFreeTextLayout &&
                    !IsSpecial &&
                    PrimaryValue != null &&
                    (PrimaryValue.StartsWith(",") || PrimaryValue.StartsWith("."))) {
                    return false;
                }

                return !string.IsNullOrEmpty(CurrentChar);
            }
        }

        bool IsBgAlwaysVisible {
            get {
                // TODO should probably make some keys always have bg, like space etc like gboard
                return false;// IsSpaceBar;// || IsPrimarySpecial || IsShiftKeyAndOnTemp;
            }
        }
        #endregion

        #region Layout

        #region Factors
        public double PopupScale => 2;
        public double PopupKeyWidthRatio =>
            1.07;
        public double OuterPadX =>
            IsPopupKey ?
                0 :
                Parent.DefaultOuterPadX;
        public double OuterPadY =>
            IsPopupKey ?
                0 :
                Parent.DefaultOuterPadY;
        public double PrimaryFontSizeRatio {
            get {
                if(Parent.IsTablet && Parent.IsTextLayout) {
                    if(IsSpecial || IsDotComKey) {
                        return 0.25;
                    }
                    return 0.45;
                }
                if(IsDotComKey) {
                    return 0.3;
                }
                if(IsShiftKey && Parent.IsLettersCharSet) {
                    return 1;
                }
                if(IsBackspace) {
                    return 0.5;
                }
                if(IsPrimarySpecial) {
                    return 0.33;
                }
                if(IsSpecialDisplayText) {
                    return Parent.IsShowNumberRowEnabled ? 0.5 : 0.33;
                }
                if(IsShiftKey && !Parent.IsLettersCharSet) {
                    return 0.5;
                }
                if(Parent.IsNumPadLayout && IsInput) {
                    return 0.65;
                }
                return 0.65;
            }
        }

        //CornerRadius KeyCornerRadius =>
        //    Parent.CommonCornerRadius;
        //CornerRadius PopupCornerRadius { get; set; }
        public CornerRadius CornerRadius =>
            Parent.CommonCornerRadius;//IsPopupKey ? PopupCornerRadius : KeyCornerRadius;

        #endregion
        public int ZIndex =>
            IsPopupKey ? 1 : 0;

        public Rect PopupRect { get; private set; }

        public Rect PrimaryImageRect {
            get {
                double pad = 10;
                double w = (Math.Min(InnerWidth, InnerHeight) - pad) * (Parent.IsTablet && (IsEmojiKey || IsNextKeyboardKey) ? 0.65 : 1);
                double h = w;// * (IsPrimarySpecial ? 0.75:1);
                double x = (InnerRect.Width - w) / 2;
                double y = (InnerRect.Height - h) / 2;
                return new Rect(x, y, w, h);
            }
        }

        public Point InnerOffset =>
            InnerRect.Position - KeyboardRect.Position;

        public Rect InnerRect =>
            new Rect(X + (OuterPadX / 2), Y + (OuterPadY / 2), InnerWidth, InnerHeight).RoundToInt();

        public Rect KeyboardRect =>
            new Rect(X, Y, Width, Height);

        public Rect TotalRect =>
            KeyboardRect.Move(0, Parent.KeyGridRect.Top);

        public Rect TotalHitRect { get; private set; }

        public double SecondaryFontSizeRatio =>
            IsAlphaNumericNumber ? 0.2 : 0.25;
        public int VisiblePopupColCount { get; set; }
        public int VisiblePopupRowCount { get; set; }

        public HorizontalAlignment PrimaryTextHorizontalAlignment {
            get {
                if(IsPopupKey) {
                    return HorizontalAlignment.Center;
                }
                if(Parent.IsTablet && IsSpecial) {
                    return IsRightSideKey ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                }
                return HorizontalAlignment.Center;
            }
        }
        public VerticalAlignment PrimaryTextVerticalAlignment {
            get {
                if(Parent.IsNumPadLayout) {
                    return VerticalAlignment.Center;
                }
                if(OperatingSystem.IsAndroid() && IsPopupKey) {
                    return VerticalAlignment.Bottom;
                }
                if(Parent.IsTablet && IsSpecial) {
                    return VerticalAlignment.Bottom;
                }
                return IsSecondaryVisible ? VerticalAlignment.Bottom : VerticalAlignment.Center;// Parent.IsPullEnabled ? VerticalAlignment.Bottom : VerticalAlignment.Center;
            }
        }

        public HorizontalAlignment SecondaryTextHorizontalAlignment {
            get {
                if(Parent.IsNumPadLayout) {
                    return HorizontalAlignment.Right;
                }
                return Parent.IsPullEnabled ? HorizontalAlignment.Center : HorizontalAlignment.Right;
            }
        }
        public VerticalAlignment SecondaryTextVerticalAlignment {
            get {
                if(Parent.IsNumPadLayout) {
                    return VerticalAlignment.Center;
                }
                return VerticalAlignment.Top;
            }
        }
        public Point PrimaryTextOffset {
            get {
                double x_pad = 3;
                double y_pad = 3;
                //double min_side = Math.Min(InnerWidth, InnerHeight);
                //double x_pad = min_side / 10d;
                //double y_pad = min_side / 10d;
                if(OperatingSystem.IsAndroid()) {
                    if(IsSecondaryVisible) {
                        y_pad = Parent.IsTablet ? 7 : 10;
                        //y_pad = Math.Ceiling(y_pad * (Parent.isTablet ? 2:3));
                    }
                }
                if(OperatingSystem.IsAndroid() && IsPopupKey) {
                    y_pad = InnerHeight / 3;
                }

                var result = GetAlignedOffset(PrimaryTextHorizontalAlignment, PrimaryTextVerticalAlignment, x_pad, y_pad) + new Point(0, PullTranslateY);
                return result * KeyboardViewModel.FloatFontScale;
            }
        }
        public Point SecondaryTextOffset {
            get {
                double min_x = 3;
                double max_x = InnerWidth / 2;

                double x_pad = Math.Clamp(Parent.CommonCornerRadius.TopLeft / (SecondaryValue.ToStringOrEmpty().Length + 2), min_x, max_x);
                double y_pad = 3;
                if(OperatingSystem.IsAndroid()) {
                    if(Parent.IsPullEnabled) {
                        if(IsInput) {
                            //y_pad = Math.Ceiling(y_pad * 2d);
                            y_pad = 7;
                        }
                    }
                } else {
                    y_pad = 1;
                    //y_pad = y_pad / 3d;
                }
                double max_y = y_pad;
                double min_y = InnerHeight / 4d;
                if(Parent.IsNumPadLayout) {
                    x_pad = min_x;
                    y_pad = 0;
                }
                var result = GetAlignedOffset(SecondaryTextHorizontalAlignment, SecondaryTextVerticalAlignment, x_pad, y_pad) + new Point(0, PullTranslateY);
                return result * KeyboardViewModel.FloatFontScale;
            }
        }
        public bool IsRightSideKey { get; private set; }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public double InnerWidth =>
            Width - OuterPadX;
        public double InnerHeight =>
            Height - OuterPadY;

        public bool IsVisibleKeyboardKey =>
            !IsPopupKey && IsVisible;

        public int Row { get; set; }

        public int Column { get; set; }

        #endregion

        #region State
        public bool CanDoubleTap {
            get {
                if(IsPopupKey) {
                    return false;
                }
                bool was_last = Parent.LastPressedKey == this &&
                        Parent.LastReleasedKey == this;
                if(IsSpaceBar) {
                    return
                        Parent.IsDoubleTapSpaceEnabled &&
                        !Parent.IsNumPadLayout &&
                        was_last;
                }
                return Parent.IsDoubleTapEnabled &&
                    was_last &&
                    !string.IsNullOrEmpty(SecondaryValue);
            }
        }
        public bool CanHaveShadow =>
            Parent.IsShadowsEnabled &&
            Parent.IsKeyBordersVisible &&
            IsVisible &&
                !IsPopupKey;
        public bool IsActionOnRelease =>
            !IsActionOnPress;
        public bool IsActionOnPress =>
            IsBackspace || (IsShiftKey && Parent.IsTablet);// || IsSymbolToggle || IsShiftKey;
        public bool IsHoldMenuOpen =>
            PopupKeys.Any() && PopupKeys.Skip(1).Any();
        bool IsHoldPopupKey =>
            PopupAnchorKey != null && PopupAnchorKey.IsHoldMenuOpen;
        bool IsSpecialDisplayText =>
            IsSpecial && SPECIAL_KEY_TEXTS.Contains(CurrentChar);
        public bool IsPrimaryImage =>
            CurrentChar != null && CurrentChar.EndsWith(".png");
        public bool CanShowPressPopup {
            get {
                if(!Parent.IsPressPopupsEnabled) {
                    return false;
                }
                //if(!Parent.IsShowNumberRowEnabled && Parent.IsTextLayout && Row == 0) {
                //    return true;
                //}
                return HasPressPopup;
            }
        }
        public bool CanShowHoldPopup {
            get {
                if(!Parent.IsHoldPopupsEnabled ||
                    IsPopupKey) {
                    return false;
                }
                //if(!Parent.IsShowNumberRowEnabled && Parent.IsTextLayout && Row == 0) {
                //    return true;
                //}
                return HasHoldPopup;
            }
        }
        public DateTime? PressPopupShowDt { get; set; }
        public bool IsDefaultPopupKey =>
            PopupAnchorKey != null &&
            PopupAnchorKey.PopupCharacters.FirstOrDefault() == CurrentChar;
        public bool IsPrimarySpecial =>
            IsPrimarySpecialKey(SpecialKeyType);
        public string TouchId { get; set; }
        public DateTime? LastPressDt { get; set; }
        public DateTime? LastReleaseDt { get; set; }
        public bool CanPullKey =>
            !IsPopupKey &&
            !IsHoldMenuOpen &&
            Parent.IsLettersCharSet &&
            Parent.ShiftKeys.All(x => !x.IsPressed) &&
            !string.IsNullOrEmpty(SecondaryValue);
        public bool IsPulling =>
            PullTranslateY > 5;
        public bool IsPulled =>
            PullTranslateY >= MaxPullTranslateY;
        double _pullTranslateY;
        public double PullTranslateY {
            get => CanPullKey ? _pullTranslateY : 0;
            set => _pullTranslateY = value;
        }
        public double MaxPullTranslateY =>
            InnerHeight / 4;

        public bool HasAnyPopup =>
            HasPressPopup || HasHoldPopup;
        public bool HasPressPopup =>
            IsInput && !IsSpaceBar;
        public bool HasHoldPopup =>
            IsInput && PopupCharacters.Any(x => x != CurrentChar);

        public bool IsActiveKey =>
            PopupAnchorKey != null && PopupAnchorKey.ActivePopupKey == this;

        public bool IsPressed { get; private set; }
        public bool IsSoftPressed { get; private set; }
        public bool IsPopupKey =>
            PopupKeyIdx >= 0;
        public bool IsFakePopupKey { get; set; }

        public int PopupKeyIdx { get; set; } = -1;

        public bool IsSpecial =>
            SpecialKeyType != SpecialKeyType.None;
        public bool IsPeriod =>
            CurrentChar == ".";
        public bool IsCarriageReturn =>
            SpecialKeyType == SpecialKeyType.Enter;
        public bool IsBackspace =>
            SpecialKeyType == SpecialKeyType.Backspace;
        public bool IsShiftKey =>
            SpecialKeyType == SpecialKeyType.Shift;
        public bool IsTabKey =>
            SpecialKeyType == SpecialKeyType.Tab;
        public bool IsDotComKey => Parent.DotComKey == this;
        bool IsExtraUrlKey =>
            CurrentChar == KeyConstants.URL_KEY_1 && Row == Parent.RowCount - 1;
        bool IsExtraEmailKey =>
            CurrentChar == KeyConstants.EMAIL_KEY_1 && Row == Parent.RowCount - 1;
        public bool IsSymbolToggle =>
            SpecialKeyType == SpecialKeyType.SymbolToggle;
        public bool IsSpaceBar =>
            CurrentChar == KeyConstants.SPACE_STR;
        public bool IsEmojiKey =>
            CurrentChar == EMOJI_SELECT_BTN_IMG_FILE_NAME;
        bool IsNextKeyboardKey =>
            CurrentChar == NEXT_KEYBOARD_IMG_FILE_NAME;
        bool IsAlphaNumericNumber =>
            Parent.IsNumPadLayout &&
                        (Parent.IsDigits || Parent.IsPin) &&
                        IsNumber;
        public bool IsNumber =>
            CurrentChar.Length == 1 &&
            char.IsNumber(CurrentChar[0]);
        public bool IsLetter =>
            CurrentChar.Length == 1 &&
            char.IsLetter(CurrentChar[0]);
        bool IsNumberRowKey =>
            IsNumber &&
            Parent.IsTextLayout &&
            //Parent.IsLettersCharSet &&
            Row == 0;
        public bool IsInput =>
            !IsSpecial;
        public bool IsShiftKeyAndOnTemp =>
            IsShiftKey &&
            Parent.IsLettersCharSet &&
            Parent.IsShiftOnTemp;
        public bool IsShiftKeyAndOnLock =>
            IsShiftKey &&
            Parent.IsLettersCharSet &&
            Parent.IsShiftOnLock;

        public string CurrentChar { get; private set; }

        public string SecondaryValue { get; private set; }
        public string PrimaryValue {
            get {
                if(Parent.IsAnyShiftState && IsInput && !IsDotComKey && Parent.IsLettersCharSet && !IsPopupKey) {
                    return PrimaryShiftValue;
                }
                if(Parent.IsAnyShiftState && IsPopupKey) {
                    // workaround to show shifted popups (since shift val is passive now)
                    return CurrentChar.ToUpper();
                }
                if(Parent.IsTablet && IsSpecial && IsSpecialDisplayText) {
                    return CurrentChar.ToLower();//.Replace(" ",string.Empty);
                }
                return CurrentChar;
            }
        }

        string PrimaryShiftValue { get; set; }

        public IEnumerable<string> PopupCharacters { get; private set; }

        #endregion

        #region Model
        public SpecialKeyType SpecialKeyType { get; set; }
        public ObservableCollection<string> Characters { get; set; } = [];
        #endregion

        #endregion

        #region Events
        public event EventHandler OnCleanup;

        #endregion

        #region Constructors
        public KeyViewModel(KeyboardViewModel parent, object keyObj, int r, int c) {
#if DEBUG
            PropertyChanged += KeyViewModel_PropertyChanged;
#endif
            Parent = parent;
            Row = r;
            Column = c;
            Init(keyObj);
        }

        private void KeyViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch(e.PropertyName) {
                case nameof(IsRightSideKey):
                    if(!IsRightSideKey && Characters.Any(x => x.ToLower() == "o")) {

                    }
                    break;
            }
        }

        #endregion

        #region Public Methods
        public void Cleanup() {
            OnCleanup?.Invoke(this, EventArgs.Empty);
        }

        public void SetPressed(bool isPressed, Touch t, bool isSoft = false, bool invalidate = true) {
            if(isPressed) {
                IsPressed = true;
                IsSoftPressed = isSoft;
                TouchId = t.Id;
                LastPressDt = DateTime.Now;
                if(!Parent.PressedKeys.Contains(this)) {
                    Parent.PressedKeys.Add(this);
                }
                Parent.LastPressedKey = this;
            } else {
                ActivePopupKey = null;
                IsPressed = false;
                IsSoftPressed = false;
                PullTranslateY = 0;
                LastReleaseDt = DateTime.Now;
                TouchId = null;
                Parent.PressedKeys.Remove(this);
                Parent.LastReleasedKey = this;
                ClearPopups(invalidate);
            }
            if(invalidate) {
                Renderer.PaintFrame(true);
            }
        }

        public void SetKeySize(double w, double h, bool invalidate = false) {
            if(Width == w && Height == h) {
                return;
            }
            Width = w;
            Height = h;
            if(invalidate) {
                Renderer.RenderFrame(true);
            }
        }
        public void UpdateHitRect(int vr, int vc, int tvr, int tvc) {
            if(!IsVisible) {
                TotalHitRect = new();
                return;
            }
            var IsLeftEdge = vc == 0;
            var IsTopEdge = vr == 0;
            var IsRightEdge = vc == tvc - 1;
            var IsBottomEdge = vr == tvr - 1;

            var tRect = TotalRect;
            double dl = 0, dt = 0, dr = 0, db = 0;
            if(IsLeftEdge) {
                dl = Parent.KeyGridRect.Left - tRect.Left;
            }
            if(IsTopEdge) {
                dt = Parent.KeyGridRect.Top - tRect.Top;
            }
            if(IsRightEdge) {
                dr = Parent.KeyGridRect.Right - tRect.Right;
            }
            if(IsBottomEdge) {
                db = Parent.KeyGridRect.Bottom - tRect.Bottom;
            }
            TotalHitRect = tRect.Flate(dl, dt, dr, db);
        }
        public void SetKeyPosition(double x, double y, bool invalidate = false) {
            if(X == x && Y == y) {
                return;
            }
            X = x;
            Y = y;

            IsRightSideKey = X + (Width / 2) > (Parent.KeyboardWidth / 2);

            if(invalidate) {
                Renderer.RenderFrame(true);
            }
        }

        #region Popups
        public void UpdateActivePopup(Touch touch) {
            KeyViewModel last_active = ActivePopupKey;

            if(!Parent.CanShowPopupWindows && IsHoldMenuOpen) {
                var adj_loc = touch.Location - new Point(0, PopupRect.Top);
                if(PopupKeys.FirstOrDefault(x => x.TotalHitRect.Contains(adj_loc)) is { } active_pukvm &&
                    !active_pukvm.IsFakePopupKey) {
                    ActivePopupKey = active_pukvm;
                }
            } else {
                ActivePopupKey = PopupKeys.FirstOrDefault(x => x.CheckIsActive(touch, false));
            }
            if(PopupKeys.Skip(1).Any()) {

            }
            foreach(var pukvm in PopupKeys) {
                pukvm.SetBrushes();
                pukvm.Renderer.RenderFrame(true);
            }

            if(last_active != ActivePopupKey &&
                ActivePopupKey != null) {
                Parent.InputConnection.OnFeedback(Parent.FeedbackClick);
            }
        }
        public void FitPopupInFrame(Touch touch) {
            if(!PopupKeys.Any()) {
                return;
            }

            double l = PopupKeys.Min(x => x.TotalRect.Left);
            double t = PopupKeys.Min(x => x.TotalRect.Top);
            double r = PopupKeys.Max(x => x.TotalRect.Right);
            double b = PopupKeys.Max(x => x.TotalRect.Bottom);
            PopupRect = new Rect(l, t, r - l, b - t);

            if(Parent.CanShowPopupWindows || !IsHoldMenuOpen) {
                // don't need to fit
                PopupRect = PopupRect.PositionAbove(
                    anchorRect: TotalRect,
                    outerRect: Parent.KeyGridRect);
            } else {
                PopupKeys.ForEach(x => x.SetPopupKeyRect(this));

                double tl = PopupKeys.Min(x => x.TotalRect.Left);
                double tt = PopupKeys.Min(x => x.TotalRect.Top);
                double tr = PopupKeys.Max(x => x.TotalRect.Right);
                double tb = PopupKeys.Max(x => x.TotalRect.Bottom);
                PopupRect = new Rect(tl, tt, tr - tl, tb - tt);
            }

            //var tl_kvm = PopupKeys.Aggregate((a, b) => a.TotalRect.TopLeft.Distance(PopupRect.TopLeft) < b.TotalRect.TopLeft.Distance(PopupRect.TopLeft) ? a : b);
            //var tr_kvm = PopupKeys.Aggregate((a, b) => a.TotalRect.TopRight.Distance(PopupRect.TopRight) < b.TotalRect.TopRight.Distance(PopupRect.TopRight) ? a : b);
            //var br_kvm = PopupKeys.Aggregate((a, b) => a.TotalRect.BottomRight.Distance(PopupRect.BottomRight) < b.TotalRect.BottomRight.Distance(PopupRect.BottomRight) ? a : b);
            //var bl_kvm = PopupKeys.Aggregate((a, b) => a.TotalRect.BottomLeft.Distance(PopupRect.BottomLeft) < b.TotalRect.BottomLeft.Distance(PopupRect.BottomLeft) ? a : b);

            //double cr = Parent.CommonCornerRadius.TopLeft;
            //foreach (var pu_kvm in PopupKeys) {
            //    double tl = 0;
            //    double tr = 0;
            //    double bl = 0;
            //    double br = 0;
            //    if (pu_kvm == tl_kvm) {
            //        tl = cr;
            //    }
            //    if (pu_kvm == tr_kvm) {
            //        tr = cr;
            //    }
            //    if (pu_kvm == br_kvm) {
            //        br = cr;
            //    }
            //    if (pu_kvm == bl_kvm) {
            //        bl = cr;
            //    }
            //    pu_kvm.PopupCornerRadius = new CornerRadius(tl, tr, br, bl);
            //}

            UpdateActivePopup(touch);
            this.Renderer.RenderFrame(true);
        }
        public void AddPopupAnchor(int r, int c, string disp_val) {
            //var test = Parent.PopupKeys.ToList();
            if(Parent.PopupKeys.FirstOrDefault(x => x.Row == r && x.Column == c && (x.PopupAnchorKey == null || x.PopupAnchorKey == this)) is not { } pukvm) {
                // already added
                return;
            }

            pukvm.SetPopupAnchor(this, disp_val);
            if(pukvm.IsVisible) {
                VisiblePopupColCount = Math.Max(c + 1, VisiblePopupColCount);
                VisiblePopupRowCount = Math.Max(r + 1, VisiblePopupRowCount);
            }
        }
        public void SetPopupAnchor(KeyViewModel anchor_kvm, string disp_val) {
            PopupAnchorKey = anchor_kvm;
            SetCharacters([disp_val]);
            SetPopupKeyRect(anchor_kvm);
            IsFakePopupKey = string.IsNullOrEmpty(disp_val);
            if(!Parent.VisiblePopupKeys.Contains(this)) {
                Parent.VisiblePopupKeys.Add(this);
            }
        }
        public void RemovePopupAnchor(bool invalidate) {
            PopupAnchorKey = null;
            IsFakePopupKey = false;
            Parent.VisiblePopupKeys.Remove(this);
            Renderer.LayoutFrame(false);
            SetCharacters([]);
            SetPopupKeyRect(null);
            if(invalidate) {
                Renderer.PaintFrame(true);
            }
        }

        public void ClearPopups(bool invalidate = true) {
            bool had_popups = VisiblePopupColCount > 0 || VisiblePopupRowCount > 0;
            VisiblePopupColCount = 0;
            VisiblePopupRowCount = 0;
            ActivePopupKey = null;
            var to_rmv = PopupKeys.ToArray();
            foreach(var rmv in to_rmv) {
                rmv.RemovePopupAnchor(invalidate);
            }
            if(had_popups) {
                Parent.HidePopup(this);
                //OnHidePopup?.Invoke(this, EventArgs.Empty);
            }
        }


        #endregion

        #region Display Values

        public IEnumerable<string> GetPopupCharacters() {
            List<string> pucl = [];

            if(!string.IsNullOrEmpty(SecondaryValue)) {
                // insert secondary for mobile
                pucl.Add(SecondaryValue);
                if(!Parent.IsShowLetterGroupPopupsEnabled) {
                    return pucl;
                }
            }
            if(string.IsNullOrEmpty(CurrentChar)) {
                return pucl;
            }
            // add primary char to beginning
            string lcc = CurrentChar.ToLower();
            if(Parent.IsShowLetterGroupPopupsEnabled &&
                Parent.LetterGroups.FirstOrDefault(x => x.Any(y => y == lcc)) is { } lgl) {
                // add letter group (with current char first)
                pucl.AddRange(lgl.OrderBy(x => x == lcc ? 0 : 1));
            } else {
                pucl.Add(CurrentChar);
            }
            return pucl;
        }
        public void SetCharacters(IEnumerable<string> chars) {
            Characters.Clear();
            Characters.AddRange(chars);
            UpdateCharacters();
            if(IsPopupKey) {
                return;
            }
            PopupCharacters = GetPopupCharacters();
        }
        public void UpdateCharacters() {
            if(IsShiftKey) {
                if(Parent.IsMobile) {
                    if(Parent.IsSymbols1CharSet) {
                        CurrentChar = SHIFT_TEXT_1;
                    } else if(Parent.IsSymbols2CharSet) {
                        CurrentChar = SHIFT_TEXT_2;
                    } else if(IsShiftKeyAndOnLock) {
                        CurrentChar = SHIFT_LOCK_IMG_FILE_NAME;
                    } else if(IsShiftKeyAndOnTemp) {
                        CurrentChar = SHIFT_ON_IMG_FILE_NAME;
                    } else {
                        CurrentChar = SHIFT_IMG_FILE_NAME;
                    }
                } else {
                    CurrentChar = Parent.IsLettersCharSet ?
                        SHIFT_TEXT_0 :
                        Parent.IsSymbols1CharSet ?
                            SYMBOLS1_TEXT :
                            SYMBOLS2_TEXT;
                }
            } else if(IsSymbolToggle && Parent.IsMobile && !Parent.IsNumPadLayout) {
                if(Parent.IsLettersCharSet) {
                    CurrentChar = SYMBOLS1_TEXT;
                } else {
                    CurrentChar = SYMBOLS2_TEXT;
                }
            } else if(Characters.Any()) {
                int char_idx = Parent.CharSetIdx >= Characters.Count ? 0 : Parent.CharSetIdx;
                CurrentChar = Characters[char_idx] ?? string.Empty;
                if(IsAlphaNumericNumber) {
                    SecondaryValue = GetAlphasForNumeric(CurrentChar);
                } else {
                    int next_idx = char_idx + 1;
                    if(next_idx >= Characters.Count) {
                        SecondaryValue = string.Empty;
                    } else {
                        SecondaryValue = Characters[next_idx] ?? string.Empty;
                    }
                }
            } else {
                CurrentChar = string.Empty;
            }
        }

        public void ForceCurrentChar(string displayVal) {
            CurrentChar = displayVal;
            //MpConsole.WriteLine($"Primary val forced to '{PrimaryValue}' CurrChar: '{CurrentChar}'");
            this.Renderer.RenderFrame(true);
        }
        #endregion

        public override string ToString() {
            return $"'{PrimaryValue}' X:{(int)X} Y:{(int)Y} W:{(int)Width} H:{(int)Height}";
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        private void Init(object keyObj) {
            List<string> chars = [];

            string ks = default;
            string sv = string.Empty;

            if(keyObj is Tuple<string, string> input_tup &&
                input_tup.Item1 is { } ks1) {
                ks = ks1;
                sv = input_tup.Item2 ?? string.Empty;
            } else if(keyObj is string ks2) {
                ks = ks2;
            }

            if(ks is string keyStr &&
                keyStr.SplitNoEmpty(",") is { } keyParts) {
                // special strings:
                // none
                // comma
                if(!keyParts.Any() && keyStr == ",") {
                    // encode comma for comma popup from its decoded display value
                    keyParts = ["comma"];
                }
                foreach(string kp in keyParts) {
                    string text = kp;
                    if(text.ToLower() == "none") {
                        // none implies this row is centered (needs translation)
                        // Parent.TranslatedRow = VisibleRow;
                        text = null;
                        sv = null;
                    } else if(text.ToLower() == "comma" || text.ToLower() == "commacommacomma") {
                        text = ",";
                        sv = null;
                        //sv = text;
                    }
                    chars.Add(text);
                }
                PrimaryShiftValue = sv;
                if(string.IsNullOrEmpty(PrimaryShiftValue) &&
                    chars.FirstOrDefault() is { } pv) {
                    PrimaryShiftValue = pv.ToUpper();
                }

            } else if(keyObj is SpecialKeyType skt) {
                SpecialKeyType = skt;
                chars.AddRange(GetSpecialKeyCharsOrResourceKeys(skt, Parent.IsTablet));
            } else if(keyObj is int popupIdx) {
                PopupKeyIdx = popupIdx;
            }
            SetCharacters(chars);
            //Renderer.RenderFrame(true);
        }

        public void SetPopupKeyRect(KeyViewModel anchor_kvm) {
            if(!IsPopupKey) {
                return;
            }

            Width = anchor_kvm == null ? 0 : anchor_kvm.Width;
            Height = anchor_kvm == null ? 0 : anchor_kvm.Height;

            bool is_preview_popup = Parent.CanShowPopupWindows ||
                   (!Parent.CanShowPopupWindows && !IsHoldPopupKey);

            if(is_preview_popup) {
                Width *= PopupKeyWidthRatio;
            } else if(anchor_kvm != null) {
                // full grid popup
                int cols = anchor_kvm.VisiblePopupColCount;
                int rows = anchor_kvm.VisiblePopupRowCount;
                Width = KeyboardViewModel.KeyGridRect.Width / cols;
                Height = KeyboardViewModel.KeyGridRect.Height / rows;
            }
            X = Column * Width;
            Y = Row * Height;

            TotalHitRect = TotalRect;
        }
        public void SetBrushes() {
            PaletteColorType bg = PaletteColorType.DefaultKeyBg;
            PaletteColorType fg = PaletteColorType.Fg;

            bool pressed = IsPressed;
            if(IsSpecial) {
                if(SpecialKeyType == SpecialKeyType.CapsLock && Parent.IsShiftOnLock) {
                    pressed = true;
                }
                if(IsShiftKey && Parent.IsAnyShiftState && Parent.IsTablet) {
                    pressed = true;
                }
                if(Parent.IsTablet) {
                    if(IsPrimarySpecial) {
                        bg = PaletteColorType.SpecialShiftBg;
                    } else {
                        bg = PaletteColorType.SpecialBg;
                    }
                    if(pressed) {
                        fg = PaletteColorType.Bg;
                    }
                } else {
                    switch(SpecialKeyType) {
                        case SpecialKeyType.Shift:
                            bg = PaletteColorType.SpecialShiftBg;
                            break;
                        case SpecialKeyType.SymbolToggle:
                            bg = PaletteColorType.SpecialSymbolBg;
                            break;
                        case SpecialKeyType.Backspace:
                            bg = PaletteColorType.SpecialBackspaceBg;
                            break;
                        case SpecialKeyType.Emoji:
                            //bg = PaletteColorType.DefaultKeyBg;
                            break;
                        default:
                            if(IsPrimarySpecialKey(SpecialKeyType)) {
                                bg = PaletteColorType.SpecialPrimaryBg;
                            }
                            break;
                    }
                }

            } else if(IsPopupKey) {
                bg = IsActiveKey ? PaletteColorType.HoldFocusBg : PaletteColorType.HoldBg;
                fg = PaletteColorType.HoldFg;
            }
            BgHex = KeyboardPalette.Get(bg, pressed);
            PrimaryHex = KeyboardPalette.P[fg];

            if(IsSecondaryVisible) {
                SecondaryHex = KeyboardPalette.P[PaletteColorType.Fg2];
            } else {
                SecondaryHex = KeyboardPalette.Transparent;
            }

            if(!IsPopupKey &&
                !IsPressed &&
                !Parent.IsKeyBordersVisible &&
                !IsBgAlwaysVisible) {
                BgHex = KeyboardPalette.Transparent;
            }
        }
        bool CheckIsActive(Touch touch, bool debug) {
            if(IsPressed) {
                // popupkeys take active for input
                return !HasAnyPopup;
            }
            if(!IsPopupKey ||
                IsFakePopupKey ||
                PopupAnchorKey is not { } anchor_kvm ||
                anchor_kvm.DefaultPopupKey is not { } def_kvm) {
                return false;
            }

            int rc = anchor_kvm.VisiblePopupRowCount;
            int cc = anchor_kvm.VisiblePopupColCount;
            // count empties on last row so pivot (on right side is last VISIBLE idx)
            //int empty_count = anchor_kvm.PopupCharacters.Count() % (rc * cc);
            int pivot_row = 0;//anchor_kvm.IsRightSideKey ? rc - 1 : 0;
            int pivot_col = anchor_kvm.IsRightSideKey ? cc - 1 : 0;//anchor_kvm.IsRightSideKey ? cc - 1 - empty_count : 0;

            var disp_characters = anchor_kvm.PopupCharacters.ToList();
            if(anchor_kvm.IsRightSideKey) {
                disp_characters.Reverse();
            }
            double multiplier = 1d;
            if(!Parent.CanShowPopupWindows && IsHoldPopupKey) {
                //multiplier = PopupAnchorKey.VisiblePopupColCount * PopupAnchorKey.VisiblePopupRowCount;
                pivot_col = (int)(touch.PressLocation.X / Width);
                pivot_row = (int)(touch.PressLocation.Y / Height);
            }
            bool is_last = disp_characters.IndexOf(CurrentChar) == disp_characters.Count - 1;
            //MpConsole.WriteLine($"pivot r: {pivot_row} c: {pivot_col}");
            return new Rect(X, Y, Width, Height).IsPopupHit(
                r: Row,
                c: Column,
                pivotRow: pivot_row,
                pivotCol: pivot_col,
                isLast: is_last,
                rows: rc,
                cols: cc,
                anchorRect: def_kvm.KeyboardRect,
                rootRect: KeyboardViewModel.TotalRect,//new Rect(0, 0, Parent.KeyboardWidth, Parent.KeyGridHeight),
                pressLocation: touch.PressLocation,
                location: touch.Location,
                lastLocation: touch.LastLocation,
                multiplier: multiplier);
        }

        Point GetAlignedOffset(HorizontalAlignment ha, VerticalAlignment va, double x_pad, double y_pad) {
            double x = 0;
            double y = 0;
            switch(ha) {
                case HorizontalAlignment.Left:
                    x = x_pad;
                    break;
                case HorizontalAlignment.Right:
                    x = -x_pad;
                    break;
            }
            switch(va) {
                case VerticalAlignment.Top:
                    y = y_pad;
                    break;
                case VerticalAlignment.Bottom:
                    y = -y_pad;
                    break;
            }
            return new Point(x, y);
        }


        void DrawActiveDebug(Rect hitRect, Point p) {
#if DEBUG && false
            var colors = new IBrush[]{
                Brushes.Red,
                Brushes.Orange,
                Brushes.Yellow,
                Brushes.Green,
                Brushes.Blue,
                Brushes.Indigo,
                Brushes.Violet,
                Brushes.Purple,
                Brushes.Red,
                Brushes.Orange,
                Brushes.Yellow,
                Brushes.Green,
                Brushes.Blue,
                Brushes.Indigo,
                Brushes.Violet,
                Brushes.Purple, };
            Color color = Colors.Pink;
            if (PopupKeyIdx < colors.Length && colors[PopupKeyIdx] is ImmutableSolidColorBrush scb) {
                color = scb.Color;
            }

            var cnvs = KeyboardGridView.DebugCanvas;

            var rect = new Avalonia.Controls.Shapes.Rectangle() {
                Tag = PopupKeyIdx,
                Opacity = 0.5,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(color, 0.5),
                Width = hitRect.Width,
                Height = hitRect.Height
            };

            cnvs.Children.Add(rect);
            Canvas.SetLeft(rect, hitRect.X);
            Canvas.SetTop(rect, hitRect.Y);

            double r = 2.5;
            var ellipse = new Avalonia.Controls.Shapes.Ellipse() {
                Opacity = 0.5,
                Fill = Brushes.Red,
                Width = r * 2,
                Height = r * 2
            };

            cnvs.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, p.X - r);
            Canvas.SetTop(ellipse, p.Y - r);
#endif
        }
        #endregion
    }
}
