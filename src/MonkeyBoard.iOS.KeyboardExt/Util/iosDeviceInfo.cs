using Avalonia;
using CoreGraphics;
using MonkeyPaste.Common;
using System;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public static class iosDeviceInfo {
        public static Size UnscaledPortraitSize { get; private set; }
        public static Size UnscaledLandscapeSize { get; private set; }
        public static Size UnscaledSize =>
            IsPortrait ?
                UnscaledPortraitSize : UnscaledLandscapeSize;
        public static Size ScaledPortraitSize { get; private set; }
        public static Size ScaledLandscapeSize { get; private set; }
        public static Size ScaledSize =>
            IsPortrait ?
                ScaledPortraitSize : ScaledLandscapeSize;
        public static double Scaling =>
            UIScreen.MainScreen.Scale;

        static bool? _isPortrait;
        public static bool IsPortrait {
            get {
                if (_isPortrait.HasValue) {
                    return _isPortrait.Value;
                }
                return UIScreen.MainScreen.Bounds.Width < UIScreen.MainScreen.Bounds.Height;
            }
        }

        static Version _sdkVersion;
        public static Version SdkVersion {
            get {
                if (_sdkVersion == null) {
                    _sdkVersion = new Version(UIDevice.CurrentDevice.SystemVersion);
                }
                return _sdkVersion;
            }
        }
        static iosDeviceInfo() {
            double scaling = (double)UIScreen.MainScreen.Scale;
            double a = (double)UIScreen.MainScreen.Bounds.Width;
            double b = (double)UIScreen.MainScreen.Bounds.Height;
            double w = Math.Min(a, b);
            double h = Math.Max(a, b);
            UnscaledPortraitSize = new Size(w, h);
            UnscaledLandscapeSize = new Size(h, w);

            ScaledPortraitSize = new Size(w / scaling, h / scaling);
            ScaledLandscapeSize = new Size(ScaledPortraitSize.Height, ScaledPortraitSize.Width);
        }

        public static bool UpdateOrientation(CGSize newSize) {
            // returns true if changed
            bool was_portrait = IsPortrait;
            _isPortrait = newSize.Width < newSize.Height;
            return was_portrait != IsPortrait;
        }

        public static void LogInfo() {
            MpConsole.WriteLine($"Version: {SdkVersion} Portrait: {IsPortrait} Scaling: {Scaling} Scaled Size: {ScaledSize} Unscaled Size: {UnscaledSize}");
        }
    }
}
