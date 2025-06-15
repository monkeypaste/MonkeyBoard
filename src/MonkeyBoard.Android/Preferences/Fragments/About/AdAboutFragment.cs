using Android.OS;
using MonkeyBoard.Common;
using System;
using System.Linq;

namespace MonkeyBoard.Android {
    public class AdAboutFragment : AdPreferencesFragment {
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey) {
            base.OnCreatePreferences(savedInstanceState, rootKey);
            Title = ResourceStrings.U["ABOUT_TITLE"].value;
            SetNavTitle();

            var context = PreferenceManager.Context;
            var screen = PreferenceManager.CreatePreferenceScreen(context);
            this.PreferenceScreen = screen;

            AddPref(screen, PrefKeys.APP_NAME);
            AddPref(screen, PrefKeys.APP_VERSION);

            screen.AddPreference(AdPreferenceSubFragment.CreateEntry(
                    context,
                    new() {
                        Title = ResourceStrings.U["PrivacyTitle"].value,
                        Items = [PrefKeys.PRIVACY_TEXT],
                    }));


            UpdateAll();
        }
    }
}
