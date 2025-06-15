using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Preference;
using AndroidX.RecyclerView.Widget;
using Java.Lang;
//using Microsoft.Maui.Storage;
using MonkeyPaste.Common;
using MonkeyBoard.Common;
using Net.ArcanaStudio.ColorPicker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Enum = System.Enum;
using Exception = System.Exception;

namespace MonkeyBoard.Android {
    public abstract class AdSettingsFragmentBase :
        PreferenceFragmentCompat,
        IMenuProvider,
        ISharedPreferencesOnSharedPreferenceChangeListener {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties


        Handler _handler;
        protected Handler Handler {
            get {
                if(_handler == null) {
                    _handler = new Handler(Context.MainLooper);
                }
                return _handler;
            }
        }
        protected string Title { get; set; } = string.Empty;

        #endregion

        #region Events
        #endregion

        #region Constructors
        #endregion

        #region Public Methods

        public override void OnResume() {
            base.OnResume();
            SetNavTitle();
        }
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey) {
            PreferenceManager
                .GetDefaultSharedPreferences(PreferenceManager.Context)
                .RegisterOnSharedPreferenceChangeListener(this);

            if(PreferenceManager.Context is AdSettingsActivity sa) {
                string tag = this.GetType().ToString();
                sa.SupportFragmentManager
                    .BeginTransaction()
                    .Replace(Resource.Id.content, this, tag);

            }
        }

        public override RecyclerView OnCreateRecyclerView(LayoutInflater inflater, ViewGroup parent, Bundle savedInstanceState) {
            var rv = base.OnCreateRecyclerView(inflater, parent, savedInstanceState);
            var div = new DividerItemDecoration(Context, RecyclerView.Vertical);
            rv.AddItemDecoration(div);
            return rv;
        }
        public virtual void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key) {
            // get default value
            if(PrefService is not { } prefm ||
                prefm.DefPrefValLookup is not { } def_vals ||
                !Enum.TryParse(key, out PrefKeys prefKey) ||
                !def_vals.TryGetValue(prefKey, out object def_val_obj)) {
                return;
            }

            // save new value

            try {
                if(def_val_obj is bool def_bool_val &&
                sharedPreferences.GetBoolean(key, def_bool_val) is bool new_bool_val) {
                    prefm.SetPrefValue(prefKey, new_bool_val);
                } else if(def_val_obj is int def_int_val &&
                            sharedPreferences.GetInt(key, def_int_val) is int new_int_val) {
                    prefm.SetPrefValue(prefKey, new_int_val);
                } else if(def_val_obj is SliderPrefProps spp &&
                            sharedPreferences.GetInt(key, spp.Default) is int new_slider_int_val) {
                    prefm.SetPrefValue(prefKey, new_slider_int_val);
                } else if(def_val_obj is string def_str_val &&
                            sharedPreferences.GetString(key, def_str_val) is string new_str_Val) {
                    prefm.SetPrefValue(prefKey, new_str_Val);
                }
            }
            catch(Exception ex) {
                ex.Dump();
                if(prefKey == PrefKeys.CUSTOM_BG_COLOR && sharedPreferences.GetInt(key, 0) is int color_id) {
                    prefm.SetPrefValue(prefKey, AdHelpers.ToHex(color_id));
                }
            }

            UpdateAll();
        }
        #region Nav Menu Bases
        public virtual void OnPrepareMenu(IMenu menu) {
            menu.Clear();
        }
        public virtual void OnMenuClosed(IMenu menu) {
            menu.Clear();
        }
        public virtual void OnCreateMenu(IMenu menu, MenuInflater mi) {
            menu.Clear();
        }
        public virtual bool OnMenuItemSelected(IMenuItem menuItem) {
            return false;
        }
        #endregion
        #endregion

        #region Protected Methods
        protected void SetNavTitle() {
            if(PreferenceManager is { } pm &&
                pm.Context is AdSettingsActivity sa && sa.SupportActionBar is { } sab &&
                !string.IsNullOrEmpty(Title)) {
                sab.Title = Title;
            }
        }
        protected SharedPrefWrapper PrefService {
            get {
                if(PreferenceManager.Context is not AdSettingsActivity sa) {
                    return null;
                }
                return sa.PrefManager;
            }
        }
        protected void UpdateAll() {
            SetHiddenPrefs();
            UpdateSummaries();
        }


        #region Model builder
        protected Preference AddPref(
            PreferenceGroup category,
            PrefKeys prefKey,
            string fallbackTitle = "",
            string fallbackDetail = "") {
            var context = PreferenceManager.Context;
            Preference pref = null;
            if(!ResourceStrings.U.TryGetValue($"{prefKey}_TITLE", out var titleObj) ||
                titleObj.value is not { } title) {
                title = fallbackTitle;
            }
            if(!ResourceStrings.U.TryGetValue($"{prefKey}_DETAIL", out var detailObj) ||
                detailObj.value is not { } detail) {
                detail = fallbackDetail;
            }
            if(!PrefService.DefPrefValLookup.TryGetValue(prefKey, out object prefValObj)) {
                pref = new(PreferenceManager.Context) {
                    Visible = false
                };
            } else {

                switch(prefValObj) {
                    case bool boolVal:
                        if(PrefService.IsButton(prefKey)) {
                            pref = new Preference(context);
                        } else {
                            var bool_pref = new SwitchPreferenceCompat(context);
                            bool_pref.Checked = PrefService.GetPrefValue<bool>(prefKey);
                            pref = bool_pref;
                        }

                        break;
                    case SliderPrefProps spp:
                        var int_pref = new SeekBarPreference(context);
                        int_pref.UpdatesContinuously = true;
                        int_pref.Min = spp.Min;
                        int_pref.Max = spp.Max;
                        int_pref.Value = PrefService.GetPrefValue<int>(prefKey);
                        pref = int_pref;
                        break;
                    case string strVal:
                        switch(prefKey) {
                            case PrefKeys.CUSTOM_BG_COLOR: {
                                    var color_pref = new ColorPreference(context, null);
                                    if(PrefService.GetPrefValue<string>(PrefKeys.CUSTOM_BG_COLOR) is { } color_hex &&
                                        !string.IsNullOrEmpty(color_hex)) {
                                        color_pref.SaveValue(color_hex.ToAdColor());
                                    }
                                    color_pref.SetPresets(KeyboardPalette.PalettePresets.Select(x => x.ToAdColor()).ToArray());
                                    //color_pref.SaveValue(co)
                                    //color_pref.SetDefaultValue(Integer.ParseInt(color_hex.ToAdColor().ToInt().ToString()));
                                    color_pref.PreferenceChange += Color_pref_PreferenceChange1;
                                    pref = color_pref;
                                }

                                break;
                            case PrefKeys.CUSTOM_BG_PATH: {
                                    if(PrefService.GetPrefValue<string>(prefKey) is { } prefPath &&
                                        !string.IsNullOrEmpty(prefPath)) {
                                        detail = Path.GetFileName(prefPath);
                                    } else {
                                        // none selected
                                        detail = ResourceStrings.U["CommonNoneText"].value;
                                    }

                                    var file_pref = new Preference(context);
                                    file_pref.PreferenceClick += File_pref_PreferenceClick;
                                    pref = file_pref;
                                }
                                break;
                            default: {
                                    var itemsPrefix = $"{prefKey}_ITEM";
                                    var itemsCount = ResourceStrings.U.Keys.Where(x => x.StartsWith(itemsPrefix)).Count();
                                    if(itemsCount == 0) {
                                        // static string pref
                                        pref = new Preference(context);
                                        title = strVal;
                                        break;
                                    }
                                    var subEnum = PrefService.GetListEnum(prefKey);
                                    var entries = Enumerable.Range(0, itemsCount)
                                            .Select(x => ResourceStrings.U[$"{itemsPrefix}{x}"].value)
                                            .ToArray();
                                    var list_pref = new ListPreference(context);
                                    list_pref.SetEntries(entries);
                                    list_pref.SetEntryValues(Enum.GetNames(subEnum));
                                    int sel_idx = (int)PrefService.GetPrefValue<string>(prefKey).ToEnum(subEnum);
                                    list_pref.SetValueIndex(sel_idx);
                                    list_pref.PreferenceChange += List_PreferenceChange;
                                    if(string.IsNullOrEmpty(detail) && entries.ElementAtOrDefault(sel_idx) is { } sel_label) {
                                        detail = sel_label;
                                    }
                                    pref = list_pref;
                                }
                                break;
                        }
                        break;

                    default:
                        // unhandled
                        Debugger.Break();
                        break;
                }
            }

            if(pref != null) {
                pref.Key = prefKey.ToString();
                pref.Title = title;
                pref.Summary = detail;
                category.AddPreference(pref);
                if(PrefService.DepLookup.FirstOrDefault(x => x.Value.Contains(prefKey)) is { } dep_kvp &&
                    !dep_kvp.IsDefault()) {
                    pref.Dependency = dep_kvp.Key.ToString();
                }
            }
            return pref;
        }

        protected void Color_pref_PreferenceChange1(object sender, Preference.PreferenceChangeEventArgs e) {
            if(sender is not ColorPreference pref ||
                !pref.Key.TryToEnum<PrefKeys>(out var prefKey)
                ) {
                return;
            }
            if(e.NewValue is Java.Lang.Integer jintVal) {
                string hex = AdHelpers.ToHex(Integer.ParseInt(jintVal.ToString()));
                PrefService.SetPrefValue<string>(prefKey, hex);
            }
            var test = e.NewValue;
            //
        }

        protected void File_pref_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e) {
            if(sender is not Preference pref ||
                !pref.Key.TryToEnum<PrefKeys>(out var prefKey)) {
                return;
            }

            async Task<FileResult> PickAndShow(PickOptions options) {
                try {

                    var result = await FilePicker.PickAsync(options);
                    return result;
                }
                catch(Exception ex) {
                    // The user canceled or something went wrong
                    ex.Dump();
                }

                return null;
            }

            Handler.Post(async () => {
                var result = await PickAndShow(new PickOptions() { FileTypes = FilePickerFileType.Images });
                if(result is not { } fr) {
                    return;
                }
                PrefService.SetPrefValue(PrefKeys.CUSTOM_BG_PATH, fr.FullPath);
                pref.Summary = Path.GetFileName(fr.FullPath);
            });
        }

        protected void List_PreferenceChange(object sender, Preference.PreferenceChangeEventArgs e) {
            if(sender is not ListPreference lp ||
                !lp.Key.TryToEnum<PrefKeys>(out var pref_key) ||
                PrefService is not { } pfs ||
                e.NewValue is not Java.Lang.String jstrVal ||
                jstrVal.ToString() is not { } newValStr) {
                return;
            }

            int sel_idx = lp.FindIndexOfValue(lp.Value);
            pfs.SetPrefValue<string>(pref_key, newValStr);
            lp.Summary = newValStr;

        }
        #endregion
        #endregion

        #region Private Methods
        void SetHiddenPrefs() {
            IEnumerable<Preference> GetDescendants(Preference pref, bool includeSelf = true) {
                List<Preference> desc = [];
                if(pref == null) {
                    return desc;
                }
                if(includeSelf) {
                    desc.Add(pref);
                }
                if(pref is PreferenceGroup pg) {
                    for(int i = 0; i < pg.PreferenceCount; i++) {
                        if(pg.GetPreference(i) is not { } cp) {
                            continue;
                        }
                        desc.AddRange(GetDescendants(cp));
                    }
                }
                return desc;
            }

            var all_prefs = GetDescendants(this.PreferenceScreen).ToList();
            foreach(var pref in all_prefs) {
                var key = pref.Key.ToEnum<PrefKeys>();
                bool visible = !PrefService.IsHidden(key);
                if(key == PrefKeys.None ||
                    visible == pref.Visible) {
                    continue;
                }

                pref.Visible = visible;
            }
            foreach(var pc in all_prefs.OfType<PreferenceCategory>()) {
                if(Enumerable.Range(0, pc.PreferenceCount).Select(x => pc.GetPreference(x)) is not { } pc_prefs) {
                    continue;
                }
                pc.Visible = pc_prefs.Any(x => x.Visible);
            }
        }
        void UpdateSummaries() {
            if(PrefService is not { } prefm) {
                return;
            }
            foreach(var widget_key in Enum.GetNames(typeof(PrefKeys))) {
                if(!Enum.TryParse(widget_key, out PrefKeys prefKey) ||
                    !PrefService.DefPrefValLookup.TryGetValue(prefKey, out object defValObj) ||
                    defValObj is not SliderPrefProps spp ||
                    this.FindPreference(widget_key) is not SeekBarPreference w) {
                    continue;
                }
                string dep_key = w.Dependency;
                if(dep_key == null && w.Parent is { } w_parent) {
                    dep_key = w_parent.Dependency;
                }
                if(dep_key != null &&
                    this.FindPreference(dep_key) is SwitchPreferenceCompat dep_w &&
                    !dep_w.Checked) {
                    w.Summary = ResourceStrings.U["DisabledText"].value;
                } else {
                    int min = spp.Min;
                    int max = spp.Max;
                    int val = w.Value;
                    int disp_val = val;
                    string suffix = string.Empty;
                    switch(spp.SliderType) {
                        case SliderPrefType.Percent:
                            suffix = "%";
                            disp_val = (int)(((double)val / (double)(max - min)) * 100d);
                            break;
                        case SliderPrefType.Count:

                            break;
                        case SliderPrefType.Milliseconds:
                            suffix = " ms";
                            break;
                    }
                    w.Summary = $"{disp_val}{suffix}";
                }
            }
        }
        #endregion      

    }
}
