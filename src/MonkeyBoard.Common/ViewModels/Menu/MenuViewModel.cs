using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class MenuViewModel : FrameViewModelBase {

        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
        }

        public override void MeasureFrame(bool invalidate) {
            RaisePropertyChanged(nameof(MenuRect));
            RaisePropertyChanged(nameof(BackButtonWidth));
            RaisePropertyChanged(nameof(OptionsButtonWidth));
        }

        public override void PaintFrame(bool invalidate) {
            RaisePropertyChanged(nameof(BackButtonBgHexColor));
            RaisePropertyChanged(nameof(OptionsButtonBgHexColor));
        }

        public override void RenderFrame(bool invalidate) {
            (this as IFrameRenderer).LayoutFrame(false);
            (this as IFrameRenderer).MeasureFrame(false);
            (this as IFrameRenderer).PaintFrame(invalidate);
        }

        #endregion
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new KeyboardViewModel Parent { get; private set; }

        MenuTabViewModelBase[] Pages =>
            [EmojiPagesViewModel, SpeechPageViewModel, ClipboardPageViewModel, PluginsPageViewModel, ConfigPageViewModel];


        public EmojiPagesViewModel EmojiPagesViewModel { get; private set; }
        public SpeechViewModel SpeechPageViewModel { get; private set; }
        public ClipboardPageViewModel ClipboardPageViewModel { get; private set; }
        public ConfigPageViewModel ConfigPageViewModel { get; private set; }
        public PluginsPageViewModel PluginsPageViewModel { get; private set; }

        public MenuStripViewModel MenuStripViewModel { get; private set; }

        public TextAutoCompleteViewModel TextAutoCompleteViewModel { get; private set; }
        EmojiAutoCompleteViewModel EmojiAutoCompleteViewModel =>
            EmojiPagesViewModel.EmojiSearchViewModel.EmojiAutoCompleteViewModel;

        AutoCompleteViewModelBase[] _autoCompleteItems;
        AutoCompleteViewModelBase[] AutoCompleteItems {
            get {
                if(_autoCompleteItems == null) {
                    _autoCompleteItems = [TextAutoCompleteViewModel, EmojiAutoCompleteViewModel];
                }
                return _autoCompleteItems;
            }
        }

        IEnumerable<AutoCompleteViewModelBase> VisibleAutoCompletes =>
            AutoCompleteItems.Where(x => x.IsVisible);
        AutoCompleteViewModelBase ActiveCompl =>
            TouchOwner.ownerType == MenuItemType.TextCompletionItem ?
                TextAutoCompleteViewModel :
                TouchOwner.ownerType == MenuItemType.EmojiCompletionItem ?
                    EmojiAutoCompleteViewModel :
                    null;
        #endregion

        #region Appearance

        public object CancelIconSourceObj => "close.png";
        public object BackIconSourceObj => "edgearrowleft.png";
        public object OptionsIconSourceObj => "dots_1x3.png";
        public string MenuBgHexColor => KeyboardPalette.P[PaletteColorType.MenuBg];
        public string MenuFgHexColor => KeyboardPalette.P[PaletteColorType.MenuFg];
        public string MenuItemPressedBgHexColor => KeyboardPalette.P[PaletteColorType.MenuItemPressedBg];
        public string BackButtonBgHexColor => TouchOwner.ownerType == MenuItemType.BackButton ?
            MenuItemPressedBgHexColor : MenuBgHexColor;
        public string OptionsButtonBgHexColor => TouchOwner.ownerType == MenuItemType.OptionsButton ?
            MenuItemPressedBgHexColor : MenuBgHexColor;
        public CornerRadius MenuCornerRadius =>
            KeyboardViewModel.IsFloatingLayout ?
                new CornerRadius(KeyboardViewModel.CommonCornerRadius.TopLeft, KeyboardViewModel.CommonCornerRadius.TopRight, 0, 0) :
                new()
            ;
        #endregion

        #region Layout
        public override Rect Frame => MenuRect.Move(new(0, -MenuRect.Height));

        double EmojiSearchHeight =>
            EmojiPagesViewModel.EmojiSearchViewModel.IsVisible ?
                EmojiPagesViewModel.EmojiSearchViewModel.InnerContainerRect.Height : 0;

        Rect MenuHitRect {
            get {
                double esh = EmojiSearchHeight;
                double w = MenuRect.Width;
                double h = MenuRect.Height + esh;
                double x = MenuRect.X;
                double y = MenuRect.Y - esh;
                return new Rect(x, y, w, h);
            }
        }
        public Rect MenuRect =>
            Parent.MenuRect;

        public Rect InnerMenuRect {
            get {
                double w = MenuRect.Width - OptionsButtonRect.Width - BackButtonRect.Width;
                double h = MenuRect.Height;
                double x = BackButtonRect.Right;
                double y = 0;
                return new Rect(x, y, w, h);
            }
        }

        public Rect BackButtonRect {
            get {
                if(!IsBackButtonVisible) {
                    return new();
                }
                double x = 0;
                double y = 0;
                double w = MenuRect.Height;//MenuRect.Width * ButtonMenuWidthRatio;
                double h = w;// MenuRect.Height;
                return new Rect(x, y, w, h);
            }
        }
        public double BackButtonWidth =>
            BackButtonRect.Width;

        public Rect BackButtonImageRect {
            get {
                //double w = Math.Min(BackButtonRect.Width, BackButtonRect.Height) * ButtonImageSizeRatio;
                //double h = w;
                //double x = BackButtonRect.Left + ((BackButtonRect.Width - w) / 2d);
                //double y = (BackButtonRect.Height / 2) - (h / 2);
                //return new Rect(x, y, w, h);
                var rect = BackButtonRect;
                return rect.Inset(rect.Width / KeyConstants.PHI, rect.Height / KeyConstants.PHI);
            }
        }

        public Rect OptionsButtonRect {
            get {
                double w = MenuRect.Height; //MenuRect.Width * ButtonMenuWidthRatio;
                double h = w; //MenuRect.Height;
                double x = MenuRect.Right - w;
                double y = 0;//MenuRect.Top;
                return new Rect(x, y, w, h);
            }
        }

        public Rect OptionButtonImageRect {
            get {
                //double w = Math.Min(OptionsButtonRect.Width, OptionsButtonRect.Height) * ButtonImageSizeRatio;
                //double h = w;
                //double x = OptionsButtonRect.Left + ((OptionsButtonRect.Width - w) / 2d);
                //double y = (OptionsButtonRect.Height / 2) - (h / 2);
                //return new Rect(x, y, w, h);
                var rect = OptionsButtonRect;
                return rect.Inset(rect.Width / KeyConstants.PHI, rect.Height / KeyConstants.PHI);
            }
        }
        public double OptionsButtonWidth =>
            OptionsButtonRect.Width;

        #endregion

        #region State
        public override bool IsVisible =>
            true;// KeyboardViewModel.IsVisible;
        public (MenuItemType ownerType, int ownerIdx) TouchOwner { get; set; }
        string TouchId { get; set; }
        public MenuPageType CurMenuPageType { get; private set; } = MenuPageType.TabSelector;
        public MenuTabItemType SelectedTabItemType { get; private set; } = MenuTabItemType.None;
        public bool IsBackButtonVisible =>
            CurMenuPageType == MenuPageType.TextCompletions;
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events

        #endregion

        #region Constructors
        public MenuViewModel(KeyboardViewModel parent) {
            Parent = parent;
            TextAutoCompleteViewModel = new TextAutoCompleteViewModel(this);

            EmojiPagesViewModel = new EmojiPagesViewModel(this);
            SpeechPageViewModel = new SpeechViewModel(this);
            ClipboardPageViewModel = new ClipboardPageViewModel(this);
            PluginsPageViewModel = new PluginsPageViewModel(this);
            ConfigPageViewModel = new ConfigPageViewModel(this);

            MenuStripViewModel = new MenuStripViewModel(this);
        }
        #endregion

        #region Public Methods
        public void Init() {
            TextAutoCompleteViewModel.Init();

            ClipboardPageViewModel.Init();
            PluginsPageViewModel.Init();
            EmojiPagesViewModel.Init();

            MenuStripViewModel.Init(Pages);
            SetMenuPage(MenuPageType.TabSelector);
        }
        public void HandleCursorChange(SelectableTextRange textInfo) {
            // NOTE do completion AFTER shift state has been changed
            if(SpeechPageViewModel.IsVisible) {
                return;
            }

            if(TextAutoCompleteViewModel.IsVisible) {
                TextAutoCompleteViewModel.ShowCompletion(null, false);
            } else if(!EmojiPagesViewModel.IsVisible) {
                if(Parent.LastTextInfo != null && !textInfo.IsValueEqual(Parent.LastTextInfo)) {
                    // show text completions on any insert CHANGE not by manual trigger
                    SetMenuPage(MenuPageType.TextCompletions, SelectedTabItemType);
                }

            }
        }
        public void SetMenuPage(MenuPageType menuPageType, MenuTabItemType tabItemType = MenuTabItemType.None) {
            if(CurMenuPageType == menuPageType && SelectedTabItemType == tabItemType) {
                return;
            }
            var lastPageType = CurMenuPageType;
            var lastTabType = SelectedTabItemType;
            CurMenuPageType = menuPageType;
            SelectedTabItemType = tabItemType;

            switch(SelectedTabItemType) {
                case MenuTabItemType.Emoji:
                    if(!EmojiPagesViewModel.IsLoaded) {
                        EmojiPagesViewModel.Init();
                    }

                    if(CurMenuPageType != MenuPageType.TextCompletions) {
                        // NOT emoji search
                        EmojiPagesViewModel.ShowEmojiPages();
                    }

                    break;
                case MenuTabItemType.Speech:
                    SpeechPageViewModel.StartSpeech();
                    break;
                case MenuTabItemType.None:
                    if(!TextAutoCompleteViewModel.IsLoaded && CurMenuPageType == MenuPageType.TextCompletions) {
                        TextAutoCompleteViewModel.Init();
                    }
                    if(!EmojiPagesViewModel.EmojiSearchViewModel.IsVisible) {
                        EmojiPagesViewModel.HideEmojiPages();
                    }
                    break;
            }
            if(SelectedTabItemType != MenuTabItemType.Speech) {
                SpeechPageViewModel.StopSpeech();
            }
            KeyboardViewModel.Renderer.RenderFrame(true);

            if(EmojiPagesViewModel.EmojiSearchViewModel.IsVisible) {
                //EmojiPagesViewModel.EmojiSearchViewModel.Renderer.RenderFrame(true);
                //EmojiPagesViewModel.EmojiSearchViewModel.EmojiAutoCompleteViewModel.Renderer.RenderFrame(true);
            }
        }
        public bool HandleMenuTouch(TouchEventType touchType, Touch touch) {
            if(Pages.FirstOrDefault(x => x.HandleTouch(touchType, touch)) is { } handled_page) {
                return true;
            }

            if(touchType == TouchEventType.Press &&
                TouchId == null &&
                MenuHitRect.Contains(touch.Location)) {
                SetPressed(touch, true);

                if(ActiveCompl is { } ac_vm && ac_vm.IsCompletionAllowed) {
                    // check for compl hold (omit item)
                    ac_vm.Scroller.Update(touch, touchType);
                    InputConnection.MainThread.Post(async () => {
                        string touch_id = TouchId;
                        int item_idx = TouchOwner.ownerIdx;

                        await Task.Delay(1_000);
                        if(TouchId != touch_id ||
                            ac_vm.IsScrolling ||
                            TouchOwner.ownerType != ac_vm.CompletionType ||
                            TouchOwner.ownerIdx != item_idx) {
                            return;
                        }
                        ac_vm.SetOmit(item_idx);

                        SetPressed(touch, false);
                        Renderer.PaintFrame(true);
                    });
                }
                Renderer.PaintFrame(true);
                return true;
            }

            if(TouchId != touch.Id) {
                return false;
            }

            if(touchType == TouchEventType.Move) {
                if(ActiveCompl != null) {
                    ActiveCompl.Scroller.Update(touch, touchType);
                }
            } else if(touchType == TouchEventType.Release) {
                if(CanPerformAction(touch)) {
                    PerformMenuAction(TouchOwner, touch);
                }
                if(ActiveCompl is { } ac_vm) {
                    ActiveCompl.Scroller.Update(touch, touchType);
                }
                SetPressed(touch, false);
            }
            Renderer.RenderFrame(true);
            return true;
        }

        public void CloseEmojiSearch() {
            SetMenuPage(MenuPageType.TabSelector, MenuTabItemType.Emoji);
        }

        public void GoBack() {
            MenuPageType back_to_page = CurMenuPageType;
            MenuTabItemType back_to_item = MenuTabItemType.None;
            switch(CurMenuPageType) {
                case MenuPageType.TextCompletions:
                    if(SelectedTabItemType == MenuTabItemType.Emoji) {
                        back_to_page = MenuPageType.TabSelector;
                        //back_to_item = MenuTabItemType.Emoji;
                    } else {
                        back_to_page = MenuPageType.TabSelector;
                    }

                    break;
                case MenuPageType.TabSelector:
                    back_to_page = MenuPageType.TextCompletions;
                    break;
            }
            SetMenuPage(back_to_page, back_to_item);
        }

        public void ResetState() {
            TouchId = null;
            TouchOwner = default;
            TextAutoCompleteViewModel.ResetState();
            EmojiPagesViewModel.ResetState();
        }

        #endregion

        #region Private Methods

        #region Touch Actions
        bool CanPerformAction(Touch touch) {
            var release_owner = FindTouchOwner(touch);
            if(TouchOwner.ownerType != release_owner.ownerType ||
                TouchOwner.ownerIdx != release_owner.ownerIdx) {
                // release not over press or was a scroll
                return false;
            }
            if(ActiveCompl is { } ac_vm) {
                return ac_vm.CanPerformCompletionAction(touch);
            }
            return true;
        }
        void PerformMenuAction((MenuItemType, int) owner, Touch touch) {
            if(owner.Item1 != MenuItemType.EmojiCompletionItem) {
                // emoji does feedback in DoEmojiText
                InputConnection.OnFeedback(Parent.FeedbackClick);
            }


            switch(owner.Item1) {
                case MenuItemType.BackButton:
                    GoBack();
                    break;
                case MenuItemType.OptionsButton:
                    // since pref change event not received from pref activity mark IsDirty so next init resets
                    KeyboardViewModel.IsDirty = true;
                    InputConnection.OnShowPreferences(null);
                    break;
                case MenuItemType.TabItem:
                    var tab_item = (MenuTabItemType)owner.Item2;
                    if(SelectedTabItemType == tab_item) {
                        // already selected, toggle off
                        SetMenuPage(MenuPageType.TextCompletions);
                    } else {
                        SetMenuPage(MenuPageType.TabSelector, tab_item);
                    }

                    break;
                case MenuItemType.TextCompletionItem:
                    TextAutoCompleteViewModel.HandleCompletion(owner.Item2, touch);
                    break;
                case MenuItemType.EmojiCompletionItem:
                    EmojiAutoCompleteViewModel.HandleCompletion(owner.Item2, touch);
                    break;
            }
        }
        (MenuItemType ownerType, int ownerIdx) FindTouchOwner(Touch touch) {
            if(touch == null) {
                return default;
            }
            if(BackButtonRect.Contains(touch.Location)) {
                return (MenuItemType.BackButton, 0);
            }
            if(OptionsButtonRect.Contains(touch.Location)) {
                return (MenuItemType.OptionsButton, 0);
            }
            var eac_vm = EmojiPagesViewModel.EmojiSearchViewModel.EmojiAutoCompleteViewModel;
            var tac_vm = TextAutoCompleteViewModel;
            if(tac_vm.IsVisible && tac_vm.GetItemIdxUnderPoint(touch.Location) is { } text_idx) {
                return (MenuItemType.TextCompletionItem, text_idx);

            }
            if(eac_vm.IsVisible &&
                    eac_vm.GetItemIdxUnderPoint(touch.Location) is { } em_idx) {
                return (MenuItemType.EmojiCompletionItem, em_idx);
            }
            if(MenuStripViewModel.IsVisible &&
                MenuStripViewModel.GetMenuItemUnderPoint(touch.Location) is { } tab_item) {
                return (MenuItemType.TabItem, (int)tab_item.TabItemType);
            }
            return default;
        }
        void SetPressed(Touch touch, bool isPressed) {
            if(TouchOwner == default) {
                TouchOwner = FindTouchOwner(touch);
            }
            //string new_bg_color = isPressed ? MenuItemPressedBgHexColor : MenuBgHexColor;
            switch(TouchOwner.Item1) {
                case MenuItemType.BackButton:
                    //BackButtonBgHexColor = new_bg_color;
                    break;
                case MenuItemType.OptionsButton:
                    //OptionsButtonBgHexColor = new_bg_color;
                    break;
                case MenuItemType.TextCompletionItem:
                    TextAutoCompleteViewModel.SetPressed(touch, TouchOwner.ownerIdx, isPressed);
                    break;
                case MenuItemType.EmojiCompletionItem:
                    EmojiAutoCompleteViewModel.SetPressed(touch, TouchOwner.ownerIdx, isPressed);
                    break;
                case MenuItemType.TabItem:
                    if(MenuStripViewModel.Items.FirstOrDefault(x => x.TabItemType == (MenuTabItemType)TouchOwner.ownerIdx) is { } tab_item) {
                        tab_item.IsPressed = isPressed;
                    }
                    break;
            }
            TouchId = isPressed ? touch.Id : null;
            TouchOwner = isPressed ? TouchOwner : default;
        }

        #endregion

        #endregion

        #region Commands
        #endregion
    }
}
