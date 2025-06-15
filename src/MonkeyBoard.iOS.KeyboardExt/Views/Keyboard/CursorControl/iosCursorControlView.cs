using Avalonia.Controls;
using CoreGraphics;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosCursorControlView : FrameViewBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.CursorControlRect.ToCGRect();
            base.MeasureFrame(invalidate);
        }

        #endregion

        #endregion
        #region Properties
        #region View Models
        public new CursorControlViewModel DC { get; set; }
        #endregion

        #region Views
        iosSelectAllButtonView SelectAllView { get; set; }
        #endregion

        #endregion

        #region Constructors
        public iosCursorControlView(CursorControlViewModel dc) {
            DC = dc;
            ResetRenderer();
            SelectAllView = new iosSelectAllButtonView(DC);
            this.AddSubview(SelectAllView);
            RenderFrame(false);
        }



        #endregion

        #region Public Methods
        public void ResetRenderer() {
            DC.SetRenderContext(this);
        }
        public void Unload() {
            if(DC == null) {
                return;
            }
            DC.SetRenderContext(null);
        }

        public override void Draw(CGRect rect) {
            var context = UIGraphics.GetCurrentContext();
            // bg
            context.SetFillColor(DC.BgHexColor.ToUIColor().CGColor);
            context.FillRect(Bounds);

            // title
            nfloat title_offset = rect.Height / 4;
            var title_size = new CGSize(40, 40);
            context.DrawText(
                DC.SelectAllRect.ToCGRect().Move(0,title_offset),
                DC.TitleText,
                DC.TitleFontSize.UnscaledF(),
                iosKeyboardView.DEFAULT_FONT_FAMILY,
                DC.TitleFgHexColor.ToUIColor());

            // title icon
            context.DrawText(
                DC.SelectAllRect.ToCGRect().Move(0,title_offset + title_size.Height + 3),
                DC.TitleIconSourceObj.ToString(),
                DC.TitleFontSize.UnscaledF(),
                iosEmojiPagesView.EMOJI_FONT_FAMILY_NAME,
                UIColor.White);
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion
    }
}