using CoreGraphics;

using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosKeyGridView : FrameViewBase {
        #region Interfaces

        #region IKeyboardViewRenderer

        public override void LayoutFrame(bool invalidate) {
            this.Hidden = !DC.IsKeyGridVisible;
            base.LayoutFrame(invalidate);
        }
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.KeyGridRect.ToCGRect();
            base.MeasureFrame(invalidate);
        }
        public override void PaintFrame(bool invalidate) {
            this.BackgroundColor = DC.KeyGridBgHexColor.ToUIColor();
            base.PaintFrame(invalidate);
        }
        #endregion

        #endregion
        #region Properties
        #region View Models
        public new KeyboardViewModel DC { get; set; }
        #endregion

        #region Views

        List<iosPopupContainerView> PopupContainers { get; set; } = [];

        public IEnumerable<iosKeyView> KeyViews =>
            Subviews.OfType<iosKeyView>();
        #endregion

        #endregion

        #region Constructors
        public iosKeyGridView(KeyboardViewModel dc) {
            DC = dc;
            DC.OnShowPopup += DC_OnShowPopup;
            DC.OnHidePopup += DC_OnHidePopup;
        }



        #endregion

        #region Public Methods
        public void Unload() {

            if(DC != null) {

                DC.OnShowPopup -= DC_OnShowPopup;
                DC.OnHidePopup -= DC_OnHidePopup;
            }
        }

        public void ResetRenderer() {
            this.Subviews.ToList().ForEach(x => x.RemoveAndDispose());

            PopupContainers.Clear();

            foreach (var kvm in DC.Keys.Where(x=>!x.IsPopupKey)) {
                var kv = new iosKeyView(kvm).SetDefaultProps();
                this.AddSubview(kv);
            }
        }

        public override void Draw(CGRect rect) {
            if (!DC.IsKeyGridVisible) {
                return;
            }
            //var img_rect = new CGRect(0, 0, 150, 150);
            //var img = iosHelpers.LoadBitmap("backspace.png");
            //if (this.Subviews.OfType<UIImageView>().FirstOrDefault() is not { } img_view) {
            //    // add image subview
            //    img_view = new UIImageView(img_rect);
            //    img_view.Image = img.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            //    this.AddSubview(img_view);
            //}
            //img_view.TintColor = UIColor.White;

            //base.Draw(rect);
        }
        #endregion

        #region Private Methods

        void CreateShadowLayer(bool invalidate) {
            /*
            UIImage DrawEmojiPageToImage(bool clear, EmojiPageViewModel pg_vm) {
            var fontSize = DC.EmojiFontSize.UnscaledF();
            var pg_rect = pg_vm.PageRect.ToCGRect().ToBounds();
            UIGraphics.BeginImageContextWithOptions(pg_rect.Size, false, 1);
            var temp_img_view = new UIImageView(pg_rect);
            var context = UIGraphics.GetCurrentContext();
            if (clear) {
                context.ClearRect(pg_rect);
            }
            foreach (var evm in pg_vm.EmojiKeys) {
                var evm_rect = evm.EmojiRect.ToCGRect();

                
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

                context.DrawCenteredText(evm_rect, evm.PrimaryValue, iosKeyboardView.DEFAULT_FONT_FAMILY, evm.EmojiFontSize.UnscaledF(), "#FFFFFFFF", 0, 0);

            }
            temp_img_view.Layer.RenderInContext(context);
            var img = new UIImage(UIGraphics.GetImageFromCurrentImageContext().AsJPEG(10));
            UIGraphics.EndImageContext();
            if(clear) {
                context.ClearRect(pg_rect);
            }
            context.RestoreState();
            return img;
        }
            */
        }

        private void DC_OnHidePopup(object sender, KeyViewModel e) {
            if(e is not { } anchor_kvm ||
                PopupContainers.FirstOrDefault(x=>x.TagObj == anchor_kvm) is not { } pucv) {
                return;
            }
            pucv.RemoveAndDispose();
            PopupContainers.Remove(pucv);
            this.RenderFrame(true);
            iosHelpers.DoGC();
        }

        private void DC_OnShowPopup(object sender, KeyViewModel e) {
            if(e is not { } anchor_kvm ||
                anchor_kvm.PopupKeys is not { } popup_kvml2 ||
                popup_kvml2.ToList() is not { } popup_kvml) {
                return;
            }
            if(PopupContainers.FirstOrDefault(x=>x.Hidden) is not { } pucv) {
                pucv = new iosPopupContainerView().SetDefaultProps();
                this.AddSubview(pucv);
                PopupContainers.Add(pucv);
                pucv.RoundCorners(DC.CommonCornerRadius);
            }

            if (anchor_kvm.IsHoldMenuOpen) {
                pucv.Frame = anchor_kvm.PopupRect.ToCGRect().ToBounds();
            } else {
                pucv.Frame = anchor_kvm.PopupRect.Move(0,-anchor_kvm.TotalRect.Height/2).ToCGRect();
            }
            pucv.MeasureFrame(false);
            
            pucv.TagObj = anchor_kvm;
            var pucv_kvl = pucv.Subviews.OfType<iosKeyView>().ToList();
            int full_count = Math.Max(popup_kvml.Count, pucv_kvl.Count);
            for (int i = 0; i < full_count; i++) {
                if(popup_kvml.ElementAtOrDefault(i) is not { } popup_kvm) {
                    pucv_kvl[i].Hidden = true;
                    continue;
                }
                if(pucv_kvl.ElementAtOrDefault(i) is not { } popup_kv) {
                    popup_kv = new iosKeyView(popup_kvm);
                    pucv.AddSubview(popup_kv);
                } else {
                    popup_kv.Init(popup_kvm);
                }
            }
            pucv.RenderFrame(true);
        }
        #endregion
    }
}