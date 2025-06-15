using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MonkeyBoard.Common {
    public static class Touches {
        static List<string> IgnoredTouchIds { get; set; } = [];
        static List<Touch> _touches = [];
        static Dictionary<Touch,object> _owners = [];

        public static bool TrySetOwner(object owner, Touch touch, out string touchId) {
            touchId = null;
            if(_owners.ContainsKey(touch)) {
                // already owned
                return false;
            }
            if(_owners.ContainsValue(owner)) {
                // already owning 
                return false;
            }
            touchId = touch.PlatformId;
            _owners.Add(touch, owner);
            return true;
        }
        public static bool IsOwner(Touch touch, object owner) {
            if(_owners.TryGetValue(touch, out object touchOwnerObj) &&
                touchOwnerObj == owner) {
                return true;
            }
            return false;
        }

        public static int Count =>
            _touches.Count;
        public static Touch LocateByLocation(Point p) {
            return _touches.OrderBy(x => Dist(x.Location, p)).FirstOrDefault();
        }
        public static Touch LocateById(string platformId) {
            return _touches.FirstOrDefault(x => x.PlatformId == platformId);
        }
        public static Touch Update(string platformId, Point p, Point rawP, TouchEventType touchType) {
            // returns touch at p loc
            if(touchType == TouchEventType.Press) {
                var nt = new Touch(platformId,p,rawP);
                if(IsDuplicateTouch(nt)) {
                    // HACK intermittent android bug, somehow events are being reported twice sometimes (double types every character)
                    MpConsole.WriteLine($"Duplicate touch detected! Ignoring it...");
                    return null;
                }
                _touches.Add(nt);                
                return nt;
            }

            if(LocateById(platformId) is not { } t) {
                return null;
            }
            t.SetLocation(p,rawP, touchType);
            if(touchType == TouchEventType.Release) {
                RemoveTouch(t);
            }
            return t;

        }
        public static void Clear() {
            _touches.Clear();
            _owners.Clear();
        }
        public static double Dist(Point p1, Point p2) {
            return Math.Sqrt(DistSquared(p1,p2));
        }
        public static double DistSquared(Point p1, Point p2) {
            return Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2);
        }
        static void RemoveTouch(Touch t) {
            var up_time = DateTime.Now;
            _touches.Remove(t);
            _owners.Remove(t);
        }

        static bool IsDuplicateTouch(Touch touch) {
            return _touches.Any(x => x.Location.Distance(touch.Location) <= 1);
        }
    }
}
