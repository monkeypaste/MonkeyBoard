using MonoTouch.Dialog;
using System.Collections.Generic;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class ListItemGroup : RadioGroup, IKeyedElement {
        public static List<ListItemGroup> AllGroups { get; set; } = [];

        public ListItemGroup(string key, int selected) : base(key, selected) {
            AllGroups.Add(this);
        }

        string IKeyedElement.Key => this.Key;
    }
}