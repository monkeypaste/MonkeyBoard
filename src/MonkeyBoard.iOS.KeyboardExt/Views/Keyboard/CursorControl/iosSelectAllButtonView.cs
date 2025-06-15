using CoreGraphics;
using MonkeyPaste.Keyboard.Common;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosSelectAllButtonView : FrameViewBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.SelectAllRect.ToCGRect();
            base.MeasureFrame(invalidate);
        }
        public override void PaintFrame(bool invalidate) {
            this.BackgroundColor = DC.SelectAllBgHexColor.ToUIColor();
            this.Layer.BorderColor = this.BackgroundColor.CGColor;
            base.PaintFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new CursorControlViewModel DC { get; private set; }
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
        public iosSelectAllButtonView(CursorControlViewModel dc) {
            DC = dc;
            Layer.CornerRadius = (nfloat)DC.SelectAllCornerRadius.TopLeft;
            Layer.BorderWidth = 1;
            Layer.MasksToBounds = true;
            MeasureFrame(false);
        }
        #endregion

        #region Public Methods
        public override void Draw(CGRect rect) {
            if(!DC.IsSelectAllVisible) {
                return;
            }
            var context = UIGraphics.GetCurrentContext();
            base.Draw(rect);
            // select all text
            context.DrawText(
                DC.SelectAllRect.ToCGRect().ToBounds(),
                DC.SelectAllText,
                DC.TitleFontSize.UnscaledF(),
                iosKeyboardView.DEFAULT_FONT_FAMILY,
                DC.SelectAllFgHexColor.ToUIColor());
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