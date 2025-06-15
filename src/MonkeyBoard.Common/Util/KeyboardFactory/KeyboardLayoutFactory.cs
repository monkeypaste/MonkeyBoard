using MonkeyPaste.Common;
using MonkeyPaste.Common.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class KeyboardLayoutConfig {
        public bool NeedsNextKeyboardKey { get; set; }
        public bool IsEmojiButtonVisible { get; set; }
        public bool IsNumberRowVisible { get; set; }
        public bool IsReset { get; set; }
        public bool IsLandscape { get; set; }
    }
    public class KeyboardLayoutResult {
        public List<List<object>> Rows { get; set; }
        public List<List<string>> Groups { get; set; }
    }
    public static class KeyboardLayoutFactory {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Properties

        static IKeyboardInputConnection InputConnection { get; set; }
        static KeyboardCollectionFormat CurrentKbc { get; set; }
        public static string DefaultKeyboardGuid { get; private set; }
        static string LastLocalizedKeyboardGuid { get; set; }
        #endregion

        #region Events
        #endregion

        #region Constructors
        #endregion

        #region Public Methods

        public static KeyboardLayoutResult Build(
            IKeyboardInputConnection ic,
            KeyboardFlags kbFlags,
            KeyboardLayoutConfig config) {
            //
            try {
                InputConnection = ic;
                if(InputConnection == null || InputConnection.SharedPrefService is not { } sps) {
                    return default;
                }
                if(config.IsReset) {
                    ResetState();
                }
                DefaultKeyboardGuid = sps.GetPrefValue<string>(PrefKeys.DEFAULT_KB_GUID);
                if(CurrentKbc == null || CurrentKbc.culture != CultureManager.CurrentKbCulture) {
                    CurrentKbc = LoadKeyboardCollection(CultureManager.CurrentKbCulture);                    
                }
                bool is_landscape = kbFlags.HasFlag(KeyboardFlags.Landscape);
                bool is_tablet = kbFlags.HasFlag(KeyboardFlags.Tablet);

                KeyboardFormat kb = null;
                bool is_num_pad = kbFlags.HasAnyFlag(KeyboardFlags.Numbers | KeyboardFlags.Pin | KeyboardFlags.Digits);
                if (is_num_pad) {                    
                    if(is_landscape) {
                        kb = CurrentKbc
                                .keyboards.FirstOrDefault(x => x.isNumPad).Clone();
                                if (kbFlags.HasFlag(KeyboardFlags.Pin)) {
                                    // remove alt primaries
                                    kb.rows
                                        .SelectMany(x => x.keys)
                                        .Where(x => !x.primaryValues.IsSpecialKeyStr())
                                        .ForEach(x => x.primaryValues = x.primaryValues.Substring(0, 1));
                                }
                    } else {
                        return BuildFallback(kbFlags, config);
                    }
                    
                } else {
                    kb = CurrentKbc.keyboards.Where(x=>!x.isNumPad).FirstOrDefault(x => x.guid == DefaultKeyboardGuid);
                    if(kb == null) {
                        if(CurrentKbc.keyboards.FirstOrDefault(x=>x.isDefault) is { } def_kb) {
                            kb = def_kb;
                        } else if(CurrentKbc.keyboards.Where(x=>x.FilePath.ToLowerInvariant().Contains("qwerty")).OrderBy(x=>x.label.Length).FirstOrDefault() is { } qwerty_kb) {
                            // select qwerty if available
                            kb = qwerty_kb;
                        } else {
                            // just pick first
                            kb = CurrentKbc.keyboards.Where(x => !x.isNumPad).FirstOrDefault();
                        }
                        if(kb == null) {
                            // error
                            MpConsole.WriteLine($"Keyboard factory error! Can't find default w/ guid: '{DefaultKeyboardGuid}'");
                            return default;
                        }
                        // update pref
                        DefaultKeyboardGuid = kb.guid;
                        sps.SetPrefValue(PrefKeys.DEFAULT_KB_GUID, DefaultKeyboardGuid);
                    }
                    // NOTE must make new instance of layout or any inserts/removes accumulate (and clearing would be harder)
                    kb = kb.Clone();
                    LocalizeKeyboard(kb);

                    // CONDITIONAL SPECIAL KEYS
                    // url: ,/<space>.<.com>
                    // email: ,@<space>.<.com>

                    var last_row = kb.rows.Last().keys;
                    int row = kb.rows.Count - 1;

                    // EMOJI KEY
                    int emoj_col = 1;
                    last_row.Where(x => x.column >= emoj_col).ForEach(x => x.column++);
                    last_row.Insert(emoj_col, new KeyFormat() { row = row, column = emoj_col, primaryValues = SpecialKeyType.Emoji.ToTypedString() });
                    // URL
                    int url_col = 3; 
                    last_row.Where(x => x.column >= url_col).ForEach(x => x.column++);
                    last_row.Insert(url_col, new KeyFormat() { row=row, column=url_col,primaryValues = KeyConstants.URL_KEY_1 });

                    // EMAIL
                    int email_col = 4; 
                    last_row.Where(x => x.column >= email_col).ForEach(x => x.column++);
                    last_row.Insert(email_col, new KeyFormat() {row=row,column=email_col, primaryValues = KeyConstants.EMAIL_KEY_1 });

                    // DOMAIN
                    string dom_char = ResourceStrings.U["DomainKeyValue"].value;
                    int dom_ins_idx = last_row.IndexOf(last_row.FirstOrDefault(x => x.primaryValues == SpecialKeyType.Enter.ToTypedString()));
                    last_row.Where(x => x.column >= dom_ins_idx).ForEach(x => x.column++);
                    last_row.Insert(dom_ins_idx, new KeyFormat() {row=row,column=dom_ins_idx, primaryValues = dom_char });

                    if(!config.IsNumberRowVisible) {
                        var first_row = kb.rows.First().keys;
                        var second_row = kb.rows.Skip(1).First().keys;
                        var third_row = kb.rows.Skip(2).First().keys;

                        // get all secondary values from input keys (ignore num row secondaries)
                        // ordered by pg
                        var all_secondary = first_row
                            .Select(x => x.primaryValues.Substring(0, 1))
                            .Union(second_row.Select(x => x.primaryValues.Substring(1, 1)))
                            .Union(third_row.Where(x => !x.primaryValues.StartsWith("Special")).Select(x => x.primaryValues.Substring(x.primaryValues.StartsWith("none") ? "none".Length : 1, 1)))
                            .Union(second_row.Select(x => x.primaryValues.Substring(2, 1)))
                            .Union(third_row.Where(x => !x.primaryValues.StartsWith("Special")).Select(x => x.primaryValues.Substring(x.primaryValues.StartsWith("none") ? "none".Length+1 : 2, 1)))
                            .Distinct()
                            .ToList();

                        //int close_paren_idx = all_secondary.IndexOf(")");
                        //all_secondary[close_paren_idx] = "]";
                        //all_secondary.Insert(close_paren_idx + 1, "!");
                        //all_secondary = all_secondary.Distinct().ToList();

                        //all_secondary = "1234567890@#$_&-+()/*\"':;!?~`|•✔π÷×§Δ£₵€¥^°={}\\%©️®️™️✓[]<>".ToCharArray().Select(x=>new string(x,1)).ToList();

                        // replace avail secondaries
                        int sec_idx = 0;
                        var all_inputs =
                            second_row.Union(third_row.Where(x => !x.primaryValues.StartsWith("Special")/* && !x.primaryValues.Contains("none")*/)).ToList();
                        for (int ins_idx = 1; ins_idx < 3; ins_idx++) {
                            foreach (var input_key in all_inputs) {
                                input_key.primaryValues = input_key.primaryValues.Insert(input_key.primaryValues.StartsWith("none") ? "none".Length - 1 + ins_idx : ins_idx, all_secondary[sec_idx++]).Substring(0, input_key.primaryValues.StartsWith("none") ? "none".Length + 2 : 3);
                            }
                        }

                        //for (int i = 0; i < first_row.Count; i++) {                            
                        //    string opv2 = second_row[i].primaryValues.Substring(1, 1);
                        //    string npv = second_row[i].primaryValues.Insert(1, first_row[i].primaryValues.Substring(0, 1)).Substring(0, 3);
                        //    second_row[i].primaryValues = npv;

                        //    if (third_row[i].primaryValues.ToStringOrEmpty().StartsWith("Special")) {
                        //        continue;
                        //    }
                        //    string opv3 = third_row[i].primaryValues.Substring(1, 1);
                        //    third_row[i].primaryValues = third_row[i].primaryValues.Replace(opv3, opv2);
                        //}
                    }

                    if(kbFlags.HasFlag(KeyboardFlags.Tablet)) {
                        /*
                        keys = new List<List<object>> {
                    (["1", "2", "3", "4", "5", "6", "7", "8", "9", "0", SpecialKeyType.Backspace]),
                    ([SpecialKeyType.Tab, "q,+,`", "w,×,~", "e,÷,\\", "r,=,|", "t,/,{", "y,_,}", "u,<,€", "i,>,£", "o,[,¥", "p,],₩"]),
                    ([SpecialKeyType.CapsLock, "a,!,○", "s,@,•", "d,#,⚪", "f,$,⚫", "g,%,□", "h,^,⬜", "j,&,♤", "k,*,♡", "l,(,♢", $"{KeyConstants.HIDDEN_CSV_STR},),♧", primarySpecialType]),
                    ([SpecialKeyType.Shift, "z,-,☆", "x,',▪", "c,\",▫", "v,:,≪", "b,;,≫", $"n,{KeyConstants.COMMA_CSV_STR},¡", "m,?,¿", $"{KeyConstants.COMMA_CSV_STR}",".", SpecialKeyType.Shift]),
                    ([SpecialKeyType.SymbolToggle, SpecialKeyType.Emoji, " ", SpecialKeyType.ArrowLeft, SpecialKeyType.ArrowRight, SpecialKeyType.NextKeyboard])
                };
                        */

                        // row 1 +tab, +backspace
                        int c = 1;
                        kb.rows[1].keys.Where(x => x.column >= 0).ForEach(x => x.column++);
                        kb.rows[1].keys.Insert(0, new KeyFormat() { row = 1, column = 0, primaryValues = SpecialKeyType.Tab.ToTypedString(c) });
                        kb.rows[1].keys.Add(new KeyFormat() { row = 1, column = kb.rows[1].keys.Count, primaryValues = SpecialKeyType.Backspace.ToTypedString(c) });

                        // row 2 +caps lock, +return
                        kb.rows[2].keys.Where(x => x.column >= 0).ForEach(x => x.column++);
                        kb.rows[2].keys.Insert(0, new KeyFormat() { row = 2, column = 0, primaryValues = SpecialKeyType.CapsLock.ToTypedString(c) });
                        kb.rows[2].keys.Add(new KeyFormat() { row = 2, column = kb.rows[2].keys.Count, primaryValues = SpecialKeyType.Enter.ToTypedString(c) });

                        // row 3 -backspace, + shift 
                        kb.rows[3].keys.RemoveAt(kb.rows[3].keys.Count - 1);
                        kb.rows[3].keys.Add(new KeyFormat() { row = 3, column = kb.rows[3].keys.Count, primaryValues = SpecialKeyType.Shift.ToTypedString(c) });

                        // row 4 -enter,-comma,-period, +next kb, + right symbols, + collapse
                        kb.rows[4].keys.RemoveAt(kb.rows[4].keys.Count - 1);
                        if(config.NeedsNextKeyboardKey) {
                            kb.rows[4].keys.Where(x => x.column >= 0).ForEach(x => x.column++);
                            kb.rows[4].keys.Insert(0, new KeyFormat() { row = 4, column = 0, primaryValues = SpecialKeyType.NextKeyboard.ToTypedString(c) });
                        }
                        kb.rows[4].keys.Add(new KeyFormat() { row = 4, column = kb.rows[4].keys.Count, primaryValues = SpecialKeyType.SymbolToggle.ToTypedString(c) });
                        kb.rows[4].keys.Add(new KeyFormat() { row = 4, column = kb.rows[4].keys.Count, primaryValues = SpecialKeyType.Collapse.ToTypedString(c) });
                    }

                }

                // set primary value
                SpecialKeyType def_primary_type = SpecialKeyType.Enter;
                if(kb.rows.SelectMany(x=>x.keys).Where(x=>x.primaryValues.Contains(def_primary_type.ToTypedString())) is { } primary_keys) {
                    SpecialKeyType actual_primary_type = GetPrimarySpecialKey(kbFlags);
                    primary_keys.ForEach(x=>x.primaryValues = x.primaryValues.Replace(def_primary_type.ToTypedString(), actual_primary_type.ToTypedString()));
                }

                return new() {
                    Rows = kb.ToKeyRows(),
                    Groups = kb.ToLetterGroups()
                };
            }
            catch (Exception ex) {
                ex.Dump();
            }
            return default;
        }
        public static KeyboardCollectionFormat LoadKeyboardCollection(string cc) {
            string cul_dir = CultureManager.GetCultureDir(cc);
            if(!cul_dir.IsDirectory()) {
                return new(); 
            }
            var kb_paths = Directory.GetFiles(cul_dir).Where(x => x.EndsWith(".kb"));
            var kbc = new KeyboardCollectionFormat();
            foreach(string kb_path in kb_paths) {
                if(MpFileIo.ReadTextFromFile(kb_path).DeserializeObject<KeyboardFormat>() is { } kbf) {
                    kbf.FinishInit(kbc, kb_path);
                    kbc.keyboards.Add(kbf);
                }
            }

            kbc.culture = cc;
            return kbc;
        }
        public static void ResetPrimaryKey(KeyViewModel primaryKey, KeyboardFlags flags) {
            if (primaryKey == null) {
                return;
            }
            SpecialKeyType primarySpecialType = GetPrimarySpecialKey(flags);
            if (primaryKey.SpecialKeyType == primarySpecialType) {
                return;
            }
            primaryKey.SpecialKeyType = primarySpecialType;
            var result = KeyViewModel.GetSpecialKeyCharsOrResourceKeys(primaryKey.SpecialKeyType, flags.HasFlag(KeyboardFlags.Tablet));
            primaryKey.SetCharacters(result);
        }

        public static void SetDefaultKeyboard(string guid) {

            if (InputConnection is { } ic && ic.SharedPrefService is { } sps) {
                sps.SetPrefValue(PrefKeys.DEFAULT_KB_GUID,guid);
            }
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        static void ResetState() {
            LastLocalizedKeyboardGuid = null;
        }
        static void LocalizeKeyboard(KeyboardFormat kb) {
            if(LastLocalizedKeyboardGuid == kb.guid) {
                // already done
                return;
            }
            LastLocalizedKeyboardGuid = kb.guid;

            try {
                string Locale = CultureManager.CurrentUiCulture;
                RegionInfo ri = null;
                if (Locale.Contains("-")) {
                    if (CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(x => x.Name.ToLower() == Locale.ToLower()) is { } ci) {
                        ri = new RegionInfo(ci.Name);
                    }
                }
                if (ri == null) {
                    if (Locale.SplitNoEmpty("-")[0] is { } cu &&
                        CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(x => x.Name.ToLower().StartsWith(cu.ToLower())) is { } ci1) {
                        ri = new RegionInfo(ci1.Name);
                    }
                }
                if (ri != null) {
                    //var ri = new RegionInfo(Locale);
                    string isoCurrencySymbol = ri.CurrencySymbol;
                    if (isoCurrencySymbol == "$") {

                    } else {
                        MpConsole.WriteLine($"CUrrency updated to: {isoCurrencySymbol}");
                        kb.rows.SelectMany(x => x.keys).Where(x => x.primaryValues.Contains("$")).ForEach(x => x.primaryValues = x.primaryValues.Replace("$", isoCurrencySymbol));
                        if (kb.letterGroups.FirstOrDefault(x => x.Contains("$")) is { } currency_group &&
                            !currency_group.Contains(isoCurrencySymbol)) {
                            int currency_idx = kb.letterGroups.IndexOf(currency_group);
                            if (currency_idx >= 0) {
                                kb.letterGroups[currency_idx] = isoCurrencySymbol + kb.letterGroups[currency_idx];
                            }
                        }
                    }
                } else {

                }
            }
            catch { }
        }
        static SpecialKeyType GetPrimarySpecialKey(KeyboardFlags kbFlags) {
            //if (kbFlags.HasFlag(KeyboardFlags.MultiLine)) {
            //    // NOTE always show enter if multi-line known
            //    return SpecialKeyType.Enter;
            //}
            if (kbFlags.HasFlag(KeyboardFlags.Done)) {
                return SpecialKeyType.Done;
            }
            if (kbFlags.HasFlag(KeyboardFlags.Go)) {
                return SpecialKeyType.Go;
            }
            if (kbFlags.HasFlag(KeyboardFlags.Previous)) {
                return SpecialKeyType.Previous;
            }
            if (kbFlags.HasFlag(KeyboardFlags.Next)) {
                return SpecialKeyType.Next;
            }
            if (kbFlags.HasFlag(KeyboardFlags.Search)) {
                return SpecialKeyType.Search;
            }
            if (kbFlags.HasFlag(KeyboardFlags.Send)) {
                return SpecialKeyType.Send;
            }

            return SpecialKeyType.Enter;
        }

        static List<List<string>> ToLetterGroups(this KeyboardFormat kbf) {
            var lgl = new List<List<string>>();
            foreach (var x in kbf.letterGroups) {
                lgl.Add(x.ToCharArray().Select(x => x.ToString()).Distinct().ToList());
            }
            return lgl;
        }
        static List<List<object>> ToKeyRows(this KeyboardFormat kbf) {
            List<List<object>> rows = [];
            string[] subs = [
                "comma",
                "none",
                ResourceStrings.U["DomainKeyValue"].value
                ];
            foreach (var r in kbf.rows) {
                var row = new List<object>();
                foreach (var k in r.keys) {
                    if (k.primaryValues.StartsWith(nameof(SpecialKeyType))) {
                        if (Enum.TryParse(
                            typeof(SpecialKeyType),
                            k.primaryValues
                            .Replace(nameof(SpecialKeyType), string.Empty)
                            .Replace(".", string.Empty), out object sktObj) &&
                            sktObj is SpecialKeyType skt) {
                            row.Add(skt);
                        }
                    } else {
                        string pre_val = k.primaryValues;
                        if(pre_val.StartsWith("ncomm")) {

                        }
                        var val_parts = new List<string>();
                        for (int i = 0; i < k.primaryValues.Length; i++) {
                            string remaining = k.primaryValues.Substring(i);
                            if(subs.FirstOrDefault(x=>remaining.StartsWith(x)) is { } sub_str) {
                                i += sub_str.Length - 1;
                                val_parts.Add(sub_str);
                            } else {
                                val_parts.Add(remaining.Substring(0, 1));
                            }
                        }
                        if(k.shiftValues.ToStringOrEmpty().Length > 1) {
                            k.shiftValues = k.shiftValues.Substring(0, 1);
                        }

                        string k_cell = string.Join(",", val_parts);
                        row.Add(new Tuple<string,string>(k_cell,k.shiftValues.ToStringOrEmpty()));
                    }
                }
                rows.Add(row);
            }

            return rows;
        }
        #endregion


        #region Fallbacks
        public static KeyboardLayoutResult BuildFallback(KeyboardFlags kbFlags, KeyboardLayoutConfig config) {
            return new() {
                Rows = GetFallbackKeyRows(kbFlags,config),
                Groups = GetDefaultLetterGroups()
            };
        }

        static List<List<object>> GetFallbackKeyRows(KeyboardFlags kbFlags, KeyboardLayoutConfig config) {
            List<List<object>> keys = null;
            SpecialKeyType primarySpecialType = GetPrimarySpecialKey(kbFlags);
            if (kbFlags.HasFlag(KeyboardFlags.Numbers) || kbFlags.HasFlag(KeyboardFlags.Digits)) {
                return new List<List<object>> {
                        (["1,(", "2,/", "3,)", SpecialKeyType.Backspace]),
                        (["4,N", "5,comma", "6,.", primarySpecialType]),
                        (["7,*", "8,;", "9,#", SpecialKeyType.NumberSymbolsToggle]),
                        (["*,-", "0,+", "#,__", "comma"])
                    };
            } else if (kbFlags.HasFlag(KeyboardFlags.Pin)) {
                return new List<List<object>> {
                        (["1", "2", "3", SpecialKeyType.Backspace]),
                        (["4", "5", "6", primarySpecialType]),
                        (["7", "8", "9", ""]),
                        (["", "0", "", ""])
                    };
            }
            if (kbFlags.HasFlag(KeyboardFlags.Mobile)) {
                keys = [
                            (["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"]),
                            (["q,+,`", "w,×,~", "e,÷,\\", "r,=,|", "t,/,{", "y,_,}", "u,<,€", "i,>,£", "o,[,¥", "p,],₩"]),
                            (["a,!,○", "s,@,•", "d,#,⚪", "f,$,⚫", "g,%,□", "h,^,⬜", "j,&,♤", "k,*,♡", "l,(,♢", $"{KeyConstants.HIDDEN_CSV_STR},),♧"]),
                            ([SpecialKeyType.Shift, "z,-,☆", "x,',▪", "c,\",▫", "v,:,≪", "b,;,≫", $"n,{KeyConstants.COMMA_CSV_STR},¡", "m,?,¿", SpecialKeyType.Backspace]),
                            ([SpecialKeyType.SymbolToggle, $"{KeyConstants.COMMA_CSV_STR}", " ", ".", primarySpecialType])
                        ];

                if (kbFlags.HasFlag(KeyboardFlags.Email) || kbFlags.HasFlag(KeyboardFlags.Url)) {
                    // add input type char after symbol toggle
                    string ins_char1 = kbFlags.HasFlag(KeyboardFlags.Email) ? "@" : "/";
                    int ins_idx1 = keys.Last().IndexOf(SpecialKeyType.SymbolToggle) + 1;
                    keys.Last().Insert(ins_idx1, ins_char1);

                    // insert .com after period
                    string ins_char2 = ResourceStrings.U["DomainKeyValue"].value;
                    int ins_idx2 = keys.Last().IndexOf(".") + 1;
                    keys.Last().Insert(ins_idx2, ins_char2);
                }
                if (config.IsEmojiButtonVisible) {
                    // insert emoji key before space bar on last row
                    keys.Last().Insert(1, SpecialKeyType.Emoji);
                }

                if (!config.IsNumberRowVisible) {
                    // numbers become 2nd row secondary 
                    var num_row = keys[0];
                    keys.Remove(num_row);
                    List<object> new_top_row = [];
                    for (int i = 0; i < keys[0].Count; i++) {
                        if (keys[0][i] is not string top_set_val) {
                            continue;
                        }
                        var val_parts = top_set_val.SplitNoEmpty(KeyConstants.CSV_COL_SEP).ToList();
                        val_parts.Insert(1, num_row[i] as string);
                        new_top_row.Add(string.Join(KeyConstants.CSV_COL_SEP, val_parts));
                    }
                    keys[0] = new_top_row;
                }

            } else {
                // tablet
                keys = new List<List<object>> {
                    (["1", "2", "3", "4", "5", "6", "7", "8", "9", "0", SpecialKeyType.Backspace]),
                    ([SpecialKeyType.Tab, "q,+,`", "w,×,~", "e,÷,\\", "r,=,|", "t,/,{", "y,_,}", "u,<,€", "i,>,£", "o,[,¥", "p,],₩"]),
                    ([SpecialKeyType.CapsLock, "a,!,○", "s,@,•", "d,#,⚪", "f,$,⚫", "g,%,□", "h,^,⬜", "j,&,♤", "k,*,♡", "l,(,♢", $"{KeyConstants.HIDDEN_CSV_STR},),♧", primarySpecialType]),
                    ([SpecialKeyType.Shift, "z,-,☆", "x,',▪", "c,\",▫", "v,:,≪", "b,;,≫", $"n,{KeyConstants.COMMA_CSV_STR},¡", "m,?,¿", $"{KeyConstants.COMMA_CSV_STR}",".", SpecialKeyType.Shift]),
                    ([SpecialKeyType.SymbolToggle, SpecialKeyType.Emoji, " ", SpecialKeyType.ArrowLeft, SpecialKeyType.ArrowRight, SpecialKeyType.NextKeyboard])
                };
            }
            return keys;
        }
        static List<List<string>> GetDefaultLetterGroups() {
            return new List<List<string>>() {
                        // letters
                        (["a","à","á","â","ä","æ","ã","å","ā","ǎ","ă","ą"]),
                        (["c","ç","ć","č","ċ"]),
                        (["d","ď","ð"]),
                        (["e","è","é","ê","ë","ē","ė","ę","ě","ẽ"]),
                        (["g","ğ","ġ"]),
                        (["h","ħ"]),
                        (["i","ì","į","ī","í","ï","î","ı","ĩ","ǐ"]),
                        (["k","ķ"]),
                        (["l","ł","ľ","ļ"]),
                        (["n","ń","ñ","ň","ņ"]),
                        (["o","õ","ō","ø","œ","ó","ò","ö","ô","ő","ǒ"]),
                        (["r","ř"]),
                        (["s","ß","ś","š","ş","ș"]),
                        (["t","ț","ť","þ"]),
                        (["u","ū","ú","ù","ü","û","ų","ů","ű","ũ","ǔ"]),
                        (["w","ŵ"]),
                        (["y","ÿ","ŷ","ý"]),
                        (["z","ž","ź","ż"]),
                        (["b"]),
                        (["f"]),
                        (["j"]),
                        (["m"]),
                        (["p"]),
                        (["q"]),
                        (["v"]),
                        (["x"]),

                        // symbols/numbers
                        (["0","º"]),
                        (["-","–","—","•"]),
                        (["/","\\"]),
                        (["$","₽","¥","€","¢","£","₩"]),
                        (["&","§"]),
                        (["\"","«","»","„","“","”"]),
                        ([".","…"]),
                        (["?","¿"]),
                        (["!","¡"]),
                        (["'","`","‘","’"]),
                        (["%","‰"]),
                        (["=","≈","≠"]),
                        ([" "]),
                        ([","]),
                        (["1"]),
                        (["2"]),
                        (["3"]),
                        (["4"]),
                        (["5"]),
                        (["6"]),
                        (["7"]),
                        (["8"]),
                        (["9"]),
                        ([":"]),
                        ([";"]),
                        (["("]),
                        ([")"]),
                        (["@"]),
                        ([""]),
                        (["["]),
                        (["]"]),
                        (["{"]),
                        (["}"]),
                        (["#"]),
                        (["^"]),
                        (["*"]),
                        (["+"]),
                        (["_"]),
                        (["|"]),
                        (["~"]),
                        (["<"]),
                        ([">"]),
                        (["€"]),
                        (["£"]),
                        (["¥"]),
                        (["•"])
                    };
        }
        #endregion
    }
}
