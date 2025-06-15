using SQLite;

using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class Word {
        [PrimaryKey]
        public int Id { get; set; }

        [Indexed]
        public string WordText { get; set; }

        [Indexed]
        public int Rank { get; set; }

        [Indexed]
        public int Uses { get; set; }

        [Indexed]
        public int Omitted { get; set; } = 0;

        public int UserWord { get; set; } = 0;

        public string Culture { get; set; } = string.Empty;

        public override string ToString() {
            return WordText;//$"{WordText} R:{Rank} U:{Uses} O: {Omitted}";
        }
        public virtual Word Clone() {
            return new Word() {
                Id = Id,
                WordText = WordText,
                Rank = Rank,
                Uses = Uses,
                Omitted = Omitted,
                Culture = Culture,
                UserWord = UserWord
            };
        }
    }

    public class WordComparision : Word {
        public double EditDistance { get; set; }

        [Ignore]
        public virtual string Summary {
            get {
                string use = $"{ResourceStrings.U["UsesText"].value} {Uses}";
                string rank = $"{ResourceStrings.U["RankText"].value} {Rank}";
                string learn = $"{ResourceStrings.U["LearnedText"].value} {(UserWord == 0 ? ResourceStrings.U["NoText"].value : ResourceStrings.U["NoText"].value)}";
                string omit = $"{ResourceStrings.U["ForgottenText"].value} {(Omitted == 0 ? ResourceStrings.U["NoText"].value : ResourceStrings.U["NoText"].value)}";
                string result = $"{use} | {rank} | {learn} | {omit}";
                return result;
            }
        }
        public override string ToString() {
            return $"{WordText} {EditDistance}";
        }
        public new WordComparision Clone() {

            return new WordComparision() {
                Id = Id,
                WordText = WordText,
                Rank = Rank,
                Uses = Uses,
                Omitted = Omitted,
                Culture = Culture,
                UserWord = UserWord,
                EditDistance = EditDistance
            };
        }
    }
    public class EmojiComparision : WordComparision {
        Emoji Emoji { get; set; }
        public override string Summary => Emoji.Summary;
        public EmojiComparision(Emoji emoji, string text, double distance) {
            Emoji = emoji;
            WordText = text;
            EditDistance = distance;
        }
    }
}




