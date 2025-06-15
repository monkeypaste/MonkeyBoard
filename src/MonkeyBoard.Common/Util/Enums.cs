using System;

namespace MonkeyBoard.Common {
    public enum KeyRowLayoutType {
        None = 0,
        Left,
        Center,
        Right,
        Stretch
    }
    public enum VectorDirection {
        None = 0,
        Up,
        Right,
        Down,
        Left
    }
    public enum TouchEventType {
        None,
        Press,
        Move,
        Release
    }
    public enum EmojiQualificationType {
        Unqualified = 0,
        MinimallyQualified,
        FullyQualified,
        Component
    }
    public enum EmojiSkinToneType {
        None = 0,
        Light,
        MediumLight,
        Medium,
        MediumDark,
        Dark
    }
    public enum EmojiHairStyleType {
        None = 0,
        Red,
        Curly,
        White,
        Bald
    }
    public enum EmojiPageType {
        None = 0,
        Recents,
        Smileys,
        People,
        Animals,
        Food,
        World,
        Activities,
        Objects,
        Symbols,
        Flags,
        Component
    }
    public enum SliderPrefType {
        None = 0,
        Percent,
        Milliseconds,
        Count
    }

    public enum MenuItemType {
        None = 0,
        BackButton,
        OptionsButton,
        TextCompletionItem,
        EmojiCompletionItem,
        TabItem
    }
    public enum MenuPageType {
        //None = 0,
        TabSelector,
        TextCompletions,
    }
    public enum MenuTabItemType {
        None = 0,
        Emoji,
        Speech,
        Clipboard,
        Plugins,
        Config,
        Gifly
    }
    [Flags]
    public enum WordBreakTypes : long {
        None = 0,
        Grammatical = 1L << 1,
        UpperToLowerCase = 1L << 2, // camel or pascal case
        UnderScore = 1L << 3, // snake case
        Hyphen = 1L << 4, // kebob case
    }

    [Flags]
    public enum KeyboardFeedbackFlags : long {
        None = 0,
        Vibrate = 1L << 1,
        Click = 1L << 2,
        Return = 1L << 3,
        Delete = 1L << 4,
        Space = 1L << 5,
        Invalid = 1L << 7,
    }
    public enum SpecialKeyType {
        None = 0,
        Shift,
        Backspace,
        SymbolToggle,
        NumberSymbolsToggle,
        Tab,
        CapsLock,
        Emoji,
        ArrowLeft,
        ArrowRight,
        NextKeyboard,
        Done, // PrimarySpecial (default)
        Search, // PrimarySpecial (ios only)
        Go, // PrimarySpecial
        Enter, // PrimarySpecial
        Next, // PrimarySpecial
        Previous,
        Send, // PrimarySpecial

        // new (unused)
        QuickText,
        Ctrl,
        ArrowUp,
        ArrowDown,
        Space,
        Escape,
        Settings,
        VoiceInput,
        AlphabetToggle,
        KeyboardModeChange,
        Collapse,
        Domain,
        End,
        Home,
        QuickTextPopup,
        Disabled

    }
    public enum ShiftStateType {
        None = 0,
        Shift,
        ShiftLock
    }
    public enum CharSetType {
        Letters = 0,
        Symbols1,
        Symbols2,
        Numbers1,
        Numbers2,
    }
    public enum KeyboardThemeType {
        Default = 0,
        Light,
        Dark
    }
    public enum SettingsPageType {
        None = 0,
        PREFERENCES,
        LANG_PACKS,
        FEEDBACK,
        ABOUT
    }
    public enum PrefCategoryType {
        None = 0,
        LOOK_FEEL,
        KEYS,
        EMOJIS,
        KEY_PRESS,
        EDITING,
        COMPLETION,
        AUTO_CORRECT,
        CURSOR_CONTROL,
        DIAGNOSTIC
    }
    public enum PrefKeys {
        None = 0,
        IS_PREF_SETUP_COMPLETE,
        DO_NUM_ROW,
        DO_EMOJI_KEY,
        DO_SOUND,
        SOUND_LEVEL, // 0-100, 15
        DO_VIBRATE,
        VIBRATE_LEVEL, // 0-5, 1
        DO_POPUP,
        DO_LONG_POPUP,
        LONG_POPUP_DELAY, // 0-1000, 500
        THEME_TYPE,
        DO_KEY_BORDERS,
        BG_OPACITY, // 0-255, 255
        FG_OPACITY, // 0-255, 255
        FG_BG_OPACITY,
        DO_NEXT_WORD_COMPLETION,
        MAX_TEXT_COMPLETION_COUNT, //0-20, 8
        MAX_EMOJI_COMPLETION_COUNT, //0-40, 16
        DO_AUTO_CORRECT,
        DO_BACKSPACE_UNDOS_LAST_AUTO_CORRECT,
        DO_AUTO_CAPITALIZATION,
        DO_DOUBLE_SPACE_PERIOD,
        DO_CURSOR_CONTROL,
        CURSOR_CONTROL_SENSITIVITY, //0-100, 50
        BACKSPACE_REPEAT_SPEED_MS,
        RECENT_EMOJIS_CSV,
        BACKSPACE_REPEAT_DELAY_MS,
        DOUBLE_TAP_SPACE_DELAY_MS,
        DO_BACKSPACE_REPEAT,
        DO_ADD_NEW_WORDS,
        OMITTED_EMOJIS_CSV,
        DO_ADD_NEW_PASSWORDS,
        LAST_WORD_RANKED_CULTURE,
        LAST_WORD_RANKED_COUNT,
        AVG_WORD_LEN,
        WORD_RANK_CSV,
        WORD_RANK_TYPE,
        DEFAULT_UI_CULTURE,
        DEFAULT_KB_CULTURE,
        DEFAULT_KB_GUID,
        DEFAULT_SKIN_TONE_TYPE,
        DEFAULT_HAIR_STYLE_TYPE,
        DO_DOUBLE_TAP_CURSOR_CONTROL,
        DO_SWIPE_LEFT_DELETE_WORD,
        DO_SHOW_LETTER_GROUPS,
        DO_SMART_QUOTES,
        DO_EXTENDED_SMART_QUOTES,
        DO_TABS_AS_SPACES,
        TAB_SP_COUNT,
        DEFAULT_CORNER_RADIUS_FACTOR,
        DO_SHADOWS,
        DO_DYNAMIC_SHADOWS,
        DO_DOUBLE_TAP_INPUT,
        DO_SMART_PUNCTUATION,
        DO_SHOW_IGNORE_AUTO_CORRECT,
        DO_SHOW_EMOJI_IN_TEXT_COMPLETIONS,
        DO_LOGGING,
        DO_CLEAR_LOG,
        DO_COPY_LOG_TO_CLIPBOARD,
        DO_FLOAT_LAYOUT,
        LOG_LEVEL,

        DO_CUSTOM_BG_COLOR,
        CUSTOM_BG_COLOR,
        DO_CUSTOM_BG_PATH,
        CUSTOM_BG_PATH,

        // pref cmds
        DO_UNINSTALL_LANG,
        DO_RESET_DB,
        DO_RESET_ALL,
        DO_HW_ACCEL,
        DO_SET_UI_LANG,
        DO_RESTORE_DEFAULT_KB,

        // static/runtime 
        APP_VERSION,
        APP_NAME,
        TERMS_TEXT,
        PRIVACY_TEXT,
        IS_TABLET,

        LOG
    }

    public enum WordRankType {
        Frequency,
        RowId
    }
    public enum KeyboardLayoutType {
        None = 0,
        Normal,
        PhoneNumber,
        Digits,
        Pin,
        Url,
        Email
    }

    [Flags]
    public enum KeyboardFlags : long {
        None = 0,

        // PLATFORM
        Android = 1L << 1,
        iOS = 1L << 2,

        // ORIENTATION
        Portrait = 1L << 3,
        Landscape = 1L << 4,

        // LAYOUT
        FloatLayout = 1L << 5,
        FullLayout = 1L << 6,

        // MODES
        Normal = 1L << 7,
        Numbers = 1L << 8,
        Digits = 1L << 9,
        Pin = 1L << 10,
        Url = 1L << 11,
        Email = 1L << 12,

        // ACTIONS
        Next = 1L << 13,
        Search = 1L << 14,
        Done = 1L << 15,
        Go = 1L << 16,
        Previous = 1L << 17,
        Send = 1L << 18,

        // SPECIAL STATES
        Password = 1L << 19,
        MultiLine = 1L << 20,

        // THEME
        Light = 1L << 21,
        Dark = 1L << 22,

        // DEVICE
        Mobile = 1L << 23,
        Tablet = 1L << 24,

        // LOOK & FEEL
        PlatformView = 1L << 25,
        OneHanded = 1L << 26,

        // RESET
        NotDirty = 1L << 27,
        EmojiSearch = 1L << 28,

        // TO ADD
        Join = 1L << 29,
        //?
        Tweet = 1L << 30,
        // ?
        Call = 1L << 31


    }
}
