using Android.Content;
using Android.Graphics;
using MonkeyBoard.Common;
using System.Linq;

namespace MonkeyBoard.Android {
    public class AdEmojiPopupView : AdCustomView {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.PopupContainerRect.ToRectF().ToBounds();
            base.MeasureFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new EmojiKeyViewModel DC { get; set; }
        #endregion

        #endregion

        #region Constructors
        public AdEmojiPopupView(Context context, Paint paint, EmojiKeyViewModel dc) : base(context, paint) {
            DC = dc;
            DC.SetRenderContext(this);
            this.MeasureFrame(false);
        }
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if(!DC.IsPopupOpen) {
                return;
            }

            var emoji_texts = DC.Items;
            var emoji_rects = DC.PopupRects.Select(x => x.ToRectF()).ToArray();
            var bg = DC.PopupItemBgHexColor.ToAdColor();
            var sel_bg = DC.PopupItemFocusedBgHexColor.ToAdColor();
            int sel_idx = DC.SelectedIdx;

            // bg
            SharedPaint.Color = bg;
            Path bg_path = DC.PopupContainerCornerRadius.ToPath(Bounds);
            canvas.DrawPath(bg_path, SharedPaint);

            // items
            float fontSize = DC.EmojiFontSize.UnscaledF();
            var text_loc = DC.EmojiTextLoc.ToPointF();

            for (int i = 0; i < emoji_texts.Length; i++) {
                string emoji_text = emoji_texts[i];
                var emoji_rect = emoji_rects[i];
                if(i == sel_idx) {
                    // sel item bg
                    SharedPaint.Color = sel_bg;
                    Path sel_bg_path = DC.SelectedPopupCornerRadius.ToPath(emoji_rect);
                    canvas.DrawPath(sel_bg_path, SharedPaint);
                }
                float x = text_loc.X + emoji_rect.Left;
                float y = text_loc.Y + emoji_rect.Top;

                AdHelpers.DrawEmoji(canvas, SharedPaint, emoji_text, x, y, fontSize);
            }

        }
        #endregion

        #region Private Methods
        #endregion
    }
}
