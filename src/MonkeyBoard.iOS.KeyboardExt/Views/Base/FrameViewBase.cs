using CoreAnimation;
using CoreGraphics;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class FrameViewBase : UIView, IFrameRenderer {

        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IFrameRenderer Implementation
        public virtual void LayoutFrame(bool invalidate) {
            if (IsDisposed) {
                return;
            }
            SubFrames.ForEach(x => x.LayoutFrame(invalidate));
            if (invalidate) {
                this.Redraw(RedrawNeedsLayout);                
            }
        }

        public virtual void MeasureFrame(bool invalidate) {
            if (IsDisposed) {
                return;
            }
            SubFrames.ForEach(x => x.MeasureFrame(invalidate));

            if (invalidate) {
                this.Redraw(RedrawNeedsLayout);
            }
        }

        public virtual void PaintFrame(bool invalidate) {
            if (IsDisposed) {
                return;
            }
            SubFrames.ForEach(x => x.PaintFrame(invalidate));

            if (invalidate) {
                this.Redraw(RedrawNeedsLayout);
            }
        }

        public virtual void RenderFrame(bool invalidate) {
            if(IsDisposed) {
                return;
            }
            SubFrames.ForEach(x => x.RenderFrame(invalidate));

            LayoutFrame(false);
            MeasureFrame(false);
            PaintFrame(false);

            if(invalidate) {
                this.Redraw(RedrawNeedsLayout);
            }
        }

        #endregion

        #endregion

        #region Properties
        public FrameViewModelBase DC { get;  }
        public object TagObj { get; set; }
        public IEnumerable<IFrameRenderer> SubFrames {
            get {
                try {
                    return IsDisposed ? [] : Subviews.OfType<IFrameRenderer>().Where(x => !x.IsDisposed);
                }catch(Exception ex) {
                    ex.Dump();
                }
                return [];
            }
        }
            
        public bool IsDisposed { get; private set; }
        public IMainThread Handler =>
            iosKeyboardViewController.Instance.MainThread;
        public bool RedrawNeedsLayout { get; set; }
        #endregion

        #region Events
        public event EventHandler OnViewDidLoad;
        #endregion

        #region Constructors
        public FrameViewBase() {
            //this.Opaque = false;
            this.ClearsContextBeforeDrawing = true;
            this.BackgroundColor = UIColor.FromRGBA(0,0,0,0);
            this.ClipsToBounds = true;
            this.UserInteractionEnabled = false;
            this.TranslatesAutoresizingMaskIntoConstraints = false;
        }
        #endregion

        #region Public Methods

        public override void MovedToSuperview() {
            base.MovedToSuperview();
            OnViewDidLoad?.Invoke(this, EventArgs.Empty);
        }

        public new void Dispose() {
            IsDisposed = true;
            base.Dispose();
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