
using Avalonia;
using Avalonia.Controls;
using CoreGraphics;
using CoreText;
using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
   
    public class iosEmojiSearchTextView : FrameViewBase {//UITextField, IFrameRenderer {
        #region Private Variables
        bool? show_caret = null;
        #endregion

        #region Constants
        #endregion

        #region Statics
        public static bool SIMULATE_INTERACTION => false;
        #endregion

        #region Interfaces

        #region IFrameRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            this.Hidden = !DC.IsVisible;
            ClearTextImageView.Hidden = this.Hidden;
            base.LayoutFrame(invalidate);
        }
        public override void MeasureFrame(bool invalidate) {
            //if(!DC.IsVisible) {
            //    return;
            //}
            this.Frame = DC.SearchBoxRect.ToCGRect();

            base.MeasureFrame(invalidate);
        }
        #endregion

        #endregion

        #region Properties

        #region Members
        public SelectableTextRange Range {
            get {
                return new SelectableTextRange(this.Text, this.Text.Length, 0);
            }
        }
        #endregion

        #region Views
        UIImageView ClearTextImageView { get; set; }

        #endregion

        #region View Models
        public new EmojiSearchViewModel DC { get; private set; }
        #endregion

        #region Appearance
        #endregion

        #region Layout
        #endregion

        #region State

        public string Text {
            get => DC.SearchText;
            set {
                if(DC.SearchText != value) {
                    DC.SetSearchText(value);
                    this.Redraw();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        public event EventHandler SelectionChanged;
        #endregion

        #region Constructors
        public iosEmojiSearchTextView(EmojiSearchViewModel dc) {
            DC = dc;
            this.MeasureFrame(false);
            DC.SetRenderContext(this);
            ClearTextImageView = new UIImageView();
            ClearTextImageView.Hidden = false;
            ClearTextImageView.Frame = DC.ClearTextButtonRect.ToCGRect();
            ClearTextImageView.Frame = ClearTextImageView.Frame.Place(ClearTextImageView.Frame.X,(this.Frame.Height/2)-(ClearTextImageView.Frame.Height/2));
            ClearTextImageView.Image = iosHelpers.LoadBitmap(DC.ClearTextButtonIconSourceObj.ToStringOrEmpty(), ClearTextImageView.Frame.Size).ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            this.AddSubview(ClearTextImageView);
        }

        #endregion

        #region Public Methods
        public void DoConstraints() {
            if(iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardContainerView is not { } kbcv ||
                kbvc.KeyboardView is not { } kbv ||
                kbcv.Subviews.OfType<iosAutoCompleteView>().FirstOrDefault() is not { } acv) {
                return;
            }
            MeasureFrame(false);
            float priority = 500;
            this.ClearConstraints();
            NSLayoutConstraint.ActivateConstraints([
                this.TopAnchor.ConstraintEqualTo(acv.BottomAnchor).WithPriority(priority),
                this.BottomAnchor.ConstraintEqualTo(kbv.TopAnchor).WithPriority(priority),
                this.WidthAnchor.ConstraintEqualTo(this.Frame.Width).WithPriority(priority),
                this.HeightAnchor.ConstraintEqualTo(this.Frame.Height).WithPriority(priority),
                ]);
            this.NeedsUpdateConstraints();
        }

        public override void Draw(CGRect rect) {
            var context = UIGraphics.GetCurrentContext();
            if(!DC.IsVisible) {
                ClearTextImageView.Hidden = true;
                context.SetFillColor(UIColor.Clear.CGColor);
                context.FillRect(Bounds);
                base.Draw(rect);
                return;
            }
            context.SetFillColor(DC.EmojiSearchBoxBgHexColor.ToUIColor().CGColor);
            context.FillRect(Bounds);

            //if (show_caret == null) {
            //    StartCaretLoop();
            //}
            //bool is_caret = Range.SelectionStartIdx == Range.SelectionEndIdx;
            //var caret_rect = GetCaretRect();
            //if (show_caret is true || !is_caret) {
            //    DrawSel(context, caret_rect);
            //}

            if (string.IsNullOrEmpty(Text)) {
                ClearTextImageView.Hidden = true;
                context.DrawText(
                    rect,
                    DC.PlaceholderText,
                    DC.SearchBoxFontSize.UnscaledF(),
                    "Helvetica",
                    DC.EmojiSearchBoxPlaceholderFgHexColor.ToUIColor(),
                    UITextAlignment.Left,
                    UIControlContentVerticalAlignment.Center);
                return;
            }

            context.DrawText(
                rect,
                Text, 
                DC.SearchBoxFontSize.UnscaledF(), 
                "Helvetica", 
                DC.EmojiSearchBoxFgHexColor.ToUIColor(), 
                UITextAlignment.Left, 
                UIControlContentVerticalAlignment.Center);
            ClearTextImageView.Hidden = false;
            ClearTextImageView.TintColor = DC.ClearTextButtonFgHexColor.ToUIColor();
        }

        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        CGRect GetCaretRect() {
            nfloat x_pad = 1d.UnscaledF();

            var s_rect = new CGRect(new CGPoint(),iosHelpers.MeasureText(Text,"Helvetica",DC.SearchBoxFontSize.UnscaledF(),out _, out _));
            s_rect = s_rect.Flate(s_rect.Width - x_pad, 0, 0, 0);
            var e_rect = s_rect;

            nfloat x = s_rect.Left + x_pad;
            nfloat y = (nfloat)Math.Min(s_rect.Top, e_rect.Top);
            nfloat w = e_rect.Right - s_rect.Left;
            nfloat h = (nfloat)Math.Max(s_rect.Bottom, e_rect.Bottom) - y;
            return new CGRect(x, (this.Frame.Height / 2) - (h / 2), w, h);
        }
        void StartCaretLoop() {
            show_caret = true;
            if (iosKeyboardViewController.Instance is { } kbvc &&
                       kbvc.MainThread is { } mt) {
                mt.Post(async () => {
                    var last_draw = DateTime.Now;
                    var delay = TimeSpan.FromMilliseconds(500);
                    while (true) {
                        await Task.Delay(5);
                        if (this.Hidden) {
                            return;
                        }

                        if (DateTime.Now - last_draw >= delay) {
                            show_caret = !show_caret;
                            this.Redraw();
                            last_draw = DateTime.Now;
                        }
                    }
                });
            }
        }

        void DrawSel(CGContext context, CGRect caretRect) {
            // NOTE presumes text is single line
            var caret_color =
                Range.SelectionStartIdx == Range.SelectionEndIdx ?
                    DC.EmojiSearchBoxCaretHexColor.ToUIColor() :
                    DC.EmojiSearchBoxSelHexColor.ToUIColor();
            context.SetFillColor(caret_color.CGColor);
            context.FillRect(caretRect);
        }
        #endregion


    }
}