
using MonkeyPaste.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MonkeyBoard.Builder {
    [XmlRoot(ElementName = "Keyboards")]
    public class Keyboards : KeyboardFileBase {

        public static Keyboards Deserialize(string path, IEnumerable<string> excluded_kbs) {
            try {
                string xml = File.ReadAllText(path);
                xml = xml.Replace("android:", string.Empty).Replace("ask:", string.Empty);
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                XmlSerializer serializer = new XmlSerializer(typeof(Keyboards));

                using (var reader = new System.Xml.XmlTextReader(ms) { Namespaces = false }) {
                    var kbc = (Keyboards)serializer.Deserialize(reader);
                    if (kbc == null) {
                        return null;
                    }
                    kbc.FilePath = path;

                    kbc.Items.ForEach(x => x.InitFromSet(Path.GetDirectoryName(path)));
                    kbc.Items = kbc.Items.Where(x => excluded_kbs.All(y => !x.FilePath.ToLower().Contains(y.ToLower()))).ToList();

                    //if(kbc.Items.Where(x=>!string.IsNullOrEmpty(x.LandscapeResId)).Select(x=>x.LandscapeResId).Distinct()
                    //    is { } landscape_kb_ids) {
                    //    // remove landscape keyboards because they're stored as LandscapeRow w/ portrait
                    //    var to_remove = kbc.Items.Where(x => landscape_kb_ids.Contains(x.LayoutResId)).ToList();
                    //    foreach(var ls_kb in to_remove) {
                    //        kbc.Items.Remove(ls_kb);
                    //    }
                    //}

                    return kbc;
                }
            }
            catch { }
            return null;
        }
        string _locale;
        public string Locale {
            get {
                if (_locale == null) {
                    // FilePath is addons\languages\<lang_name>
                    if (string.IsNullOrEmpty(FilePath)) {
                        // neutral
                        _locale = string.Empty;
                    } else {
                        string gradle_path =
                        Path.Combine(
                            new DirectoryInfo(Path.GetDirectoryName(FilePath))
                                .Parent
                                .Parent
                                .Parent
                                .Parent
                                .FullName,
                            "build.gradle");
                        _locale = File.ReadAllText(gradle_path)
                            .SplitByLineBreak()
                            .FirstOrDefault()
                            .SplitNoEmpty("\"")[1];
                    }
                }
                return _locale;

            }
            set => _locale = value;
        }

        public void NormalizeCollection(Keyboards numpadColl) {
            if (FilePath.Contains("numpad")) {
                return;
            }

            Items.ForEach(x => NormalizeKeyboard(x));
            numpadColl.Items.ForEach(x => x.IsNumPad = true);
            Items.AddRange(numpadColl.Items);

            if (Items.GroupBy(x => x.Id.ToLower()).Where(x => x.Count() > 1) is { } dup_labels && dup_labels.Any()) {

            }
        }
        void NormalizeKeyboard(Keyboard kb) {
            if (kb.FilePath.Contains("arabic")) {

            }
            if (kb.Row.Count == 4 && kb.Row.All(x => x.Key.Count == 4)) {
                // numpad!
                return;
            }
            var keys = kb.Row.SelectMany(x => x.Key);
            bool needs_num_row = kb.Row.All(x => !x.IsProvidedNumbersRow);

            if (needs_num_row) {
                kb.Row.SelectMany(x => x.Key).ForEach(x => x.Row = x.Row + 1);
                if (Items.FirstOrDefault(x => x.Row.Any(y => y.IsProvidedNumbersRow)) is { } kb_with_num_row) {
                    // HACK arabic linux kb has nums but is too long w/ extra keys at end
                    var to_insert = kb_with_num_row.Row[0].Clone();
                    to_insert.Key = to_insert.Key.Take(10).ToList();
                    kb.Row.Insert(0, to_insert);
                } else {
                    var num_row = new Row() {
                        FilePath = FilePath,
                        Key =
                            new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" }
                            .Select((x, idx) => new Key() {
                                FilePath = kb.FilePath,
                                Row = 0,
                                Column = idx,
                                PrimaryCharacter = x
                            })
                            .ToList()
                    };
                    kb.Row.Insert(0, num_row);
                }

                // remove numbers from secondaries since they'll be added later
                var num_strs = kb.Row[0]
                    .Key.Where(x => !string.IsNullOrEmpty(x.PrimaryCharacter))
                    .Select(x => x.PrimaryCharacter[0])
                    .Where(x => char.IsNumber(x))
                    .Select(x => x.ToString());

                foreach (var lower_row in kb.Row.Skip(1)) {
                    foreach (var k in lower_row.Key) {
                        foreach (var num_str in num_strs) {
                            k.PrimaryCharacter = k.PrimaryCharacter.Replace(num_str, string.Empty);
                        }
                        k.SecondaryCharacters = k.SecondaryCharacters.Where(x => !num_strs.Contains(x)).ToList();
                    }
                }
            } else {
                MpConsole.WriteLine($"{kb.FilePath} has unique num row");
            }

            if (!keys.All(x => x.PrimaryCharacter.StartsWith("SpecialKeyType.SymbolToggle"))) {
                // add btm row
                string[] bottom_chars = [
                    "SpecialKeyType.SymbolToggle",
                    "comma",
                    " ",
                    ".",
                    "SpecialKeyType.Enter",
                    ];

                var btm_row = new Row() { FilePath = FilePath, Key = [] };
                btm_row.Key = bottom_chars.Select((x, idx) => new Key() {
                    FilePath = FilePath,
                    PrimaryCharacter = x,
                    SecondaryCharactersData = string.Empty,
                    Row = kb.Row.Count,
                    Column = idx
                }).ToList();

                kb.Row.Add(btm_row);
            }

            if (Keyboard.NeutralSymbols1 != null && Keyboard.NeutralSymbols2 != null) {
                // add 'none' keys for unmapped symbols 
                if (Locale == "en" && kb.KeyboardTypeName.ToLower().Contains("qwerty")) {

                }
                List<Key> missing_inputs = [];
                var all_symbols = Keyboard.NeutralSymbols1.Row.SelectMany(x => x.Key)
                    .Union(Keyboard.NeutralSymbols2.Row.SelectMany(x => x.Key)).ToList();
                foreach (var symb_key in all_symbols) {
                    if (symb_key.Row == 2 && symb_key.Column == 9) {

                    }
                    if (keys.Where(x => x.Row == symb_key.Row && x.Column == symb_key.Column) is { } matches &&
                        matches.Any()) {
                        continue;
                    }
                    missing_inputs.Add(symb_key);
                }

                if (missing_inputs.Any()) {
                    var missing_groups = missing_inputs.GroupBy(x => new { x.Row, x.Column });

                    foreach (var x in missing_groups) {
                        string merged_pc = "none";
                        int r = 0;
                        int c = 0;
                        foreach (var k in x) {
                            r = k.Row;
                            c = k.Column;
                            if (!merged_pc.Contains(k.PrimaryCharacter)) {
                                merged_pc += k.PrimaryCharacter;
                            }
                        }
                        var merged_key = new Key() {
                            FilePath = kb.FilePath,
                            Row = r,
                            Column = c,
                            PrimaryCharacter = merged_pc
                        };
                        kb.Row[r].Key.Add(merged_key);
                    }
                }

            }

        }

        public Keyboards Clone() {
            var kc = new Keyboards() {
                Items = Items.Select(x => x.Clone()).ToList(),
                Locale = Locale
            };
            return kc;
        }

        [XmlElement(ElementName = "Keyboard")]
        public List<Keyboard> Items { get; set; } = [];
    }
}