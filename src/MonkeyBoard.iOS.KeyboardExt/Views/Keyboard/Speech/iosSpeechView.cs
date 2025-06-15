using MonkeyPaste.Keyboard.Common;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosSpeechView : FrameViewBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new SpeechViewModel DC { get; private set; }
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
        public iosSpeechView(SpeechViewModel dc) {
            DC = dc;
            ResetRenderer();
        }
        #endregion

        #region Public Methods
        public void ResetRenderer() {
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