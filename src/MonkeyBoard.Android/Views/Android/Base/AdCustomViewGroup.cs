using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using MonkeyBoard.Common;

namespace MonkeyBoard.Android {
    public interface IFrameView {
        RectF Frame { get; }
    }
    public class AdCustomViewGroup : FrameLayout, IFrameRenderer, IFrameView {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public bool IsDisposed => false;
        public virtual void LayoutFrame(bool invalidate) {
            for (int i = 0; i < ChildCount; i++) {
                if(this.GetChildAt(i) is IFrameRenderer ckbvr) {
                    ckbvr.LayoutFrame(invalidate);
                }
            }
            if(invalidate) {
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

            for (int i = 0; i < ChildCount; i++) {
                if (this.GetChildAt(i) is IFrameRenderer ckbvr) {
                    ckbvr.MeasureFrame(invalidate);
                }
            }
            if (invalidate) {
                this.Redraw();
            }
        }

        public virtual void PaintFrame(bool invalidate) {
            for (int i = 0; i < ChildCount; i++) {
                if (this.GetChildAt(i) is IFrameRenderer ckbvr) {
                    ckbvr.PaintFrame(invalidate);
                }
            }
            if (invalidate) {
                this.Redraw();
            }
        }

        public virtual void RenderFrame(bool invalidate) {
            LayoutFrame(false);
            MeasureFrame(false);
            PaintFrame(false);

            for (int i = 0; i < ChildCount; i++) {
                if (this.GetChildAt(i) is IFrameRenderer ckbvr) {
                    ckbvr.RenderFrame(invalidate);
                } else if(invalidate) {
                    this.GetChildAt(i).Redraw();
                }
            }
            if (invalidate) {
                this.Redraw();
            }
        }
        #endregion
        #endregion

        #region Properties
        public FrameViewModelBase DC { get; }
        public Paint SharedPaint { get; set; } = new();

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
        public object TagObj { get; set; }

        protected virtual bool RequiresHwAccel { get; }
        #endregion

        #region Constructors

        public AdCustomViewGroup(Context context) : this(context, null) { }
        public AdCustomViewGroup(Context context, Paint paint) : base(context) {
            if(context is not AdInputMethodService ims) {
                return;
            }
            SharedPaint = paint;
            this.SetWillNotDraw(false);
            this.SetBackgroundColor(Color.Transparent);


            // NOTE HW accel is enabled at app level
            // and can only be DISABLED at view level
            // https://developer.android.com/topic/performance/hardware-accel#view-level
            LayerType lt = ims.IsHwAccelEnabled || this.RequiresHwAccel ? LayerType.Hardware : LayerType.Software;
            this.SetLayerType(lt, SharedPaint);
        }
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
            SetMeasuredDimension((int)Frame.Width(), (int)Frame.Height());

            for (int i = 0; i < ChildCount; i++) {
                var child = GetChildAt(i);
                //child.Measure(widthMeasureSpec, heightMeasureSpec);
                if (child.Visibility != ViewStates.Gone) {
                    MeasureChild(child, widthMeasureSpec, heightMeasureSpec);
                }
            }
        }
        protected override void OnLayout(bool changed, int l, int t, int r, int b) {
            for (int i = 0; i < ChildCount; i++) {
                var child = GetChildAt(i);
                if (child is IFrameView fv) {
                    var frame = fv.Frame ?? new();
                    child.UpdateLayout(frame);
                }
                //if (child is AdCustomViewGroup cvg) {
                //    cvg.OnLayout(changed, l, t, r, b);
                //}
            }
        }

        protected override void OnDraw(Canvas canvas) {
            //base.OnDraw(canvas);
            //if(AdInputMethodService.IsDismissed) {

            //}
            //SharedPaint.Color = Color.Transparent;
            //canvas.DrawRect(Bounds, SharedPaint);
            //canvas.DrawColor(Color.Transparent, BlendMode.Multiply);
        }

        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}