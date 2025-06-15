
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using MonkeyPaste.Common;
using System;
using System.Runtime.CompilerServices;
using Rect = Avalonia.Rect;
using Size = Avalonia.Size;

namespace MonkeyBoard.Android {
    public static class AdDeviceInfo {
        static Context Context { get; set; }
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
        public static RectF UnscaledPortraitWorkAreaRect =>
            new RectF(0, StatusBarHeight, (float)UnscaledPortraitSize.Width, (float)UnscaledPortraitSize.Height - NavBarHeight);
        public static RectF UnscaledLandscapeWorkAreaRect =>
            new RectF(0, StatusBarHeight, (float)UnscaledLandscapeSize.Width - NavBarHeight, (float)UnscaledLandscapeSize.Height);
        public static RectF UnscaledWorkAreaRect =>
            IsPortrait ? UnscaledPortraitWorkAreaRect : UnscaledLandscapeWorkAreaRect;

        public static int NavBarHeight { get; private set; }
        public static int StatusBarHeight { get; private set; }
        public static double Scaling { get; private set; }
        public static bool IsPortrait =>
            Context == null ? true : Context.Resources.DisplayMetrics.WidthPixels < Context.Resources.DisplayMetrics.HeightPixels;
        public static int OsVersion { get; private set; }

        public static void Init(Context context) {
            Context = context;

            Scaling = context.Resources.DisplayMetrics.Density;
            OsVersion = (int)Build.VERSION.SdkInt;
            NavBarHeight = GetNavBarHeight(context);
            StatusBarHeight = GetStatusBarHeight(context);

            double a = context.Resources.DisplayMetrics.WidthPixels;
            double b = context.Resources.DisplayMetrics.HeightPixels;
            double w = Math.Min(a, b);
            double h = Math.Max(a, b);
            UnscaledPortraitSize = new Size(w, h + NavBarHeight);
            UnscaledLandscapeSize = new Size(h + NavBarHeight, w);

            ScaledPortraitSize = new Size(w / Scaling, h / Scaling);
            ScaledLandscapeSize = new Size(ScaledPortraitSize.Height, ScaledPortraitSize.Width);

            MpConsole.WriteLine($"UNSCALED : {UnscaledSize} SCALED: {ScaledSize} PORTRAIT: {IsPortrait}");
        }

        static int GetNavBarHeight(Context context) {
            int res_id = context.Resources.GetIdentifier("navigation_bar_height", "dimen", "android");
            if(res_id > 0) {
                return context.Resources.GetDimensionPixelSize(res_id);
            }
            return 0;
        }
        static int GetStatusBarHeight(Context context) {
            int res_id = context.Resources.GetIdentifier("status_bar_height", "dimen", "android");
            if(res_id > 0) {
                return context.Resources.GetDimensionPixelSize(res_id);
            }
            return 0;
        }
    }
}
