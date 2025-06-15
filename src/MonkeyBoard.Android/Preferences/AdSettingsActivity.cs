using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using MonkeyBoard.Common;
using System;
using System.Diagnostics;
using System.Linq;
using Xamarin.Essentials;

namespace MonkeyBoard.Android {
    [Activity(
        Label = "Settings",
        Theme = "@style/SettingsTheme"
        )]
    public class AdSettingsActivity : AppCompatActivity {
        public static Context CurrentContext { get; private set; }
        public AdMainSettingsFragment RootFragment { get; set; }
        public SharedPrefWrapper PrefManager { get; private set; }
        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);
            CurrentContext = this;

            Platform.Init(this, savedInstanceState);
            //Microsoft.Maui.ApplicationModel.Platform.Init(this);

            // from https://stackoverflow.com/a/37774966/105028

            if(Intent.Extras.GetBinder(AdInputMethodService.PREF_BUNDLE_KEY) is FragmentDataBinder<SharedPrefWrapper> pmb) {
                PrefManager = pmb.BoundData;
            }

            // from https://stackoverflow.com/a/37224262/105028

            SupportActionBar.Title = ResourceStrings.U["MainTitle"].value;
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            var frame_layout = new FrameLayout(this);
            frame_layout.Id = Resource.Id.content;
            frame_layout.LayoutParameters = new ViewGroup.LayoutParams(FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.MatchParent);

            var lin_layout = new LinearLayout(this);
            lin_layout.Orientation = Orientation.Vertical;
            lin_layout.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
            lin_layout.AddView(frame_layout);
            SetContentView(lin_layout);
            //SetContentView(MpAdHostBridgeBase.Instance.HostResources.SettingsLayoutId);//Resource.Layout.settings_layout);

            RootFragment = new AdMainSettingsFragment();
            SupportFragmentManager
                .BeginTransaction()
                .Replace(Resource.Id.content, RootFragment)
                .Commit();
        }
        protected override void OnDestroy() {
            base.OnDestroy();
            CurrentContext = null;
        }

        public override bool OnOptionsItemSelected(IMenuItem item) {
            // NOTE this is called from header back button (and options buttons), it just triggers device back
            if(item.TitleFormatted != null) {
                // only back button should not have a title
                return false;
            }
            TriggerBackButton();
            return true;
        }

        public void TriggerBackButton() {
            this.DispatchKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.Back));
            this.DispatchKeyEvent(new KeyEvent(KeyEventActions.Up, Keycode.Back));
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e) {
            // NOTE this is called from device back button
            if(keyCode == Keycode.Back) {
                HandleNavUp();
            }
            return base.OnKeyUp(keyCode, e);
        }
        void HandleNavUp() {
            string root_title = ResourceStrings.U["MainTitle"].value;
            if(SupportActionBar.Title == root_title) {
                this.FinishAndRemoveTask();
                return;
            }
            if(RootFragment != null &&
                SupportFragmentManager.Fragments.FirstOrDefault(x => x.IsVisible) is { } vis_frag &&
                RootFragment.ChildFragmentTypes.Contains(vis_frag.GetType())) {
                SupportActionBar.Title = root_title;
            } else {
                var test = SupportFragmentManager.Fragments.ToList();
            }

        }
    }
}
