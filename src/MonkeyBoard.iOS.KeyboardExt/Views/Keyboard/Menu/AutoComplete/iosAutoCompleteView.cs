
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CoreGraphics;
using CoreText;
using Foundation;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard.Common;
using System;
using System.Drawing;
using System.Linq;
using UIKit;
using AvRect = Avalonia.Rect;
namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosAutoCompleteView : FrameViewBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            this.Hidden = !DC.IsVisible;
            base.LayoutFrame(invalidate);
        }
        public override void MeasureFrame(bool invalidate) {
            //if(!DC.IsVisible) {
            //    return;
            //}
            Frame = DC.AutoCompleteRect.ToCGRect().Move(0, DC.FrameOffsetY.UnscaledF());
            base.MeasureFrame(invalidate);
        }

        #endregion

        #endregion

        #region Properties

        #region View Models
        public new AutoCompleteViewModelBase DC { get; set; }
        #endregion

        #region Views
        UIImageView CancelOmitButtonImgView { get; set; }
        UIImageView ConfirmOmitButtonImgView { get; set; }
        UIImageView CloseButtonImgView { get; set; }

        #endregion

        #region Appearance
        #endregion

        #region Layout
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors

        public iosAutoCompleteView(AutoCompleteViewModelBase dc) {
            DC = dc;
            ResetRenderer();

            var btnSize = new AvRect(0, 0, DC.OmitButtonWidth, DC.OmitButtonWidth).ToCGRect().Size;
            CancelOmitButtonImgView = new UIImageView();
            CancelOmitButtonImgView.Image = iosHelpers.LoadBitmap(DC.OmitCancelIconSourceObj.ToString(), btnSize).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            this.AddSubview(CancelOmitButtonImgView);

            ConfirmOmitButtonImgView = new UIImageView();
            ConfirmOmitButtonImgView.Image = iosHelpers.LoadBitmap(DC.OmitConfirmIconSourceObj.ToString(), btnSize).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            this.AddSubview(ConfirmOmitButtonImgView);

            if(DC is EmojiAutoCompleteViewModel emacvm &&
                emacvm.Parent is { } emsvm) {
                CloseButtonImgView = new UIImageView();
                CloseButtonImgView.Frame = emsvm.CloseButtonImageRect.ToCGRect();
                CloseButtonImgView.TintColor = emsvm.CloseButtonFgHexColor.ToUIColor();
                CloseButtonImgView.Image = iosHelpers.LoadBitmap(emsvm.CloseButtonIconSourceObj.ToString(), CloseButtonImgView.Frame.Size).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
                this.AddSubview(CloseButtonImgView);
            }
        }

        #endregion

        #region Public Methods
        public void DoConstraints() {
            if(DC is not EmojiAutoCompleteViewModel) {
                throw new Exception("Only emoji does constraints");
            }
            if(iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardContainerView is not { } kbcv ||
                kbvc.KeyboardView is not { } kbv) {
                return;
            }
            MeasureFrame(false);
            float priority = 500;
            this.ClearConstraints();
            NSLayoutConstraint.ActivateConstraints([
                this.TopAnchor.ConstraintEqualTo(kbcv.TopAnchor).WithPriority(priority),
                this.WidthAnchor.ConstraintEqualTo(this.Frame.Width).WithPriority(priority),
                this.HeightAnchor.ConstraintEqualTo(this.Frame.Height).WithPriority(priority),
                ]);
            this.NeedsUpdateConstraints();
        }
        public void ResetRenderer() {
            DC.SetRenderContext(this);
        }
        public override void Draw(CGRect rect) {
            if(!DC.IsVisible) {
                return;
            }
            var context = UIGraphics.GetCurrentContext();

            bool needs_restore = false;
            nfloat offset_x = 0;
            if(DC is EmojiAutoCompleteViewModel emacvm &&
                emacvm.Parent is { } esvm) {
                // only draw bg for emoji since iosMenuView draws text bg
                context.SetFillColor(DC.AutoCompleteBgHexColor.ToUIColor().CGColor);
                context.FillRect(Bounds);

                // close btn
                var close_btn_rect = esvm.CloseButtonRect.ToCGRect();
                if(esvm.CloseButtonBgHexColor is { } close_bg_hex) {
                    // close btn bg
                    context.SetFillColor(close_bg_hex.ToUIColor().CGColor);
                    context.FillRect(close_btn_rect.ToBounds());
                }
                // close btn fg
                CloseButtonImgView.TintColor = esvm.CloseButtonFgHexColor.ToUIColor();
                CloseButtonImgView.Frame = esvm.CloseButtonImageRect.ToCGRect();
                context.SaveState();
                offset_x = close_btn_rect.Width;
                context.TranslateCTM(offset_x, 0);
                context.ClipToRect(Bounds.Move(offset_x, 0));
                needs_restore = true;
            }

            var CompletionBgColors = DC.CompletionItemBgHexColors.Select(x => x.ToUIColor()).ToArray();
            var CompletionFgColor = DC.FgHexColor.ToUIColor();
            var CompletionTexts = DC.CompletionDisplayValues.ToArray();

            var CompletionRects = DC.CompletionItemRects.Select(x => x.ToCGRect()).ToArray();
            var CompletionTextLocs = DC.CompletionItemTextLocs.Select(x => x.ToCGPoint()).ToArray();

            // bg
            //context.SetFillColor(DC.AutoCompleteBgHexColor.ToUIColor().CGColor);
            //context.FillRect(Bounds);

            // items
            int avail_count = Math.Min(CompletionTexts.Length, Math.Min(CompletionRects.Length, Math.Min(CompletionBgColors.Length, CompletionTextLocs.Length)));
            for(int i = 0; i < avail_count; i++) {
                var comp_item_rect = CompletionRects[i];
                if(comp_item_rect.Right < Bounds.Left ||
                    comp_item_rect.Left > Bounds.Right) {
                    // clipped
                    continue;
                }

                var comp_item_text = CompletionTexts[i];
                var comp_item_bg = CompletionBgColors[i];
                var comp_item_loc = CompletionTextLocs[i];
                // draw item bg
                if(comp_item_bg != null) {
                    context.SetFillColor(comp_item_bg.CGColor);
                    context.FillRect(comp_item_rect);
                }

                // draw item outline
                nfloat lw = 1f;//.UnscaledF();

                if(i > 0) {
                    // draw sep line on left 
                    nfloat lh = comp_item_rect.Height / (nfloat)KeyConstants.PHI;
                    nfloat lhhdiff = (comp_item_rect.Height - lh) / 2f;

                    nfloat l = comp_item_rect.Left + lw + 3;
                    nfloat t = comp_item_rect.Top + lhhdiff;
                    nfloat r = l + lw;
                    nfloat b = comp_item_rect.Bottom - lhhdiff;
                    var line_rect = new CGRect(l, t, r - l, b - t);
                    context.SetFillColor(DC.SeparatorHexColor.ToUIColor().CGColor);
                    context.FillRect(line_rect);
                }

                // omit button
                nfloat text_x = comp_item_loc.X;
                if(i == DC.OmittableItemIdx) {

                    // cancel
                    var cancel_omit_btn_rect = DC.CancelOmitButtonHitRect.ToCGRect();
                    CancelOmitButtonImgView.Frame = cancel_omit_btn_rect.Move(offset_x, 0);
                    CancelOmitButtonImgView.TintColor = DC.CancelOmitButtonFgHexColor.ToUIColor();

                    // confirm
                    var confirm_omit_btn_rect = DC.ConfirmOmitButtonHitRect.ToCGRect();
                    ConfirmOmitButtonImgView.Frame = confirm_omit_btn_rect.Move(offset_x, 0);
                    ConfirmOmitButtonImgView.TintColor = DC.ConfirmOmitButtonFgHexColor.ToUIColor();
                }

                nfloat fontSize = DC.GetCompletionTextFontSize(comp_item_text, i, out string formatted_text).UnscaledF();
                bool isOmit = DC.IsOmitButtonsVisible && i == DC.OmittableItemIdx && DC.IsOmitLabelVisible;
                DrawComplText(context, DC.FgHexColor, comp_item_rect, formatted_text, comp_item_loc, fontSize, isOmit);
            }
            bool hide_omit = DC.OmittableItemIdx < 0;
            CancelOmitButtonImgView.Hidden = hide_omit;
            ConfirmOmitButtonImgView.Hidden = hide_omit;

            if(needs_restore) {
                context.RestoreState();
            }
            //base.Draw(rect);
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        //nfloat? ascent;
        //nfloat? descent;
        void DrawComplText(
            CGContext context,
            string fgHexColor,
            CGRect comp_item_rect,
            string comp_item_text,
            CGPoint comp_item_loc,
            nfloat fontSize,
            bool isOmit) {
            //if(!ascent.HasValue || !descent.HasValue) {
            //    var font = new CTFont(iosKeyboardView.DEFAULT_FONT_FAMILY, fontSize);
            //    ascent = font.AscentMetric;
            //    descent = font.DescentMetric;
            //}
            //var lines = comp_item_text.SplitNoEmpty(Environment.NewLine);
            //nfloat tlh = descent.Value - ascent.Value;
            //nfloat tx = comp_item_loc.X;
            //nfloat ty = comp_item_loc.Y - ((lines.Length - 1) * (tlh / 2));
            //foreach (string line in lines) {
            //    // draw each line of text
            //    canvas.DrawText(line, tx, ty, SharedPaint);
            //    ty += tlh;
            //}
            if(isOmit) {
                context.DrawText_CT(
                comp_item_rect,
                comp_item_text,
                fontSize,
                iosKeyboardView.DEFAULT_FONT_FAMILY,
                fgHexColor.ToUIColor(),
                0, 0,
                isOmit);
            } else {
                context.DrawText(
                comp_item_rect,
                comp_item_text,
                fontSize,
                iosKeyboardView.DEFAULT_FONT_FAMILY,
                fgHexColor.ToUIColor(),
                italics: isOmit);
            }



        }
        #endregion

        #region Touch
        //public override void TouchesBegan(NSSet touches, UIEvent evt) {
        //    if (touches.FirstOrDefault() is not UITouch t ||
        //        iosKeyboardViewController.Instance is not { } kbvc ||
        //        kbvc.iosKeyboardView is not { } kbv ||
        //        kbvc is not IKeyboardInputConnection kic) {
        //        return;
        //    }
        //    kbv.TriggerTouchEvent(t.LocationInView(kbv), TouchEventType.Press);

        //}
        //public override void TouchesMoved(NSSet touches, UIEvent evt) {
        //    if (touches.FirstOrDefault() is not UITouch t ||
        //        iosKeyboardViewController.Instance is not { } kbvc ||
        //        kbvc.iosKeyboardView is not { } kbv ||
        //        kbvc is not IKeyboardInputConnection kic) {
        //        return;
        //    }
        //    kbv.TriggerTouchEvent(t.LocationInView(kbv), TouchEventType.Move);
        //}
        //public override void TouchesEnded(NSSet touches, UIEvent evt) {
        //    if (touches.FirstOrDefault() is not UITouch t ||
        //        iosKeyboardViewController.Instance is not { } kbvc ||
        //        kbvc.iosKeyboardView is not { } kbv ||
        //        kbvc is not IKeyboardInputConnection kic) {
        //        return;
        //    }
        //    kbv.TriggerTouchEvent(t.LocationInView(kbv), TouchEventType.Release);
        //}
        #endregion
    }
}