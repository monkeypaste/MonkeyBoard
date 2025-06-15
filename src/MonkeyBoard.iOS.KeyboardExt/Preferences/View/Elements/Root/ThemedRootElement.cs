using MonoTouch.Dialog;
using System;
using System.Collections.Generic;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class ThemedRootElement : RootElement, IKeyedElement {
        public static List<ThemedRootElement> KeyedRoots { get; set; } = [];
        public ThemedRootElement(string caption) : base(caption) {
        }
        public ThemedRootElement(string key,string caption) : base(caption) {
            Key = key;
            KeyedRoots.Add(this);
        }

        public ThemedRootElement(string caption, Func<RootElement, UIViewController> createOnSelected) : base(caption, createOnSelected) {
        }

        public ThemedRootElement(string key, string caption, Group group) : base(caption, group) {
            Key = key;
            KeyedRoots.Add(this);
        }

        public ThemedRootElement(string caption, int section, int element) : base(caption, section, element) {
        }
        protected override UIViewController MakeViewController() {
            var vc = base.MakeViewController();

            vc.OverrideUserInterfaceStyle = PrefView.Instance.IsDark ? UIUserInterfaceStyle.Dark : UIUserInterfaceStyle.Light;
            return vc;
        }

        public string Key { get; private set; }
    }
}