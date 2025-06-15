using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class EmojiSearchViewModel : FrameViewModelBase {
        #region Private Variables
        #endregion

        #region Constants
        const int CLOSE_BUTTON_ID = 1;
        const int CLEAR_TEXT_BUTTON_ID = 2;
        const int OVERLAY_ID = 3;
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new EmojiFooterMenuViewModel Parent { get; private set; }
        public EmojiAutoCompleteViewModel EmojiAutoCompleteViewModel { get; private set; }
        #endregion

        #region Appearance

        #region Overlay
        public string OverlayBgHexColor =>
            "#50000000";
        #endregion

        #region Search Box
        public string EmojiSearchBoxBgHexColor =>
            KeyboardPalette.P[PaletteColorType.EmojiSearchBg];
        public string EmojiSearchBoxFgHexColor =>
            KeyboardPalette.P[PaletteColorType.EmojiSearchTextFg];
        public string EmojiSearchBoxPlaceholderFgHexColor =>
            KeyboardPalette.P[PaletteColorType.Fg2];
        public string EmojiSearchBoxSelHexColor =>
            KeyboardPalette.P[PaletteColorType.EmojiSearchTextSel];
        public string EmojiSearchBoxCaretHexColor =>
            KeyboardPalette.P[PaletteColorType.EmojiSearchTextCaret];
        public string PlaceholderText =>
            ResourceStrings.K["EmojiSearchPlaceholderText"].value;

        #endregion

        #region Clear Text Button
        public object ClearTextButtonIconSourceObj =>
            "delete.png";
        public string ClearTextButtonFgHexColor =>
            IsClearTextButtonPressed ? EmojiSearchBoxFgHexColor.AdjustAlpha(0.33) : EmojiSearchBoxFgHexColor;
        

        #endregion
        
        #region Close Button
        public object CloseButtonIconSourceObj =>
            "close.png";
        public string CloseButtonFgHexColor =>
            EmojiSearchBoxFgHexColor;
        public string CloseButtonBgHexColor =>
            IsCloseButtonPressed ? KeyboardPalette.P[PaletteColorType.MenuItemPressedBg] : null;

        #endregion

        #endregion

        #region Layout

        #region Close Button
        public Rect CloseButtonRect {
            get {
                double w = EmojiAutoCompleteViewModel.AutoCompleteHeight;
                double h = w;
                double x = 0;
                double y = SearchBoxRect.Top - h;
                return new Rect(x,y,w,h);
            }
        }
        public Rect CloseButtonImageRect {
            get {
                double margin = OperatingSystem.IsAndroid() ? 15:5;
                double w = CloseButtonRect.Width - margin;
                double h = w;
                double x = (margin/2);
                double y = (margin / 2);
                return new Rect(x, y, w, h);
            }
        }
        #endregion

        #region Clear Text Button
        public Rect ClearTextButtonRect {
            get {
                double w = SearchBoxHeight / (KeyConstants.PHI * 1);
                double h = w;
                double margin = (SearchBoxHeight - w) / 2;
                double x = SearchBoxRect.Right - w - margin;
                double y = SearchBoxRect.Center.Y - (h/2);
                return new Rect(x, y, w, h);
            }
        }
        #endregion

        #region Search Box
        public Rect SearchBoxRect {
            get {
                double w = TotalWidth;
                double h = SearchBoxHeight;
                double x = 0;
                double y = KeyboardViewModel.CanShowPopupWindows ? KeyboardViewModel.TotalRect.Top - h : EmojiAutoCompleteViewModel.AutoCompleteRect.Bottom;
                return new Rect(x, y, w, h);
            }
        }
        #endregion
        public Rect InnerContainerRect {
            get {
                double w = TotalWidth;
                double h = InnerContainerHeight;
                double x = 0;
                double y = KeyboardViewModel.CanShowPopupWindows ?  KeyboardViewModel.TotalRect.Top - h : 0;
                return new Rect(x, y, w, h);
            }
        }
        public Rect OverlayRect {
            get {
                double w = TotalWidth;
                double h = OverlayHeight;
                double x = 0;
                double y = 
                        KeyboardViewModel.TotalRect.Top - 
                        InnerContainerHeight -
                        h;
                return new Rect(x, y, w, h);
            }
        }
        public Rect TotalRect {
            get {
                double w = TotalWidth;
                double h = InnerContainerHeight + OverlayHeight;
                double x = 0;
                double y = KeyboardViewModel.CanShowPopupWindows ? KeyboardViewModel.TotalRect.Top - h : 0;
                return new Rect(x, y, w, h);
            }
        }
        public double OverlayHeight {
            get {
                if (KeyboardViewModel.ScaledScreenSize is not { } ss ||
                    KeyboardViewModel.IsFloatingLayout ||
                    KeyboardViewModel.IsFloatingLayout ||
                    !KeyboardViewModel.CanShowPopupWindows) {
                    return default;
                }
                return ss.Height - KeyboardViewModel.TotalRect.Height - InnerContainerHeight;
            }
        }
        public double SearchBoxHeight =>
            (SearchBoxFontSize * (OperatingSystem.IsAndroid() ? 7:1.5)) + 
            (KeyboardViewModel.CanShowPopupWindows ? 0 : SearchBoxPadding.Top + SearchBoxPadding.Bottom);
        public double TotalWidth =>
            Parent.Parent.TotalRect.Width;
        public double InnerContainerHeight {
            get {
                return EmojiAutoCompleteViewModel.AutoCompleteHeight +
                       SearchBoxHeight;
            }
        }            

        public Thickness SearchBoxPadding { get; private set; } = new Thickness(5,3);
       
        public double SearchBoxFontSize => OperatingSystem.IsAndroid() ? 8:24;

        #endregion

        #region State
        int TouchOwnerId { get; set; }
        string TouchId { get; set; }
        bool IsClearTextButtonPressed =>
            TouchOwnerId == CLEAR_TEXT_BUTTON_ID;
        bool IsCloseButtonPressed =>
            TouchOwnerId == CLOSE_BUTTON_ID;
        public bool IsClearTextButtonVisible => 
            !string.IsNullOrEmpty(SearchText);
        public new bool IsVisible { get; private set; }

        public string SearchText { get; private set; } = string.Empty;

        KeyboardFlags OriginalKeyboardFlags { get; set; }
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        public event EventHandler<double> OnSearchHeightChanged;

        public event EventHandler OnShowEmojiSearch;
        public event EventHandler OnHideEmojiSearch;
        #endregion

        #region Constructors
        public EmojiSearchViewModel(EmojiFooterMenuViewModel parent) {
            Parent = parent;
            EmojiAutoCompleteViewModel = new EmojiAutoCompleteViewModel(this);
        }
        #endregion

        #region Public Methods
        public void Init() {
            SearchText = string.Empty;
            EmojiAutoCompleteViewModel.Init();
        }
        public void ShowSearchBox() {
            // NOTE LayoutFrame tells kbv to switch to keygrid layer, then EmojiPages sees box is visible and shows it
            if(IsVisible) {
                return;
            }
            IsVisible = true;
            Parent.Parent.Parent.SetMenuPage(MenuPageType.TextCompletions, MenuTabItemType.Emoji);
            SetSearchText(SearchText);

            SetEmojiSearchKeyboardFlags();
            OnShowEmojiSearch?.Invoke(this, EventArgs.Empty);
            OnSearchHeightChanged?.Invoke(this, InnerContainerHeight);
        }
        public void HideSearchBox() {
            if(!IsVisible) {
                return;
            }
            IsVisible = false;
            SearchText = string.Empty;
            EmojiAutoCompleteViewModel.ClearCompletions();
            RestoreKeyboardFlags();
            OnSearchHeightChanged?.Invoke(this, 0); 
            OnHideEmojiSearch?.Invoke(this, EventArgs.Empty);
        }
        public void SetSearchText(string text) {
            //bool was_hidden = EmojiAutoCompleteViewModel.IsHidden;
            SearchText = text; 
            if(!IsVisible) {
                return;
            }
            EmojiAutoCompleteViewModel.ShowCompletion(new SelectableTextRange(text, 0, text.Length), false);

            //if(EmojiAutoCompleteViewModel.IsHidden != was_hidden) {
            //    // resize pad for autocomplete
            //    OnSearchHeightChanged?.Invoke(this, InnerContainerHeight);
            //    if(!EmojiAutoCompleteViewModel.IsHidden && OperatingSystem.IsAndroid() &&
            //        InputConnection.MainThread is { } mt) {
            //        // BUG first draw on android is weird, needs redraw
            //        mt.Post(async () => {
            //            await Task.Delay(500);
            //            EmojiAutoCompleteViewModel.Renderer.RenderFrame(true);
            //        });
                    
            //    }
            //}
        }

        public bool HandleTouch(TouchEventType touchType, Touch touch) {
            if(!IsVisible) {
                return false;
            }
            int last_owner_id = TouchOwnerId;
            bool handled = false;
            switch(touchType) {
                case TouchEventType.Press:
                    if(TouchId != null) {
                        break;
                    }
                    last_owner_id = TouchOwnerId = GetButtonIdUnderPoint(touch.Location);
                    handled = TouchOwnerId != 0;
                    if(handled) {
                        TouchId = touch.Id;
                    }
                    break;
                case TouchEventType.Move:
                    handled = TouchId == touch.Id;
                    break;
                case TouchEventType.Release:
                    if(TouchId != touch.Id) {
                        break;
                    }
                    handled = true;
                    if(CanPerformAction(touch)) {
                        PerformAction(TouchOwnerId);
                    }
                    TouchOwnerId = 0;
                    TouchId = null;

                    break;
            }
            if(handled) {
                this.Renderer.PaintFrame(true);
                if(last_owner_id == CLOSE_BUTTON_ID) {
                    EmojiAutoCompleteViewModel.Renderer.PaintFrame(true);
                }
            }


            return handled;
        }

        public void ResetState() {
            TouchOwnerId = 0;
            TouchId = null;

            if (!KeyboardViewModel.KeyboardFlags.HasFlag(KeyboardFlags.EmojiSearch)) {
                HideSearchBox();
            }
            EmojiAutoCompleteViewModel.ResetState();
        }

        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        void SetEmojiSearchKeyboardFlags() {
            OriginalKeyboardFlags = KeyboardViewModel.KeyboardFlags;

            var em_kbf = 
                KeyboardFlags.Normal | 
                KeyboardFlags.Done | 
                KeyboardFlags.NotDirty | 
                KeyboardFlags.EmojiSearch;
            if (OriginalKeyboardFlags.HasFlag(KeyboardFlags.Android)) {
                em_kbf |= KeyboardFlags.Android;
            } else if (OriginalKeyboardFlags.HasFlag(KeyboardFlags.iOS)) {
                em_kbf |= KeyboardFlags.iOS;
            } 

            if(OriginalKeyboardFlags.HasFlag(KeyboardFlags.Portrait)) {
                em_kbf |= KeyboardFlags.Portrait;
            } else if(OriginalKeyboardFlags.HasFlag(KeyboardFlags.Landscape)) {
                em_kbf |= KeyboardFlags.Landscape;
            }

            if(OriginalKeyboardFlags.HasFlag(KeyboardFlags.FloatLayout)) {
                em_kbf |= KeyboardFlags.FloatLayout;
            } else if(OriginalKeyboardFlags.HasFlag(KeyboardFlags.FullLayout)) {
                em_kbf |= KeyboardFlags.FullLayout;
            }

            if(OriginalKeyboardFlags.HasFlag(KeyboardFlags.Light)) {
                em_kbf |= KeyboardFlags.Light;
            } else if(OriginalKeyboardFlags.HasFlag(KeyboardFlags.Dark)) {
                em_kbf |= KeyboardFlags.Dark;
            }

            if (OriginalKeyboardFlags.HasFlag(KeyboardFlags.Mobile)) {
                em_kbf |= KeyboardFlags.Mobile;
            } else if (OriginalKeyboardFlags.HasFlag(KeyboardFlags.Tablet)) {
                em_kbf |= KeyboardFlags.Tablet;
            }

            if (OriginalKeyboardFlags.HasFlag(KeyboardFlags.PlatformView)) {
                em_kbf |= KeyboardFlags.PlatformView;
            } 
            if (OriginalKeyboardFlags.HasFlag(KeyboardFlags.OneHanded)) {
                em_kbf |= KeyboardFlags.OneHanded;
            }

            KeyboardViewModel.Init(em_kbf);
        }
        void RestoreKeyboardFlags() {
            KeyboardViewModel.Init(OriginalKeyboardFlags);
            OriginalKeyboardFlags = default;
        }
        int GetButtonIdUnderPoint(Point p) {
            if (!KeyboardViewModel.CanShowPopupWindows) {
                p = new Point(p.X, p.Y + TotalRect.Height);
            }
            //InputConnection.OnLog($"TOUCH: {p} CLEAR: {ClearTextButtonRect}");
            
            if (CloseButtonRect.Contains(p)) {
                return CLOSE_BUTTON_ID;
            } 
            if (ClearTextButtonRect.Contains(p)) {
                return CLEAR_TEXT_BUTTON_ID;
            } 
            if(OverlayRect.Contains(p)) {
                return OVERLAY_ID;
            }
            return 0;
        }
        bool CanPerformAction(Touch touch) {
            return GetButtonIdUnderPoint(touch.Location) == TouchOwnerId && TouchOwnerId != 0;
        }
        void PerformAction(int ownerId) {
            if(ownerId != 0) {
                InputConnection.OnFeedback(KeyboardViewModel.FeedbackClick);
            }
            switch(ownerId) {
                case CLEAR_TEXT_BUTTON_ID:
                    KeyboardViewModel.DoSelectAll();
                    InputConnection.OnBackspace(1);
                    break;
                case CLOSE_BUTTON_ID:
                case OVERLAY_ID:
                    KeyboardViewModel.MenuViewModel.CloseEmojiSearch();
                    break;
            }
        }
        #endregion

        #region Commands
        #endregion
    }
}
