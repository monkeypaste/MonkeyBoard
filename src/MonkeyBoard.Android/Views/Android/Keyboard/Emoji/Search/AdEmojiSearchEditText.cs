using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Avalonia.Media;
using Java.Lang;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static Android.Provider.UserDictionary;
using static Android.Telephony.CarrierConfigManager;
using static System.Net.Mime.MediaTypeNames;
using Color = Android.Graphics.Color;
using GPaint = Android.Graphics.Paint;
using Math = System.Math;
using Point = Avalonia.Point;

namespace MonkeyBoard.Android {
    public class AdEmojiSearchEditText : CustomEditText {
        #region Private Variables
        bool? show_caret = null;
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        EmojiSearchViewModel DC { get; set; }
        #endregion

        #region Appearance
        Bitmap ClearTextButtonBmp { get; set; }
        #endregion

        #region Layout
        float CaretPad => 3d.UnscaledF();
        #endregion

        #region State
        double MinTouchMoveDist => 20;
        int MinHoldDelayMs => 300;
        int MaxDoubleTapDelayMs => 500;
        (int sidx,int eidx)? LastDownWordRange { get; set; }
        DateTime? LastDownDt { get; set; }
        Point TouchDownLoc { get; set; }
        Point LastLoc { get; set; }
        int? TouchId { get; set; }
        int? LastTouchTextIdx { get; set; }
        bool IsTouchMoving { get; set; }
        Dictionary<int, (Point,Point)> _touches { get; set; } = [];
        bool IsSelectedTextChanging { get; set; }

        public string SelectedText {
            get => SelectionStart < Text.Length ? this.Text.Substring(SelectionStart, SelectionEnd - SelectionStart) : string.Empty;
            set {
                if(SelectedText != value) {
                    IsSelectedTextChanging = true;
                    string new_sel_text = value ?? string.Empty;
                    int sidx = SelectionStart;
                    int eidx = sidx + new_sel_text.Length;

                    string leading_text = 
                        SelectionStart < Text.Length ?
                            Text.Substring(0, SelectionStart) :
                            Text;
                    string trailing_text =
                        SelectionEnd<Text.Length?
                            Text.Substring(SelectionEnd, Math.Max(0, Text.Length - SelectionEnd)) :
                            string.Empty;

                    string new_text = leading_text + new_sel_text + trailing_text;
                    Text = new_text;
                    IsSelectedTextChanging = false;
                    eidx = Math.Min(eidx, Text.Length);
                    SetSelection(eidx, eidx);
                }
            }
        }
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        public event EventHandler<(int selectionStart, int selectionEnd)> OnSelChanged;
        #endregion

        #region Constructors
        public AdEmojiSearchEditText(Context context, GPaint paint, EmojiSearchViewModel dc) : base(context, paint) {
            DC = dc;
            this.TextChanged += EmojiSearchEditText_TextChanged;
        }

        public void Unload() {
            this.TextChanged -= EmojiSearchEditText_TextChanged;
        }

        #endregion

        #region Public Methods
        public void DoBackspace() {
            int last_len = new StringInfo(Text).LengthInTextElements;

            while (true) {
                if (string.IsNullOrEmpty(SelectedText)) {
                    if (SelectionStart == 0) {
                        return;
                    }
                    SetSelection(SelectionStart - 1, SelectionStart);
                }

                SelectedText = string.Empty;
                int cur_len = new StringInfo(Text).LengthInTextElements;
                if (cur_len == last_len) {
                    // backspacing over emoji continue until elements decremented
                    continue;
                }
                break;
            }
        }
        public override bool OnTouchEvent(MotionEvent e) {
            var changed_touches = e.GetMotions(_touches);
            if(!changed_touches.Any()) {
                return true;
            }
            TouchEventType action = default;
            Point loc = default;
            if(TouchId == null && changed_touches.OrderBy(x=>x.id).FirstOrDefault() is { } first_touch) {
                TouchId = first_touch.id;
                loc = first_touch.p.loc;
                action = first_touch.eventType;
            } else if(TouchId != null) {
                if(changed_touches.FirstOrDefault(x=>x.id == TouchId.Value) is { } cur_touch) {
                    loc = cur_touch.p.loc;
                    action = cur_touch.eventType;
                } else {
                    // ignore other touches
                    return true;
                }
            }
            int touch_text_idx = this.GetOffsetForPosition((float)loc.X,(float)loc.Y);
            if(touch_text_idx < 0) {
                // not on text
                return true;
            }
            switch (action) {
                case TouchEventType.Press:
                    TouchDownLoc = loc;
                    var down_dt = DateTime.Now;
                    var down_word_range = GetWordRangeAtOffset(touch_text_idx);

                    if (LastDownDt is { } last_down_dt &&
                        LastDownWordRange is { } last_range &&
                        down_dt - last_down_dt <= TimeSpan.FromMilliseconds(MaxDoubleTapDelayMs) &&
                        touch_text_idx >= last_range.sidx && touch_text_idx <= last_range.eidx) {
                        // double tap
                        SelectRange(last_range);
                    } else {
                        this.SetSelection(touch_text_idx, touch_text_idx);
                        LastDownDt = down_dt;
                        LastDownWordRange = down_word_range;
                    }
                    LastDownDt = down_dt;
                    LastDownWordRange = down_word_range;

                    if (Context is AdInputMethodService ims) {
                        ims.Post(async () => {
                            DateTime down_time = DateTime.Now;
                            while (true) {
                                if (TouchId == null || IsTouchMoving) {
                                    // touch was a tap
                                    return;
                                }
                                if (DateTime.Now - down_time >= TimeSpan.FromMilliseconds(MinHoldDelayMs)) {
                                    // hold
                                    //select current word
                                    if (GetWordRangeAtOffset(SelectionStart) is { } word_range) {
                                        SelectRange(word_range);
                                    }

                                    return;
                                }
                                await Task.Delay(10);
                            }
                        });
                    }
                    break;
                case TouchEventType.Move:
                    IsTouchMoving = IsTouchMoving || Touches.Dist(loc, TouchDownLoc) >= MinTouchMoveDist;
                    if (!IsTouchMoving) {
                        break;
                    }

                    int sdist = Math.Abs(touch_text_idx - this.SelectionStart);
                    int edist = Math.Abs(touch_text_idx - this.SelectionEnd);

                    int other_idx = sdist <= edist ? this.SelectionEnd : this.SelectionStart;
                    SelectRange((touch_text_idx, other_idx));
                    break;
                case TouchEventType.Release:
                    IsTouchMoving = false;
                    TouchId = null;
                    TouchDownLoc = default;
                    _touches.Clear();
                    if(LastTouchTextIdx is { } ltidx && ltidx == touch_text_idx) {
                        this.ShowContextMenu();
                    }

                    LastTouchTextIdx = touch_text_idx;
                    break;
            }

            LastLoc = loc;
            return true;
        }

        #endregion

        #region Protected Methods
        protected override void OnSelectionChanged(int selStart, int selEnd) {
            if(IsSelectedTextChanging) {
                // ignore and wait for followup SetSelection
                return;
            }
            //MpConsole.WriteLine($"EmojiSearch Sel start: {selStart} End: {selEnd}");
            base.OnSelectionChanged(selStart, selEnd);
            OnSelChanged?.Invoke(this, (SelectionStart, SelectionEnd));
            this.Redraw();
        }

        protected override void OnDraw(Canvas canvas) {
            base.OnDraw(canvas);

            if(show_caret == null) {
                StartCaretLoop();
            }
            bool is_caret = SelectionStart == SelectionEnd;
            var caret_rect = GetCaretRect(canvas);
            if (show_caret is true || !is_caret) {
                DrawSel(canvas,caret_rect);
            }
            if (DC.IsClearTextButtonVisible) {
                // cleartext button img
                SharedPaint.SetTint(DC.ClearTextButtonFgHexColor.ToAdColor());
                // position clear btn along center of caret line
                var cleartext_btn_rect = DC.ClearTextButtonRect.ToRectF();
                float clear_text_y = caret_rect.CenterY() - (cleartext_btn_rect.Height() / 2);
                cleartext_btn_rect = cleartext_btn_rect.Place(cleartext_btn_rect.Left, clear_text_y);
                ClearTextButtonBmp = ClearTextButtonBmp.LoadRescaleOrIgnore(DC.ClearTextButtonIconSourceObj.ToString(), cleartext_btn_rect);
                canvas.DrawBitmap(ClearTextButtonBmp, cleartext_btn_rect.Left, cleartext_btn_rect.Top, SharedPaint);
                SharedPaint.SetTint(null);
            }
        }

        #endregion

        #region Private Methods
        void SelectRange((int sidx, int eidx) range) {
            this.SetSelection(Math.Min(range.sidx,range.eidx), Math.Max(range.sidx,range.eidx));
            if(Context is AdInputMethodService ims &&
                ims.KeyboardView is { } kbv) {
                kbv.ShowContextMenuForChild(kbv.HiddenEditTextForContextMenu);
            }
            
        }
        (int sidx,int eidx)? GetWordRangeAtOffset(int offset, int len = 0) {
            string word = TextRangeTools.GetWordAtCaret(
                textRange: new SelectableTextRange(this.Text, offset, len),
                rangeStartIdx: out int startIdx,
                hasTrailingSpace: out _,
                isAtWordTerminator: out _,
                breakOnCompoundWords: false
                );
            return (startIdx, startIdx + word.Length);
        }
        private void EmojiSearchEditText_TextChanged(object sender, global::Android.Text.TextChangedEventArgs e) {            
            string new_text = new string(e.Text.ToArray());
            if(new_text.Contains(Environment.NewLine)) {
                // BUG even though SetMaxLines(1) is set it still adds a new line so remove it
                this.Text = this.Text.Replace(Environment.NewLine, string.Empty);
            }
            int end_idx = e.AfterCount;
            DC.SetSearchText(new_text);
        }

        void StartCaretLoop() {
            show_caret = true;
            if(Context is AdInputMethodService ic) {

                // draw caret
                ic.Post(async () => {
                    var last_draw = DateTime.Now;
                    var delay = TimeSpan.FromMilliseconds(500);
                    while (true) {
                        await Task.Delay(5);
                        if (this.WindowVisibility != ViewStates.Visible) {
                            return;
                        }

                        if (DateTime.Now - last_draw >= delay) {
                            show_caret = !show_caret;
                            this.Redraw();
                            last_draw = DateTime.Now;
                        }
                    }
                });
            }
        }

        void DrawSel(Canvas canvas,RectF caretRect) {
            // NOTE presumes text is single line
            SharedPaint.TextSize = this.TextSize;
            SharedPaint.TextAlign = GPaint.Align.Left;
            SharedPaint.SetTypeface(this.Typeface);            

            SharedPaint.Color = 
                SelectionStart == SelectionEnd ? 
                    DC.EmojiSearchBoxCaretHexColor.ToAdColor() : 
                    DC.EmojiSearchBoxSelHexColor.ToAdColor();
            canvas.DrawRect(caretRect, SharedPaint);
        }

        RectF GetCaretRect(Canvas canvas) {
            float x_pad = SelectionStart == SelectionEnd ? 1d.UnscaledF() : 0;
            float l = this.PaddingLeft + this.Layout.GetPrimaryHorizontal(SelectionStart);
            float r = this.PaddingLeft + this.Layout.GetSecondaryHorizontal(SelectionEnd) + x_pad;

            float y_pad = this.LineHeight / 2f;//3d.UnscaledF();
            float t = y_pad;// (canvas.Height / 2) - (range_bounds.Height() / 2) - pad;
            float b = canvas.Height - y_pad;// (canvas.Height / 2) + (range_bounds.Height() / 2) + pad;
            return new RectF(l, t, r, b);
        }
        #endregion
    }
}