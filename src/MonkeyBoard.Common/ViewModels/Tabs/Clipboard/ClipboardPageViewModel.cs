namespace MonkeyBoard.Common {
    public class ClipboardPageViewModel : MenuTabViewModelBase {
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
        #endregion

        #region Appearance
        protected override object TabIconSourceObj => "📋";
        #endregion

        #region Layout
        #endregion

        #region State
        protected override MenuTabItemType TabItemType => MenuTabItemType.Clipboard;
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public ClipboardPageViewModel(MenuViewModel parent) : base(parent) { }
        #endregion

        #region Public Methods
        public void Init() {
            if(KeyboardViewModel.InputConnection is not { } ic) {
                return;
            }
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
