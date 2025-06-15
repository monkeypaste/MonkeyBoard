using MonoTouch.Dialog;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class CustomDialogViewController : DialogViewController {
        bool IsDark { get; set; }
        public CustomDialogViewController(RootElement root, bool pushing, bool isDark) : base(root, pushing) {
            IsDark = isDark;
        }
        public override void ViewDidLoad() {
            base.ViewDidLoad();
            OverrideUserInterfaceStyle = IsDark ? UIUserInterfaceStyle.Dark : UIUserInterfaceStyle.Light;
            View.TraitOverrides.UserInterfaceStyle = OverrideUserInterfaceStyle;
            //PrefView.Instance.DVC.ReloadData();
        }


        public override void DidReceiveMemoryWarning() {
            //GC.Collect(2, GCCollectionMode.Aggressive);
        }

    }

    public class ColorPickerViewControler : CustomDialogViewController {
        public ColorPickerViewControler(RootElement root, bool pushing, bool isDark) : base(root, pushing, isDark) {
        }
    }
}