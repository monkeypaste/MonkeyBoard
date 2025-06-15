using Avalonia.Controls;
using CoreGraphics;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard.Common;
using System.Collections.Generic;
using System.Linq;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {

    public class iosMenuView : FrameViewBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer

        public override void MeasureFrame(bool invalidate) {
            Frame = DC.MenuRect.ToCGRect();
            base.MeasureFrame(invalidate);
        }

        #endregion

        #endregion

        #region Properties

        #region View Models
        public new MenuViewModel DC { get; set; }
        #endregion

        #region Views
        iosAutoCompleteView TextAutoCompleteView { get; set; }
        UIImageView BackImageView { get; set; }
        UIImageView OptionsImageView { get; set; }
        #endregion

        #endregion

        #region Constructors
        public iosMenuView(MenuViewModel dc) {
            DC = dc;
            var btnSize = DC.OptionButtonImageRect.ToCGRect().Size;
            BackImageView = new UIImageView();
            BackImageView.Image = iosHelpers.LoadBitmap(DC.BackIconSourceObj.ToStringOrEmpty(), btnSize).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            this.AddSubview(BackImageView);

            OptionsImageView = new UIImageView();
            OptionsImageView.Image = iosHelpers.LoadBitmap(DC.OptionsIconSourceObj.ToString(), btnSize).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            this.AddSubview(OptionsImageView);

            TextAutoCompleteView = new iosAutoCompleteView(DC.TextAutoCompleteViewModel);
            this.AddSubview(TextAutoCompleteView);
        }

        #endregion

        #region Public Methods
        public void ResetRenderer() {
            DC.SetRenderContext(this);
            DC.MenuStripViewModel.SetRenderContext(this);
            TextAutoCompleteView.ResetRenderer();
        }

        public override void Draw(CGRect rect) {
            var context = UIGraphics.GetCurrentContext();
            context.SetFillColor(DC.MenuBgHexColor.ToUIColor().CGColor);
            context.FillRect(rect);

            // press bg
            if(DC.TouchOwner != default) {
                if(DC.TouchOwner.ownerType == MenuItemType.BackButton) {
                    context.SetFillColor(DC.BackButtonBgHexColor.ToUIColor().CGColor);
                    context.FillRect(DC.BackButtonRect.ToCGRect());
                } else if(DC.TouchOwner.ownerType == MenuItemType.OptionsButton) {
                    context.SetFillColor(DC.OptionsButtonBgHexColor.ToUIColor().CGColor);
                    context.FillRect(DC.OptionsButtonRect.ToCGRect());
                }
            }
            if(DC.CurMenuPageType == MenuPageType.TabSelector && DC.MenuStripViewModel.IsVisible) {
                // tabs
                foreach(var tab_item in DC.MenuStripViewModel.Items) {
                    var tab_item_rect = tab_item.TabItemRect.ToCGRect();

                    // tab bg
                    if(tab_item.IsPressed) {
                        context.SetFillColor(DC.MenuStripViewModel.PressedTabItemBgHexColor.ToUIColor().CGColor);
                        context.FillRect(tab_item_rect);
                    } else if(tab_item.IsSelected) {
                        context.SetFillColor(DC.MenuStripViewModel.SelectedTabItemBgHexColor.ToUIColor().CGColor);
                        context.FillRect(tab_item_rect);
                    }

                    // tab fg
                    if(tab_item.IconSourceObj is string { } icon_text) {
                        // text fg
                        context.DrawText(
                            tab_item_rect,
                            icon_text,
                            DC.MenuStripViewModel.MenuItemFontSize.UnscaledF(),
                            iosEmojiPagesView.EMOJI_FONT_FAMILY_NAME,
                            DC.MenuFgHexColor.ToUIColor());
                    } else {
                        // image fg?
                    }
                }
            }
            if(DC.IsBackButtonVisible) {
                BackImageView.Frame = DC.BackButtonImageRect.ToCGRect();
                BackImageView.Hidden = false;
                BackImageView.TintColor = DC.MenuFgHexColor.ToUIColor();
            } else {
                BackImageView.Hidden = true;
            }

            OptionsImageView.Frame = DC.OptionButtonImageRect.ToCGRect();
            OptionsImageView.TintColor = DC.MenuFgHexColor.ToUIColor();

            //base.Draw(rect);
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion
    }
}