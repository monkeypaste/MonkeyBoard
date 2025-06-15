using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class EmojiFooterMenuItemViewModel : FrameViewModelBase {
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
        public override IFrameRenderer Renderer =>
            Parent.Renderer;

        #endregion

        #region View Models
        public new EmojiFooterMenuViewModel Parent { get; private set; }
        #endregion

        #region Appearance
        public object IconSourceObj { get; private set; }
        public string MenuItemBgHexColor =>
            IsPressed ?
                KeyboardPalette.P[PaletteColorType.MenuItemPressedBg] :
                IsSelected ?
                    null :
                    EmojiPageType == EmojiPageType.None ?
                            KeyboardPalette.P[PaletteColorType.EmojiMenuItemBg] :
                            KeyboardPalette.P[PaletteColorType.EmojiMenuItem2Bg];
        public string MenuItemFgHexColor =>
            KeyboardPalette.P[PaletteColorType.Fg2];

        #endregion

        #region Layout

        #endregion

        #region State
        public EmojiPageType EmojiPageType { get; private set; } = EmojiPageType.None;
        public bool IsSelected { get; set; }
        public bool IsPressed { get; set; }
        public int MenuItemIdx { get; private set; }
        public bool IsBackspace =>
            MenuItemIdx == Parent.BackspaceIdx;
        #endregion

        #region Models

        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public EmojiFooterMenuItemViewModel(EmojiFooterMenuViewModel parent, int menuIdx) {
            Parent = parent;
            MenuItemIdx = menuIdx;

            if (MenuItemIdx == Parent.SelectKeyboardIdx) {
                IconSourceObj = "⌨";
            } else if (MenuItemIdx == Parent.SearchIdx) {
                IconSourceObj = "🔍";
            } else if (MenuItemIdx == Parent.BackspaceIdx) {
                IconSourceObj = "⌫";
            } else {
                EmojiPageType = (EmojiPageType)(MenuItemIdx - Parent.CategoryStartIdx + 1);
                if(Parent.Parent.EmojiPages.FirstOrDefault(x=>x.EmojiPageType == EmojiPageType) is { } page_vm) {
                    IconSourceObj = page_vm.IconResourceObj;
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetPressed(bool isPressed) {
            IsPressed = isPressed;
            this.Renderer.PaintFrame(true);
        }
        public void ResetState() {
            IsPressed = false;
            IsSelected = false;
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
