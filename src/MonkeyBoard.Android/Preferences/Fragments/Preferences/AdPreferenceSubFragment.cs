using Android.Content;
using Android.OS;
using AndroidX.Preference;
using Java.Util;
using MonkeyBoard.Common;
using System;
using System.Collections.Generic;
using static Android.Graphics.Paint;

namespace MonkeyBoard.Android {
    public abstract class AdPrefItemModelBase {
        public string Title { get; set; }
        public object Icon { get; set; }
    }
    public class AdPrefModel : AdPrefItemModelBase {
        public PrefKeys Key { get; set; }
        public string Detail { get; set; }
    }
    public class AdListPrefModel : AdPrefModel {
        public Type SubEnum { get; set; }
        public string ItemsPrefix { get; set; }
        public int ItemsCount { get; set; }
    }
    public class AdPrefSubFragmentModel : AdPrefItemModelBase {

        public List<PrefKeys> Items { get; set; } = [];
    }

    public class AdPreferenceSubFragment : AdSettingsFragmentBase {
        const string PREF_FRAG_MODEL_KEY = "PrefFragModel";
        const string PREF_FRAG_CLICK_KEY = "PrefFragClick";

        Action<Preference> OnClickHandler { get; set; }
        public static Preference CreateEntry(Context context, AdPrefSubFragmentModel model, Action<Preference> onClick = default) {
            var entry_pref = new Preference(context);
            entry_pref.Title = model.Title;
            entry_pref.Fragment = typeof(AdPreferenceSubFragment).ToJavaClassName();
            var b = new Bundle();
            b.PutBinder(PREF_FRAG_MODEL_KEY, new FragmentDataBinder<AdPrefSubFragmentModel>(model));
            entry_pref.Extras.PutBundle(PREF_FRAG_MODEL_KEY, b);

            var b2 = new Bundle();
            b2.PutBinder(PREF_FRAG_CLICK_KEY, new FragmentDataBinder<Action<Preference>>(onClick));
            entry_pref.Extras.PutBundle(PREF_FRAG_CLICK_KEY, b2);
            return entry_pref;
        }

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey) {
            base.OnCreatePreferences(savedInstanceState, rootKey);
            var context = PreferenceManager.Context;
            
            var screen = PreferenceManager.CreatePreferenceScreen(context);
            this.PreferenceScreen = screen;

            if (this.Arguments.GetBundle(PREF_FRAG_MODEL_KEY) is not { } b ||
                b.GetBinder(PREF_FRAG_MODEL_KEY) is not { } binder ||
                binder is not FragmentDataBinder<AdPrefSubFragmentModel> { } lfb ||
                lfb.BoundData is not { } model) {
                return;
            }

            Action<Preference> onClick = default;
            if (this.Arguments.GetBundle(PREF_FRAG_CLICK_KEY) is { } b2 &&
                b2.GetBinder(PREF_FRAG_CLICK_KEY) is { } binder2 &&
                binder2 is FragmentDataBinder<Action<Preference>> { } lfb2) {
                onClick = lfb2.BoundData;
            }
            Title = model.Title;
            SetNavTitle();

            foreach(var pref_key in model.Items) {
                var pref = AddPref(screen, pref_key);
                pref.PreferenceClick += (s, e) => {
                    onClick?.Invoke(pref);
                };
            }

            UpdateAll();
        }
    }
}
