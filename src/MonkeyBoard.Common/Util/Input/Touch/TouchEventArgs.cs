using Avalonia;
using System;

namespace MonkeyBoard.Common {
    public class TouchEventArgs : EventArgs {
        public string TouchId { get; private set; } = "UNDEFINED";
        public Point Location { get; private set; }
        public Point RawLocation { get; private set; }
        public TouchEventType TouchEventType { get; private set; }
        public TouchEventArgs(Point location, Point rawLocation, TouchEventType touchEventType, string touchId) : this(location, touchEventType) {
            TouchId = touchId;
            RawLocation = rawLocation;
        }
        public TouchEventArgs(Point location, TouchEventType touchEventType) {
            RawLocation = Location = location;
            TouchEventType = touchEventType;
        }

        public override string ToString() {
            return $"[{TouchId}] {TouchEventType} {Location}";
        }
    }
}
