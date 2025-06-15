using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using MonkeyBoard.Common;
using System;

using GPaint = Android.Graphics.Paint;

namespace MonkeyBoard.Android {
    public class CustomEditText : EditText , IFrameRenderer {
        #region ctors

        public CustomEditText(Context context, Paint paint) : base(context) {
            SharedPaint = paint;
        }
        #endregion
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        public Paint SharedPaint { get; set; }
        #endregion

        #region View Models
        #endregion

        #region Appearance
        #endregion

        #region Layout
        private RectF _frame = new();
        public RectF Frame {
            get => _frame;
            set {
                _frame = value;
                Bounds = _frame.ToBounds();
            }
        }
        public RectF Bounds { get; private set; } = new();
        #endregion

        #region State
        public bool IsDisposed => false;
        public string Name { get; set; } = "Unnamed";
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events

        #endregion

        #region Constructors
        #endregion

        #region Public Methods

        public virtual void LayoutFrame(bool invalidate) {
            if (invalidate) {
                this.Redraw();
            }
        }

        public virtual void MeasureFrame(bool invalidate) {
            this.UpdateLayout(Frame);
            if (invalidate) {
                this.Redraw();
            }
        }

        public virtual void PaintFrame(bool invalidate) {
            if (invalidate) {
                this.Redraw();
            }
        }
        public virtual void RenderFrame(bool invalidate) {
            LayoutFrame(false);
            MeasureFrame(false);
            PaintFrame(false);

            if (invalidate) {
                this.Redraw();
            }
        }
        public override string ToString() {
            return $"{Name} {Frame}";
        }
        #endregion

        #region Protected Methods

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
            SetMeasuredDimension((int)Frame.Width(), (int)Frame.Height());
        }
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}
