using Android.Content;
using Android.Graphics;
using Android.Views;

using KeyboardLib;

using System;
using System.Linq;

using static Android.Views.View;

using Color = Android.Graphics.Color;
using GPaint = Android.Graphics.Paint;
using PointF = Android.Graphics.PointF;
using Rect = Android.Graphics.Rect;
using RectF = Android.Graphics.RectF;

namespace iosKeyboardTest.Android {
    public class EmojiCompletionsView : CustomView, IFrameRenderer {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            if (DC.Parent.Parent.Parent.IsVisible) {
                this.Visibility = ViewStates.Visible;
            } else {
                this.Visibility = ViewStates.Invisible;
                return;
            }
            //CompletionTexts = DC.CompletionDisplayValues.ToArray();
            //CompletionAlignment = DC.CompletionTextAlignment.ToAdAlign();

            base.LayoutFrame(invalidate);
        }
        //public override void Measure(bool invalidate) {
        //    Frame = DC.AutoCompleteRect.ToRectF();
        //    CompletionRects = DC.CompletionItemRects.Select(x => x.ToRectF()).ToArray();
        //    CompletionTextLocs = DC.CompletionItemTextLocs.Select(x => x.ToPointF()).ToArray();

        //    base.Measure(invalidate);
        //}

        public override void PaintFrame(bool invalidate) {
            BackgroundColor = DC.AutoCompleteBgHexColor.ToAdColor();
            CompletionBgColors = DC.CompletionItemBgHexColors.Select(x => x.ToAdColor()).ToArray();
            CompletionFgColor = DC.AutoCompleteFgHexColor.ToAdColor();

            base.PaintFrame(invalidate);
        }
        #endregion

        #endregion

        #region Properties

        #region View Models
        public EmojiCompletionsViewModel DC { get; set; }
        #endregion

        #region Views
        RectF[] CompletionRects { get; set; } = [];
        PointF[] CompletionTextLocs { get; set; } = [];
        string[] CompletionTexts { get; set; } = [];
        Color[] CompletionBgColors { get; set; } = [];
        Color CompletionFgColor { get; set; }
        float CompletionFontSize { get; set; }
        GPaint.Align CompletionAlignment { get; set; }
        #endregion

        #region Appearance
        #endregion

        #region Layout
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors

        public EmojiCompletionsView(Context context, GPaint paint, EmojiCompletionsViewModel dc) : base(context, paint) {
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
            if (this.Visibility == ViewStates.Invisible) {
                return;
            }

            // clip completions to inner frame
            canvas.ClipRect(Bounds);

            int avail_count = GetAvailableComplCount();
            for (int i = 0; i < avail_count; i++) {
                var comp_item_rect = CompletionRects[i];
                if (comp_item_rect.Right < Bounds.Left ||
                    comp_item_rect.Left > Bounds.Right) {
                    // clipped
                    continue;
                }

                var comp_item_text = CompletionTexts[i];
                var comp_item_bg = CompletionBgColors[i];
                var comp_item_loc = CompletionTextLocs[i];
                // draw item bg
                //SharedPaint.SetStyle(GPaint.Style.Fill);
                SharedPaint.Color = comp_item_bg;
                canvas.DrawRect(comp_item_rect, SharedPaint);

                // draw item outline
                //SharedPaint.SetStyle(GPaint.Style.Stroke);
                //SharedPaint.Color = DC.MenuFgHexColor.ToColor();
                //canvas.DrawRect(comp_item_rect, SharedPaint);

                // draw item text
                //SharedPaint.SetStyle(GPaint.Style.Fill);
                SharedPaint.TextAlign = CompletionAlignment;
                SharedPaint.TextSize = DC.EmojiSearchCompletionFontSize.UnscaledF();
                SharedPaint.Color = CompletionFgColor;
                canvas.DrawText(comp_item_text, comp_item_loc.X, comp_item_loc.Y, SharedPaint);
            }
        }

        #endregion

        #region Private Methods
        int GetAvailableComplCount() {
            int avail_count = Math.Min(CompletionTexts.Length, Math.Min(CompletionRects.Length, Math.Min(CompletionBgColors.Length, CompletionTextLocs.Length)));
            int max_count = Math.Max(CompletionTexts.Length, Math.Max(CompletionRects.Length, Math.Max(CompletionBgColors.Length, CompletionTextLocs.Length)));
            if (avail_count != max_count) {
                // mismatch!
            }
            return avail_count;
        }
        #endregion

        #region Commands
        #endregion
    }
}