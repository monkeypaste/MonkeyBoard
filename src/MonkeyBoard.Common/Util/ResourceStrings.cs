using MonkeyPaste.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;

namespace MonkeyBoard.Common {
    public static class ResourceStrings {
        public static bool IsLoaded =>
            !string.IsNullOrEmpty(LoadedUiCulture);
        static string LoadedUiCulture { get; set; }
        static string LoadedKbCulture { get; set; }
        public static Dictionary<string, (string value, string comment)> U { get; private set; }
        public static Dictionary<string, (string value, string comment)> K { get; private set; }
        public static Dictionary<string, (string value, string comment)> E { get; private set; }
        public static void Init(string ui_cc,string kb_cc) {
            if(LoadedUiCulture == ui_cc && LoadedKbCulture == kb_cc) {
                return;
            }
            string ui_cul_dir = Path.Combine(CultureManager.CulturesRootDir,ui_cc);
            string kb_cul_dir = Path.Combine(CultureManager.CulturesRootDir,kb_cc);

            MpDebug.Assert(ui_cul_dir.IsDirectory(), $"Error ui cul dir '{ui_cul_dir}' doesn't exist");
            MpDebug.Assert(kb_cul_dir.IsDirectory(), $"Error kb cul dir '{kb_cul_dir}' doesn't exist");
            
            string resx_path1 =
                Path.Combine(
                    ui_cul_dir,
                    $"UiStrings.{ui_cc}.resx");
            U = MpResxTools.ReadResxFromPath(resx_path1);

            string resx_path2 =
                Path.Combine(
                    kb_cul_dir,
                    $"KeyboardStrings.{kb_cc}.resx");
            K = MpResxTools.ReadResxFromPath(resx_path2);

            string resx_path3 =
                Path.Combine(
                    kb_cul_dir,
                    $"EmojiStrings.{kb_cc}.resx");
            var test = MpFileIo.ReadTextFromFile(resx_path3);
            E = MpResxTools.ReadResxFromPath(resx_path3);

            if (ui_cc != kb_cc) {
                // when ui & kb cc are different merge and distinct value for searchability
                string resx_path4 =
                Path.Combine(
                    ui_cul_dir,
                    $"EmojiStrings.{ui_cc}.resx");
                var E2 = MpResxTools.ReadResxFromPath(resx_path4);
                for (int i = 0; i < E.Count; i++) {
                    var kvp1 = E.ElementAt(i);
                    var kvp2 = E2.ElementAt(i);
                    string new_value = string.Join(" ",(kvp1.Value.value + " "+ kvp2.Value.value).ToWords().Distinct());
                    E[kvp1.Key] = (new_value, kvp1.Value.comment);
                }
            }

            LoadedUiCulture = ui_cc;
        }
    }
}
