using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using MonkeyBoard.Common;
using GPaint = Android.Graphics.Paint;

namespace MonkeyBoard.Android {
    public class AdCursorControlView : AdCustomViewGroup, IFrameRenderer {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation

        public override void MeasureFrame(bool invalidate) {
            Frame = DC.CursorControlRect.ToRectF();

            base.MeasureFrame(invalidate);
        }
        #endregion
        #endregion

        #region Properties

        #region View Models
        public new CursorControlViewModel DC { get; set; }
        #endregion

        #region Views
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

        public AdCursorControlView(Context context, Paint paint, CursorControlViewModel dc) : base(context, paint) {
            DC = dc;
            DC.SetRenderContext(this);
            this.Visibility = ViewStates.Invisible;
            DC.OnShowCursorControl += DC_OnShowCursorControl;
            DC.OnHideCursorControl += DC_OnHideCursorControl;

        }

        #endregion

        #region Public Methods
        public void Unload() {
            if(DC == null) {
                return;
            }

            DC.OnShowCursorControl -= DC_OnShowCursorControl;
            DC.OnHideCursorControl -= DC_OnHideCursorControl;
        }
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if(!DC.IsVisible) {
                return;
            }
            // bg
            SharedPaint.Color = DC.BgHexColor.ToAdColor();
            canvas.DrawRect(Bounds, SharedPaint);

            // title
            SharedPaint.Color = DC.TitleFgHexColor.ToAdColor();
            SharedPaint.TextSize = DC.TitleFontSize.UnscaledF();
            SharedPaint.TextAlign = GPaint.Align.Center;
            var text_loc = DC.TitleTextLoc.ToPointF();
            canvas.DrawText(DC.TitleText, text_loc.X,text_loc.Y, SharedPaint);
            // icon
            var icon_loc = DC.TitleIconLoc.ToPointF();
            canvas.DrawText(DC.TitleIconSourceObj.ToString(), icon_loc.X,icon_loc.Y, SharedPaint);

            if(DC.IsSelectAllVisible) {
                // select all bg
                SharedPaint.Color = DC.SelectAllBgHexColor.ToAdColor();
                canvas.DrawPath(DC.SelectAllCornerRadius.ToPath(DC.SelectAllRect.ToRectF()), SharedPaint);
                // select all text
                SharedPaint.Color = DC.SelectAllFgHexColor.ToAdColor();
                SharedPaint.TextSize = DC.SelectAllFontSize.UnscaledF();
                SharedPaint.TextAlign = GPaint.Align.Center;
                var sa_text_loc = DC.SelectAllTextLoc.ToPointF();
                canvas.DrawText(DC.SelectAllText, sa_text_loc.X, sa_text_loc.Y, SharedPaint);
            }
        }
        #endregion

        #region Private Methods
        private void DC_OnHideCursorControl(object sender, System.EventArgs e) {
            this.Visibility = ViewStates.Invisible;
        }

        private void DC_OnShowCursorControl(object sender, System.EventArgs e) {
            this.Visibility = ViewStates.Visible;
        }
        #endregion

        #region Commands
        #endregion

    }
}