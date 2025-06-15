using Avalonia;
using CoreGraphics;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class UIBezierPathExt : UIBezierPath {
        // from https://stackoverflow.com/a/53128198/105028
        public UIBezierPathExt(CGRect rect, CGSize topLeftRadius, CGSize topRightRadius, CGSize bottomRightRadius, CGSize bottomLeftRadius) {
            base.Init();

            var path = new CGPath();

            var topLeft = rect.Location;
            var topRight = new CGPoint(rect.GetMaxX(), rect.GetMinY());
            var bottomRight = new CGPoint(rect.GetMaxX(), rect.GetMaxY());
            var bottomLeft = new CGPoint(rect.GetMinX(), rect.GetMaxY());

            if(topLeftRadius != CGSize.Empty) {
                path.MoveToPoint(new CGPoint(topLeft.X - topLeftRadius.Width, topLeft.Y));
            } else {
                path.MoveToPoint(new CGPoint(topLeft.X, topLeft.Y));
            }
            
            if(topRightRadius != CGSize.Empty) {
                path.AddLineToPoint(new CGPoint(topRight.X + topRightRadius.Width, topRight.Y));
                path.AddCurveToPoint(
                    new CGPoint(topRight.X, topRight.Y - topRightRadius.Height), 
                    new CGPoint(topRight.X,topRight.Y), 
                    new CGPoint(topRight.X,topRight.Y - topRightRadius.Height));
            } else {
                path.AddLineToPoint(new CGPoint(topRight.X, topRight.Y));
            }

            if(bottomRightRadius != CGSize.Empty) {
                path.AddLineToPoint(new CGPoint(bottomRight.X, bottomRight.Y + bottomRightRadius.Height));
                path.AddCurveToPoint(
                    new CGPoint(bottomRight.X + bottomRightRadius.Width, bottomRight.Y),
                    new CGPoint(bottomRight.X, bottomRight.Y),
                    new CGPoint(bottomRight.X + bottomRightRadius.Width, bottomRight.Y));
            } else {
                path.AddLineToPoint(new CGPoint(bottomRight.X, bottomRight.Y));
            }
            
            if(bottomLeftRadius != CGSize.Empty) {
                path.AddLineToPoint(new CGPoint(bottomLeft.X - bottomLeftRadius.Width, bottomLeft.Y));
                path.AddCurveToPoint(
                    new CGPoint(bottomLeft.X, bottomLeft.Y + bottomLeftRadius.Height),
                    new CGPoint(bottomLeft.X, bottomLeft.Y),
                    new CGPoint(bottomLeft.X, bottomLeft.Y + bottomLeftRadius.Height));
            } else {
                path.AddLineToPoint(new CGPoint(bottomLeft.X, bottomLeft.Y));
            }

            if (topLeftRadius != CGSize.Empty) {
                path.AddLineToPoint(new CGPoint(topLeft.X, topLeft.Y - topLeftRadius.Height));
                path.AddCurveToPoint(
                    new CGPoint(topLeft.X - topLeftRadius.Width, topLeft.Y),
                    new CGPoint(topLeft.X, topLeft.Y),
                    new CGPoint(topLeft.X - topLeftRadius.Width, topLeft.Y));
            } else {
                path.AddLineToPoint(new CGPoint(topLeft.X, topLeft.Y));
            }
            path.CloseSubpath();

            this.CGPath = path;
        }
    }
}