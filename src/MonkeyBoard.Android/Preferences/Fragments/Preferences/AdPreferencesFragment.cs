using Android.OS;
using AndroidX.Preference;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System;
using System.Linq;

namespace MonkeyBoard.Android {
    public class AdPreferencesFragment : AdSettingsFragmentBase {

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey) {
            base.OnCreatePreferences(savedInstanceState, rootKey);
            Title = ResourceStrings.U["PREFERENCES_TITLE"].value;
            SetNavTitle();

            var context = PreferenceManager.Context;
            var screen = PreferenceManager.CreatePreferenceScreen(context);
            this.PreferenceScreen = screen;

            Action<Preference> onClick = (pref) => {
                if(!pref.Key.ToStringOrEmpty().TryToEnum<PrefKeys>(out var prefKey) ||
                    !PrefService.PrefClickActionLookup.TryGetValue(prefKey, out var action)) {
                    return;
                }
                action.Invoke(null);
            };

            foreach(PrefCategoryType cat in typeof(PrefCategoryType).GetKeys()) {
                if(!PrefService.CatLookup.TryGetValue(cat, out var pref_keys) ||
                    !pref_keys.Any()) {
                    continue;
                }
                if(!ResourceStrings.U.TryGetValue($"{cat}_TITLE", out var catObj) ||
                    catObj.value is not { } cat_title) {
                    cat_title = string.Empty;
                }
                //Action<Preference> onClick = default;

                //if (cat == PrefCategoryType.COMPLETION) {
                //    onClick = (pref) => {
                //        if (pref.Key.ToStringOrEmpty().ToEnum<PrefKeys>() == PrefKeys.DO_RESET_DB) {
                //            DoDbResetAsync().FireAndForgetSafeAsync();
                //        }
                //    };
                //}
                var frag = screen.AddPreference(AdPreferenceSubFragment.CreateEntry(
                    context,
                    new() {
                        Title = cat_title,
                        Items = pref_keys.ToList(),
                    },
                    onClick));
            }

            UpdateAll();
        }
    }
}
