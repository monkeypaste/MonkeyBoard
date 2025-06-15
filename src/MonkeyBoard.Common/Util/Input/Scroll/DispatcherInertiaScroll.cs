using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class DispatcherInertiaScroll : InertiaScrollerBase {
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
        IMainThread Dispatcher { get; set; }
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
        public DispatcherInertiaScroll(IInertiaScroll host, IMainThread dispatcher) : base(host) {
            Dispatcher = dispatcher;
        }
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        protected override void StartInertiaScroll(Touch touch) {
            string scroll_touch_id = touch.Id;

            Dispatcher.Post(async () => {
                if (NeedsSnap()) {
                    await DoSnapScrollAsync();
                    vel = new();
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

                while (true) {
                    if (LastTouchId != release_touch_id) {
                        // new touch, cancel
                        vel = new();
                        return;
                    }
                    if (IsFuzzyZero(vel)) {
                        // done
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

                    await Task.Delay(delay_ms);
                }
            });
        }
        #endregion

        #region Private Methods


        
        #endregion

        #region Commands
        #endregion
        
    }
}
