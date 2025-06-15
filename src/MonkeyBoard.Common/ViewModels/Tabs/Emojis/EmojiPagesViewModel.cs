using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class EmojiPagesViewModel : MenuTabViewModelBase, IInertiaScroll {
        #region Private Variables
        object _emojiDataSetLock = new object();
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IInertiaScroll Implementation

        #endregion

        #region IKeyboardMenuTabItem Implementation
        #endregion
        #endregion

        #region Properties

        #region Members
        InertiaScrollerBase Scroller { get; set; }
        #endregion

        #region View Models
        public ObservableCollection<EmojiPageViewModel> EmojiPages { get; private set; } = [];
        public EmojiPageViewModel SelectedEmojiPage {
            get => EmojiPages.FirstOrDefault(x => x.IsSelected);
            set => EmojiPages.ForEach(x => x.IsSelected = x == value);
        }
        public EmojiFooterMenuViewModel EmojiFooterMenuViewModel { get; private set; }
        public EmojiSearchViewModel EmojiSearchViewModel =>
            EmojiFooterMenuViewModel.SearchBoxViewModel;

        #endregion

        #region Appearance
        protected override object TabIconSourceObj => "☺";
        public string EmojiPagesBgHexColor =>
            KeyboardViewModel.KeyGridBgHexColor;
        public string EmojiPagesFgHexColor =>
            KeyboardPalette.P[PaletteColorType.Fg];
        #endregion

        #region Layout
        public override Rect Frame => TotalRect;
        public int MaxColCount => 8;
        public int MaxVisibleRowCount => 5;

        public Rect TotalRect {
            get {
                double w = KeyboardViewModel.KeyboardWidth;
                double h = KeyboardViewModel.KeyGridHeight;
                double x = 0;
                double y = KeyboardViewModel.MenuHeight;
                return new Rect(x, y, w, h);
            }
        }
        public Rect PageClipRect {
            get {
                double x = 0;
                double y = 0;
                double w = TotalRect.Width;
                double h = TotalRect.Height - EmojiFooterMenuViewModel.EmojiFooterRect.Height;
                return new Rect(x, y, w, h);
            }
        }
        public double DefaultEmojiWidth =>
            PageClipRect.Width / MaxColCount;
        public double DefaultEmojiHeight =>
            PageClipRect.Height / MaxVisibleRowCount;

        public double EmojiFontSize =>
            KeyboardLayoutConstants.EmojiFontSize * KeyboardViewModel.FloatEmojiScale;

        #region Scroll
        InertiaScrollerBase CurTouchScroller { get; set; }
        public bool IsScrollingX =>
            Scroller.IsUserScrolling;
        public double ScrollOffsetX =>
            Scroller.ScrollOffset.X;
        public bool CanScroll =>
            EmojiPages.All(x => !x.IsScrollingY);
        #endregion

        #endregion

        #region State
        protected override MenuTabItemType TabItemType => MenuTabItemType.Emoji;
        public bool IsAnyPopupVisible =>
            SelectedEmojiPage != null && SelectedEmojiPage.IsAnyPopupVisible;
        EmojiPageType DefaultEmojiPage =>
            EmojiPageType.Smileys;
        DateTime LastRecentEmojiAddNtfDt { get; set; }
        DateTime? FirstRecentEmojiAddNtfDt { get; set; }
        int MinRecentEmojiChangeDelayMs => 3_000;
        public override bool IsVisible =>
            Parent != null &&
            EmojiSearchViewModel != null &&
            Parent.SelectedTabItemType == MenuTabItemType.Emoji &&
            !EmojiSearchViewModel.IsVisible;
        //public bool IsEmojiSetChanged { get; set; }
        int MaxRecentEmojiCount =>
            MaxColCount * (MaxVisibleRowCount + 1);
        #endregion

        #region Models
        public string DefaultSkinToneCodePoint { get; private set; } = string.Empty;
        public string DefaultHairStyleCodePoint { get; private set; } = string.Empty;

        #endregion

        #endregion

        #region Events
        public event EventHandler OnShowEmojiPages;
        public event EventHandler OnHideEmojiPages;

        public event EventHandler OnEmojisLoaded;

        public event EventHandler<EmojiKeyViewModel> OnShowEmojiPopup;
        public event EventHandler OnHideEmojiPopup;

        public event EventHandler<EmojiPageType> OnEmojiPageContentChanged;
        #endregion

        #region Constructors
        public EmojiPagesViewModel(MenuViewModel parent) : base(parent) {
            Scroller = InertiaScrollerBase.Create(this, InputConnection);
            EmojiFooterMenuViewModel = new EmojiFooterMenuViewModel(this);
        }
        #endregion

        #region Public Methods
        public void Init() {
            var EmojiData = CreateEmojiData();

            EmojiPages.Clear();
            foreach(var kvp in EmojiData) {
                var epvm = CreateEmojiPageViewModel(kvp.Key, kvp.Value, EmojiPages.Count);
                EmojiPages.Add(epvm);
            }

            Scroller.SetExtent(0, 0, EmojiPages.Max(x => x.ScrollRect.Right), 0);
            Scroller.SetViewport(TotalRect);
            Scroller.SetSnapItemSize(PageClipRect.Size.Width, 0);

            EmojiFooterMenuViewModel.Init();
            var def_page = EmojiPages.FirstOrDefault(x => x.EmojiPageType == DefaultEmojiPage);
            SelectPage(def_page);

            IsLoaded = true;
            OnEmojisLoaded?.Invoke(this, EventArgs.Empty);
        }
        public override bool HandleTouch(TouchEventType touchType, Touch touch) {
            if(EmojiFooterMenuViewModel.HandleTouch(touchType, touch)) {
                return true;
            }
            if(!IsVisible) {
                return false;
            }

            var last_sel_page = SelectedEmojiPage;
            bool handled = false;
            switch(touchType) {
                case TouchEventType.Press:
                    if(!TotalRect.Contains(touch.Location)) {
                        break;
                    }
                    if(SelectedEmojiPage is { } sel_pg_vm) {
                        handled = sel_pg_vm.HandleTouch(touchType, touch);
                    }
                    if(handled) {
                        TouchId = touch.Id;
                    }
                    break;
                default:
                    handled = TouchId == touch.Id;
                    break;

            }
            if(handled) {
                switch(touchType) {
                    case TouchEventType.Press:
                        Scroller.Update(touch, touchType);
                        SelectedEmojiPage.Scroller.Update(touch, touchType);
                        CurTouchScroller = SelectedEmojiPage.Scroller;
                        break;
                    case TouchEventType.Move:
                        if(touch.LocationHistory.Count < 5) {
                            break;
                        }
                        if(touch.LocationHistory.Count == 5) {
                            var t1 = touch.LocationHistory.First().Value;
                            var t2 = touch.LocationHistory.Last().Value;
                            if(t2.Distance(t1) > 5) {
                                var dir = t2.GetDirection(t1);
                                //MpConsole.WriteLine($"DIR: {dir}");
                                CurTouchScroller = dir == VectorDirection.Left || dir == VectorDirection.Right ? Scroller : SelectedEmojiPage.Scroller;
                            } else {
                                CurTouchScroller = SelectedEmojiPage.Scroller;
                            }
                        }
                        if(CurTouchScroller == Scroller && SelectedEmojiPage.PressedEmojiKeys.Any(x => x.IsPopupOpen)) {
                            // BUG sometimes it thinks it can scroll x w/ popup open
                            CurTouchScroller = SelectedEmojiPage.Scroller;
                        }
                        if(CurTouchScroller == Scroller) {
                            CurTouchScroller.Update(touch, touchType);
                            var OtherTouchScroller = CurTouchScroller == Scroller ? SelectedEmojiPage.Scroller : Scroller;
                            OtherTouchScroller.Cancel(true);
                        } else {
                            Scroller.Cancel(true);
                        }
                        // page needs to reset its state not just scroller
                        SelectedEmojiPage.HandleTouch(touchType, touch);
                        break;
                    case TouchEventType.Release:
                        if(CurTouchScroller == Scroller) {
                            CurTouchScroller.Cancel(false);
                            CheckAndDoPageTransition(touch);
                        } else {
                            SelectedEmojiPage.HandleTouch(touchType, touch);
                        }
                        CurTouchScroller = null;
                        //OtherTouchScroller = null;
                        TouchId = null;
                        break;
                }
            }
            this.Renderer.RenderFrame(true);
            return handled;
        }

        public void SelectPage(EmojiPageViewModel pvm) {
            EmojiPages.ForEach(x => x.IsSelected = x == pvm);
            double scroll_x = pvm == null ? 0 : pvm.PageRect.Left;
            Scroller.ForceOffset(scroll_x, 0);
        }
        public void ShowEmojiPages() {
            if(EmojiSearchViewModel.IsVisible) {
                EmojiSearchViewModel.HideSearchBox();
            }

            SelectPage(SelectedEmojiPage);
            OnShowEmojiPages?.Invoke(this, EventArgs.Empty);
        }
        public void HideEmojiPages() {
            //EmojiFooterMenuViewModel.SearchBoxViewModel.HideSearchBox();
            Renderer.RenderFrame(true);
            OnHideEmojiPages?.Invoke(this, EventArgs.Empty);
        }
        public void DoEmojiText(string text) {
            if(InputConnection is { } ic &&
                ic.MainThread is { } mt) {
                mt.Post(() => ic.OnText(text, true));
                ic.MainThread.Post(() => ic.OnFeedback(KeyboardViewModel.FeedbackClick));
                AddRecentEmoji(text);
            }
        }

        public void ShowHoldPopup(EmojiKeyViewModel evm, Touch touch) {
            evm.SetIsPopupOpen(true);
            InputConnection.MainThread.Post(() => OnShowEmojiPopup?.Invoke(this, evm));
        }
        public void HideHoldPopup(EmojiKeyViewModel evm) {
            evm.SetIsPopupOpen(false);
            InputConnection.MainThread.Post(() => OnHideEmojiPopup?.Invoke(this, EventArgs.Empty));
        }
        public void ResetState() {
            TouchId = null;
            EmojiSearchViewModel.ResetState();
            EmojiFooterMenuViewModel.ResetState();
        }
        public Emoji FindEmoji(string text) {
            if(EmojiPages
                .Where(x => x.EmojiPageType != EmojiPageType.Recents)
                .SelectMany(x => x.EmojiKeys)
                .FirstOrDefault(x => x.Items.Contains(text)) is not { } emvm) {
                return null;
            }
            return emvm.EmojiModel;
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        EmojiPageViewModel CreateEmojiPageViewModel(EmojiPageType ept, IEnumerable<Emoji> page_data, int idx) {
            return new EmojiPageViewModel(this, ept, page_data, idx);
        }
        Dictionary<EmojiPageType, List<Emoji>> CreateEmojiData() {
            if(InputConnection is not { } ic ||
                ic.SharedPrefService is not { } ps ||
                ic.TextTools is not { } tt ||
                ps.GetPrefValue<string>(PrefKeys.RECENT_EMOJIS_CSV) is not { } recent_emoji_csv ||
                recent_emoji_csv.SplitNoEmpty(",") is not { } recents) {
                if(OperatingSystem.IsIOS()) {
                    throw new Exception($"no emoji data! {(string.IsNullOrEmpty(InputConnection.SharedPrefService.GetPrefValue<string>(PrefKeys.RECENT_EMOJIS_CSV)) ? "No recent emoji csv" : string.Empty)} {(InputConnection.TextTools == null ? "no text tools" : string.Empty)}");
                }
                return new();
            }

            var lookup = new Dictionary<string, EmojiPageType>() {
                            {"Smileys and Emotion",EmojiPageType.Smileys},
                            {"People and Body",EmojiPageType.People},
                            {"Component",EmojiPageType.Component},
                            {"Animals and Nature",EmojiPageType.Animals},
                            {"Food and Drink",EmojiPageType.Food},
                            {"Travel and Places",EmojiPageType.World},
                            {"Activities",EmojiPageType.Activities},
                            {"Objects",EmojiPageType.Objects},
                            {"Symbols",EmojiPageType.Symbols},
                            {"Flags" ,EmojiPageType.Flags}
                        };
            // RECREATE MODEL
            var es = EmojiSet.ParseFromResx(ResourceStrings.E);

            // CREATE DATA LOOKUP
            Dictionary<EmojiPageType, List<Emoji>> emoji_data = new() {
                {EmojiPageType.Recents, recents.Select(x=>new Emoji(x)).ToList() }
            };
            var valid_emojis = es.Emojis
                .Where(x =>
                    x.QualificationType != EmojiQualificationType.Unqualified &&
                    x.QualificationType != EmojiQualificationType.Component &&
                    x.Parent.Parent.GroupName != "Component" &&
                    tt.CanRender(x.EmojiStr.SplitNoEmpty(",").First().ToStringOrEmpty()))
                .OrderBy(x => x.SortOrder);

            foreach(var em in valid_emojis) {
                if(!lookup.TryGetValue(em.Parent.Parent.GroupName, out var emoji_cat)) {
                    continue;
                }
                if(!emoji_data.ContainsKey(emoji_cat)) {
                    emoji_data.Add(emoji_cat, new());
                }
                emoji_data[emoji_cat].Add(em);
            }

            MpConsole.WriteLine($"Ignored emojis: {es.Emojis.Count() - emoji_data.Count}");

            // CREATE COMPONENT LOOKUP
            int skin_idx = (int)KeyboardViewModel.DefaultSkinToneType - 1;
            if(skin_idx < 0) {
                DefaultSkinToneCodePoint = string.Empty;
            } else {
                DefaultSkinToneCodePoint =
                    es.Emojis
                    .Where(x => x.Parent.SubGroupName.ToLower() == "skin tone")
                    .OrderBy(x => x.SortOrder)
                    .ElementAt(skin_idx)
                    .EmojiStr.ToCodePointStr();
            }
            MpConsole.WriteLine($"Default skin type: '{KeyboardViewModel.DefaultSkinToneType}' Code Point: '{DefaultSkinToneCodePoint}'");

            int hair_idx = (int)KeyboardViewModel.DefaultHairStyleType - 1;
            if(hair_idx < 0) {
                DefaultHairStyleCodePoint = string.Empty;
            } else {
                DefaultHairStyleCodePoint =
                    es.Emojis
                    .Where(x => x.Parent.SubGroupName.ToLower() == "hair style")
                    .OrderBy(x => x.SortOrder)
                    .ElementAt(hair_idx)
                    .EmojiStr.ToCodePointStr();
            }
            MpConsole.WriteLine($"Default hair style: '{KeyboardViewModel.DefaultHairStyleType}' Code Point: '{DefaultHairStyleCodePoint}'");
            return emoji_data;
        }
        void AddRecentEmoji(string text) {
            // NOTE/BUG setting pref is kinda slow and hitting a bunch of emojis will lock
            // up the ui
            if(EmojiPages.FirstOrDefault(x => x.EmojiPageType == EmojiPageType.Recents) is not { } recent_epvm) {
                return;
            }
            if(recent_epvm.EmojiKeys.FirstOrDefault(x => x.PrimaryValue == text) is { } match_evm) {
                recent_epvm.MoveEmoji(match_evm.PageItemIdx, 0);
            } else {
                recent_epvm.AddEmoji(new Emoji(text), 0);
            }


            Task.Run(async () => {

                var this_add_dt = DateTime.Now;
                LastRecentEmojiAddNtfDt = this_add_dt;

                if(FirstRecentEmojiAddNtfDt.HasValue) {
                    // another task is waiting for no new emojis
                    return;
                }
                // this is an initial change since last ntf
                FirstRecentEmojiAddNtfDt = this_add_dt;
                while(true) {
                    if(DateTime.Now - LastRecentEmojiAddNtfDt >= TimeSpan.FromMilliseconds(MinRecentEmojiChangeDelayMs)) {
                        // no new emojis for a little while, signal ntf
                        break;
                    }
                    await Task.Delay(100);
                }

                // set pref
                if(InputConnection is { } ic &&
                    ic.SharedPrefService is { } ps) {
                    ps.SetPrefValue(PrefKeys.RECENT_EMOJIS_CSV, string.Join(",", recent_epvm.EmojiKeys.Select(x => x.PrimaryValue)));
                }
                // send ntf

                // reset ntf delay
                FirstRecentEmojiAddNtfDt = null;
                OnEmojiPageContentChanged?.Invoke(this, EmojiPageType.Recents);
            });
        }

        void CheckAndDoPageTransition(Touch touch) {
            // NOTE only checked on release
            InputConnection.MainThread.Post(async () => {
                // > 0 is swiping left
                double x_diff = touch.Location.X - touch.PressLocation.X;
                EmojiPageViewModel trans_page = SelectedEmojiPage;
                if(Math.Abs(x_diff) >= (TotalRect.Width / 6)) {
                    // do transition
                    int trans_idx = Math.Clamp(SelectedEmojiPage.PageIdx + (x_diff > 0 ? -1 : 1), 0, EmojiPages.Count - 1);
                    trans_page = EmojiPages.ElementAt(trans_idx);
                }
                double scroll_start = ScrollOffsetX;
                double scroll_end = trans_page.PageRect.Left;
                await scroll_start.AnimateDoubleAsync(
                    end: scroll_end,
                    tts: 0.1d,
                    fps: 60,
                    (scroll_x) => {
                        Scroller.ForceOffset(scroll_x, 0);
                        return false;
                    });
                SelectPage(trans_page);
            });
        }



        #endregion

        #region Commands
        #endregion
    }
}
