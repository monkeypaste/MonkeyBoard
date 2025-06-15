using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public abstract class InertiaScrollerBase {
        #region Private Variables
        #endregion

        #region Constants

        protected const double MAX_DT_FOR_INERTIA_SCROLL = 0.3d;
        protected const double INERTIA_FRICTION = 0.07d;
        protected const double MIN_INTERTIA_VEL = 0.1;
        protected const double INERTIA_FPS = 60d;
        protected const double SNAP_RATIO = KeyConstants.PHI * 3;
        
        public const double MIN_SCROLL_DISPLACEMENT = 12d;

        #endregion

        #region Statics
        public static InertiaScrollerBase Create(IInertiaScroll host, IKeyboardInputConnection ic) {
            if (ic.AnimationTimer is { } timer) {
                return new TimerInertiaScroll(host, timer);
            }
            return new DispatcherInertiaScroll(host, ic.MainThread);
        }
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        protected IInertiaScroll ScrollHost { get; set; }
        #endregion

        #region Layout
        protected double Friction { get; set; } = INERTIA_FRICTION;
        protected Rect ScrollExtent { get; set; } = new();
        protected Rect ScrollViewport { get; set; } = new();

        protected Rect SnapExtent {
            get {
                double sw = ScrollExtent.Width > 0 ? SnapDist.Width : 0;
                double sh = ScrollExtent.Height > 0 ? SnapDist.Height : 0;
                return ScrollExtent.Flate(-sw, -sh, sw, sh);
            }
        }
        protected Size SnapDist =>
            new Size(ScrollViewport.Width / SNAP_RATIO, ScrollViewport.Height / SNAP_RATIO);
        protected Size SnapItemSize { get; set; } = default;
        public Point ScrollOffset { get; private set; }
        #endregion

        #region State
        Action _scrollChangedHandler;
        Action ScrollChangedHandler {
            get {
                if(_scrollChangedHandler == null &&
                    ScrollHost is FrameViewModelBase fvm &&
                    fvm.InputConnection is { } ic &&
                    ic.MainThread is { } mt) {
                    _scrollChangedHandler = () => {                        
                        mt.Post(()=>ScrollHost.Renderer.PaintFrame(true));
                    };
                }
                return _scrollChangedHandler;
            }
            set => _scrollChangedHandler = value;
        }
        protected Point vel { get; set; }
        protected Point ScrollDisplacement { get; set; } = new();
        protected Point PressScrollOffset { get; set; }
        public bool IsScrolling =>
            IsUserScrolling || !IsFuzzyZero(vel);
        public bool IsUserScrolling =>
            ScrollDisplacement.X > MIN_SCROLL_DISPLACEMENT ||
            ScrollDisplacement.Y > MIN_SCROLL_DISPLACEMENT;
        //bool IsAnimating { get; set; }
        protected string LastTouchId { get; set; }

        public bool IsCanceled { get; private set; }
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        protected InertiaScrollerBase(IInertiaScroll host) {
            ScrollHost = host;
        }
        #endregion

        #region Public Methods
        public void SetFriction(double friction) {
            Friction = friction;
        }
        public void ScrollToHome() => SetOffset(ScrollExtent.Left, ScrollExtent.Top);
        public void ScrollToEnd() => SetOffset(ScrollExtent.Right, ScrollExtent.Bottom);
        public void SetExtent(double minX, double minY, double maxX, double maxY) {
            double w = Math.Abs(minX) + Math.Abs(maxX);
            double h = Math.Abs(minY) + Math.Abs(maxY);
            ScrollExtent = new Rect(minX, minY, w, h);
        }
        public void SetViewport(Rect viewport) {
            ScrollViewport = viewport;
        }
        public void SetSnapItemSize(double w, double h) {
            SnapItemSize = new Size(w, h);
        }
        public void ForceOffset(double x, double y) {
            SetOffset(x, y, false);
            ResetInertia();
        }
        public void Update(Touch touch, TouchEventType eventType) {
            switch (eventType) {
                case TouchEventType.Press:
                    IsCanceled = false;
                    LastTouchId = touch.Id;
                    PressScrollOffset = ScrollOffset;
                    ResetInertia();
                    break;
                case TouchEventType.Move:
                    var delta = touch.Location - touch.LastLocation;
                    var new_offset = ScrollOffset - delta;
                    SetOffset(new_offset.X,new_offset.Y);
                    break;
                case TouchEventType.Release:
                    StartInertiaScroll(touch);
                    break;
            }
        }
        public void Cancel(bool reset) {
            LastTouchId = null;
            if(reset) {
                SetOffset(PressScrollOffset.X, PressScrollOffset.Y);
            }
            ResetInertia();
            IsCanceled = true;
        }
        public void OnScrollChanged(Action action) {
            ScrollChangedHandler = action;
        }
        #endregion

        #region Protected Methods
        protected async Task DoSnapScrollAsync() {
            double dx = 0;
            double dy = 0;

            if (ScrollOffset.X < ScrollExtent.Left) {
                dx = ScrollExtent.Left - ScrollOffset.X;
            } else if (ScrollOffset.X > ScrollExtent.Right) {
                dx = ScrollExtent.Right - ScrollOffset.X;
            } else if (ScrollOffset.Y < ScrollExtent.Top) {
                dy = ScrollExtent.Top - ScrollOffset.Y;
            } else if (ScrollOffset.Y > ScrollExtent.Bottom) {
                dy = ScrollExtent.Bottom - ScrollOffset.Y;
            }
            if (dx == 0 && dy == 0) {
                // should have at least 1 non-zero
                Debugger.Break();
                return;
            }
            string release_touch_id = LastTouchId;
            var start = ScrollOffset.ToPortablePoint();
            var end = start + new MpPoint(dx, dy);

            await start.AnimatePointAsync(
                end: end,
                tts: 0.1d,
                fps: INERTIA_FPS,
                tickWithVelocity: false,
                tick: (p) => {
                    SetOffset(p.X, p.Y);
                    // cancel when animating (prolly shouldn't)
                    return LastTouchId != release_touch_id;
                });
        }
        #endregion

        #region Private Methods
        protected void SetOffset(double x, double y, bool allowSnap = true) {
            bool was_scrolling = IsUserScrolling;
            var last_offset = ScrollOffset;
            double disp_x = Math.Min(ScrollExtent.Width, Math.Abs(x - last_offset.X));
            double disp_y = Math.Min(ScrollExtent.Height, Math.Abs(y - last_offset.Y));

            ScrollDisplacement += new Point(disp_x,disp_y);

            var extent = allowSnap ? SnapExtent : ScrollExtent;
            x = Math.Clamp(x, extent.Left, extent.Right);
            y = Math.Clamp(y, extent.Top, extent.Bottom);
            ScrollOffset = new Point(x, y);

            if(ScrollOffset != last_offset) {
                ScrollChangedHandler?.Invoke();
            }
        }
        protected abstract void StartInertiaScroll(Touch touch);
        protected bool IsFuzzyZero(Point vel, double threshold = MIN_INTERTIA_VEL) {
            return Math.Abs(vel.X) < threshold && Math.Abs(vel.Y) < threshold;
        }
        protected Point FindSnapCompletionTarget() {

            int max_x = SnapItemSize.Width == 0 ? 0 : (int)(ScrollExtent.Width / SnapItemSize.Width);
            int max_y = SnapItemSize.Height == 0 ? 0 : (int)(ScrollExtent.Height / SnapItemSize.Height);

            int snap_x = SnapItemSize.Width == 0 ? 0 : (int)ScrollOffset.X % (int)SnapItemSize.Width;
            if(snap_x >= (int)(SnapItemSize.Width/2)) {
                snap_x = Math.Min(snap_x + 1,max_x);
            }
            int snap_y = SnapItemSize.Height == 0 ? 0 : (int)ScrollOffset.Y % (int)SnapItemSize.Height;
            if(snap_y >= (int)(SnapItemSize.Height/2)) {
                snap_y = Math.Min(snap_y + 1,max_y);
            }
            return new Point(snap_x, snap_y);
        }
        protected void ResetInertia() {
            ScrollDisplacement = new();
            //IsAnimating = false;
        }
        protected bool NeedsSnap() {
            return SnapExtent.Contains(ScrollOffset) && !ScrollExtent.Contains(ScrollOffset);
        }
        #endregion
    }
}
