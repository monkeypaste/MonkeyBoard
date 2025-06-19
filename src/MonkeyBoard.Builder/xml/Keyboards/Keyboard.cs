using MonkeyPaste.Common;
using MonkeyBoard.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MonkeyBoard.Builder {

    [XmlRoot(ElementName = "Keyboard")]
    public class Keyboard : KeyboardFileBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics

        public static Keyboard NeutralSymbols1 { get; set; }
        public static Keyboard NeutralSymbols2 { get; set; }

        static List<List<string>> GetNeutralRows() {
            List<List<string>> keys = [
                            (["q,+,`", "w,×,~", "e,÷,\\", "r,=,|", "t,/,{", "y,_,}", "u,<,€", "i,>,£", "o,[,¥", "p,],₩"]),
                            (["a,!,○", "s,@,•", "d,#,⚪", "f,$,⚫", "g,%,□", "h,^,⬜", "j,&,♤", "k,*,♡", "l,(,♢", $"none,),♧"]),
                            (["SpecialKeyType.Shift", "z,-,☆", "x,',▪", "c,\",▫", "v,:,≪", "b,;,≫", $"n,comma,¡", "m,?,¿", "SpecialKeyType.Backspace"]),
                            (["SpecialKeyType.SymbolToggle", "comma", " ", ".", "SpecialKeyType.Enter"])
                        ];
            return keys;
        }


        public static Keyboard CreateNeutralSet(int setIdx) {
            var keys = GetNeutralRows();
            Keyboard kb = new Keyboard();
            kb.Row = [];
            foreach (var keyRow in keys) {
                var row = new Row() {
                    Key = []
                };
                foreach (var keySet in keyRow) {
                    if (keySet.SplitNoEmpty(",") is not { } keySetParts) {
                        continue;
                    }
                    int idx = setIdx;
                    if (idx >= keySetParts.Length) {
                        idx = keySetParts.Length - 1;
                    }
                    var key = new Key() {
                        IsNeutral = true,
                        Codes = keySetParts[idx]
                    };
                    if (key.Codes.StartsWith("Special")) {
                        key.Codes = string.Empty;
                    }
                    row.Key.Add(key);
                }
                kb.Row.Add(row);
            }
            kb.Init(string.Empty);
            // adjust symbols to be on 2nd row (so merge is 'better')
            kb.Row.SelectMany(x => x.Key).ForEach(x => x.Row = x.Row + 1);
            return kb;
        }

        public static Keyboard Deserialize(string path) {
            try {
                string xml = File.ReadAllText(path);
                xml = xml.Replace("android:", string.Empty).Replace("ask:", string.Empty);
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                XmlSerializer serializer = new XmlSerializer(typeof(Keyboard));

                using (var reader = new System.Xml.XmlTextReader(ms) { Namespaces = false }) {
                    var kb = (Keyboard)serializer.Deserialize(reader);
                    if (kb == null) {
                        return null;
                    }
                    kb.Init(path);
                    return kb;
                }
            }
            catch { }
            return null;
        }
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region XML


        [XmlElement(ElementName = "Row")]
        public List<Row> Row { get; set; } = [];

        [XmlAttribute(AttributeName = "android")]
        public string Android { get; set; }

        [XmlAttribute(AttributeName = "keyWidth")]
        public string KeyWidth { get; set; }

        [XmlAttribute(AttributeName = "keyHeight")]
        public string KeyHeight { get; set; }
        [XmlAttribute(AttributeName = "nameResId")]
        public string NameResId { get; set; }
        [XmlAttribute(AttributeName = "landscapeResId")]
        public string LandscapeResId { get; set; }

        [XmlAttribute(AttributeName = "iconResId")]
        public string IconResId { get; set; }

        [XmlAttribute(AttributeName = "layoutResId")]
        public string LayoutResId { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "defaultDictionaryLocale")]
        public string DefaultDictionaryLocale { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }

        [XmlAttribute(AttributeName = "index")]
        public int Index { get; set; }

        [XmlAttribute(AttributeName = "defaultEnabled")]
        public bool DefaultEnabled { get; set; }

        [XmlAttribute(AttributeName = "physicalKeyboardMappingResId")]
        public string PhysicalKeyboardMappingResId { get; set; }
        #endregion

        #region Custom
        public string DescriptionValue { get; set; }
        public string IdValue { get; set; }

        public void InitFromSet(string set_dir) {
            string res_dir = Path.GetDirectoryName(set_dir);
            List<AdResString> all_resc_strs = [];
            List<string> xml_paths =
                Directory.GetFiles(Path.Combine(res_dir, "xml")).Where(x => x.EndsWith(".xml")).ToList();

            string GetRescValue(string id) {
                if (!id.StartsWith(@"@string")) {
                    return id.ToStringOrEmpty();

                }
                string desc_key = id.Replace("@string/", string.Empty);
                if (all_resc_strs.FirstOrDefault(x => x.Name == desc_key) is not { } desc_kvp) {
                    return id.ToStringOrEmpty();
                }
                return desc_kvp.Text;
            }

            string vals_dir = Path.Combine(res_dir, "values");
            foreach (var val_path in Directory.GetFiles(vals_dir).Where(x => x.EndsWith(".xml"))) {
                if (AdResources.Deserialize(val_path) is not { } resc) {
                    continue;
                }
                all_resc_strs.AddRange(resc.ResString);
            }

            DescriptionValue = GetRescValue(Description);
            KeyboardTypeName = GetRescValue(NameResId);
            if(Id.Contains("main_english_keyboard_id")) {

            }
            IdValue = GetRescValue(Id);

            string layout_path = string.Empty;
            string ls_layout_path = string.Empty;
            string layout_name = LayoutResId.ToStringOrEmpty().Replace("@xml/", string.Empty);
            if (xml_paths.FirstOrDefault(x => x.EndsWith($"{layout_name}.xml")) is { } layout_path_val) {
                layout_path = layout_path_val;
            } else {
                // what is it?
                Debugger.Break();
            }
            if (!string.IsNullOrEmpty(LandscapeResId)) {
                string ls_layout_name = LandscapeResId.ToStringOrEmpty().Replace("@xml/", string.Empty);
                if (xml_paths.FirstOrDefault(x => x.EndsWith($"{ls_layout_name}.xml")) is { } ls_layout_path_val) {
                    ls_layout_path = ls_layout_path_val;
                    if (Keyboard.Deserialize(ls_layout_path_val) is { } ls_kb &&
                        ls_kb.Clone() is { } ls_kb_clone) {
                        LandscapeRow = ls_kb_clone.Row;
                    }

                }
            }
            var kb = Keyboard.Deserialize(layout_path);
            Row = kb.Row.Select(x => x.Clone()).ToList();

            Init(layout_path);
        }
        public void Init(string path) {
            // go up to find build.gradle at  pack/src/main/res/xml/*.xml
            if (string.IsNullOrEmpty(path)) {
                // neutral
                Locale = string.Empty;
            } else {
                string gradle_path =
                Path.Combine(
            new DirectoryInfo(Path.GetDirectoryName(path))
                .Parent
                .Parent
                .Parent
                .Parent
                .FullName,
            "build.gradle");
                if (!File.Exists(gradle_path)) {

                }
                Locale = File.ReadAllText(gradle_path)
                    .SplitByLineBreak()
                    .FirstOrDefault()
                    .SplitNoEmpty("\"")[1];
            }


            FilePath = path;
            Row.ForEach(x => x.FilePath = path);
            Row.SelectMany(x => x.Key).ForEach(x => x.FilePath = path);

            if (Row.Where(x => !string.IsNullOrEmpty(x.KeyboardMode)) is { } mode_rows &&
                mode_rows.Any()) {
                // only include normal mode bottom row
                if (mode_rows.All(x => !x.KeyboardMode.Contains("keyboard_mode_normal"))) {
                    // what are the modes?
                    Debugger.Break();
                } else {
                    var modes_to_remove =
                    mode_rows.Where(x => !x.KeyboardMode.Contains("keyboard_mode_normal")).ToList();
                    modes_to_remove.ForEach(x => Row.Remove(x));
                }

            }
            for (int r = 0; r < Row.Count; r++) {
                var row = Row[r];
                for (int c = 0; c < row.Key.Count; c++) {
                    var key = row.Key[c];
                    key.Row = r;
                    key.Column = c;
                }
            }

        }

        public List<Row> LandscapeRow { get; set; } = [];

        public string Locale { get; set; }

        string _keyboardTypeName;
        public string KeyboardTypeName {
            get {
                //if(_keyboardTypeName != null) {
                //    return _keyboardTypeName;
                //}
                //if(IsNeutral) {
                //    return $"neutral{(this == NeutralSymbols1 ? 1 : 2)}";
                //}
                //string name = Path.GetFileNameWithoutExtension(FilePath)
                //    .ToLower()
                //    .Replace(Locale, string.Empty)
                //    .Replace(LanguageName, string.Empty)
                //    .Replace("_", " ")
                //    .RemoveExtraSpaces()
                //    .ToTitleCase();
                //return name;
                return _keyboardTypeName;
            }
            set => _keyboardTypeName = value;
        }
        public string DisplayName {
            get {
                return $"{Locale} {KeyboardTypeName}";
            }
        }

        string _letterGroupData;
        public string LetterGroupsData {
            get {
                if (_letterGroupData != null) {
                    return _letterGroupData;
                }
                var sb = new StringBuilder();
                foreach (var lg in LetterGroups) {
                    if (!lg.Any() || lg.All(x => string.IsNullOrEmpty(x))) {
                        continue;
                    }
                    sb.AppendLine(string.Join(string.Empty, lg));
                }
                return sb.ToString();
            }
            set => _letterGroupData = value;
        }

        #region Default Letter groups
        public static List<List<string>> GetDefaultLetterGroups() {
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
                        (["1","¹","₁"]),
                        (["2","²","₂"]),
                        (["3","³","₃"]),
                        (["4","⁴","₄"]),
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
        [XmlIgnore]
        #endregion
        public List<List<string>> LetterGroups {
            get {

                // TODO somehow need to separate primary and secondary into diff groups
                // so there are no repeats. It seems to only be a problem on the middle row
                // i think its something when adding the numbers or but i don't know and
                // Apples letter groups are better in most cases but not for non-latin languages...

                if(Locale.StartsWith("en-")) {
                    return GetDefaultLetterGroups();
                }
                
                var primary_strs = Row
                    .SelectMany(x => x.Key)
                    .Where(x => !x.PrimaryCharacter.Contains("Special"))
                    .Select(x=>x.PrimaryCharacter.Replace("comma",",").Replace("none",string.Empty))
                    .Where(x=>!string.IsNullOrEmpty(x))
                    .SelectMany(x=>x.ToCharArray())
                    .Select(x=>x.ToString())
                    .Distinct()
                    .ToList();
                
                var secondary_strs = Row
                    .SelectMany(x => x.Key)
                    .SelectMany(x=>x.SecondaryCharacters)
                    .Where(x => !x.Contains("Special"))
                    .Select(x=>x.Replace("comma",",").Replace("none",string.Empty))
                    .Where(x=>!string.IsNullOrEmpty(x))
                    .SelectMany(x=>x.ToCharArray())
                    .Select(x=>x.ToString())
                    .Distinct()
                    .ToList();

                var lgl = new List<List<string>>();
                foreach(string ps in primary_strs) {

                }

                foreach (var key in Row.SelectMany(x => x.Key)) {
                    string pc = key.PrimaryCharacter;
                    if (pc.Contains("Special") || pc == "none") {
                        continue;
                    }
                    //if(pc == "comma") {
                    //    pc = ",";
                    //}
                    var lg = new List<string>();
                    lg.Add(pc);
                    if (key.SecondaryCharacters is { } scl) {
                        lg.AddRange(scl.Where(x =>!x.Contains("Special") && !string.IsNullOrEmpty(x) && !pc.Contains(x)));
                    }
                    //var neutral_symbols =
                    //    NeutralSymbols1.Row.SelectMany(x => x.Key)
                    //    .Union(NeutralSymbols2.Row.SelectMany(x => x.Key))
                    //    .Where(x => x.Row == key.Row && x.Column == key.Column && !x.PrimaryCharacter.StartsWith("Special") && !string.IsNullOrEmpty(x.PrimaryCharacter))
                    //    .Select(x => x.PrimaryCharacter);
                    //lg.AddRange(neutral_symbols);
                    lg = lg.Distinct().ToList();
                    foreach(var l in lg) {
                        // remove any letters/numbers from groups
                        // since 
                        if(l.StartsWith("d&")) {

                        }
                    }
                    lgl.Add(lg);
                }
                return lgl;
            }
        }
        public bool IsNumPad { get; set; }
        #endregion

        #endregion

        #region Constructors
        #endregion

        #region Public Methods
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine(DisplayName);
            Row.ForEach(x => sb.AppendLine(x.ToString()));
            sb.AppendLine("Letter Groups:");
            LetterGroups.ForEach(x => sb.AppendLine(string.Join(",", x)));
            return sb.ToString();
        }

        public Keyboard Clone() {
            var kb = new Keyboard() {
                IsNumPad = IsNumPad,
                LetterGroupsData = LetterGroupsData,
                KeyboardTypeName = KeyboardTypeName,
                DescriptionValue = DescriptionValue,
                IdValue = IdValue,
                NameResId = NameResId,
                LandscapeResId = LandscapeResId,
                IconResId = LandscapeResId,
                LayoutResId = LayoutResId,
                Id = Id,
                DefaultDictionaryLocale = DefaultDictionaryLocale,
                Description = Description,
                Index = Index,
                DefaultEnabled = DefaultEnabled,
                PhysicalKeyboardMappingResId = PhysicalKeyboardMappingResId,
                LandscapeRow = LandscapeRow.Select(x => x.Clone()).ToList(),
                Row = Row.Select(x => x.Clone()).ToList()

            };
            return kb;
        }

        public KeyboardFormat ToKeyboardFormat() {

            //public KeyboardFormat(Keyboard kb) {
            //    if (kb.FilePath.ToLower().Contains("colemak")) {

            //    }
            //    guid = kb.Id;
            //    isDefault = kb.DefaultEnabled;
            //    description = kb.DescriptionValue;
            //    isNumPad = kb.IsNumPad;
            //    
            //}
            return new KeyboardFormat() {
                FilePath = FilePath,
                guid = IdValue,
                description = DescriptionValue,
                isDefault = DefaultEnabled,
                isNumPad = IsNumPad,
                letterGroups = LetterGroupsData.SplitNoEmpty(Environment.NewLine).ToList(),
                label = KeyboardTypeName,
                landscapeRows = LandscapeRow.Select(x => x.ToRowFormat()).ToList(),
                rows = Row.Select(x => x.ToRowFormat()).ToList()
            };
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion
    }

}