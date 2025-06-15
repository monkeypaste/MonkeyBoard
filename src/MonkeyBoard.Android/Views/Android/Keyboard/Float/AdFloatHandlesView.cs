using Android.Content;
using Android.Graphics;
using MonkeyBoard.Common;
using System;
using System.Linq;

namespace MonkeyBoard.Android {
    public class AdFloatHandlesView : AdCustomViewGroup {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void MeasureFrame(bool invalidate) {
            this.Frame = DC.ContainerRect.ToBounds().ToRectF();
            base.MeasureFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new FloatContainerViewModel DC { get; private set; }
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
        public AdFloatHandlesView(FloatContainerViewModel dc, Context context, Paint paint) : base(context,paint) {
            DC = dc;
        }
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if(!DC.IsHandlesVisible) {
                return;
            }

            var rects = DC.HandleRects.Select(x => x.ToRectF()).ToArray();
            var radii = DC.HandleCornerRadii.ToArray();
            var color = DC.FloatBorderHex.ToAdColor();
            int avail_count = Math.Min(rects.Length, radii.Length);
            SharedPaint.Color = color;
            //var debug_colors = new Color[] { Color.Red, Color.Green, Color.Blue, Color.Orange };
            for (int i = 0; i < avail_count; i++) {
                canvas.DrawPath(radii[i].ToPath(rects[i]), SharedPaint);
                //canvas.DrawRect(rects[i], SharedPaint);
            }
        }
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}