using Android.Content;
using Android.Graphics;
using Android.Views;
using MonkeyPaste.Common;
using MonkeyBoard.Common;
using System;
using System.Linq;
using static Android.Views.View;
using Color = Android.Graphics.Color;
using GPaint = Android.Graphics.Paint;
using PointF = Android.Graphics.PointF;
using Rect = Android.Graphics.Rect;
using RectF = Android.Graphics.RectF;

namespace MonkeyBoard.Android {
    public class AdAutoCompleteView : AdCustomView, IFrameRenderer {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            base.LayoutFrame(invalidate);
        }
        public override void MeasureFrame(bool invalidate) {
            var new_frame = DC.AutoCompleteRect.ToRectF().Move(0, DC.FrameOffsetY.UnscaledF());
            bool needs_layout = new_frame != Frame;
            Frame = new_frame;
            //if (needs_layout) {
            //    this.UpdateLayout(Frame);
            //}
            ContainerPath = DC.ContainerCornerRadius.ToPath(Frame.ToBounds());
            base.MeasureFrame(invalidate);
        }

        public override void PaintFrame(bool invalidate) {
            CancelOmitButtonBmp = CancelOmitButtonBmp.LoadRescaleOrIgnore(DC.OmitCancelIconSourceObj.ToString(), DC.CancelOmitButtonHitRect.ToRectF());
            ConfirmOmitButtonBmp = ConfirmOmitButtonBmp.LoadRescaleOrIgnore(DC.OmitConfirmIconSourceObj.ToString(), DC.ConfirmOmitButtonHitRect.ToRectF());

            if(DC is EmojiAutoCompleteViewModel emacvm) {
                CloseBmp = CloseBmp.LoadRescaleOrIgnore(emacvm.Parent.CloseButtonIconSourceObj.ToString(), emacvm.Parent.CloseButtonImageRect.ToRectF());
            }

            base.PaintFrame(invalidate);
        }
        public override void RenderFrame(bool invalidate) {
            LayoutFrame(false);
            MeasureFrame(false);
            PaintFrame(false);

            if(invalidate) {
                this.Redraw(true);
            }
        }
        #endregion

        #endregion

        #region Properties

        #region View Models
        public new AutoCompleteViewModelBase DC { get; set; }
        #endregion

        #region Views
        Bitmap CancelOmitButtonBmp { get; set; }
        Bitmap ConfirmOmitButtonBmp { get; set; }
        Bitmap CloseBmp { get; set; }

        #endregion

        #region Appearance
        Path ContainerPath { get; set; }
        #endregion

        #region Layout
        #endregion

        #region State
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors

        public AdAutoCompleteView(Context context, GPaint paint, AutoCompleteViewModelBase dc) : base(context, paint) {
            DC = dc;
            ResetRenderer();
        }



        #endregion

        #region Public Methods
        public void ResetRenderer() {
            DC.SetRenderContext(this);
        }

        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if(DC is EmojiAutoCompleteViewModel eacvm) {
            }
            if(DC is TextAutoCompleteViewModel) {

            }
            if(!DC.IsVisible) {
                return;
            }


            bool needs_restore = false;
            if(DC is EmojiAutoCompleteViewModel emacvm && emacvm.Parent is { } esvm) {
                // only draw emoji bg, text is problemattic w/ alpha (and clearing btn bg)
                SharedPaint.Color = DC.AutoCompleteBgHexColor.ToAdColor();
                canvas.DrawRect(Bounds, SharedPaint);

                // close btn
                var close_btn_rect = esvm.CloseButtonRect.ToRectF();
                if(esvm.CloseButtonBgHexColor is { } close_bg_hex) {
                    // close btn bg
                    SharedPaint.Color = close_bg_hex.ToAdColor();
                    canvas.DrawRect(close_btn_rect.ToBounds(), SharedPaint);
                }
                if(CloseBmp != null) {
                    // close btn fg
                    SharedPaint.SetTint(esvm.CloseButtonFgHexColor.ToAdColor());
                    var close_bmp_rect = esvm.CloseButtonImageRect.ToRectF();
                    canvas.DrawBitmap(CloseBmp, close_bmp_rect.Left, close_bmp_rect.Top, SharedPaint);
                    SharedPaint.SetTint(null);

                    canvas.Save();
                    canvas.Translate(close_btn_rect.Width(), 0);
                    needs_restore = true;
                }
            }
            // clip completions to inner frame
            canvas.ClipRect(Bounds);

            var CompletionBgColors = DC.CompletionItemBgHexColors.Select(x => x.ToAdColor()).ToArray();
            var CompletionFgColor = DC.FgHexColor.ToAdColor();
            var CompletionTexts = DC.CompletionDisplayValues.ToArray();
            //var CompletionAlignment = DC.CompletionHorizontalTextAlignment.ToAdAlign();

            var CompletionRects = DC.CompletionItemRects.Select(x => x.ToRectF()).ToArray();
            var CompletionTextLocs = DC.CompletionItemTextLocs.Select(x => x.ToPointF()).ToArray();

            // items
            int avail_count = Math.Min(CompletionTexts.Length, Math.Min(CompletionRects.Length, /*Math.Min(*/CompletionBgColors.Length/*, CompletionTextLocs.Length)*/));
            for(int i = 0; i < avail_count; i++) {
                var comp_item_rect = CompletionRects[i];
                if(comp_item_rect.Right < Bounds.Left ||
                    comp_item_rect.Left > Bounds.Right) {
                    // clipped
                    continue;
                }

                var comp_item_text = CompletionTexts[i];
                var comp_item_bg = CompletionBgColors[i];
                var comp_item_loc = CompletionTextLocs[i];

                float lw = 1f;//.UnscaledF();
                // draw item bg
                if(comp_item_bg != null) {
                    SharedPaint.Color = comp_item_bg;
                    canvas.DrawRect(comp_item_rect.Resize(comp_item_rect.Width() + 4, comp_item_rect.Height()), SharedPaint);
                }

                // draw item outline

                if(i > 0) {
                    // draw sep line on left 
                    float lh = comp_item_rect.Height() / (float)KeyConstants.PHI;
                    float lhhdiff = (comp_item_rect.Height() - lh) / 2f;

                    float l = comp_item_rect.Left + lw + 3;
                    float t = comp_item_rect.Top + lhhdiff;
                    float r = l + lw;
                    float b = comp_item_rect.Bottom - lhhdiff;
                    var line_rect = new RectF(l, t, r, b);
                    SharedPaint.Color = DC.SeparatorHexColor.ToAdColor();
                    canvas.DrawRect(line_rect, SharedPaint);
                }

                // omit button
                if(CancelOmitButtonBmp != null &&
                    i == DC.OmittableItemIdx) {

                    // cancel
                    var cancel_omit_btn_rect = DC.CancelOmitButtonHitRect.ToRectF();
                    SharedPaint.SetTint(DC.CancelOmitButtonFgHexColor.ToAdColor());
                    canvas.DrawBitmap(CancelOmitButtonBmp, cancel_omit_btn_rect.Left, cancel_omit_btn_rect.Top, SharedPaint);

                    // confirm
                    var confirm_omit_btn_rect = DC.ConfirmOmitButtonHitRect.ToRectF();
                    SharedPaint.SetTint(DC.ConfirmOmitButtonFgHexColor.ToAdColor());
                    canvas.DrawBitmap(ConfirmOmitButtonBmp, confirm_omit_btn_rect.Left, confirm_omit_btn_rect.Top, SharedPaint);

                    SharedPaint.SetTint(null);
                }

                // compl item text
                float fontSize = DC.GetCompletionTextFontSize(comp_item_text, i, out string formatted_text).UnscaledF();
                bool isOmit = DC.IsOmitButtonsVisible && i == DC.OmittableItemIdx && DC.IsOmitLabelVisible;
                DrawComplText(canvas, CompletionFgColor, comp_item_rect, formatted_text, comp_item_loc, fontSize, isOmit);
            }

            if(needs_restore) {
                canvas.Restore();
            }
        }

        #endregion

        #region Private Methods

        void DrawComplText(Canvas canvas, Color fgColor, RectF comp_item_rect, string comp_item_text, PointF comp_item_loc, float fontSize, bool isOmit) {
            // draw item text
            SharedPaint.TextAlign = GPaint.Align.Center;
            SharedPaint.Color = fgColor;
            SharedPaint.TextSize = fontSize;

            Typeface last_typeface = SharedPaint.Typeface;
            if(isOmit) {
                // draw 'Forget' in italics
                SharedPaint.SetTypeface(Typeface.Create(
                    Typeface.Default
                    /*Resources.GetFont(AdKeyboardView.DEFAULT_FONT_RES_ID)*/,
                    TypefaceStyle.Italic));
            }
            var lines = comp_item_text.SplitNoEmpty(Environment.NewLine);
            float tlh = SharedPaint.Descent() - SharedPaint.Ascent();
            float tx = comp_item_loc.X;
            float ty = comp_item_loc.Y - ((lines.Length - 1) * (tlh / 2));
            foreach(string line in lines) {
                // draw each line of text
                canvas.DrawText(line, tx, ty, SharedPaint);
                ty += tlh;
            }

            SharedPaint.SetTypeface(last_typeface);
        }
        #endregion
    }
}