using Avalonia.Layout;
using CoreGraphics;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosFooterView : FrameViewBase {
        #region Statics
        static iosFooterView _instance;
        static bool Frozen { get; set; }
        public static void SetLabel(string text, bool freeze = false) {
            if (_instance == null || _instance.DC == null || (Frozen && !freeze)) {
                return;
            }
            Frozen = freeze;
            _instance.DC.SetLabelText(text);
        }
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer

        public override void MeasureFrame(bool invalidate) {
            Frame = DC.FooterRect.ToCGRect();
            RightButton.Frame = DC.RightButtonRect.ToCGRect();
            NextKeyboardButton.Frame = DC.LeftButtonRect.ToCGRect();
            //GlobeButton.Frame = DC.LeftButtonRect.ToCGRect();
            base.MeasureFrame(invalidate);
        }
        public override void PaintFrame(bool invalidate) {
            this.BackgroundColor = DC.BgHex.ToUIColor();
            base.PaintFrame(invalidate);
        }
        #endregion

        #endregion
        #region Properties

        #region View Models
        public new FooterViewModel DC { get; set; }
        #endregion

        #region Views
        public UIControl NextKeyboardButton { get; private set; }
        public UIView RightButton { get; private set; }
        //UIImage DismissImage { get; set; }
        #endregion

        #region State
        //public string LabelText { get; private set; } = "TEST";
        #endregion

        #endregion

        #region Constructors
        public iosFooterView(FooterViewModel dc) {
            _instance = this;

            DC = dc;
            DC.SetRenderContext(this);
            this.UserInteractionEnabled = false;

            NextKeyboardButton = new NextKbButton().SetDefaultProps(true);
            NextKeyboardButton.Frame = DC.LeftButtonRect.ToCGRect();
            NextKeyboardButton.RoundCorners(DC.FooterButtonCornerRadius);
            this.AddSubview(NextKeyboardButton);

            var next_btn_img_view = new UIImageView();
            next_btn_img_view.Frame = DC.LeftButtonRelativeImageRect.ToCGRect();
            next_btn_img_view.Image = iosHelpers.LoadBitmap(DC.LeftButtonIconSourceObj.ToStringOrEmpty(),next_btn_img_view.Frame.Size).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            NextKeyboardButton.AddSubview(next_btn_img_view);

            RightButton = new UIView().SetDefaultProps();
            RightButton.Frame = DC.RightButtonRect.ToCGRect();
            RightButton.RoundCorners(DC.FooterButtonCornerRadius);
            this.AddSubview(RightButton);

            var dismiss_btn_img_view = new UIImageView();
            dismiss_btn_img_view.Frame = DC.RightButtonRelativeImageRect.ToCGRect();
            RightButton.AddSubview(dismiss_btn_img_view);
            SetDismissImage(false);
        }

        #endregion

        #region Public Methods
        public void SetDismissImage(bool collapsed) {
            if (RightButton.Subviews.OfType<UIImageView>().FirstOrDefault() is not { } dismiss_img_view) {
                return;                
            }

            UIImageOrientation io = collapsed ? UIImageOrientation.Right : UIImageOrientation.Left;
            var dismiss_img = iosHelpers.LoadBitmap(DC.RightButtonIconSourceObj.ToStringOrEmpty(), dismiss_img_view.Frame.Size).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            dismiss_img_view.Image = new UIImage(dismiss_img.CGImage, 1, io).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            dismiss_img_view.TintColor = DC.FooterFgHex.ToUIColor();
            this.Redraw();
        }
        public override void Draw(CGRect rect) {
            if (!DC.IsVisible) {
                return;
            }
            var context = UIGraphics.GetCurrentContext();
            NextKeyboardButton.BackgroundColor = DC.LeftButtonBgHex.ToUIColor();
            NextKeyboardButton.Subviews.OfType<UIImageView>().FirstOrDefault().TintColor = DC.FooterFgHex.ToUIColor();
            
            RightButton.BackgroundColor = DC.RightButtonBgHex.ToUIColor();
            RightButton.Subviews.OfType<UIImageView>().FirstOrDefault().TintColor = DC.FooterFgHex.ToUIColor();

            context.DrawText(
            //context.DrawText_CT(
                rect, 
                DC.LabelText,
                DC.LabelFontSize.UnscaledF(), 
                iosKeyboardView.DEFAULT_FONT_FAMILY, 
                DC.FooterFgHex.ToUIColor(),
                DC.LabelHorizontalAlignment.ToIosAlignment(),
                DC.LabelVerticalAlignment.ToIosAlignment(),
                0,
                0//iosKeyboardViewController.Instance.IsPreferencesVisible ? 10 : 0
                );
        }
        #endregion

        #region Private Methods
        #endregion
    }
}