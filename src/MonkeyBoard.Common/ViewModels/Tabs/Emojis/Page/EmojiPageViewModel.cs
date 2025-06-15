using Avalonia;
using Avalonia.Rendering;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MonkeyBoard.Common {
    public class EmojiPageViewModel :
        FrameViewModelBase,
        IInertiaScroll {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IInertiaScroll Implementation
        bool IInertiaScroll.CanScroll => true;
        #endregion
        #endregion

        #region Properties

        #region Members
        public override IFrameRenderer Renderer =>
            Parent.Renderer;
        public InertiaScrollerBase Scroller { get; private set; }
        #endregion

        #region View Models
        public new EmojiPagesViewModel Parent { get; set; }
        public ObservableCollection<EmojiKeyViewModel> EmojiKeys { get; private set; } = [];
        public IOrderedEnumerable<EmojiKeyViewModel> SortedEmojiKeys =>
            EmojiKeys.OrderBy(x => x.PageItemIdx);
        public List<EmojiKeyViewModel> PressedEmojiKeys { get; set; } = [];
        #endregion

        #region Appearance

        public object IconResourceObj {
            get {
                switch(EmojiPageType) {
                    case EmojiPageType.Recents:
                        return "🕒";
                    default:
                        return EmojiKeys.FirstOrDefault().PrimaryValue;
                }
            }
        }

        #endregion

        #region Layout
        public override Rect Frame => PageRect;
        public Rect PageRect { get; private set; }

        public Rect ScrollRect {
            get {
                double w = PageRect.Width;
                double h = PageRect.Height;
                double x = PageRect.Left - Parent.ScrollOffsetX;
                double y = -ScrollOffsetY;
                return new Rect(x, y, w, h);
            }
        }

        public Rect ScrollClipRect {
            get {
                double w = PageRect.Width;
                double h = Parent.PageClipRect.Height;
                double x = PageRect.Left - Parent.ScrollOffsetX;
                double y = -ScrollOffsetY;
                return new Rect(x, y, w, h);
            }
        }

        #region Scroll
        public double ScrollOffsetY =>
            Scroller.ScrollOffset.Y;
        #endregion

        #endregion

        #region State
        public bool IsAnyPopupVisible =>
            PressedEmojiKeys.Any(x => x.IsPopupOpen);
        public int PageIdx { get; private set; }
        bool CanScroll =>
            !Parent.IsScrollingX && !IsAnyPopupVisible;
        public bool IsScrollingY =>
            Scroller.IsUserScrolling;
        public bool IsSelected { get; set; }
        public string TouchId { get; set; }

        public override bool IsVisible =>
            Parent.PageClipRect.Intersects(ScrollRect);
        #endregion

        #region Models
        public EmojiPageType EmojiPageType { get; private set; }

        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public EmojiPageViewModel(EmojiPagesViewModel parent, EmojiPageType pageType, IEnumerable<Emoji> page_data, int idx) {
            Parent = parent;
            Scroller = InertiaScrollerBase.Create(this, InputConnection);
            PageIdx = idx;
            EmojiPageType = pageType;
            Init(page_data);
        }


        #endregion

        #region Public Methods
        public bool HandleTouch(TouchEventType touchType, Touch touch) {
            // NOTE only called if touch inside emoji pages rect
            bool handled = false;
            switch(touchType) {
                case TouchEventType.Press:
                    if(TouchId != null) {
                        break;
                    }
                    handled = true;

                    TouchId = touch.Id;
                    if(GetEmojiUnderPoint(touch.Location) is { } sel_evm) {
                        SetKeyPressed(touch.Id, sel_evm, true);
                        if(sel_evm.HasPopup) {
                            InputConnection.MainThread.Post(async () => {
                                await Task.Delay(KeyboardViewModel.MinDelayForHoldPopupMs);
                                if(sel_evm.IsPressed &&
                                    sel_evm.TouchId == touch.Id &&
                                    !IsScrollingY &&
                                    !Scroller.IsCanceled &&
                                    !Parent.IsScrollingX) {
                                    Parent.ShowHoldPopup(sel_evm, touch);
                                }
                            });
                        }
                    }
                    break;
                default:
                    if(TouchId != touch.Id ||
                        Scroller.IsCanceled) {
                        PressedEmojiKeys.ToList().ForEach(x => SetKeyPressed(null, x, false));
                        TouchId = null;
                        handled = Scroller.IsCanceled;
                        break;
                    }
                    handled = true;
                    switch(touchType) {
                        case TouchEventType.Move:
                            if(PressedEmojiKeys.FirstOrDefault(x => x.IsPopupOpen) is { } hold_evm) {
                                hold_evm.UpdateActivePopup(touch);
                            }
                            break;
                        case TouchEventType.Release:
                            if(PressedEmojiKeys.FirstOrDefault(x => x.TouchId == touch.Id) is { } pressed_evm) {
                                if(CanPerformAction(touch)) {
                                    Parent.DoEmojiText(pressed_evm.PrimaryValue);
                                }

                                SetKeyPressed(null, pressed_evm, false);
                                if(pressed_evm.IsPopupOpen) {
                                    Parent.HideHoldPopup(pressed_evm);
                                }
                            }
                            TouchId = null;
                            break;
                    }
                    break;
            }
            if(handled && CanScroll) {
                Scroller.Update(touch, touchType);
            }
            return handled;
        }
        public void MoveEmoji(int fromIdx, int toIdx) {
            var ordered_ekvml = SortedEmojiKeys.ToList();
            if(ordered_ekvml.FirstOrDefault(x => x.PageItemIdx == fromIdx) is not { } match_evm) {
                return;
            }
            ordered_ekvml.Remove(match_evm);
            ordered_ekvml.Insert(toIdx, match_evm);
            ordered_ekvml.ForEach((x, idx) => x.PageItemIdx = idx);
        }
        public void AddEmoji(Emoji emoji, int idx) {
            // NOTE this doesn't notify renderer of add since only occurs in recent tab

            // shift items from idx up 1
            EmojiKeys.Where(x => x.PageItemIdx >= idx).ForEach(x => x.PageItemIdx++);
            // add emoji if valid
            var new_emvm = CreateEmojiKeyViewModel(emoji, idx);
            if(new_emvm.IsVisible) {
                EmojiKeys.Add(new_emvm);
            }
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        void MeasurePage() {
            double w = Parent.PageClipRect.Width;
            double h = Math.Max(EmojiKeys.MaxOrDefault(x => x.EmojiRect.Bottom), Parent.PageClipRect.Height);
            PageRect = new Rect(PageIdx * w, 0, w, h);

            Scroller.SetExtent(0, 0, 0, h - Parent.PageClipRect.Height);
            Scroller.SetViewport(Parent.PageClipRect);
        }
        void Init(IEnumerable<Emoji> page_data) {
            var ekl = new List<EmojiKeyViewModel>();
            foreach(var (em, idx) in page_data.WithIndex()) {
                var ekvm = CreateEmojiKeyViewModel(em, idx);
                if(!ekvm.IsVisible) {
                    // no supported parts
                    continue;
                }
                ekl.Add(ekvm);
            }
            EmojiKeys = new(ekl);
            MeasurePage();
        }

        EmojiKeyViewModel CreateEmojiKeyViewModel(Emoji emoji, int idx) {
            var evm = new EmojiKeyViewModel(this, emoji, idx);
            return evm;
        }
        EmojiKeyViewModel GetEmojiUnderPoint(Point scaledPoint) {
            var p = scaledPoint;//ToPagePoint(scaledPoint);
            var result = EmojiKeys
                .Where(x => x != null)
                .FirstOrDefault(x => x.TotalRect.Contains(p));
            return result;
        }
        bool CanPerformAction(Touch touch) {
            if(KeyboardViewModel.HandleSwipe(touch)) {
                return false;
            }
            if(PressedEmojiKeys.FirstOrDefault(x => x.TouchId == touch.Id) is not { } press_evm ||
                IsScrollingY ||
                Scroller.IsScrolling ||
                Parent.IsScrollingX) {
                return false;
            }
            return true;
        }
        void SetKeyPressed(string touchId, EmojiKeyViewModel evm, bool isPressed) {
            if(evm == null) {
                return;
            }
            evm.SetPressed(touchId, isPressed);
            if(evm.IsPressed) {
                if(!PressedEmojiKeys.Contains(evm)) {
                    PressedEmojiKeys.Add(evm);
                }
            } else {
                PressedEmojiKeys.Remove(evm);
            }
        }

        #endregion

        #region Commands
        #endregion
    }
}
