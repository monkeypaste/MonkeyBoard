using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class EmojiFooterMenuViewModel : FrameViewModelBase {
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
        public new EmojiPagesViewModel Parent { get; private set; }
        public EmojiSearchViewModel SearchBoxViewModel { get; private set; }
        public ObservableCollection<EmojiFooterMenuItemViewModel> Items { get; set; } = [];
        public EmojiFooterMenuItemViewModel SelectedItem =>
            Parent.SelectedEmojiPage == null ? 
                null : 
                Items.FirstOrDefault(x => x.EmojiPageType == Parent.SelectedEmojiPage.EmojiPageType);
        public EmojiFooterMenuItemViewModel PressedItem {
            get => Items.FirstOrDefault(x => x.IsPressed);
            set => Items.ForEach(x => x.IsPressed = x == value);
        }

        #endregion

        #region Appearance

        public string EmojiFooterMenuBgHexColor =>
            KeyboardPalette.P[PaletteColorType.MenuBg];
        public string EmojiFooterMenuFgHexColor =>
            Parent.EmojiPagesFgHexColor;

        public string SelectedMenuItemBgHexColor =>
            KeyboardPalette.P[PaletteColorType.MenuItemSelectedBg];
        #endregion

        #region Layout
        public override Rect Frame => EmojiFooterRect;
        public double EmojiMenuItemFontSize =>
            14 * KeyboardViewModel.FloatEmojiScale;
        double EmojiFooterHeightRatio =>
            0.175;
        double EmojiFooterHeight =>
            Parent.TotalRect.Height * EmojiFooterHeightRatio;

        public Rect EmojiFooterRect {
            get {
                double w = Parent.TotalRect.Width;
                double h = EmojiFooterHeight;
                double x = 0;
                double y = Parent.TotalRect.Height - h;
                return new Rect(x, y, w, h);
            }
        }
        public Rect TotalRect =>
            EmojiFooterRect.Move(0, Parent.TotalRect.Top);
        
        //Rect[] _emojiFooterItemRects;
        public Rect[] EmojiFooterItemRects {
            get {
                int count = MenuItemCount;
                double th = EmojiFooterRect.Height;
                double tw = EmojiFooterRect.Width;
                double iw = tw / count;
                double ih = th;
                var _emojiFooterItemRects = new Rect[count];
                for (int i = 0; i < _emojiFooterItemRects.Length; i++) {
                    double x = i * iw;
                    double y = 0;
                    _emojiFooterItemRects[i] = new Rect(x, y, iw, ih);
                }
                return _emojiFooterItemRects;
            }
        }

        public Rect SelectionRect {
            get {
                if(SelectedMenuIdx < 0 || SelectedMenuIdx >= EmojiFooterItemRects.Length) {
                    return new();
                }
                if(IsCategoryIdx(SelectedMenuIdx)) {
                    double w = EmojiFooterItemRects.FirstOrDefault().Width;
                    double h = EmojiFooterRect.Height;
                    double page_x = Parent.ScrollOffsetX;
                    double page_max_x = Parent.EmojiPages.Max(x => x.PageRect.Left);
                    double x_offset = EmojiFooterItemRects[CategoryStartIdx].Left;
                    double category_width = (CategoryEndIdx - CategoryStartIdx + 1) * w;
                    double x = x_offset + ((page_x / page_max_x) * (category_width-w));// - (w/2);
                    double y = EmojiFooterRect.Top;
                    return new Rect(x, y, w, h);
                }
                return EmojiFooterItemRects[SelectedMenuIdx];
            }
        }

        #endregion

        #region State
        public int CategoryStartIdx =>
            IsSearchEnabled ? SearchIdx+1:1;
        public int CategoryEndIdx =>
            EmojiFooterItemRects.Length - 2;

        int _menuItemCount = -1;
        public int MenuItemCount {
            get {
                if(_menuItemCount < 0) {
                    // 2 is search and delete buttons, 
                    int extra_btns = IsSearchEnabled ? 3:2;
                    // 1 is component
                    int hidden_page_types = 1;
                    _menuItemCount = extra_btns + Enum.GetNames(typeof(EmojiPageType)).Length - 1 - hidden_page_types;
                }
                return _menuItemCount;
            }
        }
        public string TouchId { get; private set; }
        public int SelectedMenuIdx =>
            Items.IndexOf(SelectedItem);
        public int PressedMenuIdx =>
            PressedItem == null ? -1 : Items.IndexOf(PressedItem);

        public int SelectKeyboardIdx => 0;
        public int SearchIdx => IsSearchEnabled ? 1:-55;
        public int BackspaceIdx => MenuItemCount - 1;
        public bool IsBackspacePressed =>
            PressedMenuIdx == BackspaceIdx;
        bool IsSearchEnabled => !KeyboardViewModel.IsFloatingLayout;
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public EmojiFooterMenuViewModel(EmojiPagesViewModel parent) {
            Parent = parent;
            SearchBoxViewModel = new EmojiSearchViewModel(this);
            KeyboardViewModel.OnIsFloatLayoutChanged += KeyboardViewModel_OnIsFloatLayoutChanged;
        }

        #endregion

        #region Public Methods
        public void Init() {
            SearchBoxViewModel.Init();

            Items.Clear();
            Items.AddRange(
                Enumerable.Range(0,MenuItemCount)
                .Select(x=>new EmojiFooterMenuItemViewModel(this,x)));
        }
        public bool HandleTouch(TouchEventType touchType, Touch touch) {
            if(SearchBoxViewModel.HandleTouch(touchType,touch)) {
                return true;
            }
            if(!Parent.IsVisible) {
                return false;
            }

            if(touchType == TouchEventType.Press &&
                TouchId == null &&
                GetMenuItemIdxUnderPoint(touch.Location) is { } presed_idx) {
                TouchId = touch.Id;

                SetMenuItemPressed(presed_idx, true);
                if(IsBackspacePressed) {
                    KeyboardViewModel.RepeatBackspace(
                        canRepeat: () => {
                            return 
                            KeyboardViewModel.IsBackspaceRepeatEnabled && 
                            IsBackspacePressed && 
                            TouchId == touch.Id;
                        });
                }
                return true;
            } 
            if(touchType == TouchEventType.Press || 
                TouchId == null ||
                touch.Id != TouchId) {
                return false;
            }
            if(touchType == TouchEventType.Move &&
                GetMenuItemIdxUnderPoint(touch.Location) is { } move_idx) {

                SetMenuItemPressed(move_idx, true);
            } else if(touchType == TouchEventType.Release) {
                int pressed_idx = PressedMenuIdx;
                SetMenuItemPressed(pressed_idx, false);
                if (GetMenuItemIdxUnderPoint(touch.Location) == pressed_idx) {
                    PerformMenuItemAction(pressed_idx);
                }
                TouchId = null;
            }

            return true;
        }

        public void ResetState() {
            _menuItemCount = -1;
            TouchId = null;
            Items.ForEach(x => x.ResetState());
        }
        public bool IsCategoryIdx(int menuItemIdx) {
            return menuItemIdx >= CategoryStartIdx && menuItemIdx <= CategoryEndIdx;
        }
        public bool IsSelectableIdx(int menuItemIdx) {
            return IsCategoryIdx(menuItemIdx) || menuItemIdx == 1;
        }
        public int GetMenuItemZIndex(int menuItemIdx) {
            if(IsCategoryIdx(menuItemIdx)) {
                return 0;
            }
            return 1;
        }
        public Point GetMenuItemTextLoc(int menuItemIdx, string menuItemText) {
            if (InputConnection is not { } ic ||
                ic.TextTools is not { } tm) {
                return new();
            }
            var item_rect = EmojiFooterItemRects.ElementAt(menuItemIdx);
            var text_rect =
                tm
                .MeasureText(menuItemText, EmojiMenuItemFontSize, out double ascent, out double descent);
            double cix = item_rect.Width / 2;
            double ciy = (item_rect.Height / 2) - ((ascent + descent) / 2);
            return new Point(cix, ciy);

        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        int? GetMenuItemIdxUnderPoint(Point loc) {
            double loc_x = loc.X;
            double loc_y = loc.Y - EmojiFooterRect.Top - Parent.TotalRect.Top;
            loc = new Point(loc_x, loc_y);
            if(EmojiFooterItemRects.FirstOrDefault(x=>x.Contains(loc)) is { } htt_rect) {
                int item_idx = EmojiFooterItemRects.IndexOf(htt_rect);
                if(item_idx >= 0) {
                    return item_idx;
                }
            }
            return null;
        }

        void SetMenuItemPressed(int itemIdx, bool isPressed) {
            var last_pressed = PressedItem;
            PressedItem = isPressed ? Items[itemIdx] : null;
            //if(last_pressed != null && last_pressed != PressedItem) {
            //    last_pressed.Renderer.PaintFrame(true);
            //}
            //if(PressedItem != null) {
            //    PressedItem.Renderer.PaintFrame(true);
            //}
            Renderer.PaintFrame(true);
        }
        void PerformMenuItemAction(int menuItemIdx) {
            if(menuItemIdx == BackspaceIdx) {
                // handled by keyboardVm
                return;
            }

            InputConnection.OnFeedback(KeyboardViewModel.FeedbackClick);
            if (menuItemIdx == SelectKeyboardIdx) {
                SetMenuItemPressed(menuItemIdx, false);
                Parent.Parent.SetMenuPage(MenuPageType.TextCompletions);
            } else if(menuItemIdx == SearchIdx) {
                SearchBoxViewModel.ShowSearchBox();
            }  else if(IsCategoryIdx(menuItemIdx)) {
                EmojiPageType page_type = (EmojiPageType)(menuItemIdx - CategoryStartIdx + 1);
                if(Parent.EmojiPages.FirstOrDefault(x=>x.EmojiPageType == page_type) is { } page_vm) {
                    Parent.SelectPage(page_vm);
                }
            }  else {
                // unknown menu idx
                Debugger.Break();
            }         
        }

        private void KeyboardViewModel_OnIsFloatLayoutChanged(object sender, EventArgs e) {
            // hide search in float
            ResetState();
            Init();
        }
        #endregion

        #region Commands
        #endregion
    }
}
