using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using CoreText;
using Foundation;
using HarfBuzzSharp;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UIKit;
using static System.Net.Mime.MediaTypeNames;

namespace MonkeyBoard.iOS.KeyboardExt {

    public class iosKeyView : FrameViewBase {
        #region Private Variables

        #endregion

        #region Constants
        #endregion

        #region Statics
        static Dictionary<string, UIImage> ImageLookup { get; set; } = [];

        #endregion

        #region Interfaces

        #region IkeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            this.Hidden = !DC.IsVisible;

            base.LayoutFrame(invalidate);
        }
        public override void MeasureFrame(bool invalidate) {
            if(IsInnerView) {
                this.Frame = DC.InnerRect.ToBounds().ToCGRect();
            } else {
                this.Frame = DC.InnerRect.ToCGRect();
            }            

            if (!DC.IsPopupKey &&
                (LastCornerRadius.TopLeft != DC.CornerRadius.TopLeft ||
                LastCornerRadius.TopRight != DC.CornerRadius.TopRight ||
                LastCornerRadius.BottomRight != DC.CornerRadius.BottomRight ||
                LastCornerRadius.BottomLeft != DC.CornerRadius.BottomLeft)) {
                this.RoundCorners(DC.CornerRadius);
                LastCornerRadius = DC.CornerRadius;
            }
            if(DC.IsPrimaryImage && !ImageLookup.ContainsKey(DC.CurrentChar)) {
                if(!KeyViewModel.IMG_FILE_NAMES.Contains(DC.CurrentChar)) {
                    iosKeyboardViewController.SetError($"Error! {DC.CurrentChar} is not a known image!");
                    return;
                }

                var img_rect = DC.PrimaryImageRect.ToCGRect();
                var img = iosHelpers.LoadBitmap(DC.CurrentChar, img_rect.Size);
                ImageLookup.AddOrReplace(DC.CurrentChar, img);
            }
            base.MeasureFrame(invalidate);
        }

        public override void PaintFrame(bool invalidate) {
            if (!DC.IsVisible) {
                return;
            }
            DC.SetBrushes();
            this.BackgroundColor = DC.BgHex.ToUIColor();
            if(DC.CanHaveShadow && !IsInnerView) {
                if(DC.KeyboardViewModel.IsShadowsEnabled) {
                    // from https://stackoverflow.com/a/6949257/105028
                    if (this.Subviews.OfType<iosKeyView>().FirstOrDefault() is not { } inner_view) {
                        inner_view = new iosKeyView(DC) {
                            IsInnerView = true
                        };
                        HasInnerView = true;
                        this.AddSubview(inner_view);

                    }
                    inner_view.BackgroundColor = this.BackgroundColor;
                    inner_view.Frame = this.Bounds;
                    inner_view.RoundCorners(DC.CornerRadius);
                    inner_view.Layer.MasksToBounds = true;


                    this.Layer.ShadowColor = KeyboardPalette.P[PaletteColorType.KeyShadowBg].ToUIColor().CGColor;
                    this.Layer.ShadowOpacity = 1f;
                    this.Layer.ShadowOffset = DC.KeyboardViewModel.ShadowOffset.ToCGPoint().ToCGSize();
                    this.Layer.ShadowRadius = (this.Layer.ShadowOffset.Width + this.Layer.ShadowOffset.Height) / 2f;
                    this.Layer.MasksToBounds = false;

                    this.BackgroundColor = UIColor.Clear;
                } else {
                    if (this.Subviews.OfType<iosKeyView>().FirstOrDefault() is { } inner_view) {
                        inner_view.RemoveFromSuperview();
                        HasInnerView = false;
                        this.Layer.ShadowColor = UIColor.Clear.CGColor;
                        this.Layer.ShadowOpacity = 0f;
                        this.Layer.MasksToBounds = true;
                    }

                }

            }
            base.PaintFrame(invalidate);
        }
        #endregion
        #endregion

        #region Properties
        public bool IsInnerView { get; set; }
        public bool HasInnerView { get; set; }

        UIImageView KeyImageView { get; set; }
        public new KeyViewModel DC { get; private set; }

        CornerRadius LastCornerRadius { get; set; }
        #endregion

        #region Constructors
        public iosKeyView(KeyViewModel kvm) : base() {
            Init(kvm);
            //InitImages();
            KeyImageView = new UIImageView();
            KeyImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
            this.AddSubview(KeyImageView);
        }
        #endregion

        #region Public Methods
        public void Init(KeyViewModel dc) {
            DC = dc;
            if (!IsInnerView) {
                dc.SetRenderContext(this);
            }

            if (!DC.IsPopupKey) {
                this.RoundCorners(DC.CornerRadius);
                LastCornerRadius = DC.CornerRadius;
            }
        }
        public override void Draw(CGRect rect) {
            if (!DC.IsVisible || HasInnerView) {
                return;
            }
            //if (DC.IsPopupKey) {
            //    DC.SetBrushes();
            //}
            var context = UIGraphics.GetCurrentContext();
            //this.BackgroundColor = DC.BgHex.ToUIColor();

            if (DC.IsPrimaryImage && ImageLookup.TryGetValue(DC.CurrentChar, out var img)) {
                var img_rect = DC.PrimaryImageRect.ToCGRect();

                KeyImageView.Frame = img_rect;
                KeyImageView.Hidden = false;

                KeyImageView.Image = img.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
                KeyImageView.TintColor = DC.PrimaryHex.ToUIColor();
                return;
            }

            KeyImageView.Hidden = true;
            if(!string.IsNullOrEmpty(DC.PrimaryValue) && !DC.IsPulling) {
                context.DrawText(
                    rect,
                    DC.PrimaryValue,
                    DC.PrimaryFontSize.UnscaledF(),
                    iosKeyboardView.DEFAULT_FONT_FAMILY,
                    DC.PrimaryHex.ToUIColor(),
                    DC.PrimaryTextHorizontalAlignment.ToIosAlignment(),
                    DC.PrimaryTextVerticalAlignment.ToIosAlignment(),
                    DC.PrimaryTextOffset.ToCGPoint());
            }

            if(DC.IsSecondaryVisible) {
                context.DrawText(
                    rect,
                    DC.SecondaryValue,
                    DC.SecondaryFontSize.UnscaledF(),
                    iosKeyboardView.DEFAULT_FONT_FAMILY,
                    DC.SecondaryHex.ToUIColor(),
                    DC.SecondaryTextHorizontalAlignment.ToIosAlignment(),
                    DC.SecondaryTextVerticalAlignment.ToIosAlignment(),
                    DC.SecondaryTextOffset.ToCGPoint());
            }
            //base.Draw(rect);
        }
        #endregion

        #region Private Methods
        void InitImages() {
            if (ImageLookup.Any()) {
                // already loaded
                return;
            }
            ImageLookup = KeyViewModel.IMG_FILE_NAMES.ToDictionary(x => x, x => iosHelpers.LoadBitmap(x));
        }
        #endregion
    }
}