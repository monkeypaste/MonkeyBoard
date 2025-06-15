using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Windows.Input;

namespace MonkeyBoard.Common {
    public abstract class ConfigItemViewModelBase : FrameViewModelBase {
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
        public new ConfigPageViewModel Parent { get; protected set; }
        #endregion

        #region Appearance
        public abstract object IconSourceObj { get; }
        public abstract string Label { get; }
        public string BgHexColor =>
            IsPressed ? KeyboardPalette.P[PaletteColorType.MenuItemPressedBg] : null;
        public string FgHexColor =>
            KeyboardPalette.P[PaletteColorType.Fg2];
        public double IconFontSize =>
            42;
        public double LabelFontSize =>
            14;
        #endregion

        #region Layout

        public Rect ItemRect {
            get {
                var cntr_rect = Parent.Frame;
                double w = cntr_rect.Width / (double)Parent.MaxItemColumns;
                double h = cntr_rect.Height / Parent.MaxVisibleItemRows;

                int row = (int)Math.Floor(SortOrderIdx / (double)Parent.MaxItemColumns);
                int col = SortOrderIdx % Parent.MaxItemColumns;
                double x = cntr_rect.Left + (col * w);
                double y = cntr_rect.Top + (row * h);
                return new Rect(x, y, w, h);
            }
        }
        public Rect IconRect {
            get {
                double w = ItemRect.Width;
                double h = ItemRect.Height * 0.75;
                double x = ItemRect.X;
                double y = ItemRect.Y;
                return new Rect(x, y, w, h);
            }
        }
        public Rect LabelRect {
            get {
                double w = ItemRect.Width;
                double h = ItemRect.Height - IconRect.Height;
                double x = ItemRect.Left;
                double y = IconRect.Bottom;
                return new Rect(x, y, w, h);
            }
        }
        #endregion

        #region State
        bool IsPressed { get; set; }
        public int SortOrderIdx => Parent.Items.IndexOf(this);
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public ConfigItemViewModelBase(ConfigPageViewModel parent) {
            Parent = parent;
        }
        #endregion

        #region Public Methods

        #endregion

        #region Protected Methods
        protected virtual bool CanSelectItem() {
            return true;
        }
        protected virtual void SelectItem() { }
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        public ICommand SelectItemCommand => new MpCommand(SelectItem, CanSelectItem);
        #endregion
    }
}
