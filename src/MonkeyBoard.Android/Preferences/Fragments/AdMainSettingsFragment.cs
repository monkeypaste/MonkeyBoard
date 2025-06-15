using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using AndroidX.Preference;
using AndroidX.RecyclerView.Widget;
using MonkeyBoard.Common;
using System;
using System.Threading.Tasks;

namespace MonkeyBoard.Android {
    public class AdMainSettingsFragment : AdSettingsFragmentBase {
        public Type[] ChildFragmentTypes => [
            typeof(AdPreferencesFragment),
            typeof(AdLanguagesFragment),
            typeof(AdFeedbackFragment),
            typeof(AdAboutFragment),
            ];
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey) {
            base.OnCreatePreferences(savedInstanceState, rootKey);

            var context = PreferenceManager.Context;
            if (context is AdSettingsActivity sa && sa.SupportActionBar is { } sab) {
                sab.Title = ResourceStrings.U["MainTitle"].value;
            }
            var screen = PreferenceManager.CreatePreferenceScreen(context);
            this.PreferenceScreen = screen;

            // PREF
            var pref_frag = new Preference(context);
            //pref_frag.Icon = AdHelpers.LoadDrawableBmp(context, 
            //    PrefService.GetPageIconName(SettingsPageType.PREFERENCES));
            pref_frag.Title = ResourceStrings.U["PREFERENCES_TITLE"].value;            
            pref_frag.Fragment = typeof(AdPreferencesFragment).ToJavaClassName();
            screen.AddPreference(pref_frag);
            
            // LANGUAGES
            var lang_frag = new Preference(context);
            //lang_frag.Icon = AdHelpers.LoadDrawableBmp(context, 
            //    PrefService.GetPageIconName(SettingsPageType.LANG_PACKS));
            lang_frag.Title = ResourceStrings.U["LANG_PACKS_TITLE"].value;            
            lang_frag.Fragment = typeof(AdLanguagesFragment).ToJavaClassName();            
            screen.AddPreference(lang_frag);


            // FEEDBACK
            var feedback_frag = new Preference(context);
            //feedback_frag.Icon = AdHelpers.LoadDrawableBmp(context,
            //    PrefService.GetPageIconName(SettingsPageType.FEEDBACK));
            feedback_frag.Title = ResourceStrings.U["FEEDBACK_TITLE"].value;
            feedback_frag.Fragment = typeof(AdFeedbackFragment).ToJavaClassName();
            screen.AddPreference(feedback_frag);
            
            // ABOUT
            var about_frag = new Preference(context);
            //about_frag.Icon = AdHelpers.LoadDrawableBmp(context,
            //    PrefService.GetPageIconName(SettingsPageType.ABOUT));
            about_frag.Title = ResourceStrings.U["ABOUT_TITLE"].value;
            about_frag.Fragment = typeof(AdAboutFragment).ToJavaClassName();
            screen.AddPreference(about_frag);

            var reset_all = new Preference(context);
            reset_all.Title = ResourceStrings.U["ResetAllTitle"].value;
            reset_all.PreferenceClick += Reset_all_PreferenceClick;
            screen.AddPreference(reset_all);

            UpdateAll();
        }

        private void Reset_all_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e) {
            if(!PrefService.PrefClickActionLookup.TryGetValue(PrefKeys.DO_RESET_ALL, out var action)) {
                return;
            }
            action.Invoke(null);
        }
    }
}
