using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Avalonia;
using MonkeyBoard.Common;
using System;
using System.Linq;

namespace MonkeyBoard.Android {
    public class AdCustomView : View, IFrameView, IFrameRenderer {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IFrameRenderer Implementation
        #endregion

        #region IFrameRenderer Implementation
        public bool IsDisposed => false;
        public virtual void LayoutFrame(bool invalidate) {
            if (invalidate) {
                this.Redraw();
            }
        }

        public virtual void MeasureFrame(bool invalidate) {
            bool needs_layout = Frame != LastFrame || (DC != null && DC.IsVisible != LastVisible);
            LastFrame = Frame;
            LastVisible = DC != null && DC.IsVisible;

            if (needs_layout) {
                this.UpdateLayout(Frame);
            }
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
        #endregion

        #endregion

        #region Properties
        public FrameViewModelBase DC { get; }
        public Paint SharedPaint { get; set; }
        private RectF _frame = new();
        public RectF Frame {
            get => _frame;
            set {
                _frame = value;
                Bounds = _frame.ToBounds();
            }
        }
        bool LastVisible { get; set; }
        RectF LastFrame { get; set; }
        public RectF Bounds { get; private set; } = new();
        public string Name { get; set; } = "Unnamed";

        protected Handler _handler;
        public new Handler Handler {
            get {
                if (_handler == null) {
                    _handler = new Handler(Context.MainLooper);
                }
                return _handler;
            }
        }

        public object TagObj { get; set; }

        #endregion

        #region Events
        #endregion

        #region Constructors

        public AdCustomView(Context context, Paint paint) : base(context) {
            SharedPaint = paint;
            this.SetBackgroundColor(Color.Transparent);

            if (context is AdInputMethodService ims) {
                // NOTE HW accel is enabled at app level
                // and can only be DISABLED at view level
                // https://developer.android.com/topic/performance/hardware-accel#view-level
                LayerType lt = ims.IsHwAccelEnabled ? LayerType.Hardware : LayerType.Software;
                this.SetLayerType(lt, SharedPaint);
            }
        }
        #endregion

        #region Public Methods
        public override string ToString() {
            return $"{Name} {Frame}";
        }
        #endregion

        #region Protected Methods
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
            SetMeasuredDimension((int)Frame.Width(), (int)Frame.Height());
        }
        protected override void OnLayout(bool changed, int left, int top, int right, int bottom) {
            changed =
                left != (int)Frame.Left ||
                top != (int)Frame.Top ||
                right != (int)Frame.Right ||
                bottom != (int)Frame.Bottom;
            base.OnLayout(changed, (int)Frame.Left, (int)Frame.Top, (int)Frame.Right, (int)Frame.Bottom);
        }
        protected override void OnDraw(Canvas canvas) {
            //canvas.DrawColor(Color.Transparent, BlendMode.Multiply);
        }

        #endregion

        #region Private Methods
        #endregion

    }
}