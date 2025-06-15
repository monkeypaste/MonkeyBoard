using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net.Wifi;
using Android.Text;
using Android.Views;
using Android.Widget;
using Avalonia.Controls;
using Java.Net;
using MonkeyBoard.Common;
using System;
using System.Linq;
using static Android.Views.View;
using GPaint = Android.Graphics.Paint;
using Point = Avalonia.Point;

public interface IOnPopupTouchListener  {
    bool OnTouch(View v, MotionEvent e);
}
namespace MonkeyBoard.Android {
    public class AdEmojiSearchPopupWindow : AdCustomPopupWindow, IOnTouchListener {
        #region Private Variables
        static IOnPopupTouchListener _touchListener;
        #endregion

        #region Constants
        #endregion

        #region Statics
        public static AdCustomViewGroup ContainerView =>
            _openPopupWindow == null ? null : _openPopupWindow.ContentView as AdCustomViewGroup;
        static AdEmojiSearchPopupWindow _openPopupWindow;
        public static void Show(View anchorView, GPaint sharedPaint,  EmojiSearchViewModel sbvm) {
            if(anchorView.Context is not { } context ||
                
                context is not AdInputMethodService ims ||
                ims.KeyboardView is not { } kbv ||
                ims.KeyboardContainerView is not { } kbcv) {
                return;
            }
            bool is_floating = sbvm.KeyboardViewModel.IsFloatingLayout;
            if (IsOpen || IsOpening) {
                return;
            }

            IsOpening = true;
            if(_touchListener == null) {
                _touchListener = ims;
            }
            ims.OnDismissed += Ims_OnDismissed;
            sharedPaint = AdKeyboardView.SetupPaint(context);

            var espv = new AdEmojiSearchPopupView(context,sharedPaint, sbvm).SetDefaultProps("Popup Search Window");

            var puw = Create<AdEmojiSearchPopupWindow>(anchorView).SetDefaultProps();
            _openPopupWindow = puw;
            puw.SetTouchInterceptor(puw);
            puw.ContentView = espv;
            puw.Width = (int)espv.Frame.Width();
            puw.Height = (int)espv.Frame.Height();

            AdCustomViewGroup anchor_frame_view = kbcv;// is_floating ? kbv : kbcv;

            double lastScaledHeight = sbvm.InnerContainerHeight;
            void OnHeightChanged(object sender, double scaledHeight) {
                lastScaledHeight = scaledHeight;

                float puw_h = scaledHeight.UnscaledF();
                float nh = kbv.Frame.Height() + puw_h;
                // adjust total height by ich so host app toolbar positions above popupwindow
                anchor_frame_view.Frame = anchor_frame_view.Frame.Resize(anchor_frame_view.Frame.Width(), nh);
                // translate keyboard down by ich (creates empty space above that's covered by popupwindow)
                kbv.ContainerOffsetY = is_floating ? 0 : puw_h;
                anchor_frame_view.RenderFrame(true);

                espv.RenderFrame(true);

                var pur = sbvm.TotalRect.ToRectF();

                float x = pur.Left;
                float y = pur.Top + kbv.ContainerOffsetY;
                if(is_floating) {
                    // HACK this translation is just guessing, its off slightly...
                    // maybe accumulated rounding issues but it ends up overlapping a tid bit on 
                    // text compl menu
                    var kbv_origin = sbvm.KeyboardViewModel.FloatContainerViewModel.FloatPosition.ToPointF();
                    x += kbv_origin.X;
                    y -= ((float)AdDeviceInfo.UnscaledSize.Height - kbv_origin.Y);
                    y += AdDeviceInfo.StatusBarHeight + AdDeviceInfo.NavBarHeight;
                }
                // since popup window is anchored (relative) to kbcv, puw must adjust popsition due its change in height 
                puw.Update((int)x,(int)y, puw.Width, puw.Height, true);
            }
            sbvm.OnSearchHeightChanged += OnHeightChanged;

            var pur = sbvm.TotalRect.ToRectF();
            
            // BUG in floating layout can't use kbv as anchor, gives bad token exception..
            puw.ShowAtLocation(kbcv, GravityFlags.NoGravity, (int)pur.Left, (int)pur.Top);
            OnHeightChanged(null, lastScaledHeight);

            void OnFloatLocationChanged(object s, EventArgs e) {
                OnHeightChanged(null,lastScaledHeight);
            }
            if (is_floating) {                
                sbvm.KeyboardViewModel.FloatContainerViewModel.OnFloatPositionChanged += OnFloatLocationChanged;
            }

            // set inner connection
            if (context is AdInputMethodService ic) {
                ic.SetInnerEditText(espv.SearchEditText);
                void OnDismissed(object sender, EventArgs e) {
                    OnHeightChanged(null, 0);
                    ims.OnDismissed -= Ims_OnDismissed;
                    sbvm.OnSearchHeightChanged -= OnHeightChanged;
                    sbvm.KeyboardViewModel.FloatContainerViewModel.OnFloatPositionChanged -= OnFloatLocationChanged;
                    _openPopupWindow = null;
                    puw.DismissEvent -= OnDismissed;
                    ic.ClearInnerEditText();
                }
                puw.DismissEvent += OnDismissed;
            }
            IsOpening = false;
        }
        public static void UpdatePosition(double scaledHeight) {

        }

        private static void Ims_OnDismissed(object sender, EventArgs e) {
            //Hide();
        }

        public static void Hide() {
            if(_openPopupWindow == null) {
                return;
            }
            _openPopupWindow.Dismiss();
            _openPopupWindow = null;
        }
        static bool IsOpening { get; set; }

        public static bool IsOpen =>
            _openPopupWindow != null && _openPopupWindow.IsShowing;


        public static Point TranslatePoint(Point popupRelativePoint) {
            if(!IsOpen) {
                return popupRelativePoint;
            }
            return new Point(popupRelativePoint.X, popupRelativePoint.Y - _openPopupWindow.Height);
        }

        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
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
        #endregion

        #region Public Methods
        public AdEmojiSearchPopupWindow() : base() {

        }

        public bool OnTouch(View v, MotionEvent e) {
            if (_touchListener is { } putl) {
                return _touchListener.OnTouch(v, e);
            }
            return false;
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}