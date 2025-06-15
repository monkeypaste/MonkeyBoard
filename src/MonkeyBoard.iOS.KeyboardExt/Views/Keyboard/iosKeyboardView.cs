using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CoreGraphics;
using CoreImage;
using CoreText;
using DeviceDiscoveryExtension;
using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosKeyboardView : FrameViewBase, ITextTools {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        public static string DEFAULT_FONT_FAMILY => "Helvetica";//"Nunito_Regular";//
        #endregion

        #region Interfaces

        #region ITextTools Implementation
        Size ITextTools.MeasureText(string text, double scaledFontSize, out double ascent, out double descent) {
            var nssize = iosHelpers.MeasureText(text, DEFAULT_FONT_FAMILY, scaledFontSize.UnscaledF(), out var unscaled_ascent, out var unscaled_descent);
            ascent = unscaled_ascent.ScaledD();
            descent = unscaled_descent.ScaledD();
            return new Size(nssize.Width.ScaledD(), nssize.Height.ScaledD());
        }

        bool ITextTools.CanRender(string text) {
            /*
            func isEmojiSupported(emoji: String) -> Bool {
  let uniChars = Array(emoji.utf16)
  let font = CTFontCreateWithName("AppleColorEmoji", 0.0, nil)
  var glyphs: [CGGlyph] = [0, 0]
  return CTFontGetGlyphsForCharacters(font, uniChars, &glyphs, uniChars.count)
}


            ///////////
            ///

            UTF32Char emojiValue;
    [data getBytes:&emojiValue length:sizeof(emojiValue)];

    // Convert UTF32Char to UniChar surrogate pair.
    // Found here: http://stackoverflow.com/questions/13005091/how-to-tell-if-a-particular-font-has-a-specific-glyph-64k#
    UniChar characters[2] = { };
    CFIndex length = (CFStringGetSurrogatePairForLongCharacter(emojiValue, characters) ? 2 : 1);

    CGGlyph glyphs[2] = { };
    CTFontRef ctFont = CTFontCreateWithName((CFStringRef)@"AppleColorEmoji", 0.0, NULL);

    // If we don't get back any glyphs for the characters array, it's not supported
    BOOL ret = CTFontGetGlyphsForCharacters(ctFont, characters, glyphs, length);
    CFRelease(ctFont);

            */
            //var uniChars = new NSString(
            //    new NSString(text).Encode(NSStringEncoding.UTF16LittleEndian),
            //    NSStringEncoding.UTF16LittleEndian)

            //// from https://stackoverflow.com/a/33701591/105028
            //var font = new CTFont(iosEmojiPagesView.EMOJI_FONT_FAMILY_NAME, 0);
            //bool success = font.GetGlyphsForCharacters(text.ToCharArray(), [0, 0]);
            //font.Dispose();
            //return success;
            return true;
        }
        #endregion

        #region IFrameRenderer
        public override void MeasureFrame(bool invalidate) {
            this.Frame = DC.TotalRect.ToCGRect().Place(0, ContainerOffsetY);
            base.MeasureFrame(invalidate);
        }
        #endregion

        #endregion

        #region Properties


        #region View Models
        public new KeyboardViewModel DC { get; set; }
        #endregion

        #region State
        #endregion

        #region Views
        public iosFooterView FooterView { get; set; }
        iosMenuView MenuView { get; set; }
        iosKeyGridView KeyGridView { get; set; }
        iosSpeechView SpeechView { get; set; }

        public iosEmojiPagesView EmojiPagesView { get; set; }
        #endregion

        #region Layout
        public nfloat ContainerOffsetY { get; set; } = 0;
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors 
        public iosKeyboardView() {
        }
        #endregion

        #region Public Methods
        public void Init(IKeyboardInputConnection conn) {
            if(conn is not iosKeyboardViewController kbvc) {
                return;
            }
            Subviews.ToList().ForEach(x => x.RemoveAndDispose());
            var kbs = KeyboardViewModel.GetTotalSizeByScreenSize(iosDeviceInfo.ScaledSize, iosDeviceInfo.IsPortrait, conn.Flags.HasFlag(KeyboardFlags.Tablet), default);
            double scale = iosDeviceInfo.Scaling;
            DC = new KeyboardViewModel(conn, kbs / scale, scale, scale);
            DC.SetRenderContext(this);
            DC.OnKeyLayoutChanged += DC_OnKeyLayoutChngedChanged;

            MenuView = new iosMenuView(DC.MenuViewModel);
            this.AddSubview(MenuView);

            FooterView = new iosFooterView(DC.FooterViewModel);
            this.AddSubview(FooterView);

            KeyGridView = new iosKeyGridView(DC);
            this.AddSubview(KeyGridView);            

            SpeechView = new iosSpeechView(DC.MenuViewModel.SpeechPageViewModel);
            this.AddSubview(SpeechView);

            EmojiPagesView = new iosEmojiPagesView(DC.MenuViewModel.EmojiPagesViewModel);
            this.AddSubview(EmojiPagesView);

            DC.CursorControlViewModel.OnShowCursorControl += DC_OnShowCursorControl;
            DC.CursorControlViewModel.OnHideCursorControl += DC_OnHideCursorControl;

            RemapRenderers();
        }


        public void RemapRenderers() {
            //MpConsole.WriteLine($"RemapRenderers called");
            // NOTE when flags change keys are recreated and renderers need to be re-assigned
            KeyGridView.ResetRenderer();
            MenuView.ResetRenderer();
            //iosCursorControlView.ResetRenderer();

            RenderFrame(true);
        }
        public void Unload() {
            HideCursorControl();

            if (DC != null) {
                DC.CursorControlViewModel.OnShowCursorControl -= DC_OnShowCursorControl;
                DC.CursorControlViewModel.OnHideCursorControl -= DC_OnHideCursorControl;

                
            }
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods


        private void DC_OnKeyLayoutChngedChanged(object sender, EventArgs e) {
            RemapRenderers();
        }


        #endregion

        #region Input Handlers

        public event EventHandler<TouchEventArgs> OnTouchEvent;

        public void TriggerTouchEvent(CGPoint p, TouchEventType eventType) {
            OnTouchEvent?.Invoke(this, new TouchEventArgs(new Point(p.X, p.Y) / iosDeviceInfo.Scaling, eventType));
        }        

        #endregion


        #region Cursor Control

        void ShowCursorControl() {
            var CursorControlView = new iosCursorControlView(DC.CursorControlViewModel);
            CursorControlView.DC.InitLayout();
            CursorControlView.MeasureFrame(false);
            this.AddSubview(CursorControlView);
            this.Redraw(true);
        }
        void HideCursorControl() {
            if (this.Subviews.OfType<iosCursorControlView>().FirstOrDefault() is { } ccv) {
                ccv.RemoveAndDispose();
                ccv.Unload();
            }
            this.Redraw(true);
        }


        private void DC_OnShowCursorControl(object sender, System.EventArgs e) {
            Handler.Post(() => {
                ShowCursorControl();
            });
        }
        private void DC_OnHideCursorControl(object sender, System.EventArgs e) {
            Handler.Post(() => {
                HideCursorControl();
            });
        }
        #endregion
    }
}