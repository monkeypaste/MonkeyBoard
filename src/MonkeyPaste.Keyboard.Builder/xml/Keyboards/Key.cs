
using MonkeyPaste.Common;
using MonkeyBoard.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;

namespace MonkeyBoard.Builder {

    [XmlRoot(ElementName = "Key")]
    public class Key : KeyboardFileBase {
        #region Statics

        [XmlIgnore]
        public static List<string> UnknownParts { get; set; } = [];
        #endregion

        #region Properties

        #region Xml
        [XmlAttribute(AttributeName = "codes")]
        public string Codes { get; set; }

        [XmlAttribute(AttributeName = "popupCharacters")]
        public string PopupCharacters { get; set; }

        [XmlAttribute(AttributeName = "keyEdgeFlags")]
        public string KeyEdgeFlags { get; set; }

        [XmlAttribute(AttributeName = "popupKeyboard")]
        public string PopupKeyboard { get; set; }

        [XmlAttribute(AttributeName = "horizontalGap")]
        public string HorizontalGap { get; set; }

        [XmlAttribute(AttributeName = "keyLabel")]
        public string KeyLabel { get; set; }

        [XmlAttribute(AttributeName = "keyWidth")]
        public string KeyWidth { get; set; }

        [XmlAttribute(AttributeName = "keyHeight")]
        public string KeyHeight { get; set; }

        [XmlAttribute(AttributeName = "isModifier")]
        public bool IsModifier { get; set; }

        [XmlAttribute(AttributeName = "isSticky")]
        public bool IsSticky { get; set; }

        [XmlAttribute(AttributeName = "isRepeatable")]
        public bool IsRepeatable { get; set; }

        [XmlAttribute(AttributeName = "shiftedCodes")]
        public string ShiftedCodes { get; set; }

        [XmlAttribute(AttributeName = "hintLabel")]
        public string HintLabel { get; set; }
        #endregion

        #region Custom

        [XmlIgnore]
        public int Row { get; set; }
        [XmlIgnore]
        public int Column { get; set; }
        [XmlIgnore]
        public bool IsNeutral { get; set; }

        string _primaryCharacter;
        [XmlIgnore]
        public string PrimaryCharacter {
            get {
                if(FilePath.Contains("qwerty_compact.xml") && (Row == 0 || Row == 1) && Column == 0) {

                }
                if (_primaryCharacter != null) {
                    return _primaryCharacter;
                }
                if (GetLetters(Codes) is { } codes_letter &&
                    codes_letter.FirstOrDefault() is { } code_let) {
                    //return code_let;
                    var result = string.Join(string.Empty, codes_letter);
                    if(result.Length > 1 && result.EndsWith("|")) {

                    }
                    return result;
                }
                return string.Empty;
            }
            set => _primaryCharacter = value;
        }
        string _shiftCharacter;
        [XmlIgnore]
        public string ShiftCharacter {
            get {
                if (FilePath != null &&
                    FilePath.ToLower().Contains("colemak") &&
                    Codes != null && Codes.Contains(",")) {

                }
                if (_shiftCharacter != null) {
                    return _shiftCharacter;
                }
                if (!string.IsNullOrEmpty(ShiftedCodes) &&
                    GetLetters(ShiftedCodes) is { } shift_letters &&
                    shift_letters.Any()) {
                    return string.Join(string.Empty, shift_letters);
                } else if (Codes != null &&
                    Codes.Contains(",") &&
                    GetLetters(Codes) is { } codes_letter &&
                    codes_letter.ToList() is { } codes &&
                    codes.Count > 1 &&
                    codes[1] is { } poss_shift_text &&
                    !int.TryParse(poss_shift_text, out _)) {
                    // don't use secondary codes if they're numbers since numbers is handled
                    return poss_shift_text;
                }
                return PrimaryCharacter.ToUpper();
            }
            set => _shiftCharacter = value;
        }

        private string _secondaryCharactersData;
        [XmlIgnore]
        public string SecondaryCharactersData {
            get {
                if (_secondaryCharactersData != null) {
                    return _secondaryCharactersData;
                }
                return string.Join(string.Empty, SecondaryCharacters);
            }
            set => _secondaryCharactersData = value;
        }
        IEnumerable<string> _secondaryCharacters;
        [XmlIgnore]
        public IEnumerable<string> SecondaryCharacters {
            get {
                if (_secondaryCharacters != null) {
                    return _secondaryCharacters;
                }
                if (GetLetters(PopupCharacters) is { } popup_letters &&
                    popup_letters.Any()) {
                    return popup_letters;
                }
                if (GetLetters(PopupKeyboard) is { } popup_kb_letters &&
                    popup_kb_letters.Any()) {
                    return popup_kb_letters;
                }
                return [];
            }
            set => _secondaryCharacters = value;
        }
        #endregion

        #region Maps
        /*
         
        */
        Dictionary<string, string> SpecialCodeConstantsLookup = new Dictionary<string, string>() {
            {"shift","SpecialKeyType.Shift"},
            {"delete","SpecialKeyType.Backspace"},
            {"mode_symbols","SpecialKeyType.SymbolToggle"},
            {"quick_text","SpecialKeyType.QuickText"},
            {"ctrl","SpecialKeyType.Ctrl"},
            {"arrow_left","SpecialKeyType.ArrowLeft"},
            {"arrow_up","SpecialKeyType.ArrowUp"},
            {"arrow_down","SpecialKeyType.ArrowDown"},
            {"arrow_right","SpecialKeyType.ArrowRight"},
            {"enter","SpecialKeyType.Enter"},
            {"tab","SpecialKeyType.Tab"},
            {"space","SpecialKeyType.Space"},
            {"escape","SpecialKeyType.Escape"},
            {"settings","SpecialKeyType.Settings"},
            {"voice_input","SpecialKeyType.VoiceInput"},
            {"mode_alphabet","SpecialKeyType.AlphabetToggle"},
            {"keyboard_mode_change","SpecialKeyType.KeyboardModeChange"},
            {"domain","SpecialKeyType.Domain"},
            {"move_end","SpecialKeyType.End"},
            {"move_home","SpecialKeyType.Home"},
            {"quick_text_popup","SpecialKeyType.QuickTextPopup"},
            {"disabled","SpecialKeyType.Disabled"},
        };
        Dictionary<string, string> CodeConstants = new Dictionary<string, string>() {
            // from KeyCodes.java
            {"SPACE","32"},
{"ENTER","10"},
{"TAB","9"},
{"ESCAPE","27"},

{"DELETE","-5"},
{"DELETE_WORD","-7"},
{"FORWARD_DELETE","-8"},

{"QUICK_TEXT","-10"},
{"QUICK_TEXT_POPUP","-102"},
{"CLEAR_QUICK_TEXT_HISTORY","-103"},
{"DOMAIN","-9"},

{"SHIFT","-1"},
{"ALT","-6"},
{"CTRL","-11"},
{"SHIFT_LOCK","-14"},
{"CTRL_LOCK","-15"},

{"MODE_SYMBOLS","-2"},
{"MODE_ALPHABET","-99"},
{"MODE_ALPHABET_POPUP","-98"},
{"KEYBOARD_CYCLE","-97"},
{"KEYBOARD_REVERSE_CYCLE","-96"},
{"KEYBOARD_CYCLE_INSIDE_MODE","-95"},
{"KEYBOARD_MODE_CHANGE","-94"},

{"ARROW_LEFT","-20"},
{"ARROW_RIGHT","-21"},
{"ARROW_UP","-22"},
{"ARROW_DOWN","-23"},
{"MOVE_HOME","-24"},
{"MOVE_END","-25"},

{"SETTINGS","-100"},
{"CANCEL","-3"},
{"CLEAR_INPUT","-13"},
{"VOICE_INPUT","-4"},

{"DISABLED","0"},

{"SPLIT_LAYOUT","-110"},
{"MERGE_LAYOUT","-111"},
{"COMPACT_LAYOUT_TO_LEFT","-112"},
{"COMPACT_LAYOUT_TO_RIGHT","-113"},

{"UTILITY_KEYBOARD","-120"},

{"CLIPBOARD_COPY","-130"},
{"CLIPBOARD_CUT","-131"},
{"CLIPBOARD_PASTE","-132"},
{"CLIPBOARD_PASTE_POPUP","-133"},
{"CLIPBOARD_SELECT","-134"},
{"CLIPBOARD_SELECT_ALL","-135"},

{"UNDO","-136"},
{"REDO","-137"},

{"IMAGE_MEDIA_POPUP","-140"},

{"PRE_PREPARED_ABBREVIATIONS_POPUP","-150"},
{"PRE_PREPARED_TEXT_POPUP","-151"},
{"PRE_PREPARED_EMAILS_POPUP","-152"},

{"EXTERNAL_INTEGRATION","-200"},
        };
        #endregion

        #endregion

        #region Constructors
        #endregion

        #region Public Methods

        public Key Clone() {
            var k = new Key() {
                Row = Row,
                Column = Column,
                PrimaryCharacter = PrimaryCharacter,
                SecondaryCharacters = SecondaryCharacters.ToList(),
                SecondaryCharactersData = SecondaryCharactersData,
                ShiftCharacter = ShiftCharacter,
                IsNeutral = IsNeutral,
                HintLabel = HintLabel,
                ShiftedCodes = ShiftedCodes,
                FilePath = FilePath,
                IsRepeatable = IsRepeatable,
                IsSticky = IsSticky,
                IsModifier = IsModifier,
                KeyHeight = KeyHeight,
                KeyWidth = KeyWidth,
                HorizontalGap = HorizontalGap,
                PopupKeyboard = PopupKeyboard,
                PopupCharacters = PopupCharacters,
                Codes = Codes
            };
            return k;
        }

        public KeyFormat ToKeyFormat() {
            string pv = PrimaryCharacter;
            if (PrimaryCharacter != null &&
                !PrimaryCharacter.StartsWith("Special") &&
                !PrimaryCharacter.StartsWith("none")) {
                // NOTE none lead keys are already setup with symbols so ignore
                var neutrals = Keyboard.NeutralSymbols1.Row.SelectMany(x => x.Key).Union(Keyboard.NeutralSymbols2.Row.SelectMany(x => x.Key));
                if (neutrals.Where(x => x.Row == Row && x.Column == Column) is { } other_primaries1 &&
                    other_primaries1.Where(x=>!x.PrimaryCharacter.StartsWith("Special")) is { } other_primaries &&
                    other_primaries.Any()) {
                    if(!PrimaryCharacter.StartsWith("comma") && PrimaryCharacter.Length > 1) {
                        // what are the characters?
                        //Debugger.Break();
                        MpConsole.WriteLine(PrimaryCharacter);
                    } else {
                        pv += string.Join(string.Empty, other_primaries.Select(x=>x.PrimaryCharacter));
                    }
                    
                }
            }
                
            return new KeyFormat() {
                row = Row,
                column = Column,
                primaryValues = pv,                
                shiftValues = ShiftCharacter
            };
        }

        public override string ToString() {

            return PrimaryCharacter;//$"{PrimaryCharacter} {string.Join(",", PopupCharacters)}";
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        IEnumerable<string> GetLetters(object element) {
            var letters = new List<string>();
            if (element is string elmStr) {
                if (!string.IsNullOrEmpty(elmStr) &&
                elmStr.SplitNoEmpty(",") is { } codes_parts) {
                    foreach (string code_part in codes_parts) {
                        if (IsNeutral) {
                            letters.Add(code_part);
                            continue;
                        }
                        // xml ref
                        if (code_part.StartsWith("@xml") &&
                                    code_part.Replace("@xml/", string.Empty) is { } rel_file_name) {
                            string ref_path = Path.Combine(
                                Path.GetDirectoryName(FilePath),
                                rel_file_name + ".xml");
                            if (!File.Exists(ref_path) ||
                                Keyboard.Deserialize(ref_path) is not { } kb_frag) {
                                // where is it?
                                Debugger.Break();
                                continue;
                            }
                            var popups = kb_frag.Row.SelectMany(x => x.Key).Select(x => x.PrimaryCharacter).Where(x => !string.IsNullOrEmpty(x)).ToList();
                            letters.AddRange(popups);
                        } else if (code_part.StartsWith("@integer")) {
                            var special_lookup = new Dictionary<string, string>() {
                               {"@integer/key_code_shift","SpecialKeyType.Shift"},
                                {"@integer/key_code_delete","SpecialKeyType.Backspace"},
                                {"@integer/key_code_mode_symbols","SpecialKeyType.SymbolToggle"},
                                {"@integer/key_code_quick_text","SpecialKeyType.QuickText"},
                                {"@integer/key_code_ctrl","SpecialKeyType.Ctrl"},
                                {"@integer/key_code_arrow_left","SpecialKeyType.ArrowLeft"},
                                {"@integer/key_code_arrow_up","SpecialKeyType.ArrowUp"},
                                {"@integer/key_code_arrow_down","SpecialKeyType.ArrowDown"},
                                {"@integer/key_code_arrow_right","SpecialKeyType.ArrowRight"},
                                {"@integer/key_code_enter","SpecialKeyType.Enter"},
                                {"@integer/key_code_tab","SpecialKeyType.Tab"},
                                {"@integer/key_code_space","SpecialKeyType.Space"},
                                {"@integer/key_code_esc","SpecialKeyType.Esc"},
                                {"@integer/key_code_settings","SpecialKeyType.Settings"},
                                {"@integer/key_code_voice_input","SpecialKeyType.VoiceInput"},
                                {"@integer/key_code_mode_alphabet","SpecialKeyType.AlphabetToggle"},
                                {"@integer/key_code_keyboard_mode_change","SpecialKeyType.KeyboardModeChange"},
                                {"@integer/key_code_domain","SpecialKeyType.Domain"},
                                {"@integer/key_code_move_end","SpecialKeyType.End"},
                                {"@integer/key_code_move_home","SpecialKeyType.Home"},
                                {"@integer/key_code_quick_text_popup","SpecialKeyType.QuickTextPopup"}
                            };
                            if (special_lookup.TryGetValue(code_part, out string sp)) {
                                letters.Add(sp);
                            } else {
                                // what is it?
                                Debugger.Break();
                            }
                            continue;
                        } else {
                            if (CodeConstants.ContainsValue(code_part) &&
                                CodeConstants.FirstOrDefault(x => x.Value == code_part) is { } match_const &&
                                SpecialCodeConstantsLookup.TryGetValue(match_const.Key.ToLower(), out string sp_enum_str)) {
                                letters.Add(sp_enum_str);
                                continue;
                            } else if (CodeConstants.ContainsValue(code_part)) {
                                // what is it?
                                Debugger.Break();
                                UnknownParts.Add(code_part);
                                continue;
                            } else if (code_part.Length > 1 &&
                                        code_part.ToCharArray().All(x => char.IsNumber(x)) &&
                                        int.TryParse(code_part, out int code_int)) {
                                // unicode id
                                try {
                                    //char code_char = Convert.ToChar(code_int);
                                    //var code_text = code_char.ToString();
                                    if (code_int == 68243) {

                                    }
                                    string code_text2 = char.ConvertFromUtf32(code_int);
                                    letters.Add(code_text2);
                                    //if (code_text == code_text2) {
                                    //} else {
                                    //    // what are they?
                                    //    Debugger.Break();
                                    //    continue;
                                    //}

                                    //
                                }
                                catch (Exception ex) {
                                    ex.Dump();
                                }
                                continue;
                            }
                            int i = 0;
                            while (i < code_part.Length) {
                                string remaining = code_part.Substring(i);
                                if (remaining.StartsWith("&") && remaining.Contains(";")) {
                                    int end_idx = remaining.IndexOf(";");
                                    string entity = remaining.Substring(0, end_idx + 1);
                                    if (HttpUtility.HtmlDecode(entity) is { } entity_text) {
                                        letters.Add(entity_text);
                                    }
                                    i += end_idx + 1;
                                } else if (remaining.StartsWith("\\u")) {
                                    // exampe \u004A
                                    int end_idx = "\\u004a".Length;
                                    string uni_str = remaining.Substring(0, end_idx);
                                    if (Regex.Unescape(uni_str) is { } actual_str) {
                                        letters.Add(actual_str);
                                    }
                                    i += end_idx;
                                } else {
                                    if (remaining.Substring(0, 1) is { } let_text) {
                                        letters.Add(let_text);
                                    }
                                    i++;
                                }
                            }
                        }
                    }
                }
            }
            return letters.Distinct();
        }
        #endregion
    }
}