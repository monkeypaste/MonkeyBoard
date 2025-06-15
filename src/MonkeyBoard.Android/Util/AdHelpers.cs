using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Java.Lang;
using MonkeyPaste.Common;
using MonkeyBoard.Common;
using MonkeyBoard.Bridge;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using AvRect = Avalonia.Rect;
using Bitmap = Android.Graphics.Bitmap;
using Color = Android.Graphics.Color;
using Exception = System.Exception;
using Fragment = AndroidX.Fragment.App.Fragment;
using GOrientation = Android.Widget.Orientation;
using Math = System.Math;
using Matrix = Android.Graphics.Matrix;
using MsPath = System.IO.Path;
using Path = Android.Graphics.Path;
using Point = Avalonia.Point;
using PointF = Android.Graphics.PointF;
using Rect = Android.Graphics.Rect;
using Size = Android.Util.Size;
using TextAlignment = Avalonia.Media.TextAlignment;
using Typeface = Android.Graphics.Typeface;

namespace MonkeyBoard.Android {
    public static class AdHelpers {
        static float Scaling =>
            (float)AdDeviceInfo.Scaling;

        #region Geometry

        public static RectF Flate(this RectF rect, float dl, float dt, float dr, float db) {
            float l = rect.Left + dl;
            float t = rect.Top + dt;

            float r = rect.Right + dr;
            float b = rect.Bottom + db;

            l = Math.Min(l, r);
            r = Math.Max(l, r);

            t = Math.Min(t, b);
            b = Math.Max(t, b);

            return new RectF(l, t, r - l, b - t);
        }
        public static RectF ToRectF(this AvRect av_rect) {
            return new RectF(
                (float)av_rect.Left * Scaling,
                (float)av_rect.Top * Scaling,
                (float)av_rect.Right * Scaling,
                (float)av_rect.Bottom * Scaling);
        }
        public static Rect ToRect(this AvRect av_rect) {
            return new Rect(
                (int)(av_rect.Left * Scaling),
                (int)(av_rect.Top * Scaling),
                (int)(av_rect.Right * Scaling),
                (int)(av_rect.Bottom * Scaling));
        }
        public static AvRect ToAvRect(this Rect rect) {
            double w = (double)rect.Width();
            double h = (double)rect.Height();
            return new AvRect(
                (double)rect.Left / (double)Scaling,
                (double)rect.Top / (double)Scaling,
                w / (double)Scaling,
                h / (double)Scaling);
        }
        public static AvRect ToAvRect(this RectF rect) {
            double w = (double)rect.Width();
            double h = (double)rect.Height();
            return new AvRect(
                (double)rect.Left / (double)Scaling,
                (double)rect.Top / (double)Scaling,
                w / (double)Scaling,
                h / (double)Scaling);
        }
        public static Rect ToRect(this RectF rectf) {
            return new Rect((int)rectf.Left, (int)rectf.Top, (int)rectf.Right, (int)rectf.Bottom);
        }
        public static PointF Position(this RectF rectf) {
            return new PointF(rectf.Left, rectf.Top);
        }

        public static RectF ToRectF(this Rect rectf) {
            return new RectF((float)rectf.Left, (float)rectf.Top, (float)rectf.Right, (float)rectf.Bottom);
        }
        public static RectF Inflate(this RectF rect, float dw, float dh) {
            float w = rect.Width() + dw;
            float h = rect.Height() + dh;
            float l = rect.Left;
            float t = rect.Top;
            float r = l + w;
            float b = t + h;
            return new RectF(l, t, r, b);
        }
        public static Rect Inflate(this Rect rect, int dw, int dh) {
            int w = rect.Width() + dw;
            int h = rect.Height() + dh;
            int l = rect.Left;
            int t = rect.Top;
            int r = l + w;
            int b = t + h;
            return new Rect(l, t, r, b);
        }
        public static RectF Resize(this RectF rect, float w, float h) {
            float l = rect.Left;
            float t = rect.Top;
            float r = l + w;
            float b = t + h;
            return new RectF(l, t, r, b);
        }
        public static RectF Place(this RectF rect, float ox, float oy) {
            float w = rect.Width();
            float h = rect.Height();
            float l = ox;
            float t = oy;
            float r = l + w;
            float b = t + h;
            return new RectF(l, t, r, b);
        }
        public static Rect Place(this Rect rect, int ox, int oy) {
            int w = rect.Width();
            int h = rect.Height();
            int l = ox;
            int t = oy;
            int r = l + w;
            int b = t + h;
            return new Rect(l, t, r, b);
        }
        public static RectF Move(this RectF rect, float dx, float dy) {
            float w = rect.Width();
            float h = rect.Height();
            float l = rect.Left + dx;
            float t = rect.Top + dy;
            float r = l + w;
            float b = t + h;
            return new RectF(l, t, r, b);
        }
        public static RectF ToBounds(this RectF rect) {
            return rect.Place(0, 0);
        }
        public static RectF ToBounds(this RectF rect, RectF outer_rect) {
            float w = rect.Width();
            float h = rect.Height();
            float l = rect.Left - outer_rect.Left;
            float t = rect.Top - outer_rect.Top;
            float r = l + w;
            float b = t + h;
            return new RectF(l, t, r, b);
        }
        public static Rect ToBounds(this Rect rect) {
            return rect.Place(0, 0);
        }
        public static Size GetSize(this Rect rect) {
            return new Size(rect.Width(), rect.Height());
        }

        public static float UnscaledF(this double d) {
            return (float)(d * Scaling);
        }
        public static int UnscaledI(this double d) {
            return (int)(d * Scaling);
        }

        public static double ScaledD(this float f) {
            return (double)((double)f / (double)Scaling);
        }
        public static double ScaledD(this int i) {
            return (double)((double)i / (double)Scaling);
        }
        public static int ScaledI(this int i) {
            return (int)(i / Scaling);
        }

        public static PointF ToPointF(this Point p) {
            return new PointF((float)p.X * Scaling, (float)p.Y * Scaling);
        }
        public static PointF Move(this PointF p, float dx, float dy) {
            return new PointF(p.X + dx, p.Y + dy);
        }
        #endregion

        #region Motion Event
        public static IEnumerable<(int id, (Point loc, Point raw_loc) p, TouchEventType eventType)> GetMotions(this MotionEvent e, Dictionary<int, (Point loc, Point raw_loc)> touches) {
            //MpConsole.WriteLine("");
            // MpConsole.WriteLine($"{e.ActionMasked}");
            List<(int, (Point, Point), TouchEventType)> changed_ids = [];
            for(int i = 0; i < e.PointerCount; i++) {
                int tid = e.GetPointerId(i);
                var tid_loc = new Point(e.GetX(i), e.GetY(i));
                var tid_raw_loc = new Point(e.GetRawX(i), e.GetRawY(i));
                var tid_loc_tup = (tid_loc, tid_raw_loc);
                if(e.ActionMasked == MotionEventActions.Move) {
                    if(touches[tid].raw_loc == tid_raw_loc) {
                        continue;
                    }

                    touches[tid] = tid_loc_tup;
                    changed_ids.Add((tid, tid_loc_tup, TouchEventType.Move));
                } else {
                    if(i == e.ActionIndex) {
                        if(e.ActionMasked.IsDown()) {
                            // new touch
                            touches.AddOrReplace(tid, tid_loc_tup);
                            changed_ids.Add((tid, tid_loc_tup, TouchEventType.Press));
                        } else if(e.ActionMasked.IsUp()) {
                            // old touch
                            changed_ids.Add((tid, tid_loc_tup, TouchEventType.Release));
                            touches.Remove(tid);
                        }
                    }
                }
            }
            //foreach(var ch in changed_ids) {
            //    MpConsole.WriteLine($"Type: {ch.Item3} Id: {ch.Item1} Loc: {ch.Item2}");
            //}
            return changed_ids;
        }

        public static bool IsDown(this MotionEventActions met) {
            return
                met == MotionEventActions.Down ||
                met == MotionEventActions.PointerDown ||
                met == MotionEventActions.Pointer1Down ||
                met == MotionEventActions.Pointer2Down ||
                met == MotionEventActions.Pointer3Down ||
                met == MotionEventActions.ButtonPress;
        }
        public static bool IsMove(this MotionEventActions met) {
            return
                met == MotionEventActions.Move;
        }
        public static bool IsUp(this MotionEventActions met) {
            return
                met == MotionEventActions.Up ||
                met == MotionEventActions.PointerUp ||
                met == MotionEventActions.Pointer1Up ||
                met == MotionEventActions.Pointer2Up ||
                met == MotionEventActions.Pointer3Up ||
                met == MotionEventActions.ButtonRelease;
        }
        #endregion

        #region Images
        public static Bitmap Compress(this Bitmap bmp, int quality) {
            MemoryStream stream = new();
            if(AdDeviceInfo.OsVersion >= 30) {
                bmp.Compress(Bitmap.CompressFormat.WebpLossy, quality, stream);
            } else {
                bmp.Compress(Bitmap.CompressFormat.Webp, quality, stream);
            }

            var bytes = stream.ToArray();

            Bitmap result = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
            return result;
        }
        public static Bitmap Scale(this Bitmap bmp, int newWidth, int newHeight, bool recycle = false) {
            //from https://stackoverflow.com/a/10703256/105028
            int w = Math.Max(1, bmp.Width);
            int h = Math.Max(1, bmp.Height);
            float scaled_w = ((float)Math.Max(1, newWidth)) / w;
            float scaled_h = ((float)Math.Max(1, newHeight)) / h;
            Matrix matrix = new Matrix();
            matrix.PostScale(scaled_w, scaled_h);
            var scaled_bmp = Bitmap.CreateBitmap(bmp, 0, 0, w, h, matrix, false);
            if(recycle) {
                bmp.Recycle();
            }
            return scaled_bmp;
        }
        public static Drawable LoadDrawableBmp(Context context, string img_name) {
            if(LoadBitmap(img_name) is not { } bmp) {
                return default;
            }
            var bmp_drawable = new BitmapDrawable(context.Resources, bmp);
            return bmp_drawable;
        }

        public static Bitmap LoadBitmap(string imgFileName) {
            //if (context is not AdInputMethodService ims ||
            //    ims.AssetLoader is not { } al ||
            //    $"Images/{imgFileName}.png" is not { } img_uri ||
            //    al.LoadStream(img_uri) is not { } asset_stream ||
            //    BitmapFactory.DecodeStream(asset_stream) is not { } bmp) {
            //    return null;
            //}
            string img_path = System.IO.Path.Combine(
                KbStorageHelpers.ImgRootDir,
                imgFileName);
            if(MpFileIo.ReadBytesFromFile(img_path) is { } bytes &&
                LoadBitmap(bytes) is { } bmp) {
                return bmp;
            }

            return null;
        }

        public static Bitmap LoadBitmap(byte[] bytes) {
            if(BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, new BitmapFactory.Options()) is { } bmp) {
                return bmp;
            }

            return null;
        }
        public static Bitmap ToBitmap(this Drawable d) {
            // from https://stackoverflow.com/a/10600736/105028
            if(d is BitmapDrawable bd && bd.Bitmap is { } bdbmp) {
                return bdbmp;
            }

            Bitmap bmp = null;
            if(d.IntrinsicWidth <= 0 || d.IntrinsicHeight <= 0) {
                bmp = Bitmap.CreateBitmap(1, 1, Bitmap.Config.Argb8888);
            } else {
                bmp = Bitmap.CreateBitmap(d.IntrinsicWidth, d.IntrinsicHeight, Bitmap.Config.Argb8888);
            }
            Canvas canvas = new Canvas(bmp);
            d.SetBounds(0, 0, canvas.Width, canvas.Height);
            d.Draw(canvas);
            return bmp;
        }

        public static Bitmap LoadRescaleOrIgnore(this Bitmap bmp, string imgFileName, RectF bmpRect, bool recycle = false) {
            if(!bmpRect.IsEmpty &&
                (bmp == null ||
                 bmp.Width != bmpRect.Width() ||
                 bmp.Height != bmpRect.Height())
                 && LoadBitmap(imgFileName) is { } unscaledBmp) {
                // first load or size change
                return unscaledBmp.Scale((int)bmpRect.Width(), (int)bmpRect.Height(), recycle);
            }
            return bmp;
        }

        #endregion

        #region Views

        public static void UpdateLayout(this View v, RectF rect) {
            //if(v.Context is AdInputMethodService ims && 
            //    ims.KeyboardView is { } kbv && 
            //    kbv.DC is { } kbvm && 
            //    kbvm.FloatContainerViewModel.IsScaling) {
            //    return;
            //}
            v.Layout((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
        }

        #region Space Measure Fix
        static Rect? SpaceBounds { get; set; }
        static Rect FindSpaceWidth(Paint SharedPaint) {

            string pad_text = "h";
            string full_text = $"{pad_text} {pad_text}";

            Rect full_bounds = new Rect();
            SharedPaint.GetTextBounds(full_text, 0, full_text.Length, full_bounds);

            Rect pad_bounds = new Rect();
            SharedPaint.GetTextBounds(pad_text, 0, pad_text.Length, pad_bounds);

            float pad_width = pad_bounds.Width() * 2;
            float space_width = full_bounds.Width() - pad_width;

            return new Rect(0, 0, (int)space_width, pad_bounds.Height());
        }
        static void GetEdgeSpaces(this string measure_text, out int leadingSpaceCount, out int trailingSpaceCount) {
            var measure_chars = measure_text.ToCharArray();

            int last_idx = measure_text.Length - 1;
            trailingSpaceCount = 0;
            while(true) {
                if(last_idx < 0 || measure_chars[last_idx] != ' ') {
                    break;
                }
                last_idx--;
                trailingSpaceCount++;
            }

            int first_idx = 0;
            leadingSpaceCount = 0;
            while(true) {
                if(first_idx >= measure_chars.Length || first_idx >= last_idx || measure_chars[first_idx] != ' ') {
                    break;
                }
                first_idx++;
                leadingSpaceCount++;
            }
        }
        public static void GetTextWithSpacesBounds(this Paint SharedPaint, string measure_text, int sidx, int len, Rect? bounds) {
            if(SpaceBounds is null) {
                SpaceBounds = FindSpaceWidth(SharedPaint);
            }
            if(measure_text == " ") {
                bounds.Set(SpaceBounds);
                return;
            }
            SharedPaint.GetTextBounds(measure_text, 0, measure_text.Length, bounds);
            measure_text.GetEdgeSpaces(out int lead_spaces, out int trail_spaces);

            if(lead_spaces + trail_spaces <= 0) {
                return;
            }
            float lead_space_width = SpaceBounds.Width() * lead_spaces;
            float trail_space_width = SpaceBounds.Width() * trail_spaces;

            float l = bounds.Left - lead_space_width;
            float t = bounds.Top;
            float r = bounds.Right + trail_space_width;
            float b = bounds.Bottom;

            bounds.Set(new RectF(l, t, r, b).ToRect());
        }
        #endregion

        public static bool IsWindowDead(this View anchorView) {
            try {
                return anchorView == null ||
                    anchorView.Context is not { } context ||
                    context.ApplicationContext is not { } ac ||
                    anchorView.RootView is not { } rv ||
                    rv.WindowToken is not { } wt ||
                    !wt.IsBinderAlive;
            }
            catch(Exception ex) {
                ex.Dump();
            }
            return true;
        }


        public static T CreatePopupWindow2<T>(this Context context, T view, int w, int h, int x, int y) where T : View {
            /*
            mParams = new WindowManager.LayoutParams(
            WindowManager.LayoutParams.MATCH_PARENT, 150, 10, 10,
            WindowManager.LayoutParams.TYPE_SYSTEM_OVERLAY,
            WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE |
            WindowManager.LayoutParams.FLAG_NOT_TOUCH_MODAL,
            PixelFormat.TRANSLUCENT);

    mParams.gravity = Gravity.CENTER;
    mParams.setTitle("Window test");

    mWindowManager = (WindowManager)getSystemService(WINDOW_SERVICE);
    mWindowManager.addView(mView, mParams);
            */
            var layout_params = new WindowManagerLayoutParams(
                w, h, x, y,
                WindowManagerTypes.SystemOverlay,
                WindowManagerFlags.NotFocusable | WindowManagerFlags.NotTouchable | WindowManagerFlags.NotTouchModal,
                Format.Translucent);
            layout_params.Gravity = GravityFlags.NoGravity;

            var wm = (IWindowManager)context.GetSystemService(Context.WindowService);
            wm.AddView(view, layout_params);

            return default;
        }

        public static Path ToPath(this CornerRadius cr, RectF rect) {
            var cra = new double[]{
                        cr.TopLeft, cr.TopLeft,        // Top, left in px
                        cr.TopRight, cr.TopRight,        // Top, right in px
                        cr.BottomRight, cr.BottomRight,          // Bottom, right in px
                        cr.BottomLeft, cr.BottomLeft           // Bottom,left in px
                    }.Select(x => x.UnscaledF()).ToArray();

            var path = new Path();
            path.AddRoundRect(rect, cra, Path.Direction.Cw);
            return path;
        }

        static event EventHandler<DialogClickEventArgs> alertHandler;
        public static void Alert(Context context, string title = "", string msg = "") {

            void OnAlertClick(object sender, DialogClickEventArgs e) {
                alertHandler -= OnAlertClick;
                if(sender is AlertDialog ad) {
                    ad.Dismiss();
                }
            }
            alertHandler += OnAlertClick;
            new AlertDialog.Builder(context)
                .SetTitle(title)
                .SetMessage(msg)
                .SetPositiveButton(ResourceStrings.U["OkButtonText"].value, alertHandler)
                .Show();
        }


        public static async Task<bool> AlertYesNoAsync(Context context, string title = "", string msg = "") {
            bool? result = null;

            void OnAlertClick(object sender, DialogClickEventArgs e) {
                alertHandler -= OnAlertClick;

                if(sender is AlertDialog ad) {
                    ad.Dismiss();
                }
                result = e.Which == (int)DialogButtonType.Positive;
            }
            alertHandler += OnAlertClick;
            new AlertDialog.Builder(context)
                .SetTitle(title)
                .SetMessage(msg)
                .SetPositiveButton(ResourceStrings.U["OkButtonText"].value, alertHandler)
                .SetNegativeButton(ResourceStrings.U["CancelButtonText"].value, alertHandler)
                .Show();

            while(result == null) {
                await Task.Delay(100);
            }
            return result.Value;
        }
        public static async Task AlertProgressAsync(Context context, string title, Func<int> progressStatus) {
            // from https://stackoverflow.com/a/49272722/105028
            MpConsole.WriteLine($"Alert shown");
            int llPadding = 30;
            LinearLayout ll = new LinearLayout(context);
            ll.Orientation = GOrientation.Horizontal;
            ll.SetPadding(llPadding, llPadding, llPadding, llPadding);
            ll.SetGravity(GravityFlags.Center);
            LinearLayout.LayoutParams llParam = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WrapContent,
                    LinearLayout.LayoutParams.WrapContent);
            llParam.Gravity = GravityFlags.Center;
            ll.LayoutParameters = llParam;

            ProgressBar progressBar = new ProgressBar(context);
            progressBar.Indeterminate = true;
            progressBar.Progress = 0;
            progressBar.SecondaryProgress = 100;
            progressBar.Max = 100;
            progressBar.SetPadding(0, 0, llPadding, 0);
            progressBar.LayoutParameters = llParam;

            llParam = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent);
            llParam.Gravity = GravityFlags.Center;
            TextView tvText = new TextView(context);
            tvText.SetText(title.ToCharArray(), 0, title.Length);
            tvText.SetTextColor(Color.ParseColor("#000000"));
            tvText.SetTextSize(ComplexUnitType.Px, 20d.UnscaledF());
            tvText.LayoutParameters = llParam;

            ll.AddView(progressBar);
            ll.AddView(tvText);

            AlertDialog.Builder builder = new AlertDialog.Builder(context);
            builder.SetView(ll);

            var dialog = builder.Create();
            dialog.Show();
            //if (dialog.Window is { } window) {
            //    WindowManagerLayoutParams layoutParams = new WindowManagerLayoutParams();                
            //    layoutParams.CopyFrom(window.Attributes);
            //    layoutParams.Width = LinearLayout.LayoutParams.WrapContent;
            //    layoutParams.Height = LinearLayout.LayoutParams.WrapContent;

            //    window.Attributes = layoutParams;
            //}

            while(true) {
                int percent = progressStatus.Invoke();
                if(percent >= 0) {
                    progressBar.Indeterminate = false;
                    progressBar.SetProgress(percent, true);
                }
                if(percent >= 100) {
                    dialog.Window.CloseAllPanels();
                    dialog.Dismiss();
                    return;
                }
                await Task.Delay(50);
            }

        }


        public static ViewStates ToViewState(this bool isVisible) {
            return isVisible ? ViewStates.Visible : ViewStates.Invisible;
        }
        public static bool IsVisible(this View view) {
            return view.Visibility == ViewStates.Visible;
        }

        public static Size TextSize(this TextView tv) {
            // from https://stackoverflow.com/a/24359594/105028
            Rect bounds = new Rect();
            Paint textPaint = tv.Paint;
            textPaint.GetTextBounds(tv.Text, 0, tv.Text.Length, bounds);
            return new Size(bounds.Width(), bounds.Height());
        }

        public static void Redraw(this View v, bool needsLayout = false) {
            if(needsLayout) {
                v.RequestLayout();
            }
            v.Invalidate();
        }

        public static void RedrawAll(this ViewGroup vg, bool needsLayout = false) {
            if(vg.Descendants(true) is not { } vl) {
                return;
            }
            vl.ForEach(x => x.Redraw(needsLayout));
        }
        public static T SetDefaultProps<T>(this T uiv, string name = default) where T : View {
            uiv.Background = null;
            uiv.Focusable = false;
            uiv.ClipToOutline = true;

            if(uiv is AdCustomView cv && !string.IsNullOrEmpty(name)) {
                cv.Name = name;
            }
            if(uiv is ViewGroup vg) {
                vg.SetClipChildren(true);
            }
            uiv.SetPadding(0, 0, 0, 0);
            uiv.SetPaddingRelative(0, 0, 0, 0);
            return uiv;
        }

        public static T SetDefaultProps<T>(this T puw) where T : PopupWindow {
            puw.Focusable = false;
            puw.ClippingEnabled = false;
            return puw;
        }


        public static void RoundBgCorners(this View v, float radius, Color bgColor) {
            var shape = new GradientDrawable();
            shape.SetCornerRadius(radius);
            shape.SetColor(bgColor);
            v.Background = shape;
        }
        public static IEnumerable<View> SelfAndAllAncestors(this View v) =>
            v.Ancestors(true);
        public static IEnumerable<View> SelfAndAllDescendants(this View v) =>
            v.Descendants(true);

        public static IEnumerable<View> Ancestors(this View v, bool includeSelf = false) {
            if(includeSelf) {
                yield return v;
            }
            ViewGroup parent = v.Parent as ViewGroup;
            while(parent != null) {
                yield return parent;
                parent = parent.Parent as ViewGroup;
            }
        }

        public static IEnumerable<View> Children(this ViewGroup vg) {
            for(int i = 0; i < vg.ChildCount; i++) {
                if(vg.GetChildAt(i) is not View v) {
                    continue;
                }
                yield return v;
            }
        }
        public static IEnumerable<View> Descendants(this View rv, bool includeSelf = false) {
            if(includeSelf) {
                yield return rv;
            }
            if(rv is not ViewGroup vg) {
                yield break;
            }
            for(int i = 0; i < vg.ChildCount; i++) {
                if(vg.GetChildAt(i) is not View v) {
                    continue;
                }
                yield return v;
                if(v is not ViewGroup cvg) {
                    continue;
                }
                var cl = cvg.Descendants(false);
                foreach(var c in cl) {
                    yield return c;
                }
            }
        }
        #endregion

        #region Context
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static string GetAppPath(Context ctx, string packageName) {
            return MsPath.Combine(MsPath.GetDirectoryName(ctx.DataDir.AbsolutePath), packageName);
        }
        public static string GetAppName(Context ctx, string packageName) {
            try {
                var pm = ctx.ApplicationContext.PackageManager;
                if(pm.GetApplicationInfo(packageName, 0) is { } appInfo) {

                    return pm.GetApplicationLabel(appInfo);
                }
            }
            catch(Exception ex) {
                ex.Dump();
            }
            return packageName;
        }
        public static string GetAppIconBase64(Context ctx, string appPath) {
            try {
                string package_name = MsPath.GetFileName(appPath);
                using(Drawable d =
                    ctx.ApplicationContext.PackageManager.GetApplicationIcon(package_name)) {
                    Bitmap bmp = null;
                    if(d is BitmapDrawable bmp_d) {
                        bmp = bmp_d.Bitmap;
                    } else {
                        // from https://stackoverflow.com/a/29091591/105028
                        int width = !d.Bounds.IsEmpty ? d.Bounds.Width() : d.IntrinsicWidth;

                        int height = !d.Bounds.IsEmpty ? d.Bounds.Height() : d.IntrinsicHeight;

                        // Now we check we are > 0
                        bmp = Bitmap.CreateBitmap(
                            width <= 0 ? 1 : width,
                            height <= 0 ? 1 : height,
                            Bitmap.Config.Argb8888);

                        Canvas canvas = new Canvas(bmp);
                        d.SetBounds(0, 0, canvas.Width, canvas.Height);
                        d.Draw(canvas);
                    }
                    if(bmp == null) {
                        return null;
                    }
                    using(var ms = new MemoryStream()) {
                        bmp.Compress(Bitmap.CompressFormat.Png, 100, ms);
                        byte[] bytes = ms.ToArray();
                        string result = bytes.ToBase64String();
                        return result;
                    }

                }
            }
            catch(Exception ex) {
                MpConsole.WriteTraceLine($"Error find icon for path '{appPath}'", ex);
                return null;
            }
        }
        #endregion

        #region Fragments
        public static Fragment GetRootFragment(this Fragment f) {
            var cur_f = f;
            while(true) {
                if(cur_f.ParentFragment is not { } pf) {
                    return cur_f;
                }
                cur_f = pf;
            }
        }
        #endregion

        #region Color
        public static void SetTint(this Paint paint, Color? color) {
            if(color is { } c) {
                paint.Color = c;
                paint.SetColorFilter(new PorterDuffColorFilter(c, PorterDuff.Mode.SrcIn));
            } else {
                paint.SetColorFilter(null);
            }
        }
        public static int ToInt(this Color c) {
            return (int)c;
        }
        public static string ToHex(this int c) {
            string hex = $"#{c.ToString("x8", CultureInfo.InvariantCulture).ToUpper()}";
            return hex;
        }
        public static Color ToColor(this int c) {
            return ToHex(c).ToAdColor();
        }
        public static Color ToAdColor(this string hex) {
            System.Drawing.Color color = ColorTranslator.FromHtml(hex);
            //return Color.FromRGBA(color.A, color.R, color.G, color.B);
            return Color.Argb(color.A, color.R, color.G, color.B);
        }
        public static Drawable ToAdDrawableColor(this string hex) {
            return new ColorDrawable(hex.ToAdColor());
        }
        public static Color ToAdColor(this SolidColorBrush scb) {
            return new(scb.Color.R, scb.Color.G, scb.Color.B, scb.Color.A);
        }
        #endregion

        #region Text

        public static void DrawAlignedText(
            this Canvas canvas,
            Paint paint,
            RectF rect,
            string text,
            float fontSize,
            Color fontColor,
            HorizontalAlignment horizontalAlignment,
            VerticalAlignment verticalAlignment,
            PointF offset = default,
            bool italics = false) {
            offset = offset ?? new();

            var last_typeface = paint.Typeface;
            if(italics) {
                paint.SetTypeface(Typeface.Create(last_typeface, TypefaceStyle.Italic));
            }
            paint.Color = fontColor;
            paint.TextAlign = Paint.Align.Left;
            paint.TextSize = fontSize;

            var text_bounds = new Rect();
            paint.GetTextBounds(text, 0, text.Length, text_bounds);

            float ox = offset.X - text_bounds.Left + rect.Left;
            float oy = offset.Y - text_bounds.Top + rect.Top;
            float tw = text_bounds.Width();
            float th = text_bounds.Height();
            var fm = paint.GetFontMetrics();
            float ascent = fm.Ascent;
            float desscent = fm.Descent;

            switch(horizontalAlignment) {
                case HorizontalAlignment.Left:
                    break;
                case HorizontalAlignment.Center:
                    ox += (rect.Width() / 2) - (text_bounds.Width() / 2);
                    break;
                case HorizontalAlignment.Right:
                    ox += rect.Width() - text_bounds.Width();
                    break;
            }
            switch(verticalAlignment) {
                case VerticalAlignment.Top:
                    break;
                case VerticalAlignment.Center:
                    fm.Ascent = 0;
                    fm.Descent = 0;
                    var test = paint.GetFontMetrics();
                    var test2 = test.Ascent;
                    //- (mPaint.descent() + mPaint.ascent()) / 2
                    oy += (rect.Height() / 2) - (text_bounds.Height() / 2) + (text_bounds.Bottom / 2);
                    break;
                case VerticalAlignment.Bottom:
                    oy += rect.Height() + text_bounds.Top; //text_bounds.Bottom;//((ascent + desscent)/2);
                    break;
            }

            //if (text == "y" || text == "t" || text == "g" || text == "h") {
            //    // y and g are up too high, ascent=-57 descent 
            //    // 't' th=41 top=-40 bottom=1
            //    // 'y' th=43 top=-31 bottom=12
            //}

            canvas.DrawText(text, ox, oy, paint);
            paint.SetTypeface(last_typeface);
        }
        public static void DrawEmoji(Canvas canvas, Paint paint, string text, float x, float y, float fontSize) {
            paint.Color = Color.White;
            paint.TextSize = fontSize;
            paint.TextAlign = Paint.Align.Center;
            canvas.DrawText(text, x, y, paint);
        }
        public static ICharSequence ToCharSequence(this string text) {
            return text == null ? null : new Java.Lang.String(text);
        }

        public static bool CanShowEmoji(string emojiText, Paint paint) {
            if(string.IsNullOrWhiteSpace(emojiText)) {
                return false;
            }
            // from https://stackoverflow.com/a/41941569/105028
            try {
                paint = paint ?? new Paint();
                return paint.HasGlyph(emojiText);
            }
            catch(NoSuchMethodError) {
                // Compare display width of single-codepoint emoji to width of flag emoji to determine
                // whether flag is rendered as single glyph or two adjacent regional indicator symbols.
                float flagWidth = paint.MeasureText(emojiText);
                float standardWidth = paint.MeasureText("\uD83D\uDC27"); //  U+1F427 Penguin
                return flagWidth < standardWidth * 1.25;
                // This assumes that a valid glyph for the flag emoji must be less than 1.25 times
                // the width of the penguin.
            }
        }
        public static Paint.Align ToAdAlign(this TextAlignment ta) {
            switch(ta) {
                case TextAlignment.Left:
                    return Paint.Align.Left;
                case TextAlignment.Right:
                    return Paint.Align.Right;
                case TextAlignment.Center:
                    return Paint.Align.Center;
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion

        #region Type
        public static Class ToJavaClass(this Type type) {
            return Java.Lang.Class.FromType(type);
        }
        public static string ToJavaClassName(this Type type) {
            return type.ToJavaClass().Name;
        }
        #endregion
    }

}