using Android.Content;
using Android.Graphics;
using AndroidX.Annotations;
using MonkeyBoard.Common;

namespace MonkeyBoard.Android {
    public class AdFooterView : AdCustomViewGroup {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void MeasureFrame(bool invalidate) {
            this.Frame = DC.FooterRect.ToRectF();
            if(!DC.IsVisible) {
                base.MeasureFrame(invalidate);
                return;
            }
            FooterPath = DC.FooterCornerRadius.ToPath(Bounds);
            HandlePath = DC.DragHandleCornerRadius.ToPath(DC.DragHandleRect.ToRectF());
            LeftButtonPath = DC.FooterButtonCornerRadius.ToPath(DC.LeftButtonRect.ToRectF());
            RightButtonPath = DC.FooterButtonCornerRadius.ToPath(DC.RightButtonRect.ToRectF());

            if (DC.IsVisible && DC.IsDragHandleVisible) {
                DragBmp = DragBmp.LoadRescaleOrIgnore(DC.DragHandleIconSourceObj.ToString(), DC.DragHandleRect.ToRectF());
            }
            LeftButtonBmp = LeftButtonBmp.LoadRescaleOrIgnore(DC.LeftButtonIconSourceObj.ToString(), DC.LeftButtonImageRect.ToRectF());
            RightButtonBmp = RightButtonBmp.LoadRescaleOrIgnore(DC.RightButtonIconSourceObj.ToString(), DC.RightButtonImageRect.ToRectF());
            base.MeasureFrame(invalidate);
        }

        #endregion

        #region Properties

        #region Members
        Bitmap LeftButtonBmp { get; set; }
        Bitmap RightButtonBmp { get; set; }
        Bitmap DragBmp { get; set; }
        Path HandlePath { get; set; }
        Path FooterPath { get; set; }
        Path LeftButtonPath { get; set; }
        Path RightButtonPath { get; set; }
        #endregion

        #region Views

        #endregion
        #region View Models
        public new FooterViewModel DC { get; private set; }
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
        public AdFooterView(Context context, Paint paint,FooterViewModel dc) : base(context,paint) {
            DC = dc;
            ResetRenderers();
        }
        #endregion

        #region Public Methods
        public void ResetRenderers() {
            DC.SetRenderContext(this);
        }
        public override void Draw(Canvas canvas) {
            if(!DC.IsVisible) {
                return;
            }
            canvas.ClipPath(FooterPath);

            SharedPaint.Color = DC.BgHex.ToAdColor();
            canvas.DrawRect(Bounds, SharedPaint);
                        

            if (DC.IsLeftButtonVisible && LeftButtonBmp != null) {
                if (DC.LeftButtonBgHex is { } bg_hex &&
                    LeftButtonPath is { } lbp) {
                    SharedPaint.Color = bg_hex.ToAdColor();
                    canvas.DrawPath(lbp, SharedPaint);
                }
                var btn_rect = DC.LeftButtonImageRect.ToRectF();
                SharedPaint.SetTint(DC.FooterFgHex.ToAdColor());
                canvas.DrawBitmap(LeftButtonBmp, btn_rect.Left, btn_rect.Top, SharedPaint);
                SharedPaint.SetTint(null);
            }


            if (RightButtonBmp != null) {
                if(DC.RightButtonBgHex is { } bg_hex &&
                    RightButtonPath is { } rbp) {
                    SharedPaint.Color = bg_hex.ToAdColor();
                    canvas.DrawPath(rbp, SharedPaint);
                }
                var btn_rect = DC.RightButtonImageRect.ToRectF();
                SharedPaint.SetTint(DC.FooterFgHex.ToAdColor());
                canvas.DrawBitmap(RightButtonBmp, btn_rect.Left, btn_rect.Top, SharedPaint);
                SharedPaint.SetTint(null);
            }
            if (!string.IsNullOrEmpty(DC.LabelText)) {
                canvas.DrawAlignedText(
                    SharedPaint,
                    Bounds,
                    DC.LabelText,
                    DC.LabelFontSize.UnscaledF(),
                    DC.FooterFgHex.ToAdColor(),
                    DC.LabelHorizontalAlignment,
                    DC.LabelVerticalAlignment);
            } else if (DC.IsDragHandleVisible && DragBmp != null) {
                SharedPaint.SetTint(DC.DragHandleFgHex.ToAdColor());
                var btn_rect = DC.DragHandleRect.ToRect();
                canvas.DrawBitmap(DragBmp, btn_rect.Left, btn_rect.Top, SharedPaint);
                SharedPaint.SetTint(null);
            }

            base.Draw(canvas);
        }
        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods
        #endregion
    }
}