using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class TimerInertiaScroll : InertiaScrollerBase {
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
        IAnimationTimer Timer { get; set; }
        #endregion

        #region View Models
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
        public TimerInertiaScroll(IInertiaScroll host, IAnimationTimer timer) : base(host) {
            Timer = timer;
        }
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        protected override void StartInertiaScroll(Touch touch) {
            if (NeedsSnap()) {
                vel = new();
                DoSnapScrollAsync().FireAndForgetSafeAsync();
                return;
            }

            if (!touch.LocationHistory.Any()) {
                vel = new();
                return;
            }

            var min_kvp = touch.LocationHistory.First();
            var max_kvp = touch.LocationHistory.Last();

            double qdt = (max_kvp.Key - min_kvp.Key).TotalSeconds;
            Point dist = max_kvp.Value - min_kvp.Value;
            Point new_vel = qdt > 0 ? -dist / qdt : default;

            if (new_vel == default || IsFuzzyZero(new_vel, 1)) {
                vel = new();
                return;
            }
            vel = new_vel;

            //MpConsole.WriteLine($"Scroll vel: {vel}. Needs Snap: {NeedsSnap()}");

            Point lp = ScrollOffset;
            Point lv = vel;
            DateTime lt = DateTime.Now;
            int delay_ms = (int)MpAnimationExtensions.FpsToDelayTime(INERTIA_FPS).TotalMilliseconds;
            string release_touch_id = LastTouchId;

            object anim_lock = new object();
            Timer.Start(anim_lock, Update);

            void Update() {
                if (LastTouchId != release_touch_id) {
                    // new touch, cancel
                    Timer.Stop(anim_lock);
                    vel = new();
                    return;
                }
                if (IsFuzzyZero(vel)) {
                    // done
                    Timer.Stop(anim_lock);
                    vel = new();
                    return;
                }

                var t = DateTime.Now;
                double dt = (t - lt).TotalSeconds;

                vel *= (1 - Friction);
                Point accel = (vel - lv) / dt;
                Point pos = ScrollOffset + (vel * dt) + (0.5d * accel * dt * dt);
                SetOffset(pos.X, pos.Y, false);
                lv = vel;
                lp = ScrollOffset;
                lt = t;
            }

        }
        #endregion

        #region Private Methods

        async Task<bool> CheckAndDoSnapScrollAsync() {
            if (!NeedsSnap()) {
                return false;
            }
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
                return false;
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
                    //return !IsAnimating;
                    return LastTouchId != release_touch_id;
                });

            return true;
        }
        #endregion

        #region Commands
        #endregion
        
    }
}
