using MonkeyPaste.Keyboard.Common;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosEmojiPageView : FrameViewBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            this.Hidden = !DC.IsVisible;
            base.LayoutFrame(invalidate);
        }

        public override void MeasureFrame(bool invalidate) {
            Frame = DC.ScrollClipRect.ToCGRect();
            base.MeasureFrame(invalidate);
        }

        #endregion

        #endregion

        #region Properties

        #region Members
        
        #endregion

        #region View Models
        public new EmojiPageViewModel DC { get; private set; }
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
        public iosEmojiPageView(EmojiPageViewModel dc) {
            DC = dc;
            RemapRenderer();
        }
        #endregion

        #region Public Methods
        public void RemapRenderer() {
            if(DC == null) {
                return;
            }
            DC.SetRenderContext(this);
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