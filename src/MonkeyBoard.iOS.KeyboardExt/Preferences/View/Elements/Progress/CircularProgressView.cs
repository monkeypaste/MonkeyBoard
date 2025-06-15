using CoreAnimation;
using CoreGraphics;
using Foundation;
using System;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {

    public class CircularProgressView : UIView {
        // from https://thulzmtetwa.medium.com/how-to-create-a-circular-progress-bar-using-uikit-swift-ios-6154470b28ef
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
        CAShapeLayer ProgressLayer { get; set; } = new CAShapeLayer();
        CAShapeLayer TrackLayer { get; set; } = new CAShapeLayer();

        CGPath ViewCGPath {
            get {
                var path = new UIBezierPath();
                path.AddArc(
                    center: new CGPoint(Frame.Size.Width / 2, Frame.Size.Height / 2),
                    radius: (nfloat)((Frame.Size.Width - 1.5) / 2),
                    startAngle: (nfloat)(-0.5 * double.Pi),
                    endAngle: (nfloat)(1.5 * double.Pi),
                    clockWise: true);
                return path.CGPath;
            }
        }
        #endregion

        #region Appearance

        UIColor ProgressColor { get; set; } = UIColor.Red;
        UIColor TrackColor { get; set; } = UIColor.White;
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public CircularProgressView(CGRect frame) : base(frame) {
            ConfigureProgressViewToBeCircular();
        }
        public CircularProgressView(NSCoder coder) : base(coder) {
            ConfigureProgressViewToBeCircular();
        }
        #endregion

        #region Public Methods

        public void SetProgressWithAnimation(TimeSpan dur, int progress) {
            var anim = new CABasicAnimation() {
                KeyPath = "strokeEnd",
                Duration = dur.TotalMilliseconds,
                From = NSNumber.FromInt32(0),//NSArray.FromObjects([NSNumber.FromInt32(0)]),
                To = NSNumber.FromFloat((float)(progress/100)),//NSArray.FromObjects([NSNumber.FromInt32(progress)]),
                TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.Linear),
            };
            ProgressLayer.StrokeEnd = progress;
            
            ProgressLayer.AddAnimation(anim, "animateCircle");
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        void ConfigureProgressViewToBeCircular() {
            this.DrawsView(TrackLayer, 10, 1);
            this.DrawsView(ProgressLayer, 10, 0);
        }
        void DrawsView(CAShapeLayer shape, nfloat startingPoint, nfloat endingPoint) {
            this.BackgroundColor = UIColor.Clear;
            this.Layer.CornerRadius = (nfloat)(this.Frame.Size.Width / 2);

            shape.Path = ViewCGPath;
            shape.FillColor = UIColor.Clear.CGColor;
            shape.StrokeColor = ProgressColor.CGColor;
            shape.LineWidth = startingPoint;
            shape.StrokeEnd = endingPoint;

            this.Layer.AddSublayer(shape);
        }
        #endregion

        #region Commands
        #endregion
    }

    /*
    class CircularProgressView: UIView {
    
    private var progressLayer = CAShapeLayer()
    private var tracklayer = CAShapeLayer()
    
    override init(frame: CGRect) {
        super.init(frame: frame)
        self.configureProgressViewToBeCircular()
    }
    
    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
        self.configureProgressViewToBeCircular()
    }
    
    var setProgressColor: UIColor = UIColor.red {
        didSet {
            progressLayer.strokeColor = setProgressColor.cgColor
        }
    }
    
    var setTrackColor: UIColor = UIColor.white {
        didSet {
            tracklayer.strokeColor = setTrackColor.cgColor
        }
    }
     //A path that consists of straight and curved line segments that you can render in your custom views.
     //Meaning our CAShapeLayer will now be drawn on the screen with the path we have specified here
     
    private var viewCGPath: CGPath? {
        return UIBezierPath(arcCenter: CGPoint(x: frame.size.width / 2.0, y: frame.size.height / 2.0),
                            radius: (frame.size.width - 1.5)/2,
                            startAngle: CGFloat(-0.5 * Double.pi),
                            endAngle: CGFloat(1.5 * Double.pi), clockwise: true).cgPath
}

private func configureProgressViewToBeCircular() {
    self.drawsView(using: tracklayer, startingPoint: 10.0, ending: 1.0)
        self.drawsView(using: progressLayer, startingPoint: 10.0, ending: 0.0)
    }
    
    private func drawsView(using shape: CAShapeLayer, startingPoint: CGFloat, ending: CGFloat) {
        self.backgroundColor = UIColor.clear
        self.layer.cornerRadius = self.frame.size.width/2.0
        
        shape.path = self.viewCGPath
        shape.fillColor = UIColor.clear.cgColor
        shape.strokeColor = setProgressColor.cgColor
        shape.lineWidth = startingPoint
        shape.strokeEnd = ending
        
        self.layer.addSublayer(shape)
    }
    
    func setProgressWithAnimation(duration: TimeInterval, value: Float) {
        let animation = CABasicAnimation(keyPath: "strokeEnd")
        animation.duration = duration


        animation.fromValue = 0 //start animation at point 0
        animation.toValue = value //end animation at point specified
        animation.timingFunction = CAMediaTimingFunction(name: CAMediaTimingFunctionName.linear)
        progressLayer.strokeEnd = CGFloat(value)
        progressLayer.add(animation, forKey: "animateCircle")
    }
}
    */
}