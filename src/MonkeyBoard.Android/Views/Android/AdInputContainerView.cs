using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Android.Telephony.CarrierConfigManager;

namespace MonkeyBoard.Android {
    public class AdInputContainerView : AdCustomViewGroup {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void LayoutFrame(bool invalidate) {
            this.SetZ(-2);
            base.LayoutFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        #endregion

        #region Views  
        public AdKeyboardView KeyboardView { get; private set; }
        public AdFloatOuterContainerView FloatView { get; private set; }

        AdCustomViewGroup PopupAnchorView =>
            this;
        #endregion

        #region View Models
        public new KeyboardViewModel DC =>
            KeyboardView == null ? null : KeyboardView.DC;
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
        public AdInputContainerView(Context context) : base(context) {
            this.Background = null;
            KeyboardView = new AdKeyboardView(context).SetDefaultProps("keyboardView");
            SharedPaint = KeyboardView.SharedPaint;
            this.AddView(KeyboardView);
        }
        #endregion

        #region Public Methods
        public void Init(AdInputMethodService ic) {
            KeyboardView.Init(ic);
            DC.OnIsFloatLayoutChanged += DC_OnIsFloatLayoutChanged;
            //DC.FloatContainerViewModel.OnFloatScaleChangeEnd += FloatContainerViewModel_OnFloatScaleChangeEnd;
            //if(DC.IsFloatingLayout) {
            //    this.Frame = new();
            //    return;
            //}
            this.Frame = KeyboardView.Frame;
            this.RenderFrame(true);
        }

        private void FloatContainerViewModel_OnFloatScaleChangeEnd(object sender, EventArgs e) {
            DC.FloatContainerViewModel.OnFloatScaleChangeEnd -= FloatContainerViewModel_OnFloatScaleChangeEnd;
            Init(Context as AdInputMethodService);
            ShowInWindow();
        }

        public void ShowKeyboard() {
            if (DC.IsFloatingLayout) {
                ShowInWindow();
            } else {
                ShowInDock();
            }
        }
        

        private void DC_OnIsFloatLayoutChanged(object sender, EventArgs e) {
            ShowKeyboard();
        }

        void ShowInDock() {
            Handler?.Post(() => {
                if(FloatView is { } fv &&
                    fv.DismissAndDetachWindow()) {
                    this.AddView(KeyboardView);                   

                    ToggleDockDismiss();
                    return;
                }
                //KeyboardView.RenderFrame(true);
                this.Frame = KeyboardView.Frame;
                //this.Redraw(true);
                //KeyboardView.Redraw(true);
                this.RenderFrame(true);
            });
        }


        void ShowInWindow() {
            Handler?.Post(() => {
                if(FloatView == null) {
                    FloatView = new AdFloatOuterContainerView(KeyboardView, Context, SharedPaint);
                } 
                PopupAnchorView.Frame = PopupAnchorView.Frame.Resize(0, 0);
                FloatView.ShowWindow(PopupAnchorView);
            });
            
        }

        void ToggleDockDismiss() {
            if(Context is not AdInputMethodService ims) {
                return;
            }
            void OnDismissed(object s, EventArgs e) {
                ims.OnDismissed -= OnDismissed;
                ims.RequestShowSelf(ShowFlags.Forced);
            }
            ims.OnDismissed += OnDismissed;
            ims.RequestHideSelf(HideSoftInputFlags.NotAlways);
        }
        
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            //canvas.DrawColor(Color.Transparent);          
        }
        #endregion

        #region Private Methods

        #endregion

        #region Commands
        #endregion
    }
}