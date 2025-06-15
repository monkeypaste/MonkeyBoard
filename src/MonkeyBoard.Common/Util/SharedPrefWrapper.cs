using MonkeyPaste.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class SharedPrefWrapper : ISharedPrefService {
        #region Private Variables
        #endregion

        #region Constants
        public const double DEF_CORNER_RADIUS = 3d;
        #endregion

        #region Statics        
        #endregion

        #region Interfaces

        #region ISharedPrefService Implementation

        public T GetPrefValue<T>(PrefKeys prefKey) {
            //if(prefKey == PrefKeys.SOUND_LEVEL) {
            //    var test = PlatformSharedPref.GetPlatformPrefValue(prefKey);
            //    InputConnection.OnLog($"{test} {test.GetType()}", true);
            //}

            if(PlatformSharedPref is not { } psp ||
                psp.GetPlatformPrefValue(prefKey) is not T tVal) {
                if(PlatformSharedPref != null &&
                    PlatformSharedPref.GetPlatformPrefValue(prefKey) is { } tValObj &&
                    tValObj is IntPtr intPtrVal &&
                    typeof(T) == typeof(int)
                        ) {
                    // BUG ios stores int as IntPtr
                    //return (T)(object)(int)Marshal.PtrToStructure(intPtrVal, typeof(int))/*Marshal.ReadInt32(intPtrVal)*/;

                    return (T)(object)intPtrVal.ToInt32();
                }
                if(DefPrefValLookup.TryGetValue(prefKey, out var defVal)) {
                    if(defVal is SliderPrefProps spp) {
                        return (T)(object)spp.Default;
                    }
                    return (T)defVal;
                }
                throw new Exception($"Unknown pref key '{prefKey}'");
            }
            return tVal;
        }
        public void SetPrefValue<T>(PrefKeys prefKey, T newVal) {
            if(PlatformSharedPref is not { } psp) {
                return;
            }
            psp.SetPlatformPrefValue(prefKey, newVal);

            if(!CurValLookup.ContainsKey(prefKey)) {
                CurValLookup.Add(prefKey, null);
            }
            object oldVal = CurValLookup[prefKey];
            CurValLookup[prefKey] = newVal;

            if(SuppressPrefChanged ||
               newVal.ToStringOrEmpty() == oldVal.ToStringOrEmpty()) {
                return;
            }
            PreferencesChanged?.Invoke(this, new PreferencesChangedEventArgs(prefKey, oldVal, newVal));
        }
        #endregion

        #endregion

        #region Properties


        Dictionary<PrefKeys, object> _defValLookup;
        public Dictionary<PrefKeys, object> DefPrefValLookup {
            get {
                if(_defValLookup == null) {
                    _defValLookup = new Dictionary<PrefKeys, object>() {
                        { PrefKeys.IS_PREF_SETUP_COMPLETE, true },
                        { PrefKeys.DO_NUM_ROW, true },
                        { PrefKeys.DO_EMOJI_KEY, true },
                        { PrefKeys.DO_SOUND, true },
                        { PrefKeys.SOUND_LEVEL, new SliderPrefProps(15,0,100,SliderPrefType.Percent) },
                        { PrefKeys.DO_VIBRATE, true },
                        { PrefKeys.VIBRATE_LEVEL, new SliderPrefProps(1,1,5,SliderPrefType.Count) },
                        { PrefKeys.DO_POPUP, true },
                        { PrefKeys.DO_LONG_POPUP, true },
                        { PrefKeys.LONG_POPUP_DELAY, new SliderPrefProps(500,100,2_000,SliderPrefType.Milliseconds) },
                        //{ PrefKeys.DO_NIGHT_MODE, false },
                        { PrefKeys.THEME_TYPE, KeyboardThemeType.Default.ToString() },
                        { PrefKeys.DO_KEY_BORDERS, true },
                        { PrefKeys.BG_OPACITY, new SliderPrefProps(255,0,255,SliderPrefType.Percent) },
                        { PrefKeys.FG_OPACITY, new SliderPrefProps(255,0,255,SliderPrefType.Percent) },
                        { PrefKeys.FG_BG_OPACITY, new SliderPrefProps(255,0,255,SliderPrefType.Percent) },
                        { PrefKeys.MAX_TEXT_COMPLETION_COUNT, new SliderPrefProps(8,0,20,SliderPrefType.Count) },
                        { PrefKeys.MAX_EMOJI_COMPLETION_COUNT, new SliderPrefProps(8,0,20,SliderPrefType.Count) },
                        { PrefKeys.DO_NEXT_WORD_COMPLETION, true },
                        { PrefKeys.DO_AUTO_CORRECT, true },
                        { PrefKeys.DO_BACKSPACE_UNDOS_LAST_AUTO_CORRECT, false },
                        { PrefKeys.DO_AUTO_CAPITALIZATION, true },
                        { PrefKeys.DO_DOUBLE_SPACE_PERIOD, true },
                        { PrefKeys.DOUBLE_TAP_SPACE_DELAY_MS, new SliderPrefProps(300,0,1000,SliderPrefType.Milliseconds) },
                        { PrefKeys.DO_CURSOR_CONTROL, true },
                        { PrefKeys.CURSOR_CONTROL_SENSITIVITY, new SliderPrefProps(50,1,100,SliderPrefType.Percent) },
                        { PrefKeys.DO_BACKSPACE_REPEAT, true },
                        { PrefKeys.BACKSPACE_REPEAT_SPEED_MS, new SliderPrefProps(50,10,1000,SliderPrefType.Milliseconds) },
                        { PrefKeys.BACKSPACE_REPEAT_DELAY_MS, new SliderPrefProps(400,0,1000,SliderPrefType.Milliseconds) },
                        { PrefKeys.RECENT_EMOJIS_CSV, "🐵" },
                        { PrefKeys.DO_ADD_NEW_WORDS, true },
                        { PrefKeys.OMITTED_EMOJIS_CSV,string.Empty },
                        { PrefKeys.DO_ADD_NEW_PASSWORDS, false },
                        { PrefKeys.LAST_WORD_RANKED_CULTURE, string.Empty },
                        { PrefKeys.LAST_WORD_RANKED_COUNT, -1 },
                        { PrefKeys.AVG_WORD_LEN, -1 },
                        { PrefKeys.WORD_RANK_CSV, string.Empty },
                        { PrefKeys.WORD_RANK_TYPE, string.Empty },
                        { PrefKeys.DEFAULT_UI_CULTURE, string.Empty },
                        { PrefKeys.DEFAULT_KB_CULTURE, string.Empty },
                        { PrefKeys.DEFAULT_KB_GUID, string.Empty },
                        { PrefKeys.DEFAULT_SKIN_TONE_TYPE, EmojiSkinToneType.None.ToString() },
                        { PrefKeys.DEFAULT_HAIR_STYLE_TYPE, EmojiHairStyleType.None.ToString() },
                        { PrefKeys.DO_DOUBLE_TAP_CURSOR_CONTROL, true },
                        { PrefKeys.DO_SWIPE_LEFT_DELETE_WORD, true },
                        { PrefKeys.DO_RESET_DB, false },
                        { PrefKeys.DO_RESET_ALL, false },
                        { PrefKeys.DO_HW_ACCEL, true },
                        { PrefKeys.DO_SET_UI_LANG, false },
                        { PrefKeys.DO_RESTORE_DEFAULT_KB, false },
                        { PrefKeys.DO_UNINSTALL_LANG, false },
                        { PrefKeys.DO_SHOW_LETTER_GROUPS, true },
                        { PrefKeys.DO_SMART_QUOTES, true },
                        { PrefKeys.DO_EXTENDED_SMART_QUOTES, true },
                        { PrefKeys.DO_TABS_AS_SPACES, false },
                        { PrefKeys.TAB_SP_COUNT, new SliderPrefProps(5,1,10,SliderPrefType.Count) },
                        { PrefKeys.DEFAULT_CORNER_RADIUS_FACTOR, new SliderPrefProps((int)DEF_CORNER_RADIUS,0,20,SliderPrefType.Count) },
                        { PrefKeys.DO_SHADOWS, true },
                        { PrefKeys.DO_DYNAMIC_SHADOWS, false },
                        { PrefKeys.DO_DOUBLE_TAP_INPUT, false },
                        { PrefKeys.DO_CUSTOM_BG_COLOR, false },
                        { PrefKeys.CUSTOM_BG_COLOR, string.Empty },
                        { PrefKeys.DO_CUSTOM_BG_PATH, false },
                        { PrefKeys.CUSTOM_BG_PATH, string.Empty },
                        { PrefKeys.DO_SMART_PUNCTUATION, true },
                        { PrefKeys.DO_SHOW_IGNORE_AUTO_CORRECT, false },
                        { PrefKeys.DO_SHOW_EMOJI_IN_TEXT_COMPLETIONS, true },
                        { PrefKeys.DO_LOGGING, true },
                        { PrefKeys.DO_CLEAR_LOG, false },
                        { PrefKeys.DO_COPY_LOG_TO_CLIPBOARD, false },
                        { PrefKeys.DO_FLOAT_LAYOUT, false },
                        { PrefKeys.LOG_LEVEL, MpLogLevel.Verbose.ToString() },

                        // static/runtime prefs
                        { PrefKeys.APP_NAME, "MonkeyBoard.Sample" },
                        { PrefKeys.APP_VERSION, "1.0.0" },
                        { PrefKeys.TERMS_TEXT, @"<terms text>" },
                        { PrefKeys.PRIVACY_TEXT, @"<privacy text>" },
                        { PrefKeys.IS_TABLET, false },

                        { PrefKeys.LOG, string.Empty },
                    };

                    if(IsTablet) {
                        _defValLookup[PrefKeys.DO_POPUP] = false;
                    }
                }
                return _defValLookup;
            }
        }

        Dictionary<PrefCategoryType, PrefKeys[]> _catLookup;
        public Dictionary<PrefCategoryType, PrefKeys[]> CatLookup {
            get {
                if(_catLookup == null) {
                    _catLookup = new Dictionary<PrefCategoryType, PrefKeys[]>() {

                    { PrefCategoryType.LOOK_FEEL, [
                        PrefKeys.THEME_TYPE,
                        PrefKeys.DO_CUSTOM_BG_COLOR,
                        PrefKeys.CUSTOM_BG_COLOR,
                        PrefKeys.DO_CUSTOM_BG_PATH,
                        PrefKeys.CUSTOM_BG_PATH,
                        PrefKeys.DO_KEY_BORDERS,
                        PrefKeys.DO_SHADOWS,
                        PrefKeys.DO_DYNAMIC_SHADOWS,
                        PrefKeys.DEFAULT_CORNER_RADIUS_FACTOR,
                        PrefKeys.BG_OPACITY,
                        PrefKeys.FG_BG_OPACITY,
                        PrefKeys.FG_OPACITY,
                        PrefKeys.DO_HW_ACCEL,
                        ]},
                    { PrefCategoryType.KEYS, [
                        PrefKeys.DO_NUM_ROW,
                        PrefKeys.DO_EMOJI_KEY,
                        ]},
                    { PrefCategoryType.EMOJIS, [
                        PrefKeys.DEFAULT_SKIN_TONE_TYPE,
                        PrefKeys.DEFAULT_HAIR_STYLE_TYPE,
                        ]},
                    { PrefCategoryType.KEY_PRESS, [
                        PrefKeys.DO_SOUND,
                        PrefKeys.SOUND_LEVEL,
                        PrefKeys.DO_VIBRATE,
                        PrefKeys.VIBRATE_LEVEL,
                        PrefKeys.DO_POPUP,
                        PrefKeys.DO_LONG_POPUP,
                        PrefKeys.LONG_POPUP_DELAY,
                        PrefKeys.DO_SWIPE_LEFT_DELETE_WORD,
                        PrefKeys.DO_DOUBLE_TAP_INPUT,
                        PrefKeys.DO_DOUBLE_SPACE_PERIOD,
                        PrefKeys.DOUBLE_TAP_SPACE_DELAY_MS,
                        PrefKeys.DO_SHOW_LETTER_GROUPS,
                        ]},
                    { PrefCategoryType.EDITING, [
                        PrefKeys.DO_AUTO_CAPITALIZATION,
                        PrefKeys.DO_SMART_PUNCTUATION,
                        PrefKeys.DO_TABS_AS_SPACES,
                        PrefKeys.TAB_SP_COUNT,
                        PrefKeys.DO_BACKSPACE_REPEAT,
                        PrefKeys.BACKSPACE_REPEAT_DELAY_MS,
                        PrefKeys.BACKSPACE_REPEAT_SPEED_MS,
                        ]},
                    { PrefCategoryType.COMPLETION, [
                        PrefKeys.DO_NEXT_WORD_COMPLETION,
                        PrefKeys.DO_SHOW_EMOJI_IN_TEXT_COMPLETIONS,
                        PrefKeys.DO_SMART_QUOTES,
                        PrefKeys.DO_EXTENDED_SMART_QUOTES,
                        PrefKeys.MAX_TEXT_COMPLETION_COUNT,
                        PrefKeys.MAX_EMOJI_COMPLETION_COUNT,
                        PrefKeys.DO_ADD_NEW_WORDS,
                        PrefKeys.DO_ADD_NEW_PASSWORDS,
                        PrefKeys.DO_RESET_DB,
                        ]},
                    { PrefCategoryType.AUTO_CORRECT, [
                        PrefKeys.DO_AUTO_CORRECT,
                        PrefKeys.DO_BACKSPACE_UNDOS_LAST_AUTO_CORRECT,
                        PrefKeys.DO_SHOW_IGNORE_AUTO_CORRECT,
                        ]},
                    { PrefCategoryType.CURSOR_CONTROL, [
                        PrefKeys.DO_CURSOR_CONTROL,
                        PrefKeys.DO_DOUBLE_TAP_CURSOR_CONTROL,
                        PrefKeys.CURSOR_CONTROL_SENSITIVITY,
                        ]},
                    { PrefCategoryType.DIAGNOSTIC, [
                        PrefKeys.DO_LOGGING,
                        PrefKeys.LOG_LEVEL,
                        PrefKeys.DO_COPY_LOG_TO_CLIPBOARD,
                        PrefKeys.DO_CLEAR_LOG,
                        ]},
                    };
                }
                return _catLookup;
            }
        }


        Dictionary<PrefKeys, Action<Action>> _prefClickActionLookup;
        public Dictionary<PrefKeys, Action<Action>> PrefClickActionLookup {
            get {
                if(_prefClickActionLookup == null) {
                    void finishCommandWrapper(PrefKeys key, Action action) {
                        if(action != null) {
                            action.Invoke();
                            return;
                        }
                        if(DefPrefValLookup[key] is bool) {
                            // unset cmd pref;
                            SetPrefValue<bool>(key, false);
                        }
                    }

                    _prefClickActionLookup = new Dictionary<PrefKeys, Action<Action>>() {
                        {
                            PrefKeys.DO_RESET_DB,
                            async(complAction) => {
                                if(InputConnection is not {} ic) {
                                    return;
                                }
                                 bool confirm = await ic.OnConfirmAlertAsync(
                                     ResourceStrings.U["ConfirmResetCompDbTitle"].value,
                                     ResourceStrings.U["ConfirmResetCompDbMsg"].value);
                                if (!confirm) {
                                    // canceled
                                    return;
                                }
                                await WordDb.ResetDbAsync_query(InputConnection);

                                ic.OnAlert(
                                    string.Empty,
                                    ResourceStrings.U["ResetComplDbCompleteMsg"].value);
                                finishCommandWrapper(PrefKeys.DO_RESET_DB,complAction);
                            }
                        },
                        {
                            PrefKeys.DO_RESET_ALL,
                            async (complAction) => {
                                if(InputConnection is not {} ic) {
                                    return;
                                }
                                bool confirm = await ic.OnConfirmAlertAsync(
                                    ResourceStrings.U["ConfirmResetAllTitle"].value,
                                    ResourceStrings.U["ConfirmResetAllMessage"].value);
                                if(!confirm) {
                                    // canceled
                                    return;
                                }
                                RestoreDefaults();
                                await Task.Delay(500);
                                ic.OnAlert(string.Empty,ResourceStrings.U["ResetCompleteText"].value);
                                finishCommandWrapper(PrefKeys.DO_RESET_ALL,complAction);
                            }
                        },
                        {
                            PrefKeys.DO_COPY_LOG_TO_CLIPBOARD,
                            (complAction) => {
                                Logger.CopyLogToClipboard();
                                finishCommandWrapper(PrefKeys.DO_COPY_LOG_TO_CLIPBOARD,complAction);
                            }
                        },
                        {
                            PrefKeys.DO_CLEAR_LOG,
                            (complAction) => {
                                Logger.ClearLog();
                                finishCommandWrapper(PrefKeys.DO_CLEAR_LOG,complAction);
                            }
                        }
                    };
                }
                return _prefClickActionLookup;
            }
        }
        Dictionary<PrefKeys, List<PrefKeys>> _depLookup;
        public Dictionary<PrefKeys, List<PrefKeys>> DepLookup {
            get {
                if(_depLookup == null) {
                    _depLookup = new Dictionary<PrefKeys, List<PrefKeys>>() {
                        {
                            PrefKeys.DO_BACKSPACE_REPEAT,
                            [
                                PrefKeys.BACKSPACE_REPEAT_DELAY_MS,
                                PrefKeys.BACKSPACE_REPEAT_SPEED_MS
                            ]
                        },
                        {
                            PrefKeys.DO_CURSOR_CONTROL,
                            [
                                PrefKeys.CURSOR_CONTROL_SENSITIVITY,
                                PrefKeys.DO_DOUBLE_TAP_CURSOR_CONTROL
                            ]
                        },
                        {
                            PrefKeys.DO_DOUBLE_SPACE_PERIOD,
                            [
                                PrefKeys.DOUBLE_TAP_SPACE_DELAY_MS
                            ]
                        },
                        {
                            PrefKeys.DO_ADD_NEW_WORDS,
                            [
                                PrefKeys.DO_ADD_NEW_PASSWORDS
                            ]
                        },
                        {
                            PrefKeys.DO_AUTO_CORRECT,
                            [
                                PrefKeys.DO_BACKSPACE_UNDOS_LAST_AUTO_CORRECT,
                                PrefKeys.DO_SHOW_IGNORE_AUTO_CORRECT,
                            ]
                        },
                        {
                            PrefKeys.DO_LONG_POPUP,
                            [
                                PrefKeys.LONG_POPUP_DELAY
                            ]
                        },
                        {
                            PrefKeys.DO_SOUND,
                            [
                                PrefKeys.SOUND_LEVEL
                            ]
                        },
                        {
                            PrefKeys.DO_VIBRATE,
                            [
                                PrefKeys.VIBRATE_LEVEL
                            ]
                        },
                        {
                            PrefKeys.DO_TABS_AS_SPACES,
                            [
                                PrefKeys.TAB_SP_COUNT
                            ]
                        },
                        {
                            PrefKeys.DO_KEY_BORDERS,
                            [
                                PrefKeys.DO_SHADOWS
                            ]
                        },
                        {
                            PrefKeys.DO_SHADOWS,
                            [
                                PrefKeys.DO_DYNAMIC_SHADOWS
                            ]
                        },
                        {
                            PrefKeys.DO_LOGGING,
                            [
                                PrefKeys.LOG_LEVEL,
                                PrefKeys.DO_CLEAR_LOG,
                                PrefKeys.DO_COPY_LOG_TO_CLIPBOARD,
                            ]
                        },
                    };
                    if(OperatingSystem.IsIOS()) {
                        _defValLookup.AddOrReplace(PrefKeys.DO_CUSTOM_BG_COLOR, new PrefKeys[] { PrefKeys.CUSTOM_BG_COLOR });
                        _defValLookup.AddOrReplace(PrefKeys.DO_CUSTOM_BG_PATH, new PrefKeys[] { PrefKeys.CUSTOM_BG_PATH });
                    }
                }
                return _depLookup;
            }
        }
        Dictionary<PrefKeys, object> CurValLookup { get; set; } = [];
        IPlatformSharedPref PlatformSharedPref { get; set; }
        IKeyboardInputConnection InputConnection { get; set; }
        bool SuppressPrefChanged { get; set; }
        public double VibrateDurMs { get; private set; }
        public float SoundVol { get; private set; }
        public KbDebugLogger Logger { get; private set; }
        bool IsTablet { get; set; }
        #endregion

        #region Events
        public event EventHandler<PreferencesChangedEventArgs> PreferencesChanged;
        #endregion

        #region Constructors
        public SharedPrefWrapper() {
        }

        #endregion

        #region Public Methods
        public void Init(IPlatformSharedPref platformSharedPref, IKeyboardInputConnection inputConnection, bool isTablet) {

            InputConnection = inputConnection;
            PlatformSharedPref = platformSharedPref;
            IsTablet = isTablet;
            Logger = new KbDebugLogger(this, InputConnection);
            PreferencesChanged += SharedPrefWrapper_PreferencesChanged;
            bool is_setup_done = GetPrefValue<bool>(PrefKeys.IS_PREF_SETUP_COMPLETE);

            if(!is_setup_done) {
                // initial startup
                MpConsole.WriteLine($"First run detected");

                RestoreDefaults();
                SetPrefValue(PrefKeys.IS_PREF_SETUP_COMPLETE, true);
            }
            CurValLookup = GetAllPrefs();

            UpdateAll();
        }
        public KeyboardFlags UpdateFlags(KeyboardFlags flags) {
            UpdateAll();
            return flags;
        }
        void UpdateAll() {

            UpdateVibration();
            UpdateVolume();
        }
        public void RestoreDefaults() {
            var old_prefs = GetAllPrefs();
            SuppressPrefChanged = true;
            foreach(var def_kvp in DefPrefValLookup) {
                string key = def_kvp.Key.ToString();
                object val = def_kvp.Value;
                if(val is bool boolVal) {
                    SetPrefValue(def_kvp.Key, boolVal);
                } else if(val is SliderPrefProps spp) {
                    SetPrefValue(def_kvp.Key, spp.Default);
                } else if(val is int intVal) {
                    SetPrefValue(def_kvp.Key, intVal);
                } else if(val is string strVal) {
                    SetPrefValue(def_kvp.Key, strVal);
                } else {
                    // unhandled
                    //Debugger.Break();

                }
            }

            // gather static pref values
            if(InputConnection is { } ic) {
                string version = ic.VersionInfo.Version;
                SetPrefValue(PrefKeys.APP_VERSION, version);

                //Task.Run(async () => {
                //    string terms = await MpAvFileIo.ReadTextFromUriAsync(@"https://www.monkeypaste.com/legal/terms");
                //    SetPrefValue(PrefKeys.TERMS_TEXT, terms);

                //    string privacy = await MpAvFileIo.ReadTextFromUriAsync(@"https://www.monkeypaste.com/legal/privacy");
                //    SetPrefValue(PrefKeys.PRIVACY_TEXT, privacy);
                //});                
            }

            SuppressPrefChanged = false;
            var new_prefs = GetAllPrefs();
            if(new_prefs.Keys.Where(x => old_prefs[x].ToStringOrEmpty() != new_prefs[x].ToStringOrEmpty()) is { } changed_keys &&
                changed_keys.Any()) {
                var change_lookup = changed_keys.ToDictionary(x => x, x => (old_prefs[x], new_prefs[x]));
                PreferencesChanged?.Invoke(this, new PreferencesChangedEventArgs(change_lookup));
            }
        }

        public string GetPageIconName(SettingsPageType pageType) {
            switch(pageType) {
                case SettingsPageType.PREFERENCES:
                    return "pref.png";
                case SettingsPageType.LANG_PACKS:
                    return "lang.png";
                case SettingsPageType.FEEDBACK:
                    return "feedback.png";
                case SettingsPageType.ABOUT:
                    return "about.png";
            }
            return "error.png";
        }
        public Type? GetListEnum(PrefKeys enumPref) {
            switch(enumPref) {
                case PrefKeys.THEME_TYPE:
                    return typeof(KeyboardThemeType);
                case PrefKeys.DEFAULT_HAIR_STYLE_TYPE:
                    return typeof(EmojiHairStyleType);
                case PrefKeys.DEFAULT_SKIN_TONE_TYPE:
                    return typeof(EmojiSkinToneType);
                case PrefKeys.LOG_LEVEL:
                    return typeof(MpLogLevel);
            }
            return default;
        }

        public bool IsButton(PrefKeys prefKey) {
            switch(prefKey) {
                case PrefKeys.DO_RESET_ALL:
                case PrefKeys.DO_RESET_DB:
                //case PrefKeys.DO_SET_UI_LANG:
                //case PrefKeys.DO_RESTORE_DEFAULT_KB:
                case PrefKeys.DO_CLEAR_LOG:
                case PrefKeys.DO_COPY_LOG_TO_CLIPBOARD:
                    return true;
            }
            return false;
        }
        public bool IsHidden(PrefKeys prefKey) {
            switch(prefKey) {
                case PrefKeys.LOG_LEVEL:
                    return true;
                case PrefKeys.CUSTOM_BG_PATH:
                    return OperatingSystem.IsAndroid() && !GetPrefValue<bool>(PrefKeys.DO_CUSTOM_BG_PATH);
                case PrefKeys.CUSTOM_BG_COLOR:
                    return OperatingSystem.IsAndroid() && !GetPrefValue<bool>(PrefKeys.DO_CUSTOM_BG_COLOR);
                case PrefKeys.SOUND_LEVEL:
                    return OperatingSystem.IsIOS();
                case PrefKeys.DO_NUM_ROW:
                    return IsTablet;
                case PrefKeys.DO_TABS_AS_SPACES:
                case PrefKeys.TAB_SP_COUNT:
                    return !IsTablet;
                case PrefKeys.DO_ADD_NEW_PASSWORDS:
                    if(OperatingSystem.IsIOS()) {
                        return true;
                    }
                    break;
            }
            return false;
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        private void SharedPrefWrapper_PreferencesChanged(object sender, PreferencesChangedEventArgs e) {
            if(e is not { } change_args ||
                InputConnection is not { } ic ||
                ic.FeedbackHandler is not { } feedbackHandler) {
                return;
            }

            foreach(var pref_kvp in change_args.ChangedPrefLookup) {
                switch(pref_kvp.Key) {
                    case PrefKeys.SOUND_LEVEL:
                        UpdateVolume();
                        if(pref_kvp.Value.newValue is not int soundVal) {
                            break;
                        }
                        feedbackHandler.PlaySound(soundVal / 100d);
                        break;
                    case PrefKeys.VIBRATE_LEVEL:
                        UpdateVibration();
                        if(pref_kvp.Value.newValue is not int vibrateLevel) {
                            break;
                        }
                        feedbackHandler.Vibrate(vibrateLevel / 4);
                        break;
                }
            }
        }

        void UpdateVibration() {
            double dur = 0;
            if(GetPrefValue<bool>(PrefKeys.DO_VIBRATE)) {
                int lvl = GetPrefValue<int>(PrefKeys.VIBRATE_LEVEL);
                dur = lvl + 1;
            }
            // ends up being 2-6 or 0
            VibrateDurMs = dur;
        }
        void UpdateVolume() {
            float volume = 0;
            if(GetPrefValue<bool>(PrefKeys.DO_SOUND)) {
                volume = (float)GetPrefValue<int>(PrefKeys.SOUND_LEVEL);
            }
            SoundVol = volume;
        }
        Dictionary<PrefKeys, object> GetAllPrefs() {
            if(PlatformSharedPref is not { } psp) {
                return [];
            }
            return DefPrefValLookup.ToDictionary(x => x.Key, x => psp.GetPlatformPrefValue(x.Key));
        }
        #endregion
    }
}
