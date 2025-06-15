using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {

    public class EmojiSet {
        public static EmojiSet ParseFromResx(Dictionary<string, (string value, string comment)> resx) {
            var es = new EmojiSet();
            int idx = 0;
            foreach(var kvp in resx) {
                var em = Emoji.ParseFromResxComment(++idx,kvp.Key, kvp.Value.value, kvp.Value.comment, out string sg_name, out string g_name);

                if (es.Groups.FirstOrDefault(x => x.GroupName == g_name) is not { } g) {
                    g = new EmojiGroup() { GroupName = g_name };
                    es.Groups.Add(g);
                }
                if (g.SubGroups.FirstOrDefault(x => x.SubGroupName == sg_name) is not { } sg) {
                    sg = new EmojiSubGroup() { SubGroupName = sg_name, Parent = g };
                    g.SubGroups.Add(sg);
                }
                em.Parent = sg;
                sg.Emojis.Add(em);
            }
            return es;
        }
        public List<EmojiGroup> Groups { get; set; } = [];
        public string Version { get; set; }
        public IReadOnlyList<Emoji> Emojis =>
            Groups.SelectMany(x => x.SubGroups).SelectMany(x => x.Emojis).ToList();
    }
    public class EmojiGroup {
        public List<EmojiSubGroup> SubGroups { get; set; } = [];
        string _groupName = string.Empty;
        public string GroupName {
            get => _groupName.SanitizeForXml();
            set => _groupName = value;
        }
    }
    public class EmojiSubGroup {
        public EmojiGroup Parent { get; set; }
        public List<Emoji> Emojis { get; set; } = [];
        string _subGroupName = string.Empty;
        public string SubGroupName {
            get => _subGroupName.SanitizeForXml();
            set => _subGroupName = value;
        }
    }

    public class Emoji {
        public static Emoji ParseFromResxComment(int idx, string key, string val, string comment, out string sg_name, out string g_name) {
            var comment_parts = comment.SplitNoEmpty(";");
            var emoji = new Emoji() {
                EmojiStr = key,
                Description = val,
                Version = comment_parts[0],
                Qualification = 
                    comment_parts[1] == "0" ? 
                        "unqualified" : 
                        comment_parts[1] == "1" ? 
                            "minimally-qualified" : 
                            "fully-qualified"
            };
            
            sg_name = comment_parts[2];
            g_name = comment_parts[3];
            return emoji;
        }
        public EmojiSubGroup Parent { get; set; }

        public string[] CodePoints =>
            CodePointStr.SplitNoEmpty(" ");

        string _codePointStr;
        public string CodePointStr {
            get => _codePointStr ?? string.Join(",", EmojiStr.SplitNoEmpty(",").Select(x => x.ToCodePointStr()));
            set => _codePointStr = value;
        }
        public string Qualification { get; set; }
        int QualificationLevel {
            get {
                switch (Qualification) {
                    case "unqualified":
                        return 0;
                    case "minimally-qualified":
                        return 1;
                    case "fully-qualified":
                        return 2;
                    case "component":
                        return 3;
                    default:
                        throw new NotSupportedException($"Unknown qualification: {Qualification}");
                }
            }
        }
        public EmojiQualificationType QualificationType =>
            (EmojiQualificationType)QualificationLevel;

        public string EmojiStr { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
        public string SearchText =>
            string.Join(" ", [Description]).SanitizeForXml();
        public string Summary =>
            string.Join(" ", SearchText.ToWords().Distinct());//$"E{Version} | {SearchText} | {Qualification.ToTitleCase()}";

        public Emoji() { }
        public Emoji(string text) { 
            EmojiStr = text; 
        }
        public string ToResxComment() {
            return $"{Version};{QualificationLevel};{Parent.SubGroupName};{Parent.Parent.GroupName}".SanitizeForXml();
        }

        public IEnumerable<Emoji> Split() {
            return
                EmojiStr
                .SplitNoEmpty(",")
                .Select((x, idx) => new Emoji() {
                    EmojiStr = x,
                    Version = Version,
                    Qualification = Qualification,
                    Description = Description,
                    SortOrder = SortOrder + idx,
                    Parent = Parent,
                });
        }
        
    }

    public static class EmojiModelExtensions {
        public static string SanitizeForXml(this string text) {
            return text.Replace("&", "and").Replace("-", " ").Replace(":", " ").Replace("flag:", string.Empty);
        }
    }
}
