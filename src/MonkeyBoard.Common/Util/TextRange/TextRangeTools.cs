using Avalonia.Controls.Platform;
using Avalonia.Media;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace MonkeyBoard.Common {
    public static class TextRangeTools {
        #region Private Variables

        // symbols that are part of words,
        // like: didn't or t-shirt
        // unlike: hello_there or whats.up
        static char[] AllowedWordSymbols => ['\'', '-'];

        static char[] EndOfSentenceSymbols => ['\n', '.', '!', '?', '\0'];
        static char[] EndOfWordChars => ['\n', '.', '!', '?', ' ', ')',']','}',',','"', '\0'];
        static char[] SmartPunctuationChars => ['.', '!', '?', '\'', ',',';'];
        #endregion

        #region Insert Change Info
        public static bool WasSpace(SelectableTextRange curRange, SelectableTextRange lastRange) {
            if (curRange != null && lastRange != null &&
                curRange.LeadingText.Length > 0 &&
                curRange.LeadingText.Substring(0, curRange.LeadingText.Length - 1) == lastRange.LeadingText &&
                curRange.LeadingText.Last() == KeyConstants.SPACE_CHAR) {
                return true;
            }
            return false;
        }
        public static bool WasBackspace(SelectableTextRange curRange, SelectableTextRange lastRange) {
            if (curRange != null && lastRange != null &&
                lastRange.LeadingText.Length > 0 &&
                curRange.LeadingText == lastRange.LeadingText.Substring(0, lastRange.LeadingText.Length - 1)) {
                return true;
            }
            return false;
        }

        public static bool WasLastChangeInsert(SelectableTextRange curRange, SelectableTextRange lastRange) {
            if(curRange != null && lastRange != null &&
                curRange.SelectionStartIdx - lastRange.SelectionStartIdx == 1 &&
                curRange.Text.Length - lastRange.Text.Length == 1) {
                return true;
            }
            return false;
        }

        #endregion

        #region Virtualization

        //public static int GetVirtualStartOffset(SelectableTextRange tri, int actualStartIdx, int max_v_len) {

        //}
        #endregion

        #region Editing
        public static void DoBackspace(SelectableTextRange tri, bool forward = false) {
            int sidx = Math.Max(0, Math.Min(tri.SelectionStartIdx, tri.SelectionEndIdx));
            int eidx = Math.Max(0, Math.Max(tri.SelectionStartIdx, tri.SelectionEndIdx));
            int len = Math.Max(0, eidx - sidx);
            if (len == 0) {
                if(forward) {
                    string new_text2 = tri.LeadingText + tri.TrailingText.Skip(1).ToString();
                    tri.SetText(new_text2, sidx, 0);
                    return;
                } 
                string new_text = tri.Text.Substring(0, Math.Max(0, sidx - 1)) + tri.Text.Substring(Math.Max(0,eidx));
                int old_idx = tri.SelectionStartIdx;
                int new_sidx = Math.Max(0, old_idx - 1);
                tri.SetText(new_text, new_sidx, 0);
            } else {
                tri.SelectedText = string.Empty;
            }
        }
        public static void DoText(SelectableTextRange tri, string text) {
            tri.SelectedText = text;
            tri.Select(tri.SelectionEndIdx, 0);
        }
        public static void DoNavigate(SelectableTextRange tri, int dx, int dy) {
            // TODO implement y somehow
            int new_sidx = Math.Clamp(tri.SelectionStartIdx + dx, 0, tri.Text.Length);
            tri.Select(new_sidx, 0);
        }

        #endregion

        #region Selected Word

        /// <summary>
        /// Finds word text from <see cref="SelectableTextRange.SelectionEndIdx"/>
        /// </summary>
        /// <param name="textRange"></param>
        /// <param name="rangeStartIdx">Start of word in <see cref="SelectableTextRange.Text"/></param>
        /// <param name="hasTrailingSpace">Used to determine if completion should insert a trailing space</param>
        /// <param name="breakOnCompoundWords"></param>
        /// <param name="trimSymbols">Removes non-alphanumeric characters</param>
        /// <param name="acceptsOneLeadingEowc"></param>
        /// <returns>Word text </returns>
        public static string GetWordAtCaret(
            SelectableTextRange textRange, 
            out int rangeStartIdx, 
            out bool hasTrailingSpace, 
            out bool isAtWordTerminator,
            bool breakOnCompoundWords = true,
            bool acceptsOneLeadingEowc = false) {

            // param uses:
            // rangeStartIdx: DoCompletion,CompletionDisplayValues
            // hasTrailingSpace: DoCompletion
            // isAtWordTerminator: GetCompletionInput

            // breakOnCompoundWords (false): CompletionDisplayValues
            // acceptsOneLeadingEowc (when isAutoCorrect): DoCompletion

            rangeStartIdx = 0;
            isAtWordTerminator = false;
            hasTrailingSpace = false;

            if (textRange == null ||
                textRange.LeadingText is not { } leading_text ||
                textRange.SelectedText is not { } sel_text ||
                textRange.TrailingText is not { } trailing_text) {
                return string.Empty;
            }   

            string pre_text = leading_text + sel_text;
            string pre_comp_text = FindWordBreak(pre_text.ToCharArray(), false, breakOnCompoundWords, acceptsOneLeadingEowc);
            string post_comp_text = FindWordBreak(trailing_text.ToCharArray(), true, breakOnCompoundWords, acceptsOneLeadingEowc);
            string result = pre_comp_text + post_comp_text;
            int actual_comp_end_idx = textRange.SelectionEndIdx + post_comp_text.Length;
            rangeStartIdx = actual_comp_end_idx - result.Length;

            int endIdxDelta = actual_comp_end_idx - textRange.SelectionEndIdx;
            if (endIdxDelta < trailing_text.Length &&
                trailing_text[endIdxDelta] == KeyConstants.SPACE_CHAR) {
                hasTrailingSpace = true;
            }
            if(sel_text.Length == 0 &&
                result.Length == 0 && 
                FindWordBreak(leading_text.ToCharArray(), false,breakOnCompoundWords,true) is { } leading_word &&
                leading_word.Length > 1 &&
                EndOfWordChars.Contains(leading_word.Last())) {
                isAtWordTerminator = true;
            }

            return result;
        }
        static string FindWordBreak(char[] text, bool forward, bool breakOnCompoundWords, bool acceptsOneLeadingEowc) {
            var sb = new StringBuilder();
            char last_char = default;
            for (int i = 0; i < text.Length; i++) {
                int text_idx = forward ? i : text.Length - 1 - i;
                char cur_char = text[text_idx];

                if (breakOnCompoundWords &&
                    last_char != default &&
                    IsCompoundWordBreakChar(cur_char, last_char)) {
                    if (!forward && char.IsLetter(cur_char)) {
                        // never want the forward character
                        // only add if break char is camel/pascal case
                        sb.Insert(0, cur_char);
                    }
                    break;
                }
                if (!forward &&
                    acceptsOneLeadingEowc &&
                    i == 0 &&
                    EndOfWordChars.Contains(cur_char)) {
                    // allow whitespace...
                    // needed when replacing autocorrected text since its replaced
                    // AFTER hitting space
                    //continue;
                } else if(!char.IsLetterOrDigit(cur_char) && !AllowedWordSymbols.Contains(cur_char)) {
                    break;
                }

                if (forward) {
                    sb.Append(cur_char);
                } else {
                    sb.Insert(0, cur_char);
                }
                last_char = cur_char;
            }
            return sb.ToString();
        }

        #endregion

        #region Compound Words

        /// <summary>
        /// Returns 'Output' from 'ThisIsOutput'
        /// </summary>
        /// <param name="word_text"></param>
        /// <returns></returns>
        public static string GetCompoundTextComponent(string word_text, int caret_idx, bool forward) {
            var sb = new StringBuilder();
            char last_char = default;
            for (int i = 0; i < word_text.Length; i++) {
                char cur_char = word_text[word_text.Length - 1 - i];
                sb.Insert(0, cur_char);
                if (i > 0 && cur_char.IsCapitalCaseChar()) {
                    break;
                }
                last_char = cur_char;
            }
            return sb.ToString();
        }
        static bool IsCompoundWordBreakChar(char cur_char, char last_char) {
            // snake_case camelCase PascalCase kebab-case
            return cur_char == '_' ||
                    cur_char == '-' ||
                    cur_char.IsCapitalCaseChar() != last_char.IsCapitalCaseChar();
        }
        #endregion

        #region Grammar
        public static bool IsSmartPunctuationChar(char let) {
            return SmartPunctuationChars.Contains(let);
        }
        public static bool IsEndOfSentenceChar(char let, bool allowNewLines, out bool isNewLine) {
            // this is weird but treat new lines as EOS for shift but not insert space in compl 
            int eos_idx = EndOfSentenceSymbols.IndexOf(let);
            isNewLine = eos_idx == 0;
            return eos_idx >= (allowNewLines ? 0:1);
        }
        #endregion
    }
}
