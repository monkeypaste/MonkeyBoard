using Android.Content;
using Android.Graphics;

using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System.Linq;

namespace MonkeyBoard.Android {
    public class AdKeyPopupView : AdCustomView {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.PopupRect.ToRectF().ToBounds();
            base.MeasureFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new KeyViewModel DC { get; set; }
        #endregion

        #endregion

        #region Constructors
        public AdKeyPopupView(Context context, Paint paint, KeyViewModel dc) : base(context, paint) {
            DC = dc;
            //DC.SetRenderContext(this);
            DC.PopupKeys.ForEach(x => x.SetRenderContext(this));
            this.MeasureFrame(false);
        }
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            var emoji_texts = DC.PopupKeys.Select(x=>x.PrimaryValue);
            var emoji_rects = DC.PopupKeys.Select(x => x.TotalRect).ToArray();
            var bg = DC.PopupBg.ToAdColor();
            var sel_bg = DC.PopupSelectedBg.ToAdColor();

            // bg
            SharedPaint.Color = bg;
            Path bg_path = DC.Parent.CommonCornerRadius.ToPath(Bounds);
            canvas.DrawPath(bg_path, SharedPaint);

            if(DC.PopupKeys.Count() > 1) {

            }
            // items
            foreach(var pu_kvm in DC.PopupKeys) {
                var pu_rect = pu_kvm.KeyboardRect.ToRectF();
                canvas.Save();
                canvas.Translate(pu_rect.Left, pu_rect.Top);

                SharedPaint.Color = pu_kvm.IsActiveKey ? sel_bg : bg;
                canvas.DrawPath(DC.Parent.CommonCornerRadius.ToPath(pu_rect.ToBounds()), SharedPaint);

                AdHelpers.DrawAlignedText(
                    canvas,
                    SharedPaint,
                    pu_rect.ToBounds(),
                    pu_kvm.PrimaryValue,
                    pu_kvm.PrimaryFontSize.UnscaledF(),
                    pu_kvm.PrimaryHex.ToAdColor(),
                    pu_kvm.PrimaryTextHorizontalAlignment,
                    pu_kvm.PrimaryTextVerticalAlignment,
                    pu_kvm.PrimaryTextOffset.ToPointF());
                canvas.Restore();
            }

        }
        #endregion

        #region Private Methods
        #endregion
    }
}
