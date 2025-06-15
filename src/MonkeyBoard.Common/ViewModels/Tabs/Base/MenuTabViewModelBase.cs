using Avalonia;

namespace MonkeyBoard.Common {
    public abstract class MenuTabViewModelBase : FrameViewModelBase, IKeyboardMenuTabItem {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardMenuTabItem Implementation
        MenuTabItemType IKeyboardMenuTabItem.TabItemType =>
            TabItemType;
        object IKeyboardMenuTabItem.IconSourceObj =>
            TabIconSourceObj;

        bool IKeyboardMenuTabItem.IsSelected =>
            Parent.SelectedTabItemType == (this as IKeyboardMenuTabItem).TabItemType;

        bool _isPressed;
        bool IKeyboardMenuTabItem.IsPressed {
            get => _isPressed;
            set {
                if(_isPressed == value) {
                    return;
                }
                _isPressed = value;

            }
        }

        Rect IKeyboardMenuTabItem.TabItemRect {
            get {
                var msvm = Parent.MenuStripViewModel;
                double w = msvm.MenuStripRect.Width / msvm.MaxVisibleMenuItemCount;
                double h = msvm.MenuStripRect.Height;

                int idx = msvm.Items.IndexOf(this);
                double x = msvm.MenuStripRect.Left + idx * w;
                double y = msvm.MenuStripRect.Top;
                return new Rect(x, y, w, h);
            }
        }
        Rect IKeyboardMenuTabItem.IconRect {
            get {
                if(InputConnection is not { } ic ||
                    ic.TextTools is not { } tm ||
                    this is not IKeyboardMenuTabItem mti) {
                    return new();
                }
                var text_rect =
                        tm
                        .MeasureText(mti.IconSourceObj.ToString(), KeyboardLayoutConstants.MenuTabItemFontSize, out double ascent, out double descent);
                var tab_item_rect = mti.TabItemRect;
                double cix = tab_item_rect.Center.X;
                double ciy = tab_item_rect.Center.Y - ((ascent + descent) / 2);
                return new Rect(cix, ciy, 0, 0);
            }
        }
        #endregion

        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new MenuViewModel Parent { get; set; }
        #endregion

        #region Appearance
        public string BgHexColor =>
            KeyboardViewModel.KeyGridBgHexColor;
        public string FgHexColor =>
            KeyboardPalette.P[PaletteColorType.Fg2];

        public double IconSize => 42;
        protected abstract object TabIconSourceObj { get; }

        #endregion

        #region Layout
        public Rect ContentRect =>
            KeyboardViewModel.KeyGridRect;

        #endregion

        #region State
        protected string TouchId { get; set; }
        public override bool IsVisible =>
            (this as IKeyboardMenuTabItem).IsSelected;
        #endregion

        #region Models
        protected abstract MenuTabItemType TabItemType { get; }
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public MenuTabViewModelBase(MenuViewModel parent) {
            Parent = parent;
        }
        #endregion

        #region Public Methods
        public virtual bool HandleTouch(TouchEventType touchType, Touch touch) {
            return false;
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
