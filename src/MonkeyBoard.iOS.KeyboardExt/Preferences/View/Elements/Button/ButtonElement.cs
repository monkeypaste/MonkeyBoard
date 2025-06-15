using MonoTouch.Dialog;
using System;
using UIKit;

#pragma warning disable CS1062 // The best overloaded Add method for the collection initializer element is obsolete
namespace MonkeyBoard.iOS.KeyboardExt {
    public class ButtonElement : StyledMultilineElement, IKeyedElement {
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
        public string Key { get; private set; }
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
        public ButtonElement(string key, string caption, string detail, UITableViewCellStyle style, Action tapped) : base(caption, detail, style) {
            this.Font = UIFont.FromName(iosKeyboardView.DEFAULT_FONT_FAMILY, 17);
            Key = key;
            if(tapped != null) {
                this.Tapped += tapped;
            }
        }

        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}