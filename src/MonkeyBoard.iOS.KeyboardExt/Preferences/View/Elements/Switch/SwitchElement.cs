using CoreGraphics;
using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using MonoTouch.Dialog;
using System;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class SwitchElement : BoolElement, IKeyedElement, IRefreshData {
        private static NSString bkey = new NSString("SwitchElement");

        private UISwitch sw;

        protected override NSString CellKey => bkey;
        public object TagObj { get; set; }
        public override bool Value {
            get {
                return base.Value;
            }
            set {
                base.Value = value;
                if (sw != null) {
                    sw.On = value;
                    PrefView.SwitchElement_ValueChanged(this, EventArgs.Empty);
                }
            }
        }
        public bool HideSwitch { get; set; }
        public string Detail { get; private set; } = string.Empty;
        public string Key { get; private set; }
        public SwitchElement(string caption, bool value, string key) : base(caption,value) {            
            Key = key;
        }
        public SwitchElement(bool hideSwitch, string caption, bool value, string key) : this(caption, value, key) {
            HideSwitch = hideSwitch;
        }
        public SwitchElement(string caption, string detail, bool value, string key) : this(caption, value, key) {
            Detail = detail;
        }

        public override UITableViewCell GetCell(UITableView tv) {
            if (sw == null) {
                sw = new UISwitch {
                    Hidden = HideSwitch,
                    BackgroundColor = UIColor.Clear,
                    Tag = (IntPtr)1,
                    On = Value
                };
                sw.AddTarget(delegate {
                    Value = sw.On;
                }, UIControlEvent.ValueChanged);
            } else {
                sw.On = Value;
            }

            UITableViewCell cell = tv.DequeueReusableCell(CellKey);
            if (cell == null) {
                cell = new SelectableTableViewCell(UITableViewCellStyle.Subtitle, CellKey);
                if(HideSwitch) {
                    cell.SelectionStyle = UITableViewCellSelectionStyle.Default;
                    (cell as SelectableTableViewCell).OnSelectionChanged += SwitchElement_OnSelectionChanged;
                } else {
                    cell.SelectionStyle = UITableViewCellSelectionStyle.None;
                }
                
            } else {
                Element.RemoveTag(cell, 1);
            }
            var config = cell.DefaultContentConfiguration;
            config.Text = Caption;
            config.SecondaryText = Detail;
            cell.ContentConfiguration = config;
            cell.AccessoryView = sw;
            return cell;
        }
        public void SetProgress(int percent) {
            try {
                if (this.GetContainerTableView() is not { } tv ||
                tv.CellAt(IndexPath) is not { } cell) {
                    return;
                }
                if (cell.AccessoryView is not UIProgressView cpv) {
                    //cpv = new CircularProgressView(sw.Frame.Inset(10, 10));
                    cpv = new UIProgressView(sw.Frame.Inset(0, 0));
                    //cpv.Style = UIProgressViewStyle.Default;
                    cell.AccessoryView = cpv;
                }
                //cpv.SetProgressWithAnimation(TimeSpan.Zero, percent);
                cpv.SetProgress((percent / 100),false);
                cpv.Redraw();
                //cell.Redraw(true);
            }catch(Exception ex) {
                ex.Dump();
                iosKeyboardViewController.SetError(ex.ToString());
            }
        }

        private void SwitchElement_OnSelectionChanged(object sender, EventArgs e) {
            if(sender is not UITableViewCell stvc ||
                sw == null) {
                return;
            }
            Value = stvc.Selected;
        }

        public void RefreshData() {
            if(sw == null ||
                PrefView.Instance is not { } prefView ||
                prefView.PrefService is not { } prefService ||
                !Enum.TryParse(typeof(PrefKeys),Key, out object keyObj) ||
                keyObj is not PrefKeys key) {
                return;
            }
            Value = prefService.GetPrefValue<bool>(key);
        }

        protected override void Dispose(bool disposing) {
            if (disposing && sw != null) {
                sw.Dispose();
                sw = null;
            }
        }
    }

    public class SelectableTableViewCell : UITableViewCell {
        public event EventHandler OnSelectionChanged;
        public SelectableTableViewCell(UITableViewCellStyle style, NSString key) : base(style,key) { }

        public override bool Selected { 
            get => base.Selected; 
            set {
                if(base.Selected != value) {
                    base.Selected = value;
                    OnSelectionChanged?.Invoke(this, EventArgs.Empty);
                }
                

            }
        }
        public override void SetSelected(bool selected, bool animated) {
            base.SetSelected(selected, animated);
            OnSelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}