using Android.Content;
using Android.OS;
using AndroidX.Preference;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBoard.Android {

    public class AdLanguagesFragment : AdSettingsFragmentBase {
        //public static string SelectedLanguageTitle { get; private set; }
        public const string LANG_FRAG_BUNDLE_KEY = "langFrag";
        PreferenceCategory InstalledCategory { get; set; }
        PreferenceCategory AvailableCategory { get; set; }
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey) {
            base.OnCreatePreferences(savedInstanceState, rootKey);
            var context = PreferenceManager.Context;
            this.PreferenceScreen = PreferenceManager.CreatePreferenceScreen(context);
        }

        public override void OnViewStateRestored(Bundle savedInstanceState) {
            base.OnViewStateRestored(savedInstanceState);
            RefreshCategories(true);
        }

        public void RefreshCategories(bool isActive) {
            Handler.Post(async () => {
                var context = PreferenceManager.Context;
                var screen = this.PreferenceScreen;

                if (isActive && context is AdSettingsActivity sa && sa.SupportActionBar is { } sab) {
                    sab.Title = ResourceStrings.U["LANG_PACKS_TITLE"].value;
                }

                // INSTALLED
                if (InstalledCategory == null) {
                    InstalledCategory = new PreferenceCategory(context);
                    InstalledCategory.Title = ResourceStrings.U["InstalledLanguagesTitle"].value;
                    screen.AddPreference(InstalledCategory);
                }
                InstalledCategory.RemoveAll();

                var installed_items = CultureManager.GetInstalledPackInfo();

                installed_items
                    .OrderBy(x => x.IsActive ? 0 : 1)
                    .ThenBy(x => x.Info.DisplayName)
                    .ForEach(x => AddLanguagePref(
                        cat: InstalledCategory,
                        label: x.Info.DisplayName,
                        cc: x.Info.Name,
                        isChecked: x.IsActive,
                        true));

                // AVAILABLE
                if (AvailableCategory == null) {
                    AvailableCategory = new PreferenceCategory(context);
                    AvailableCategory.Title = ResourceStrings.U["AvailableLanguagesTitle"].value;
                    screen.AddPreference(AvailableCategory);
                }
                AvailableCategory.RemoveAll();
                var busy_pref = new Preference(PreferenceManager.Context) {
                    Title = ResourceStrings.U["BusyLabel"].value
                };
                AvailableCategory.AddPreference(busy_pref);

                var avail_items = await CultureManager.GetAllPackInfoAsync();

                if(!avail_items.Any()) {
                    // error, no internet
                    busy_pref.Title = ResourceStrings.U["ErrorTitle"].value;
                    busy_pref.Summary = ResourceStrings.U["ErrorLabel"].value;
                    return;
                }
                AvailableCategory.RemoveAll();

                avail_items
                 .OrderBy(x => x.Info.DisplayName)
                    .ForEach(x => AddLanguagePref(
                        cat: AvailableCategory,
                        label: x.Info.DisplayName,
                        cc: x.Info.Name,
                        isChecked: x.IsInstalled,
                        false));
            });
        }

        Preference AddLanguagePref(PreferenceCategory cat, string label, string cc, bool isChecked, bool isInstallCat) {
            
            //Only show checkbox when active for installed  or not installed for avail 
            
            var context = PreferenceManager.Context;
            bool show_checkbox = (isInstallCat && isChecked) || (!isInstallCat && !isChecked);
            Preference lang_pref = null;
            if(show_checkbox) {
                var lang_cb_pref = new CheckBoxPreference(context);
                lang_cb_pref.Checked = isChecked;
                lang_pref = lang_cb_pref;
            } else {
                lang_pref = new Preference(context);
            }
            lang_pref.Title = label;
            lang_pref.Fragment = isInstallCat ? typeof(AdLanguageFragment).ToJavaClassName() : null;
            lang_pref.Extras.PutString("culture", cc);
            lang_pref.Extras.PutString("langTitle", label);
            var b = new Bundle();
            b.PutBinder(LANG_FRAG_BUNDLE_KEY, new FragmentDataBinder<AdLanguagesFragment>(this));
            lang_pref.Extras.PutBundle(LANG_FRAG_BUNDLE_KEY,b);
            lang_pref.PreferenceClick += Lang_pref_PreferenceClick;
            cat.AddPreference(lang_pref);
            return lang_pref;
        }

        private void Lang_pref_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e) {
            // NOTE check toggle happens BEFORE this gets called so !Checked is current value
            if(sender is not Preference p ||
                p.Extras.GetString("culture") is not { } cc) {
                return;
            }

            bool is_installed = CultureManager.InstalledCultures.Contains(cc);
            if(is_installed) {
                // open keyboard selector fragment
                //SelectedLanguageTitle = disp_name;
                if(p is CheckBoxPreference cbp) {
                    // keep checked
                    cbp.Checked = true;
                }
                e.Handled = false;
                return;
            }

            DownloadAndInstallPackAsync(cc).FireAndForgetSafeAsync();
        }
        int progress = -1;
        void OnProgressChanged(object sender, int percent) {
            progress = percent;
        }

        int GetProgress() {
            return progress;
        }
        async Task DownloadAndInstallPackAsync(string cc) {
            
            var last_installed = CultureManager.InstalledCultures.ToList();

            // start download
            CultureManager.DownloadProgressChanged += OnProgressChanged;
            CultureManager.InstallCultureAsync(cc).FireAndForgetSafeAsync();

            // show progress
            await AdHelpers.AlertProgressAsync(
                PreferenceManager.Context,
                ResourceStrings.U["LanguageDownloadTitle"].value, 
                GetProgress);
            progress = -1;
            string installed_cc = CultureManager.InstalledCultures.FirstOrDefault(x => !last_installed.Contains(x));

            if(installed_cc == default) {
                // download error
                AdHelpers.Alert(
                    PreferenceManager.Context,
                    ResourceStrings.U["ErrorTitle"].value,
                    ResourceStrings.U["ErrorLabel"].value);
                return;
            }
            // add installed language
            var installed_lang_pref = AddLanguagePref(InstalledCategory, cc.ToCultureDisplayName(),cc, false, true);

            RefreshCategories(true);

#pragma warning disable XAOBS001 // Type or member is obsolete
            installed_lang_pref.PerformClick();
#pragma warning restore XAOBS001 // Type or member is obsolete
        }

    }
}
