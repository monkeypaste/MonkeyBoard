using Avalonia;
using MonkeyPaste.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class MenuStripViewModel : FrameViewModelBase {
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
        public new MenuViewModel Parent { get; private set; }
        public ObservableCollection<IKeyboardMenuTabItem> Items { get; private set; } = [];
        #endregion

        #region Appearance
        public string PressedTabItemBgHexColor =>
            KeyboardPalette.P[PaletteColorType.MenuItemPressedBg];
        public string SelectedTabItemBgHexColor =>
            KeyboardPalette.P[PaletteColorType.MenuItemSelectedBg];
        #endregion

        #region Layout
        public int MaxVisibleMenuItemCount => 5;
        public Rect MenuStripRect =>
            Parent.InnerMenuRect;
        public double MenuItemFontSize =>
            KeyboardLayoutConstants.MenuTabItemFontSize * KeyboardViewModel.FloatEmojiScale;
        #endregion

        #region State
        public override bool IsVisible =>
            !KeyboardViewModel.IsNumPadLayout &&
            Parent.CurMenuPageType == MenuPageType.TabSelector;
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public MenuStripViewModel(MenuViewModel parent) {
            Parent = parent;
        }
        #endregion

        #region Public Methods
        public void Init(IEnumerable<IKeyboardMenuTabItem> items) {
            Items.Clear();
            Items.AddRange(items);
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        public IKeyboardMenuTabItem GetMenuItemUnderPoint(Point loc) {
            if(!MenuStripRect.Contains(loc)) {
                return null;
            }
            double loc_x = loc.X - MenuStripRect.Left;
            double loc_y = loc.Y - MenuStripRect.Top;
            loc = new Point(loc_x, loc_y);

            if(Items.FirstOrDefault(x => x.TabItemRect.Contains(loc)) is { } htt_item) {
                return htt_item;
            }
            return null;
        }
        #endregion

        #region Commands
        #endregion
    }
}
