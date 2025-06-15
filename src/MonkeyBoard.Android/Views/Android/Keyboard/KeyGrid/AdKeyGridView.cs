using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace MonkeyBoard.Android {
    public class AdKeyGridView : AdCustomViewGroup {
        #region Interfaces

        #region IKeyboardViewRenderer

        public override void MeasureFrame(bool invalidate) {
            Frame = DC.KeyGridRect.ToRectF();
            base.MeasureFrame(invalidate);
        }
        #endregion

        #endregion

        #region Properties

        #region View Models
        public new KeyboardViewModel DC { get; set; }
        #endregion

        #region Views
        Bitmap ShadowBmp { get; set; }
        List<AdCustomPopupWindow> PopupWindows { get; set; } = [];

        public IEnumerable<AdKeyView> KeyViews {
            get {
                for (int i = 0; i < ChildCount; i++) {
                    if (GetChildAt(i) is not AdKeyView kv) {
                        continue;
                    }
                    yield return kv;
                }
            }
        }
        #endregion

        #region State
        #endregion

        #endregion

        #region Constructors
        public AdKeyGridView(Context context, Paint paint, KeyboardViewModel dC) : base(context,paint) {
            DC = dC;
            DC.OnShowPopup += DC_OnShowPopup;
            DC.OnHidePopup += DC_OnHidePopup;
        }

        #endregion

        #region Public Methods
        public void ResetRenderer() {
            this.RemoveAllViews();
            PopupWindows.ForEach(x => x.Dismiss());
            PopupWindows.Clear();
            this.MeasureFrame(false);
            DC.Keys.Where(x => !x.IsPopupKey).ForEach(x => this.AddView(CreateKeyView(x)));
        }
        public void Unload() {
            this.RemoveAllViews();
            PopupWindows.ForEach(x => x.Dismiss());
            PopupWindows.Clear();

            if(DC != null) {
                DC.OnShowPopup -= DC_OnShowPopup;
                DC.OnHidePopup -= DC_OnHidePopup;
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if(!DC.IsKeyGridVisible) {
                return;
            }
            SharedPaint.Color = KeyboardPalette.P[PaletteColorType.Bg].ToAdColor();
            canvas.DrawRect(Bounds, SharedPaint);

            base.OnDraw(canvas);
        }
        #endregion

        #region Private Methods

        AdKeyView CreateKeyView(KeyViewModel dc) {
            return new AdKeyView(dc, this.Context, SharedPaint).SetDefaultProps();
        }

        #region Popups
        private void DC_OnShowPopup(object sender, KeyViewModel e) {            
            try {
                if (//DC.IsFloatingLayout ||
                    Context is not AdInputMethodService ims ||
                    ims.KeyboardView is not { } kbv ||
                    e is not KeyViewModel anchor_kvm ||
                    KeyViews.FirstOrDefault(x => x.DC == anchor_kvm) is not { } anchor_kv ||
                    AdCustomPopupWindow.Create(this) is not { } puw) {
                    return;
                }
                var puv = new AdKeyPopupView(Context, SharedPaint, e).SetDefaultProps();
                puw.ContentView = puv;
                PopupWindows.Add(puw);

                // set popup contents tag to anchor kvm
                puw.Tag = anchor_kv;
                // from https://stackoverflow.com/a/33363635/105028

                puw.Width = (int)puv.Frame.Width();
                puw.Height = (int)puv.Frame.Height();
                //puv.RenderFrame(true);
                var pur = anchor_kvm.PopupRect.ToRectF();
                View anchor = this;
                if (DC.KeyboardViewModel.CanInitiateFloatLayout && 
                    DC.KeyboardViewModel.IsFloatingLayout &&
                ims.KeyboardContainerView.FloatView is { } fv &&
                fv.FloatWindow is { } fw &&
                fw.AnchorView is { } floatAnchor) {
                    anchor = floatAnchor;
                    var p = fv.GetTranslatedPopupPosition(pur.Position());
                    var float_loc = DC.KeyboardViewModel.FloatContainerViewModel.FloatPosition.ToPointF();
                    pur = pur.Place(p.X + float_loc.X, p.Y + float_loc.Y);
                }
                puw.ShowAtLocation(anchor, GravityFlags.NoGravity, (int)pur.Left, (int)pur.Top);
                puw.Update((int)pur.Left, (int)pur.Top + (int)kbv.ContainerOffsetY, puw.Width, puw.Height, true);
                puv.RenderFrame(true);
            }
            catch(Exception ex) {
                ex.Dump();
                // this may cause MonoDroid: Java.Lang.RuntimeException: Unable to add window -- token null is not valid; is your activity running?
                MpConsole.WriteLine($"Error showing popup, attempting to recreate popup windows and re-show");
            }

        }

        private void DC_OnHidePopup(object sender, KeyViewModel e) {
            if(KeyViews.FirstOrDefault(x=>x.DC == e) is not { } kv ||
                PopupWindows.FirstOrDefault(x=>x.Tag == kv) is not { } puw) {
                MpConsole.WriteLine($"Popup window not found. Cannot dismiss!");
                return;
            }
            PopupWindows.Remove(puw);
            puw.Dismiss();
            puw.Dispose();
            //kv.RenderFrame(true);
        }

        #endregion
        #endregion
    }


}