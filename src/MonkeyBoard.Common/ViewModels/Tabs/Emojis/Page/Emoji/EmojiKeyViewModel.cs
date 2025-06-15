using Avalonia;
using Avalonia.Media;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MonkeyBoard.Common {
    public class EmojiKeyViewModel : FrameViewModelBase, IFrameRenderer, IFrameRenderContext {
        #region Private Variables
        #endregion
        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region Interfaces
        #endregion

        #endregion

        #region Properties

        #region Members
        public override IFrameRenderer Renderer =>
            IsPopupOpen ? _renderer ?? this : Parent.Renderer;
        #endregion

        #region View Models
        public new EmojiPageViewModel Parent { get; set; }

        public string[] Items { get; private set; }

        #endregion

        #region Appearance
        public string PopupItemBgHexColor =>
            KeyboardPalette.P[PaletteColorType.HoldBg];
        public string PopupItemFocusedBgHexColor =>
            KeyboardPalette.P[PaletteColorType.HoldFocusBg];
        public string EmojiBgHexColor =>
            IsPressed &&
            !Parent.IsScrollingY &&
            !Parent.Parent.IsScrollingX ?
                KeyboardPalette.Get(PaletteColorType.DefaultKeyBg, true) :
                null;
        public string EmojiFgHexColor =>
            Parent.Parent.EmojiPagesFgHexColor;

        public string PopupHintBgHexColor =>
            KeyboardPalette.P[PaletteColorType.Fg2];

        public string Summary =>
            $"E{EmojiModel.Version} | {EmojiModel.SearchText} | {EmojiModel.Qualification.ToTitleCase()}";
        #endregion

        #region Layout
        public override Rect Frame => EmojiRect;
        public int Row => PageItemIdx / Parent.Parent.MaxColCount;
        public int Column => PageItemIdx % Parent.Parent.MaxColCount;
        public int PageItemIdx { get; set; }
        public double EmojiFontSize =>
            Parent.Parent.EmojiFontSize;

        public Rect EmojiRect {
            get {
                double w = Parent.Parent.DefaultEmojiWidth;
                double h = Parent.Parent.DefaultEmojiHeight;
                double x = Column * w;
                double y = Row * h;
                return new Rect(x, y, w, h);
            }
        }

        public Rect TotalRect {
            get {
                double w = EmojiRect.Width;
                double h = EmojiRect.Height;
                double x =
                    EmojiRect.X +
                    Parent.ScrollRect.Left +
                    Parent.Parent.TotalRect.Left;// + Parent.Parent.ScrollOffsetX;
                double y =
                    EmojiRect.Y +
                    Parent.Parent.TotalRect.Top +
                    Parent.ScrollRect.Top
                    //- Parent.ScrollOffsetY
                    ;
                var test = this.Translate(new(), this.Root() as FrameViewModelBase);
                return new Rect(x, y, w, h);
            }
        }
        int PopupRows { get; set; }
        int PopupCols { get; set; }
        public Rect[] _popupRects;
        public Rect[] PopupRects {
            get {
                if(_popupRects == null) {
                    // count=6, cols=4, rows = 2
                    int count = Items.Length;
                    _popupRects = new Rect[count];
                    PopupRows = (int)Math.Ceiling(count / (double)MaxPopupColumns);
                    PopupCols = PopupRows <= 1 ? count : MaxPopupColumns;
                    int r = 0;
                    int c = 0;
                    double w =
                        KeyboardViewModel.CanShowPopupWindows ?
                            EmojiRect.Width :
                            KeyboardViewModel.KeyGridRect.Width / (Math.Max(1, PopupCols - 1));
                    double h =
                        KeyboardViewModel.CanShowPopupWindows ?
                            EmojiRect.Height :
                            KeyboardViewModel.KeyGridRect.Height / PopupRows;

                    PopupFontSize = EmojiFontSize;
                    if(!KeyboardViewModel.CanShowPopupWindows) {
                        PopupFontSize = Math.Min(Math.Min(w, h) / 2, 36);
                    }
                    for(int i = 0; i < count; i++) {
                        double x = c * w;
                        double y = r * h;
                        _popupRects[i] = new Rect(x, y, w, h);
                        //PopupRows = r + 1;
                        //PopupCols = Math.Max(c + 1, PopupCols);

                        c++;
                        if(c >= MaxPopupColumns) {
                            c = 0;
                            r++;
                        }
                    }
                }
                return _popupRects;
            }
        }
        public double PopupFontSize { get; private set; }

        public Rect PopupContainerRect {
            get {
                double w = PopupRects.Max(x => x.Right);
                double h = PopupRects.Max(x => x.Bottom);
                double x = 0;// TotalRect.Center.X - (w / 2);
                double y = 0;// TotalRect.Top - h;
                var pucr = new Rect(x, y, w, h);
                if(KeyboardViewModel.CanShowPopupWindows) {
                    return pucr.PositionAbove(
                        anchorRect: TotalRect,
                        outerRect: KeyboardViewModel.TotalRect);
                }
                //return pucr.FitAbove(
                //        anchorRect: TotalRect,
                //        outerRect: KeyboardViewModel.TotalRect);
                return pucr;
            }
        }
        public CornerRadius PopupContainerCornerRadius =>
            KeyboardViewModel.CommonCornerRadius;
        public CornerRadius SelectedPopupCornerRadius {
            get {
                double cr = KeyboardViewModel.CommonCornerRadius.TopLeft;
                int max_row = PopupRows - 1;
                int max_col = PopupCols - 1;
                double tl = 0;
                double tr = 0;
                double bl = 0;
                double br = 0;
                int sel_row = SelectedIdx / MaxPopupColumns;
                int sel_col = SelectedIdx % MaxPopupColumns;
                //MpConsole.WriteLine($"Sel R:{sel_row} C: {sel_col}");

                if(sel_row == 0 && sel_col == 0) {
                    tl = cr;
                }
                if(sel_row == 0 && sel_col == max_col) {
                    tr = cr;
                }
                if(sel_row == max_row && sel_col == max_col) {
                    br = cr;
                }
                if(sel_row == max_row && sel_col == 0) {
                    bl = cr;
                }
                return new CornerRadius(tl, tr, br, bl);
            }
        }

        Point[] _hasPopupHintTrianglePoints;
        public Point[] PopupHintTrianglePoints {
            get {
                if(_hasPopupHintTrianglePoints == null) {
                    _hasPopupHintTrianglePoints = new Point[3];

                    double ratio = 0.05d;
                    double target_len = Math.Max(EmojiRect.Width, EmojiRect.Height);
                    double w = target_len * ratio;
                    double h = target_len * ratio;

                    // from BR corner CW
                    double brx = EmojiRect.Width;
                    double bry = EmojiRect.Height;
                    _hasPopupHintTrianglePoints[0] = new Point(brx, bry);
                    _hasPopupHintTrianglePoints[1] = new Point(brx - w, bry);
                    _hasPopupHintTrianglePoints[2] = new Point(brx, bry - h);
                }
                return _hasPopupHintTrianglePoints;
            }
        }
        int MaxPopupColumns =>
            Items.Length > 16 ? 6 : 4;
        public TextAlignment EmojiAlignment =>
            TextAlignment.Center;
        Point _emojiTextLoc;
        public Point EmojiTextLoc {
            get {
                if(_emojiTextLoc == default) {
                    if(InputConnection == null ||
                        InputConnection.TextTools is not { } tm) {
                        return new();
                    }
                    var text_rect =
                            tm
                            .MeasureText(PrimaryValue, EmojiFontSize, out double ascent, out double descent);
                    var inner_rect = new Rect(0, 0, EmojiRect.Width, EmojiRect.Height);
                    double cix = inner_rect.Center.X;
                    double ciy = inner_rect.Center.Y - ((ascent + descent) / 2);
                    _emojiTextLoc = new Point(cix, ciy);
                }
                return _emojiTextLoc;
            }
        }
        #endregion

        #region State
        bool IsRecentEmoji =>
            Parent.EmojiPageType == EmojiPageType.Recents;
        bool IsValidModel =>
            EmojiModel != null && !string.IsNullOrEmpty(EmojiModel.Version);
        public bool HasPopup =>
            Items.Length > 1;
        public bool IsPopupOpen { get; private set; }
        public override bool IsVisible =>
            Items.Any();
        public bool IsPressed { get; private set; }
        public string TouchId { get; private set; }
        public int SelectedIdx { get; private set; } = 0;
        public string PrimaryValue =>
            IsVisible ? Items[SelectedIdx] : string.Empty;
        #endregion

        #region Models
        public Emoji EmojiModel { get; private set; }
        public string ProcessedSearchText { get; private set; }
        public string[] SearchWords { get; private set; }
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        #endregion

        #region Public Methods
        public EmojiKeyViewModel(EmojiPageViewModel parent, Emoji emojiModel, int idx) {
            Parent = parent;
            EmojiModel = emojiModel;
            PageItemIdx = idx;

            if(IsRecentEmoji) {
                ProcessedSearchText = string.Empty;
            } else {
                SearchWords = emojiModel.SearchText.ToStringOrEmpty().ToLower().ToWords().Distinct().ToArray();
                ProcessedSearchText = string.Join(" ", SearchWords);
            }

            string valid_emoji_csv = RemoveUnsupported(emojiModel.EmojiStr);
            var items = valid_emoji_csv.SplitNoEmpty(",").ToArray();
            Items = items.OrderBy(x => GetSortOrder(x, items.IndexOf(x))).ToArray();
        }
        public void SetPressed(string touchId, bool isPressed) {
            TouchId = touchId;
            if(IsPressed == isPressed) {
                return;
            }
            IsPressed = isPressed;

            if(IsPressed && IsRecentEmoji && !IsValidModel && Parent.Parent.FindEmoji(PrimaryValue) is { } em) {
                EmojiModel = em;
            }

            string info = IsPressed ? Summary : string.Empty;
            if(!IsPressed) {
                KeyboardViewModel.FooterViewModel.SetLabelText(info);
                return;
            }
            InputConnection.MainThread.Post(async () => {
                await Task.Delay(500);
                if(!IsPressed) {
                    return;
                }

                KeyboardViewModel.FooterViewModel.SetLabelText(info);
            });

        }
        public void UpdateActivePopup(Touch touch) {
            if(PopupRows == 0 || PopupCols == 0) {
                // avoid divide by zero exception (must be (un)loading?)
                return;
            }
            int last_sel_idx = SelectedIdx;
            int i = 0;
            bool is_on_right = EmojiRect.Center.X > Parent.ScrollRect.Center.X;
            int pivot_row = 0;
            int pivot_col = is_on_right ? PopupCols - 1 : 0;

            if(!KeyboardViewModel.CanShowPopupWindows) {
                var adj_loc = touch.Location - new Point(0, PopupContainerRect.Top);
                int new_idx = PopupRects.IndexOf(PopupRects.FirstOrDefault(x => x.Contains(adj_loc)));
                if(new_idx >= 0) {
                    SelectedIdx = new_idx;
                }
            } else {
                for(int r = 0; r < PopupRows; r++) {
                    for(int c = 0; c < PopupCols; c++) {
                        if(i >= PopupRects.Length) {
                            break;
                        }
                        var pu_total_rect = PopupRects[i];
                        bool is_active =
                            pu_total_rect.IsPopupHit(
                                r: r,
                                c: c,
                                pivotRow: pivot_row,
                                pivotCol: pivot_col,
                                isLast: i == Items.Length - 1,
                                rows: PopupRows,
                                cols: PopupCols,
                                anchorRect: TotalRect,
                                rootRect: KeyboardViewModel.TotalRect,
                                pressLocation: touch.PressLocation,
                                location: touch.Location,
                                lastLocation: touch.LastLocation);
                        if(is_active) {
                            SelectedIdx = i;
                            break;
                        }
                        i++;
                    }
                }

                if(KeyboardViewModel.CanShowPopupWindows) {
                    Renderer.PaintFrame(true);
                } else {
                    Parent.Renderer.PaintFrame(true);
                }
            }

            if(SelectedIdx != last_sel_idx && SelectedIdx >= 0) {
                InputConnection.OnFeedback(KeyboardViewModel.FeedbackCursorChange);
            }
        }
        public void SetIsPopupOpen(bool isOpen) {
            IsPopupOpen = isOpen;
            SelectedIdx = 0;
            if(!isOpen) {
                _popupRects = null;
            }
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        string RemoveUnsupported(string emoji_csv) {
            var emojis = emoji_csv.SplitNoEmpty(",");
            string base_emoji = emojis[0];
            var valid_emojis = new List<string>();
            foreach(string emoji in emojis) {
                if(IsModifierValid(base_emoji, emoji)
                    //&&
                    //!string.IsNullOrWhiteSpace(emoji) 
                    //&&
                    //valid_emojis.All(x=>x.ToCodePointStr() != emoji.ToCodePointStr())
                    ) {
                    valid_emojis.Add(emoji);
                }
            }
            return string.Join(",", valid_emojis);
        }
        int GetSortOrder(string emoji, int idx) {
            string emoji_code_point = emoji.ToCodePointStr();
            int score = 0;
            score += string.IsNullOrEmpty(Parent.Parent.DefaultSkinToneCodePoint) ? 0 : emoji_code_point.IndexOfAll(Parent.Parent.DefaultSkinToneCodePoint).Length;
            score += string.IsNullOrEmpty(Parent.Parent.DefaultHairStyleCodePoint) ? 0 : emoji_code_point.IndexOfAll(Parent.Parent.DefaultHairStyleCodePoint).Length;
            if(score > 0) {
                return -score;
            }
            return idx;
        }
        bool IsModifierValid(string emoji, string mod_emoji) {
            if(InputConnection.TextTools is not { } tm ||
                !tm.CanRender(mod_emoji)) {
                return false;
            }
            //var base_rect = tm.MeasureText(emoji, 12, TextAlignment.Center, out _, out _);
            //var toned_rect = tm.MeasureText(mod_emoji, 12, TextAlignment.Center, out _, out _);

            //double max_w_diff = base_rect.Width / 2;
            //double max_h_diff = base_rect.Height / 2;
            //return Math.Abs(base_rect.Width - toned_rect.Width) <= max_w_diff &&
            //    Math.Abs(base_rect.Height - toned_rect.Height) <= max_h_diff;
            return true;
        }

        #endregion

        #region Commands
        #endregion
    }
}
