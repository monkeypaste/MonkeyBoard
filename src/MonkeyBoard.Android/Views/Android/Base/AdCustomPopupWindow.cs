using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using MonkeyBoard.Common;
using MonkeyPaste.Common;

namespace MonkeyBoard.Android {
    public class AdCustomPopupWindow : PopupWindow {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        static bool EnsureCreate(View anchorView) {
            if (anchorView.IsWindowDead()) {
                MpConsole.WriteLine($"Popup view REJECTED!");
                if (anchorView != null && anchorView.Context is AdInputMethodService ims) {
                    ims.ResetService();
                }
                return false;
            }
            return true;
        }
        public static AdCustomPopupWindow Create(View anchorView) {
            if(!EnsureCreate(anchorView)) {
                return null;
            }
            
            var puw = new AdCustomPopupWindow().SetDefaultProps();
            puw.SetAnchor(anchorView);
            return puw;
        }
        public static T Create<T>(View anchorView) where T : AdCustomPopupWindow, new() {
            if (!EnsureCreate(anchorView)) {
                return null;
            }
            var puw = new T().SetDefaultProps();
            puw.SetAnchor(anchorView);
            return puw;
        }
        #endregion

        #region Interfaces
        #endregion

        #region Properties
        public View AnchorView { get; private set; }
        public override Drawable Background => null;
        public object Tag { get; set; }

        #endregion

        #region Events
        #endregion

        #region Constructors
        public AdCustomPopupWindow() : base() { }
        public AdCustomPopupWindow(AdCustomViewGroup anchorView) : base() {
            AnchorView = anchorView;
        }
        public AdCustomPopupWindow(Context context) : base(context) { }
        #endregion

        #region Public Methods
        public void SetAnchor(View anchor) {
            AnchorView = anchor;
        }
        public void Show(PointF loc) {
            this.ShowAtLocation(AnchorView, GravityFlags.NoGravity, (int)loc.X, (int)loc.Y);
        }
        public PointF GetTranslatedPopupPosition(PointF screenPoint) {
            float anchor_h =
                this.AnchorView == null ?
                    0 : this.AnchorView.Height;
            var screen_rect = AdDeviceInfo.UnscaledWorkAreaRect.ToRect();
            var x = screenPoint.X;
            var y = screenPoint.Y;
            y -= screen_rect.Height();
            y += anchor_h;
            return new PointF(x, y);
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
