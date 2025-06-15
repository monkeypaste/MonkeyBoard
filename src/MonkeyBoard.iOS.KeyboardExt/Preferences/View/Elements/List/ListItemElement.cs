using Avalonia.Animation;
using Avalonia.Controls;
using CoreGraphics;
using Foundation;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard.Common;
using MonoTouch.Dialog;
using System;
using System.Linq;
using UIKit;
using static System.Collections.Specialized.BitVector32;
using Section = MonoTouch.Dialog.Section;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class ListItemElement : RadioElement, IKeyedElement, IRefreshData {
        private static NSString skey = new NSString("StringElement");

        private static NSString skeyvalue = new NSString("StringElementValue");
        public object TagObj { get; set; }
        public string Key => this.Group;//{ get; private set; }
        public bool IsSelected { get; private set; }
        public string Detail { get; private set; }
        public ListItemElement(string caption, string group) : base(caption, group) { }
        public ListItemElement(string caption, string detail, string group) : this(caption, group) {
            Detail = detail;
        }

        public override void Selected(DialogViewController dvc, UITableView tableView, NSIndexPath indexPath) {
            base.Selected(dvc, tableView, indexPath);
            //UpdateSelection(tableView);
            PrefView.ListItemElement_OnSelected(this, EventArgs.Empty);
        }
        public override UITableViewCell GetCell(UITableView tv) {
            var cell = new UITableViewCell(UITableViewCellStyle.Subtitle, (Value == null) ? skey : skeyvalue);
            cell.SelectionStyle = UITableViewCellSelectionStyle.Blue;

            var config = cell.DefaultContentConfiguration;
            config.Text = Caption;
            config.SecondaryText = Detail;
            cell.ContentConfiguration = config;

            RootElement rootElement = (RootElement)Parent.Parent;
            if(rootElement.GetPrivateFieldValue<Group>("group") is RadioGroup group) {
                int idx = this.GetPrivateFieldValue<int>("RadioIdx");
                bool flag = idx == group.Selected;
                cell.Accessory = (UITableViewCellAccessory)(flag ? 3 : 0);
            } else {
                cell.Accessory = UITableViewCellAccessory.None;
            }
            return cell;
        }

        public void RefreshData() {
            if(PrefView.Instance is not { } prefView ||
                prefView.PrefService is not { } prefService ||
                !Enum.TryParse(typeof(PrefKeys), Key, out object keyObj) ||
                keyObj is not PrefKeys key ||
                this.GetContainerTableView() is not { } tv) {
                return;
            }

            var cur_val = prefService.GetPrefValue<string>(key);
            if(TagObj.ToStringOrEmpty() == cur_val && !IsSelected) {
                // this.Selected(prefView.DVC, tv, this.IndexPath);
            }
        }
    }
}