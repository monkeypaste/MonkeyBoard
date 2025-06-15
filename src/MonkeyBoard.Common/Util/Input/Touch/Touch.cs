using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MonkeyBoard.Common {
    public class Touch {
        const double MAX_TOUCH_QUEUE_DT_S = 0.5d;
        public Dictionary<DateTime, Point> LocationHistory { get; set; } = [];
        public string PlatformId { get; private set; }
        public string Id { get; set; }
        public Point Location { get; private set; }
        public Point RawLocation { get; private set; }
        public Point PressLocation { get; private set; }
        public Point LastLocation { get; private set; }
        public Point LastRawLocation { get; private set; }
        //public Point Velocity { get; private set; }
        //public Point Acceleration { get; private set; }
        //public double Displacement { get; private set; }
        public DateTime LastUpdateDt { get; set; }
        public Touch(string platformId, Point p, Point rawP)  {
            PressLocation = p;
            LastLocation = p;
            Location = p;
            LastRawLocation = rawP;
            RawLocation = rawP;
            LastUpdateDt = DateTime.Now;
            PlatformId = platformId;
            Id = System.Guid.NewGuid().ToString();
            //MpConsole.WriteLine($"Touch created: '{this}'");
        }
        public void SetLocation(Point p, Point rawP,TouchEventType eventType) {
            var cur_time = DateTime.Now;

            LastLocation = Location;
            Location = p;

            LastRawLocation = RawLocation;
            RawLocation = rawP;
            //Displacement += Touches.Dist(Location, LastLocation);
            //if (eventType == TouchEventType.Place) {
            //    var dist = Location - LastLocation;
            //    var t = (cur_time - LastUpdateDt).TotalSeconds;
            //    var last_vel = Velocity;
            //    Velocity = dist / t;
            //    Acceleration = (Velocity - last_vel) / t;
            //} 
            LastUpdateDt = cur_time;

            UpdateQueue();
            //MpConsole.WriteLine($"[{Id}] Velocity: {Velocity}");
        }
        public bool SetOwner(object ownerObj) {
            return Touches.TrySetOwner(ownerObj, this, out _);
        }
        public bool IsOwner(object ownerObj) {
            return Touches.IsOwner(this, ownerObj);
        }

        public override string ToString() {
            return $"Id: {Id} {Location}";
        }

        void UpdateQueue() {
            var update_t = DateTime.Now;
            LocationHistory.Add(update_t, Location);
            //if (LocationHistory.Count <= 5) {
            //    return;
            //}
            var to_remove =
                LocationHistory
                .Where(x => update_t - x.Key > TimeSpan.FromSeconds(MAX_TOUCH_QUEUE_DT_S)
                ).Select(x => x.Key).ToList();
            to_remove.ForEach(x => LocationHistory.Remove(x));
        }
    }
}
