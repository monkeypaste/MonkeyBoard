using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CoreAnimation;
using CoreGraphics;
using CoreText;
using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosEmojiPagesView : FrameViewBase {
        #region Private Variables
        #endregion

        #region Constants
        public const string EMOJI_FONT_FAMILY_NAME = "AppleColorEmoji";
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            //this.Hidden = !DC.IsVisible;
            base.LayoutFrame(invalidate);
        }

        public override void MeasureFrame(bool invalidate) {
            if (!DC.IsVisible) {
                return;
            }
            Frame = DC.TotalRect.ToCGRect();
            base.MeasureFrame(invalidate);
        }

        #endregion

        #endregion

        #region Properties

        #region View Models
        public new EmojiPagesViewModel DC { get; set; }
        #endregion

        #region State
        bool IsSearchOpen { get; set; }
        bool IsSearchOpening { get; set; }
        #endregion

        #region Views
        iosEmojiPopupView PopupView { get; set; }
        iosEmojiSearchTextView SearchInputView { get; set; }
        iosAutoCompleteView EmojiCompletionView { get; set; }
        #endregion

        #endregion

        #region Constructors
        public iosEmojiPagesView(EmojiPagesViewModel dc) {
            DC = dc;
            DC.SetRenderContext(this);

            CreateSearch();

            DC.OnShowEmojiPopup += DC_OnShowEmojiPopup;
            DC.OnHideEmojiPopup += DC_OnHideEmojiPopup;

            DC.OnEmojisLoaded += DC_OnEmojisLoaded;

            DC.OnShowEmojiPages += EmojiPagesViewModel_OnShowEmojiPages;
            DC.OnHideEmojiPages += EmojiPagesViewModel_OnHideEmojiPages;

            DC.EmojiSearchViewModel.OnShowEmojiSearch += EmojiSearchViewModel_OnShowEmojiSearch;
            DC.EmojiSearchViewModel.OnHideEmojiSearch += EmojiSearchViewModel_OnHideEmojiSearch;
        }

        private void DC_OnEmojisLoaded(object sender, EventArgs e) {
            ResetRenderer();
        }
        #endregion

        #region Public Methods
        public void ResetRenderer() {
            ClearPopups();
            //if(DC == null) {
            //    return;
            //}
            //IFrameRenderer renderer = IsDisposed ? null : this;
            //DC.SetRenderContext(renderer);
            //DC.EmojiFooterMenuViewModel.SetRenderContext(renderer);
            //DC.EmojiFooterMenuViewModel.Items.ForEach(x => x.SetRenderContext(renderer));
            //DC.EmojiPages.ForEach(x => x.SetRenderContext(renderer));

            //DC.EmojiPages.SelectMany(x => x.EmojiKeys).ForEach(x => x.SetRenderContext(renderer));

        }

        public void Unload() {
            ResetRenderer();

            if(DC == null) {
                return;
            }
            DC.OnShowEmojiPopup -= DC_OnShowEmojiPopup;
            DC.OnHideEmojiPopup -= DC_OnHideEmojiPopup;

            DC.OnEmojisLoaded -= DC_OnEmojisLoaded;

            DC.EmojiSearchViewModel.OnShowEmojiSearch -= EmojiSearchViewModel_OnShowEmojiSearch;
            DC.EmojiSearchViewModel.OnHideEmojiSearch -= EmojiSearchViewModel_OnHideEmojiSearch;
        }
        public override void Draw(CGRect rect) {
            if (!DC.IsVisible || (PopupView != null && !PopupView.Hidden)) {
                return;
            }

            var context = UIGraphics.GetCurrentContext();

            var clip_rect = DC.PageClipRect.ToCGRect();
            var footer_rect = DC.EmojiFooterMenuViewModel.EmojiFooterRect.ToCGRect();
            nfloat scroll_y = DC.SelectedEmojiPage == null ? 0 : DC.SelectedEmojiPage.ScrollOffsetY.UnscaledF();

            context.SetFillColor(DC.EmojiPagesBgHexColor.ToUIColor().CGColor);
            context.FillRect(Bounds);

            // emoji pressed bg bg_rect
            if (DC.SelectedEmojiPage != null && DC.SelectedEmojiPage.PressedEmojiKeys.FirstOrDefault() is { } pressed_evm) {
                var evm_rect = pressed_evm.EmojiRect.ToCGRect();
                evm_rect = evm_rect.Move(0, -scroll_y);
                context.SetFillColor(pressed_evm.EmojiBgHexColor.ToUIColor().CGColor);
                context.FillRect(evm_rect);
            }
            // BUG emojis draw behind footer when semi-transparent
            var adj_clip_rect = clip_rect.Resize(clip_rect.Width, clip_rect.Height - footer_rect.Height);
            var visible_pages = DC.EmojiPages.Where(x => x.IsVisible);
            foreach (var visible_page in visible_pages) {
                var scroll_rect = visible_page.ScrollRect.ToCGRect();
                foreach (var evm in visible_page.EmojiKeys) {
                    var evm_rect = evm.EmojiRect.ToCGRect().Move(scroll_rect.X, -scroll_y);
                    if (!adj_clip_rect.IntersectsWith(evm_rect)) {
                        // clipped;
                        continue;
                    }
                    if (evm.HasPopup) {
                        var p = evm.PopupHintTrianglePoints
                            .Select(x => x.ToCGPoint())
                            .Select(x => new CGPoint(x.X + evm_rect.Left, x.Y + evm_rect.Top))
                            .ToArray();

                        context.MoveTo(p[0].X, p[0].Y);
                        context.AddLineToPoint(p[1].X, p[1].Y);
                        context.AddLineToPoint(p[2].X, p[2].Y);
                        context.AddLineToPoint(p[0].X, p[0].Y);
                        context.ClosePath();
                        context.SetFillColor(evm.PopupHintBgHexColor.ToUIColor().CGColor);
                        context.DrawPath(CGPathDrawingMode.Fill);
                    }

                    context.DrawText(
                        evm_rect,
                        evm.PrimaryValue,
                        evm.EmojiFontSize.UnscaledF(),
                        EMOJI_FONT_FAMILY_NAME,
                        evm.EmojiFgHexColor.ToUIColor()
                        );
                }
            }

            // footer bg
            context.SetFillColor(DC.EmojiFooterMenuViewModel.EmojiFooterMenuBgHexColor.ToUIColor().CGColor);
            context.FillRect(footer_rect);

            CGColor fg_color = DC.EmojiFooterMenuViewModel.EmojiFooterMenuFgHexColor.ToUIColor().CGColor;

            // footer item bg
            int actual_count = Math.Min(DC.EmojiFooterMenuViewModel.MenuItemCount, DC.EmojiFooterMenuViewModel.Items.Count);
            for (int i = 0; i < actual_count; i++) {
                var evm = DC.EmojiFooterMenuViewModel.Items[i];
                if (evm.MenuItemBgHexColor == null) {
                    continue;
                }
                var bg_rect = DC.EmojiFooterMenuViewModel.EmojiFooterItemRects[i].ToCGRect().Move(0, footer_rect.Top);
                context.SetFillColor(evm.MenuItemBgHexColor.ToUIColor().CGColor);
                context.FillRect(bg_rect);

            }
            // footer selection bg
            context.SetFillColor(DC.EmojiFooterMenuViewModel.SelectedMenuItemBgHexColor.ToUIColor().CGColor);
            var sel_rect = DC.EmojiFooterMenuViewModel.SelectionRect.ToCGRect();
            context.FillRect(sel_rect);

            // footer item fg
            for (int i = 0; i < actual_count; i++) {
                var evm = DC.EmojiFooterMenuViewModel.Items[i];
                if (evm.IconSourceObj is string text) {
                    var fg_rect = DC.EmojiFooterMenuViewModel.EmojiFooterItemRects[i].ToCGRect().Move(0, footer_rect.Top);
                    context.DrawText(
                        fg_rect,
                        text,
                        DC.EmojiFontSize.UnscaledF(),
                        EMOJI_FONT_FAMILY_NAME,
                        evm.MenuItemFgHexColor.ToUIColor());
                }
            }
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        #region Pages
        private void EmojiPagesViewModel_OnShowEmojiPages(object sender, EventArgs e) {
            //Handler.Post(() => {
                iosHelpers.DoGC();
                RenderFrame(true);
           // });
        }

        private void EmojiPagesViewModel_OnHideEmojiPages(object sender, EventArgs e) {
           //Handler.Post(() => {
               iosHelpers.DoGC();
               RenderFrame(true);
            //});
        }
        #endregion

        #region Search
        private void EmojiSearchViewModel_OnShowEmojiSearch(object sender, EventArgs e) {
            //MpConsole.WriteLine($"Emoji search show");
            //Handler.Post(()=> ShowSearch());
            iosHelpers.DoGC();
            ShowSearch();
        }
        private void EmojiSearchViewModel_OnHideEmojiSearch(object sender, EventArgs e) {
            //MpConsole.WriteLine($"Emoji search hide");
            //Handler.Post(()=> HideSearch());
            iosHelpers.DoGC();
            HideSearch();
        }
        int show_count = 0;
        public void CreateSearch() {
            if (iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardView is not { } kbv ||
                kbvc.KeyboardContainerView is not { } kbcv) {
                return;
            }
            var emsvm = DC.EmojiSearchViewModel;

            SearchInputView = new iosEmojiSearchTextView(emsvm).SetDefaultProps();

            EmojiCompletionView = new iosAutoCompleteView(emsvm.EmojiAutoCompleteViewModel).SetDefaultProps();
            EmojiCompletionView.Layer.ZPosition = 10;
            EmojiCompletionView.ContentMode = UIViewContentMode.Top;

            //if (kbcv.Subviews.OfType<iosEmojiSearchTextView>().FirstOrDefault() is { } other_et) {
            //    other_et.RemoveFromSuperview();
            //}
            kbcv.InsertArrangedSubview(SearchInputView, 0);
            //if (kbcv.Subviews.OfType<iosAutoCompleteView>().FirstOrDefault() is { } other_ac) {
            //    other_ac.RemoveFromSuperview();
            //}
            kbcv.InsertArrangedSubview(EmojiCompletionView, 0);

        }
        public void SetShowCount(int count) {
            show_count = count;
            iosFooterView.SetLabel($"Show count set to: {show_count}", true);
        }
        void ShowSearch() {
            if (IsSearchOpen ||
                IsSearchOpening ||
                iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardView is not { } kbv ||
                kbvc.KeyboardContainerView is not { } kbcv) {
                HideSearch();
                return;
            }
            IsSearchOpening = true;
            iosFooterView.SetLabel($"Show count: {show_count}",true);

            RenderFrame(false);
            SearchInputView.DoConstraints();
            EmojiCompletionView.DoConstraints();
            kbcv.AdjustHeight(EmojiCompletionView.Frame.Height + SearchInputView.Frame.Height);

            SearchInputView.Redraw();

            iosKeyboardViewController.Instance.SetInnerTextField(SearchInputView);
            Task.Run(() => DC.EmojiSearchViewModel.EmojiAutoCompleteViewModel.ShowCompletion(kbvc.CurTextInfo, true));
            iosHelpers.DoGC();
            show_count++;

            IsSearchOpening = false;
            IsSearchOpen = true;
        }
        void HideSearch() {
            nfloat dh = 0;
            dh += EmojiCompletionView.Frame.Height;
            dh += SearchInputView.Frame.Height;

            if (iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardView is not { } kbv ||
                kbvc.KeyboardContainerView is not { } kbcv) {
                return;
            }
            kbcv.AdjustHeight(-dh);
            kbvc.SetInnerTextField(null);
            kbcv.ActivateConstraints();

            IsSearchOpen = false;
        }

        #endregion

        #region Popups
        void ClearPopups() {
            if(PopupView == null) {
                return;
            }
            if(PopupView.TagObj is EmojiKeyViewModel emkvm) {
                emkvm.SetRenderContext(this);
            }
            PopupView.Hidden = true;
        }
        private void DC_OnHideEmojiPopup(object sender, System.EventArgs e) {
            ClearPopups();
            this.Redraw();
            iosHelpers.DoGC();
        }

        private void DC_OnShowEmojiPopup(object sender, EmojiKeyViewModel e) {
            if (e is not { } emvm) {
                return;
            }
            if(PopupView == null) {
                PopupView = new iosEmojiPopupView(emvm).SetDefaultProps();
                this.AddSubview(PopupView);
            } else {                
                PopupView.Init(emvm);
                PopupView.Hidden = false;
            }

            PopupView.TagObj = emvm;
            PopupView.RenderFrame(true);
            iosHelpers.DoGC();
        }
        #endregion

        #endregion
    }
}