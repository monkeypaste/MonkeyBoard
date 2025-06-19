//using KeyboardLib;
using MonkeyPaste.Common;
using MonkeyBoard.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBoard.Builder {
    public static class EmojiDataBuilder {
        static string KeyboardAssetPackDir => KeyboardBuilder.KeyboardAssetPackDir;
        public static void FetchEmojiInfo() {
            bool done = false;
            string emoji_groups_path = @"C:\Users\tkefauver\Desktop\emoji_info_151.txt";
            //string emoji_groups_uri = "https://unicode.org/Public/emoji/15.1/emoji-test.txt";

            string output_path = Path.Combine(
                KeyboardAssetPackDir,
                "EmojiStrings_test.resx"
                );

            async Task<List<EmojiGroup>> GetGroupsAsync() {
                string group_prefix = "# group: ";
                string sub_group_prefix = "# subgroup: ";


                List<EmojiGroup> EmojiGroups = [];
                var sw = Stopwatch.StartNew();
                await Task.Delay(1);
                //string emoji_test_text = await MpFileIo.ReadTextFromUriAsync(emoji_groups_uri);
                string emoji_test_text = MpFileIo.ReadTextFromFile(emoji_groups_path);
                sw.Stop();
                MpConsole.WriteLine($"Time taken: {sw.ElapsedMilliseconds}ms");
                var emoji_test_lines = emoji_test_text.SplitByLineBreak();
                for (int i = 0; i < emoji_test_lines.Length; i++) {
                    string cur_line = emoji_test_lines[i];
                    if (string.IsNullOrWhiteSpace(cur_line)) {
                        continue;
                    }

                    if (cur_line.StartsWith(group_prefix)) {
                        var em_group = new EmojiGroup() {
                            GroupName = cur_line.SplitNoEmpty(group_prefix)[0].Trim()
                        };
                        EmojiGroups.Add(em_group);
                        continue;
                    }
                    if (cur_line.StartsWith(sub_group_prefix)) {
                        var em_group = new EmojiSubGroup() {
                            Parent = EmojiGroups.Last(),
                            SubGroupName = cur_line.SplitNoEmpty(sub_group_prefix)[0].Trim()
                        };
                        EmojiGroups.Last().SubGroups.Add(em_group);
                        continue;
                    }
                    if (cur_line.StartsWith("#")) {
                        // comment line
                        continue;
                    }

                    var cur_sub_group = EmojiGroups.Last().SubGroups.Last();

                    var emoji = new Emoji() { Parent = cur_sub_group, SortOrder = i };

                    // line format: code points; status # emoji name

                    var code_point_parts = cur_line.SplitNoEmpty(";");
                    emoji.CodePointStr = code_point_parts[0].Trim();

                    var status_parts = code_point_parts[1].SplitNoEmpty("#").Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
                    emoji.Qualification = status_parts[0].Trim();

                    var emoji_parts = status_parts[1].SplitNoEmpty(" E");
                    emoji.EmojiStr = emoji_parts[0].Trim();

                    emoji.Version = emoji_parts[1].SplitNoEmpty(" ")[0];
                    emoji.Description = emoji_parts[1].SplitNoEmpty($"{emoji.Version} ")[0];
                    if (cur_line.Contains("#️⃣")) {
                        emoji.EmojiStr = "#️⃣";
                        emoji.Description = "keycap: #";
                    }

                    cur_sub_group.Emojis.Add(emoji);
                }
                return EmojiGroups;
            }

            void MergeCombinations(EmojiSet es) {
                var to_remove = new List<Emoji>();
                Emoji cur_head = null;
                foreach (var em in es.Emojis.OrderBy(x => x.SortOrder)) {
                    if(em.Parent.Parent.GroupName == "Flags") {
                        // country flags code points are organized by culture code not base emoji, they're all base
                        continue;
                    }
                    if (em.QualificationType == EmojiQualificationType.Unqualified ||
                        em.QualificationType == EmojiQualificationType.MinimallyQualified) {
                        // these appear as duplicates and merging hides qualification
                        to_remove.Add(em);
                        continue;
                    }
                    if (cur_head == null ||
                        
                        em.CodePoints[0] != cur_head.CodePoints[0]) {
                        // new head
                        cur_head = em;
                        continue;
                    }
                    // append combo to base
                    cur_head.EmojiStr += "," + em.EmojiStr;

                    // mark for removal
                    to_remove.Add(em);
                }
                MpConsole.WriteLine($"Combined combinations: {to_remove.Count}");

                for (int i = 0; i < to_remove.Count; i++) {
                    var em = to_remove[i];
                    var parent = es.Groups.FirstOrDefault(x => x.GroupName == em.Parent.Parent.GroupName).SubGroups.FirstOrDefault(x => x.SubGroupName == em.Parent.SubGroupName);
                    parent.Emojis.Remove(em);
                }
            }
            void WriteToResx(string path, EmojiSet es) {
                // key = <emoji> value = <translated search text> comment = <serialized meta>
                Dictionary<string, (string value, string comment)> emoji_resx =
                    es.Emojis.ToDictionary(x => x.EmojiStr, x => (x.SearchText, x.ToResxComment()));
                MpResxTools.WriteResxToPath(path, emoji_resx);
            }


            Task.Run(async () => {
                var es = new EmojiSet() { Version = "15.1" };
                es.Groups = await GetGroupsAsync();
                MergeCombinations(es);
                WriteToResx(output_path, es);
                done = true;
            });
            while (!done) {
                Thread.Sleep(100);
            }
        }
    }
}
