using Avalonia.Controls;
using CoreGraphics;
using MonkeyPaste.Keyboard.Common;
using System;
using System.Linq;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosEmojiPopupView : iosPopupContainerView {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void PaintFrame(bool invalidate) {
            BackgroundColor = DC.PopupItemBgHexColor.ToUIColor();
            base.PaintFrame(invalidate);
        }
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.PopupContainerRect.ToCGRect().ToBounds();
            base.MeasureFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new EmojiKeyViewModel DC { get; set; }
        #endregion

        #region Appearance
        #endregion

        #region Layout
        #endregion

        #region State
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public iosEmojiPopupView(EmojiKeyViewModel dc) {
            Init(dc);
            this.RoundCorners(DC.PopupContainerCornerRadius);
        }
        #endregion

        #region Public Methods
        public void Init(EmojiKeyViewModel dc) {
            DC = dc;
            DC.SetRenderContext(this);
            this.MeasureFrame(false);
        }
        public override void Draw(CGRect rect) {
            if (!DC.IsPopupOpen) {
                return;
            }
            var context = UIGraphics.GetCurrentContext();

            var emoji_texts = DC.Items;
            var emoji_rects = DC.PopupRects.Select(x => x.ToCGRect()).ToArray();
            var sel_bg = DC.PopupItemFocusedBgHexColor.ToUIColor();
            int sel_idx = DC.SelectedIdx;

            // bg

            // items
            nfloat fontSize = DC.PopupFontSize.UnscaledF();

            int avail_count = Math.Min(emoji_rects.Length, emoji_texts.Length);
            for (int i = 0; i < avail_count; i++) {
                string emoji_text = emoji_texts[i];
                var emoji_rect = emoji_rects[i];
                if (i == sel_idx) {
                    // sel item bg
                    context.SetFillColor(sel_bg.CGColor);
                    var sel_bg_path = DC.SelectedPopupCornerRadius.ToPath(emoji_rect);
                    context.AddPath(sel_bg_path.CGPath);
                    context.DrawPath(CGPathDrawingMode.Fill);
                }

                context.DrawText(
                    emoji_rect,
                    emoji_text,
                    fontSize,
                    iosEmojiPagesView.EMOJI_FONT_FAMILY_NAME,
                    DC.EmojiFgHexColor.ToUIColor());
            }

            //base.Draw(rect);
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}