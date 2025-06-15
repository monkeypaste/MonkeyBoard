using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Preference;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System.Collections.Generic;
using System.Linq;

namespace MonkeyBoard.Android {
    public class AdLanguageFragment : AdSettingsFragmentBase {
        
        string LanguageCultureCode =>
            this.Arguments.GetString("culture", null);
        string SelectedKeyboardGuid { get; set; }
        IEnumerable<Preference> KeyboardTypePrefs { get; set; }
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey) {
            base.OnCreatePreferences(savedInstanceState, rootKey);
            var context = PreferenceManager.Context;
            var screen = PreferenceManager.CreatePreferenceScreen(context);
            this.PreferenceScreen = screen;


            if (this.Arguments.GetString("langTitle",null) is not { } title ||
                 LanguageCultureCode is not { } culture ||
                context is not AdSettingsActivity sa ||
                sa is not IMenuHost menuHost ||
                sa.SupportActionBar is not { } sab) {
                return;
            }
            sab.Title = title;

            menuHost.AddMenuProvider(this);
            // KEYBOARDS
            var kb_cat = new PreferenceCategory(context);
            kb_cat.Title = ResourceStrings.U["SelectKeyboardTitle"].value;
            screen.AddPreference(kb_cat);

            if(CultureManager.CurrentKbCulture == LanguageCultureCode) {
                SelectedKeyboardGuid = KeyboardLayoutFactory.DefaultKeyboardGuid;
            } else {
                SelectedKeyboardGuid = string.Empty;
            }

            var kbc = KeyboardLayoutFactory.LoadKeyboardCollection(LanguageCultureCode);

            var kbl = new List<Preference>();
            foreach(var kb in kbc.keyboards.Where(x=>!x.isNumPad).OrderBy(x=>x.label)) {
                var kbp = AddKeyboardType(
                    cat: kb_cat, 
                    label: kb.label, 
                    description: kb.description, 
                    guid: kb.guid, 
                    isChecked: kb.guid == SelectedKeyboardGuid);
                kbl.Add(kbp);
            }
            KeyboardTypePrefs = kbl;

            UpdateAll();
        }

        public override void OnDestroyView() {
            base.OnDestroyView();
            if(this.PreferenceManager.Context is not IMenuHost menuHost) {
                return;
            }
            menuHost.RemoveMenuProvider(this);
        }


        Preference AddKeyboardType(PreferenceCategory cat, string label, string description, string guid, bool isChecked) {
            var context = PreferenceManager.Context;
            var kb_type_pref = new CheckBoxPreference(context);
            kb_type_pref.Title = label;
            kb_type_pref.Summary = description;
            kb_type_pref.Checked = isChecked;
            kb_type_pref.Extras.PutString("guid", guid);
            kb_type_pref.PreferenceClick += Kb_type_pref_PreferenceClick;
            cat.AddPreference(kb_type_pref);

            return kb_type_pref;
        }

        private void Kb_type_pref_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e) {
            if(sender is not CheckBoxPreference pref) {
                return;
            }
            string kb_guid = pref.Extras.GetString("guid", null);
            if (kb_guid == SelectedKeyboardGuid) {
                // don't allow unchecking
                e.Handled = true;
                pref.Checked = true;
                return;
            }
            // update selection in UI
            SelectedKeyboardGuid = kb_guid;
            KeyboardTypePrefs
                .OfType<CheckBoxPreference>()
                .ForEach(x => x.Checked = x == pref);

            // persist sel
            if(CultureManager.CurrentKbCulture != LanguageCultureCode) {
                CultureManager.SetKbCulture(LanguageCultureCode);
            }
            KeyboardLayoutFactory.SetDefaultKeyboard(SelectedKeyboardGuid);
            RefreshLanguages();
            e.Handled = false;
        }

        void RefreshLanguages() {
            if (this.Arguments.GetBundle(AdLanguagesFragment.LANG_FRAG_BUNDLE_KEY) is not { } b ||
                b.GetBinder(AdLanguagesFragment.LANG_FRAG_BUNDLE_KEY) is not { } binder ||
                binder is not FragmentDataBinder<AdLanguagesFragment> { } lfb ||
                lfb.BoundData is not { } blf) {
                return;
            }
            blf.RefreshCategories(false);
        }
        #region Options Menu

        public override void OnMenuClosed(IMenu menu) {
        }
        public override void OnCreateMenu(IMenu menu, MenuInflater p1) {
        }

        public override void OnPrepareMenu(IMenu menu) {
            menu.Clear();

            var set_lang_mi = menu.Add(ResourceStrings.U["SetLanguagelLabel"].value);
            set_lang_mi.SetEnabled(LanguageCultureCode != CultureManager.CurrentUiCulture);

            var uninstall_lang_mi = menu.Add(ResourceStrings.U["UninstallLabel"].value);
            uninstall_lang_mi.SetEnabled(CultureManager.CanUninstallCulture(LanguageCultureCode));
        }
        public override bool OnMenuItemSelected(IMenuItem p0) {
            Handler.Post(async () => {
                bool is_set_lang = p0.TitleFormatted.ToString() == ResourceStrings.U["SetLanguagelLabel"].value;
                string confirm_label_key = is_set_lang ?
                    "ConfirmSetLanguagelLabel" :
                    "ConfirmUninstallLabel";
                string confirm_label = ResourceStrings.U[confirm_label_key].value.Format(LanguageCultureCode.ToCultureDisplayName());
                bool confirm = await AdHelpers.AlertYesNoAsync(
                    PreferenceManager.Context,
                    ResourceStrings.U["ConfirmTitle"].value,
                    confirm_label);
                if (!confirm) {
                    // canceled
                    return;
                }

                if (is_set_lang) {
                    // change language
                    CultureManager.SetUiCulture(LanguageCultureCode);

                    AdHelpers.Alert(
                        PreferenceManager.Context,
                        ResourceStrings.U["SetLanguagelFollowUpTitle"].value,
                        ResourceStrings.U["SetLanguagelFollowUpLabel"].value
                        );
                } else {
                    // uninstall header btn clicked
                    CultureManager.UninstallCulture(LanguageCultureCode);
                    (Context as AdSettingsActivity).TriggerBackButton();
                }
            });
            
            return true;
        }
        #endregion
    }
}
