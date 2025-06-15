using Android.Content;
using Android.Graphics;
using MonkeyBoard.Common;
using System;
using GPaint = Android.Graphics.Paint;

namespace MonkeyBoard.Android {
    public class AdSpeechView : AdCustomView {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.ContentRect.ToRectF();
            base.MeasureFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new SpeechViewModel DC { get; private set; }
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
        public AdSpeechView(Context context, Paint paint, SpeechViewModel dc) : base(context, paint) {
            DC = dc;
            DC.SetRenderContext(this);
        }
        #endregion

        #region Public Methods
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
            SharedPaint.Color = DC.FgHexColor.ToAdColor();
            SharedPaint.TextSize = DC.IconSize.UnscaledF();
            SharedPaint.TextAlign = GPaint.Align.Center;
            float x = Bounds.CenterX();
            float y = (Bounds.Height() / 3);// + SharedPaint.TextSize;
            canvas.DrawText(DC.IconSourceObj.ToString(), x, y, SharedPaint);

            SharedPaint.TextSize = DC.SpeechTextSize.UnscaledF();
            Typeface last_typeface = SharedPaint.Typeface;
            SharedPaint.SetTypeface(Typeface.Create(Typeface.Default/*Resources.GetFont(AdKeyboardView.DEFAULT_FONT_RES_ID)*/, TypefaceStyle.Italic));
            y = Bounds.Height() - (Bounds.Height() / 3);
            canvas.DrawText(DC.SpeechText, x, y, SharedPaint);
            SharedPaint.SetTypeface(last_typeface);
        }
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}