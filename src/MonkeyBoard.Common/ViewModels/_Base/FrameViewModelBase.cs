using Avalonia;
using Avalonia.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonkeyBoard.Common {
    public abstract class FrameViewModelBase : TreeViewModelBase, IFrameRenderer, IFrameRenderContext {
        #region Private Variables
        protected IFrameRenderer _renderer;
        #endregion

        #region Constants
        #endregion

        #region Statics
        private static KeyboardViewModel _kbvm;
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public virtual void MeasureFrame(bool invalidate) {
            if (this is FrameViewModelBase kvm) {
            }

        }

        public virtual void PaintFrame(bool invalidate) {
            if (this is FrameViewModelBase kvm) {
            }
        }

        public virtual void LayoutFrame(bool invalidate) {
            if (this is FrameViewModelBase kvm) {
            }
        }
        public virtual void RenderFrame(bool invalidate) {
            Renderer.MeasureFrame(false);
            Renderer.LayoutFrame(false);
            Renderer.PaintFrame(invalidate);
        }
        #endregion

        #region IKeyboardRenderSource Implementation
        public void SetRenderContext(IFrameRenderer renderer) {
            _renderer = renderer;
        }
        #endregion
        #endregion

        #region Properties

        #region Members
        public virtual IFrameRenderer Renderer {
            get {
                if(!OperatingSystem.IsWindows() && _renderer == null) {
                    // unset renderer!
                    //Debugger.Break();
                }
                return _renderer ?? this;
            }
        }
            
        #endregion

        #region View Models
        public new FrameViewModelBase Parent { get; protected set; }
        public KeyboardViewModel KeyboardViewModel => _kbvm;
        public IKeyboardInputConnection InputConnection =>
            KeyboardViewModel == null ? null : KeyboardViewModel.InputConnection;
        #endregion

        #region Appearance
        #endregion

        #region Layout
        public virtual Rect Frame => new();
        #endregion

        #region State
        public bool IsDisposed => false;
        public bool IsLoaded { get; protected set; }
        public virtual bool IsVisible { get; }
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        #endregion

        #region Public Methods
        public void SetRootViewModel(KeyboardViewModel kbvm) {
            _kbvm = kbvm;
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
