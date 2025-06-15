using Avalonia;
using MonkeyPaste.Common;
using System.Collections.ObjectModel;
using System.Linq;

namespace MonkeyBoard.Common {
    public class ConfigPageViewModel : MenuTabViewModelBase {
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
        public ObservableCollection<ConfigItemViewModelBase> Items { get; } = [];
        public FloatConfigItemViewModel FloatConfigItemViewModel { get; private set; }
        #endregion

        #region Appearance
        protected override object TabIconSourceObj => "⚙️";
        #endregion

        #region Layout
        public int MaxItemColumns => 3;
        public int MaxVisibleItemRows => 3;
        public override Rect Frame =>
            KeyboardViewModel.KeyGridRect;
        #endregion

        #region State
        protected override MenuTabItemType TabItemType => MenuTabItemType.Config;
        public int PressedItemIdx { get; private set; }
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public ConfigPageViewModel(MenuViewModel parent) : base(parent) {
            FloatConfigItemViewModel = new(this);
            Items.AddRange([FloatConfigItemViewModel]);
        }
        #endregion

        #region Public Methods
        public void Init() {
        }
        public override bool HandleTouch(TouchEventType touchType, Touch touch) {
            if(!IsVisible) {
                return false;
            }
            bool handled = false;
            switch(touchType) {
                case TouchEventType.Press:
                    if(!Frame.Contains(touch.Location) || TouchId != null) {
                        break;
                    }
                    if(Items.FirstOrDefault(x => x.ItemRect.Contains(touch.Location)) is { } touch_item) {
                        SetPressed(touch_item.SortOrderIdx, true);
                        handled = true;
                    }
                    TouchId = handled ? touch.Id : null;

                    break;
                case TouchEventType.Move:
                case TouchEventType.Release:
                    handled = TouchId == touch.Id;
                    if(!handled ||
                        touchType == TouchEventType.Move) {
                        break;
                    }
                    var pressed_item = Items.FirstOrDefault(x => x.SortOrderIdx == PressedItemIdx);
                    bool can_perform_action = pressed_item != null;
                    if(can_perform_action) {
                        pressed_item.SelectItemCommand.Execute(null);
                    }
                    SetPressed(-1, false);
                    break;
            }
            if(handled) {
                this.Renderer.RenderFrame(true);
            }
            return handled;
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        void SetPressed(int itemIdx, bool isPressed) {
            int last_pressed_idx = PressedItemIdx;
            PressedItemIdx = isPressed ? itemIdx : -1;
            if(last_pressed_idx == PressedItemIdx) {
                return;
            }
        }
        #endregion

        #region Commands
        #endregion
    }
}
