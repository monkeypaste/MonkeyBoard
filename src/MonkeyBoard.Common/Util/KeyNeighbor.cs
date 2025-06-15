using System;

namespace MonkeyBoard.Common {
    public class KeyNeighbor {
        public KeyNeighbor(KeyViewModel kvm, double min, double max) {
            Neighbor = kvm;
            StartAngle = min;
            EndAngle = max;
        }
        public KeyViewModel Neighbor { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }

        public bool Contains(double angle) {
            double min = Math.Min(StartAngle, EndAngle);
            double max = Math.Max(StartAngle, EndAngle);
            return angle >= min && angle <= max;
        }
        public override string ToString() {
            //return $"'{Neighbor.PrimaryValue}' [{StartAngle},{EndAngle}]";
            return $"{Neighbor.PrimaryValue}";
        }
    }
}
