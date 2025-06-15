using MonkeyPaste.Common;
using MonkeyPaste.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MpConsole = MonkeyPaste.Common.MpConsole;
using Path = System.IO.Path;

namespace MonkeyBoard.Builder {
    public static class KeyboardBuilder {
        public static string KeyboardAssetPackDir =>
            @"C:\Users\tkefauver\Source\Repos\MonkeyPaste\Scratch\MobileKeyboardTest\KeyboardLib\Assets\Localization\packs\";
        public static void Main(string[] args) {
            Trace.AutoFlush = true;
            //MpConsole.IsConsoleApp = true;
            //MpConsole.HideAllStamps = true;
            //Build(KeyboardAssetPackDir);
            EmojiDataBuilder.FetchEmojiInfo();
        }
        public static void Build(string KeyboardAssetPackDir) {
            // notes (AnySoft):
            // 1. 'codes' are UTF-16 decimal numbers https://asecuritysite.com/coding/asc2?val=768%2C1024
            // 2. lang code is 1st line of ../pack/build.gradle
            // 3. kb xml is in pack/src/main/res/xml/*_qwerty.xml
            // 4. some langs w/o capital letters have "ask:shiftedCodes" attributes (like hindi)
            // 5. Should scan each xml file in #3 for <Keyboard> elms and store
            // 6. Some langs (luxemburgish) store popupkeys in sep files "android:popupKeyboard".
            //    Can do a post process and match the code to the end of the file name and add popups that way
            // 7. keyboards w/ unique number row keys have 'android:keyHeight="@integer/key_short_height"' in 1st row.
            //    Should use those number rows when found, otherwise neutral

            // notes (android):
            // 1. android official keyboard layouts: https://android.googlesource.com/platform/packages/inputmethods/LatinIME/+/refs/heads/main/java/res/xml/rowkeys_qwerty3.xml
            // 2. Need to replicate 'merge' and 'include' element behavior. see this on what they do: https://stackoverflow.com/a/11093340/105028

            // notes (LetterGroups)
            // 1. Letter Groups can become ONE resx row where each group is a csv line
            // 2. As is, every input key has a letter group even if its just one letter
            // 3. Groups are created from primary value + letter popup characters

            // notes (output)
            // 1. SOME Keyboards don't have 4th row so need to add (it should have @integer/key_code_mode_symbols key i think)
            // 2. Resx has KeyboardTextLayout1-10 with the name of the layout as the comment, empty entries are ignored
            // 3. NO special text layouts are stored only free text. Special layouts (email, url, non-enter done keys) are generated 
            //    using SpecialKeys and space bar as anchors to insert extra keys and space bar just adjusts 
            // 4. Resx also contains copies of Numbers layouts (numbers,digits,pin)
            // 5. Probably best to output keyboard resx's into separate file then have actual resx file fully translated then merge keyboard into it.
            //    So doesn't make issues trying to translate with non 'invariant' comments.
            // 6. Then a zip file is created of ResourceStrings.resx and Words.db and dumped up on github (try using git push -force)

            // todo
            // - get list of all @integer types, i think most should have a SpecialKeyType
            // - look through kbs.text and prune out weird ones (or find out how they look like these 16 key ones)


            bool write_to_test_dir = false;

            var excluded_languages = new string[] {
                "hackerkeyboardlayout"
            };
            var excluded_kbs = new string[] {
                "popup",
                "arabic_qwerty_alif",
                "urdu_with_symbols_a",
                "urdu_with_symbols_c",
                "urdu_with_symbols_comma",
                "urdu_with_symbols_full_stop",
                "urdu_with_symbols_i",
                "urdu_with_symbols_j",
                "urdu_with_symbols_k",
                "urdu_with_symbols_r",
                "urdu_with_symbols_t",
                "urdu_with_symbols_v",
                "urdu_with_symbols_x",
                "urdu_with_symbols_y",
                "eng_16keys",
                "heb_qwerty_niqquds",
                "heb_qwerty_niqqud_shin",
                "turkish_qwerty_terminal"
            };


            Keyboard.NeutralSymbols1 = Keyboard.CreateNeutralSet(1);
            Keyboard.NeutralSymbols2 = Keyboard.CreateNeutralSet(2);

            // FIND XML FILES

            // scan addons/languages for res/xml files and group by language name
            string source_dir = @"C:\Users\tkefauver\Desktop\DotNetExamples\AnySoftKeyboard\addons\languages";
            var kb_xml_path_groups = new DirectoryInfo(source_dir)
                .EnumerateFiles("*.xml", SearchOption.AllDirectories)
                //.Select(x => x.FullName)
                .Where(x => x.DirectoryName.EndsWith("xml") && x.Directory.Parent.FullName.EndsWith("res"))
                .GroupBy(x => Path.GetFileName(x.Directory.Parent.Parent.Parent.Parent.Parent.FullName))
                .ToList();

            // CREATE SETS

            List<Keyboards> full_set = [];

            foreach (var lang_xml_group in kb_xml_path_groups) {
                string language_name = lang_xml_group.Key;
                if (excluded_languages.Contains(language_name)) {
                    // excluded lang
                    continue;
                }
                string xml_dir = Path.Combine(source_dir, language_name);

                Keyboards kbc = null;
                foreach (var kb_xml_path in lang_xml_group) {
                    if (excluded_kbs.Any(x => kb_xml_path.FullName.Contains(x))) {
                        // avoid popup/fragment keyboard elements
                        continue;
                    }
                    if (Keyboards.Deserialize(kb_xml_path.FullName,excluded_kbs) is { } kbs) {
                        kbc = kbs;
                        break;
                    }
                }
                if (kbc == null || !kbc.Items.Any()) {
                    continue;
                }

                full_set.Add(kbc);
            }
            Key.UnknownParts.ForEach(x => MpConsole.WriteLine(x, stampless: true));

            // NORMALIZE LAYOUTS

            if (full_set.FirstOrDefault(x => x.FilePath.Contains("numpad")) is { } numpad_kbc) {
                full_set.ForEach(x => x.NormalizeCollection(numpad_kbc));
            }

            // OUTPUT JSON

            string output_dir = null;
            if (write_to_test_dir) {
                output_dir = @"C:\Users\tkefauver\Desktop\kb";
            } else {
                output_dir = KeyboardAssetPackDir;
            }

            if (write_to_test_dir) {
                if (Directory.Exists(output_dir)) {
                    Directory.Delete(output_dir, true);
                }
                Directory.CreateDirectory(output_dir);
            }

            foreach (var kbc in full_set) {
                string set_dir = Path.Combine(output_dir, kbc.Locale);
                if (!Directory.Exists(set_dir)) {
                    if (write_to_test_dir) {
                        Directory.CreateDirectory(set_dir);
                    } else {
                        MpConsole.WriteLine($"Skipped: {kbc.Locale}");
                        continue;
                    }
                }

                if (kbc.FilePath.Contains("english")) {
                    var test = kbc.Items.FirstOrDefault(x => x.DefaultEnabled);
                    var test2 = kbc.Items.FirstOrDefault(x => x.IdValue == "c7535083-4fe6-49dc-81aa-c5438a1a343a");
                }
                var kbfc = kbc.Items.Select(x => x.ToKeyboardFormat()).ToList();

                Directory.GetFiles(set_dir).Where(x => x.EndsWith(".kb")).ToList().ForEach(x => File.Delete(x));
                foreach (var kbf in kbfc) {
                    if (kbf.SerializeObject() is not { } json1 ||
                        json1.ToPrettyPrintJson() is not { } json) {
                        continue;
                    }
                    string kb_fn = kbf.isNumPad ? "numpad" : Path.GetFileNameWithoutExtension(kbf.FilePath);
                    string kb_path = Path.Combine(set_dir, kb_fn + ".kb");

                    // prevent overwrites 
                    int count = 1;
                    while (File.Exists(kb_path)) {
                        string kb_fn_inc = kb_fn + (count++).ToString();
                        kb_path = Path.Combine(set_dir, kb_fn_inc + ".kb");
                    }
                    File.WriteAllText(kb_path, json, Encoding.Unicode);
                }
                MpConsole.WriteLine($"{kbc.Locale} DONE");
            }
        }

    }
}
