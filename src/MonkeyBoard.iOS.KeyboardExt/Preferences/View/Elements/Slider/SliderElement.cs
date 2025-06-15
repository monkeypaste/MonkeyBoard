using CoreFoundation;
using CoreGraphics;
using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using MonoTouch.Dialog;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class SliderElement : Element, IKeyedElement, IRefreshData {
        public string Key { get; private set; }
        public string Detail { get; set; } = string.Empty;
        public bool ShowCaption { get; set; } = true;
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float Value { get; set; }

        public bool SuppressValueChanged { get; set; }

        private static NSString skey = new NSString("SliderElement");
        public object TagObj { get; set; }
        UISlider slider { get; set; }

        protected override NSString CellKey => skey;
        UITableViewCell ReusableCell { get; set; }

        public SliderElement(float value, float min, float max, string key, string caption) : base(caption) {
            Key = key;
            Value = value;
            MinValue = min;
            MaxValue = max;
        }
        public SliderElement(float value, float min, float max, string key, string caption, string detail = "") : this(value,min,max,key,caption) {
            Detail = detail;
        }
        public override UITableViewCell GetCell(UITableView tv) {
            UITableViewCell cell = tv.DequeueReusableCell(CellKey);
            if (cell == null) {
                cell = new UITableViewCell(UITableViewCellStyle.Subtitle, CellKey);
                cell.SelectionStyle = UITableViewCellSelectionStyle.None;
            } else {
                Element.RemoveTag(cell, 1);
            }

            CGSize cGSize = new CGSize(0f, 0f);
            if (Caption != null && ShowCaption) {
                var config = cell.DefaultContentConfiguration;
                config.Text = Caption;
                config.SecondaryText = Detail;
                cell.ContentConfiguration = config;
            }

            if (slider == null) {
                slider = new CustomSlider(){
                    BackgroundColor = UIColor.Clear,
                    MinValue = MinValue,
                    MaxValue = MaxValue,
                    Continuous = true,
                    Value = Value,
                    Tag = (IntPtr)1,
                };
                slider.ValueChanged += Slider_ValueChanged;
            } else {
                slider.Value = Value;
            }

            cell.AccessoryView = slider;
            ReusableCell = cell; ;
            return cell;
        }
        public void SetContent(string caption, string detail) {
            DispatchQueue.MainQueue.DispatchAsync(() => {

            if (this.GetContainerTableView() is not { } tv ||
                tv.CellAt(this.IndexPath) is not { } cell) {
                    if (ReusableCell == null) {
                        MpConsole.WriteLine($"Cannot find cell");
                        return;
                    } else {
                        cell = ReusableCell;
                    }
                }
                var config = cell.DefaultContentConfiguration;
                config.Text = caption;
                config.SecondaryText = detail;
                cell.ContentConfiguration = config;
                Caption = caption;
                Detail = detail;
                cell.Redraw();
            });
        }
        private void Slider_ValueChanged(object sender, EventArgs e) {
            Value = slider.Value;
            SetContent(Caption, Value.ToStringOrEmpty());
            PrefView.Slider_ValueChanged(this, e);
            slider.Redraw();
        }


        protected override void Dispose(bool disposing) {
            if (disposing && slider != null) {
                slider.Dispose();
                slider = null;
            }
        }

        public void RefreshData() {
            if (slider == null ||
                PrefView.Instance is not { } prefView ||
                prefView.PrefService is not { } prefService ||
                !Enum.TryParse(typeof(PrefKeys), Key, out object keyObj) ||
                keyObj is not PrefKeys key) {
                return;
            }
            slider.SetValue(prefService.GetPrefValue<int>(key),true);
        }
    }

    public class CustomSlider : UISlider {
        public CustomSlider() {
            this.ClipsToBounds = false;
        }
        public override void Draw(CGRect rect) {
            base.Draw(rect);
            /*
            let trackRect =  self.slider.trackRect(forBounds: self.slider.bounds)
        let thumbRect = self.slider.thumbRect(forBounds: self.slider.bounds, trackRect: trackRect, value: self.slider.value)
        yourLabel.center = CGPoint(x: thumbRect.origin.x + self.slider.frame.origin.x + 30, y: self.slider.frame.origin.y - 60)
            */

            //var track_rect = this.TrackRectForBounds(rect);
            //var thumb_rect = this.ThumbRectForBounds(rect, track_rect, this.Value);
            //UIGraphics.GetCurrentContext()
            //    .DrawText_CT(
            //        new CGRect(rect.Left - 30,rect.Top,30,rect.Height),//thumb_rect.Height),
            //        ((int)this.Value).ToString(),
            //        iosKeyboardView.DEFAULT_FONT_FAMILY,
            //        12,
            //        "#FFFF0000"//KeyboardPalette.P[PaletteColorType.FgHex
            //                   );
        }
    }
}