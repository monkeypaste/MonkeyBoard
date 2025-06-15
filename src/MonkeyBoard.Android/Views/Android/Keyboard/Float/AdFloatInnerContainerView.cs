using Android.Content;
using Android.Graphics;
using Android.Views;
using MonkeyBoard.Common;
using System;
using System.Linq;

namespace MonkeyBoard.Android {
    public class AdFloatInnerContainerView : AdCustomViewGroup {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void MeasureFrame(bool invalidate) {
            this.Frame = DC.InnerContainerRect.ToRectF();
            base.MeasureFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        #endregion

        #region Views
        public AdKeyboardView KeyboardView { get; private set; }
        #endregion

        #region View Models
        public new FloatContainerViewModel DC =>
            KeyboardView.DC.FloatContainerViewModel;
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
        public AdFloatInnerContainerView(AdKeyboardView kbv, Context context, Paint paint) : base(context,paint) {
            KeyboardView = kbv;
        }
        #endregion

        #region Public Methods
        public bool AttachKeyboard() {
            if (KeyboardView.Parent is AdFloatInnerContainerView) {
                // already attached
                return false;
            }
            if (KeyboardView.Parent is ViewGroup vg) {
                vg.RemoveView(KeyboardView);
            }
            this.AddView(KeyboardView);
            return true;
        }
        public bool DetachKeyboard() {
            if (KeyboardView.Parent is not AdFloatInnerContainerView) {
                return false;
            }
            RemoveView(KeyboardView);
            return true;
        }
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            
        }
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}