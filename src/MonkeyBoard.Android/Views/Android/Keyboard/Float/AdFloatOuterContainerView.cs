using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using MonkeyBoard.Common;
using System;
using System.Linq;
using Point = Avalonia.Point;

namespace MonkeyBoard.Android {
    public class AdFloatOuterContainerView : AdCustomViewGroup, ITranslatePoint {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region ITranslatePoint Implementation
        Point ITranslatePoint.TranslatePoint(Point p) {
            double x_diff = InnerContainerView.Frame.Left;
            double y_diff = InnerContainerView.Frame.Top;
            return new Point(p.X - x_diff, p.Y - y_diff);
        }
        #endregion

        public override void MeasureFrame(bool invalidate) {
            var last_frame = this.Frame;
            this.Frame = DC.ContainerRect.ToRectF();


            OuterFloatPath = DC.ContainerCornerRadius.ToPath(Bounds);
            InnerFloatPath = DC.ContainerCornerRadius.ToPath(DC.InnerBorderRect.ToRectF());
            // NOTE inset is to cover seam form mask
            MaskPath = DC.ContainerCornerRadius.ToPath(DC.InnerContainerRect.Inset(2, 2).ToRectF());
            //OuterFloatPath.AddPath(InnerFloatPath);

            if(last_frame != this.Frame) {
                this.UpdateLayout(this.Frame);
            }
            if(invalidate && DC.KeyboardViewModel.FooterViewModel.IsDragging) {
                // special case when scaling avoid full render, its handled
                this.Redraw();
                HandlesView.Redraw();
                return;
            }
            base.MeasureFrame(invalidate);
        }

        public override void PaintFrame(bool invalidate) {
            base.PaintFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        public AdCustomPopupWindow FloatWindow { get; set; }
        Path OuterFloatPath { get; set; }
        Path InnerFloatPath { get; set; }
        Path MaskPath { get; set; }
        #endregion

        #region Views
        AdFloatInnerContainerView InnerContainerView { get; set; }
        AdFloatHandlesView HandlesView { get; set; }
        #endregion

        #region View Models
        public new FloatContainerViewModel DC =>
            InnerContainerView.DC;
        #endregion

        #region Appearance
        #endregion

        #region Layout
        #endregion

        #region State
        bool? WasLastShowPortrait { get; set; }
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public AdFloatOuterContainerView(AdKeyboardView kbv, Context context, Paint paint) : base(context, paint) {
            InnerContainerView = new AdFloatInnerContainerView(kbv, context, paint).SetDefaultProps();
            this.AddView(InnerContainerView);

            HandlesView = new AdFloatHandlesView(DC, context, paint).SetDefaultProps();
            this.AddView(HandlesView);

            DC.SetRenderContext(this);
            this.RenderFrame(false);

            if(Context is IOnTouchListener otl) {
                this.SetOnTouchListener(otl);
            }

            DC.OnFloatPositionChanged += DC_OnFloatLocationChanged;
        }

        #endregion

        #region Public Methods
        public void ShowWindow(AdCustomViewGroup anchor) {
            if(WasLastShowPortrait is { } was_portrait &&
                    was_portrait != AdDeviceInfo.IsPortrait) {
                // BUG when orientation changes a ghost popup is left from the last orientation\
                // BUG2 PopupWindow.IsShowing says false after orientation chnage, can't use it
                DismissWindow();
            }
            if(FloatWindow == null) {
                if(AdCustomPopupWindow.Create(anchor) is not { } fw) {
                    return;
                }
                FloatWindow = fw;
                FloatWindow.ContentView = this;
            }
            InnerContainerView.AttachKeyboard();

            PositionFloatWindow();

            WasLastShowPortrait = AdDeviceInfo.IsPortrait;
        }
        void DismissWindow() {
            if(FloatWindow == null) {
                return;
            }
            FloatWindow.Dismiss();
            FloatWindow = null;
        }

        public bool DismissAndDetachWindow() {
            bool detach = InnerContainerView.DetachKeyboard();
            DismissWindow();
            return detach;
        }

        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if(!DC.IsVisible ||
                OuterFloatPath is not { } ofp ||
                InnerFloatPath is not { } ifp ||
                MaskPath is not { } mp) {
                return;
            }
            SharedPaint.Color = DC.FloatBorderHex.ToAdColor();
            canvas.DrawPath(ofp, SharedPaint);

            SharedPaint.Color = DC.FloatBgHex.ToAdColor();
            canvas.DrawPath(ifp, SharedPaint);


            var xfer = SharedPaint.Xfermode;
            SharedPaint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));
            canvas.DrawPath(mp, SharedPaint);
            SharedPaint.SetXfermode(xfer);
        }
        #endregion

        #region Private Methods

        private void DC_OnFloatLocationChanged(object sender, EventArgs e) {
            PositionFloatWindow();
        }

        void PositionFloatWindow() {
            if(FloatWindow == null) {
                return;
            }

            var win_rect = GetFloatWindowRect();
            FloatWindow.Width = (int)win_rect.Width();
            FloatWindow.Height = (int)win_rect.Height();


            if(!FloatWindow.IsShowing) {
                FloatWindow.Show(win_rect.Position());
                WasLastShowPortrait = AdDeviceInfo.IsPortrait;
            }
            FloatWindow.Update((int)win_rect.Left, (int)win_rect.Top, (int)win_rect.Width(), (int)win_rect.Height(), true);

        }
        RectF GetFloatWindowRect() {
            var puw_rect = DC.FloatScreenRect.ToRectF();

            var loc = GetTranslatedPopupPosition(puw_rect.Position());
            float l = loc.X;
            float t = loc.Y;
            float r = l + puw_rect.Width();
            float b = t + puw_rect.Height();
            return new RectF(l, t, r, b);
        }
        public PointF GetTranslatedPopupPosition(PointF screenPoint) {
            float anchor_h = 0;

            //FloatWindow == null || FloatWindow.AnchorView == null ? 
            //    0 : FloatWindow.AnchorView.Height;
            var screen_rect = AdDeviceInfo.UnscaledWorkAreaRect.ToRect();
            var x = screenPoint.X;
            var y = screenPoint.Y;
            y -= screen_rect.Height();
            y += anchor_h;
            return new PointF(x, y);
        }


        void TestPopups() {
            if(FloatWindow == null || FloatWindow.AnchorView is not { } PopupAnchorView) {
                return;
            }
            var screen_rect = AdDeviceInfo.UnscaledWorkAreaRect.ToRect();

            GravityFlags flags = GravityFlags.NoGravity;
            int oy = (int)PopupAnchorView.Height;
            int tw_width = 150;
            int tw_height = 150;

            // BL
            var tw1 = new PopupWindow().SetDefaultProps();
            tw1.ContentView = new AdCustomView(Context, SharedPaint) { Frame = new RectF(0, 0, tw_width, tw_height) }.SetDefaultProps();
            tw1.ContentView.SetBackgroundColor(Color.Orange);
            tw1.Width = tw_width;
            tw1.Height = tw_height;
            var test3 = -tw_height + oy;
            var test4 = GetTranslatedPopupPosition(new(0, screen_rect.Height() - tw_height));
            tw1.ShowAtLocation(PopupAnchorView, flags, 0, (int)GetTranslatedPopupPosition(new PointF(0, screen_rect.Height() - tw_height)).Y); //-tw_height + oy);

            // TL
            var tw2 = new PopupWindow().SetDefaultProps();
            tw2.ContentView = new AdCustomView(Context, SharedPaint) { Frame = new RectF(0, 0, tw_width, tw_height) }.SetDefaultProps();
            tw2.ContentView.SetBackgroundColor(Color.Pink);
            tw2.Width = tw_width;
            tw2.Height = tw_height;
            var test = -(screen_rect.Height() + tw_height) + oy;
            var test2 = GetTranslatedPopupPosition(new());
            tw2.ShowAtLocation(PopupAnchorView, flags, 0, (int)GetTranslatedPopupPosition(new()).Y);

            // CL
            var tw3 = new PopupWindow().SetDefaultProps();
            tw3.ContentView = new AdCustomView(Context, SharedPaint) { Frame = new RectF(0, 0, tw_width, tw_height) }.SetDefaultProps();
            tw3.ContentView.SetBackgroundColor(Color.Blue);
            tw3.Width = tw_width;
            tw3.Height = tw_height;
            var test5 = -(int)((float)(screen_rect.Height() / 2f) + ((float)tw_height / 2f)) + oy;
            var test6 = GetTranslatedPopupPosition(new PointF(0, (screen_rect.Height() / 2f) + ((float)tw_height / 2f)));
            tw3.ShowAtLocation(PopupAnchorView, flags, 0, (int)GetTranslatedPopupPosition(new PointF(0, (screen_rect.Height() / 2f) + ((float)tw_height / 2f))).Y);// -(int)((float)(screen_rect.Height() / 2f) + ((float)tw_height / 2f)) + oy);
        }
        #endregion

        #region Commands
        #endregion
    }
}