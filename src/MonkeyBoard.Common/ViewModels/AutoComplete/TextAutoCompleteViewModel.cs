using Avalonia;
using Avalonia.Media;

using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class TextAutoCompleteViewModel : AutoCompleteViewModelBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new MenuViewModel Parent { get; private set; }
        #endregion

        #region Appearance
        #endregion

        #region Layout
        public override Rect AutoCompleteRect {
            get {
                double w = MenuViewModel.MenuRect.Width - (MenuViewModel.OptionsButtonRect.Width * 2);
                double h = MenuViewModel.MenuRect.Height;
                double x = Parent.MenuRect.Left + MenuViewModel.OptionsButtonRect.Width;
                double y = Parent.MenuRect.Top;
                return new Rect(x, y, w, h);
            }
        }
        #endregion

        #region State
        protected override int MaxVisibleCompletionItems => 3;
        protected override int MaxCompletionItemCount =>
            KeyboardViewModel.MaxTextCompletionResults;
        public override MenuItemType CompletionType => MenuItemType.TextCompletionItem;
        public override MenuTabItemType TabType => MenuTabItemType.None;
        public override bool IsVisible =>
            //KeyboardViewModel.IsVisible &&
            !KeyboardViewModel.IsNumPadLayout &&
            MenuViewModel.CurMenuPageType == MenuPageType.TextCompletions;

        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors

        public TextAutoCompleteViewModel(MenuViewModel parent) : base(parent) {
            Parent = parent;
        }

        #endregion

        #region Public Methods
        public override void Init() {
            base.Init();
            WordDb.LoadDbAsync(InputConnection, false).FireAndForgetSafeAsync();
            KeyboardViewModel.OnKeyLayoutChanged += KeyboardViewModel_OnKeyLayoutChanged;
            //if (KeyboardViewModel.CanAutoCorrect && !CommonWords.Any()) {
            //    Task.Run(async () => {
            //        while (!WordDb.IsLoaded) {
            //            await Task.Delay(100);
            //        }
            //        int count =
            //            OperatingSystem.IsIOS() ? 0 :
            //            (int)((InputConnection.SharedPrefService.GetPrefValue<int>(PrefKeys.AUTO_CORRECT_DICT_RATIO) / 100d) * WordDb.Stats.WordCount);
            //        //int count = OperatingSystem.IsIOS() ? 0 : 5_000;
            //        CommonWords = await WordDb.GetMostCommonWordsAsync(count);
            //    });
            //}
        }

        public IEnumerable<(char, Rect)> GetNeighbors(KeyViewModel kvm) {
            // NOTE neighbors are all lower case letters only
            var center = kvm.TotalRect.Center;
            double horiz_r = Math.Max(kvm.TotalRect.Size.Width, kvm.TotalRect.Size.Height);
            double vert_r = Math.Min(kvm.TotalRect.Size.Width, kvm.TotalRect.Size.Height);
            var p0_horiz = new Point(center.X, center.Y - horiz_r);
            var p0_vert = new Point(center.X, center.Y - vert_r);
            var hits = new List<(KeyViewModel, double angle)>();
            double step = 22.5;
            for(double angle = 0; angle < 360; angle += step) {
                bool is_horiz = (angle >= 45 && angle <= 135) || (angle >= 225 && angle <= 315);
                var anchor_p = is_horiz ? p0_horiz : p0_vert;
                var p = anchor_p.Rotate(center, angle);
                if(KeyboardViewModel.VisibleKeyboardKeys.FirstOrDefault(x => x.TotalRect.Contains(p)) is not { } htt_kvm) {
                    continue;
                }
                hits.Add((htt_kvm, angle));
            }
            var neighbors = hits.GroupBy(x => x.Item1).Select(x => new KeyNeighbor(x.Key, x.Min(y => y.angle), x.Max(y => y.angle))).OrderBy(x => x.StartAngle).ToList();
            if(neighbors.FirstOrDefault(x => x.StartAngle <= 90 && x.EndAngle >= 270) is { } overlap_neighbor) {
                // swap angles to keep inner arc
                double temp = overlap_neighbor.StartAngle;
                overlap_neighbor.StartAngle = overlap_neighbor.EndAngle;
                overlap_neighbor.EndAngle = temp;
            }

            return
                neighbors
                .Where(x => x.Neighbor.PrimaryValue.ToStringOrEmpty().Length == 1 && char.IsLetter(x.Neighbor.PrimaryValue[0]))
                .Select(x => (x.Neighbor.PrimaryValue.ToLower()[0], x.Neighbor.TotalHitRect));
            ;
        }

        private void KeyboardViewModel_OnKeyLayoutChanged(object sender, EventArgs e) {
            InputNeighborLookup.Clear();
            if(!KeyboardViewModel.CanAutoCorrect ||
                KeyboardViewModel.IsNumPadLayout) {
                return;
            }
            var input_keys = KeyboardViewModel.VisibleKeyboardKeys.Where(x => x.IsInput);
            foreach(var input_kvm in input_keys) {
                if(input_kvm.PrimaryValue is not { } pv ||
                    pv.Length != 1 ||
                    GetNeighbors(input_kvm) is not { } neighbor_chars) {
                    continue;
                }
                InputNeighborLookup.AddOrReplace(pv.ToLower()[0], neighbor_chars.ToList());
            }
        }

        public override void DoCompletion(
            SelectableTextRange textInfo,
            string completionText,
            bool isAutoCorrect = false) {
            // get current text to complete and its start idx
            if(TextRangeTools.GetWordAtCaret(
                textInfo,
                out int startIdx,
                out bool hasTrailingWhiteSpace,
                out _,
                acceptsOneLeadingEowc: isAutoCorrect) is not { } orig_text) {
                return;
            }
            bool is_override_auto_correct = completionText == ConfirmIgnoreAutoCorrectCompletionText;
            if(is_override_auto_correct) {
                if(!AutoCorrectUndoneWords.Contains(orig_text.ToLower().Trim())) {
                    AutoCorrectUndoneWords.Add(orig_text.ToLower().Trim());
                }
                completionText = orig_text.Trim() + KeyConstants.SPACE_STR;
            }

            KeyboardViewModel.SuppressCursorChange();
            // only insert space if:
            // 1. not email
            // 2. next insert doesn't exist
            // 3. next insert is not a space
            // 4. next insert is not the end of sentence
            int next_idx = startIdx + orig_text.Length;
            char next_char =
                next_idx < textInfo.Text.Length ?
                    textInfo.Text[next_idx] :
                    default;
            bool needs_trailing_space =
                IsNormalTextCompletion &&
                next_char != KeyConstants.SPACE_CHAR &&
                (next_char == default || next_char == '\n');
            //next_char != KeyConstants.SPACE_CHAR && 
            //!TextRangeTools.IsEndOfSentenceChar(next_char);

            if(isAutoCorrect &&
                InputConnection.OnTextRangeInfoRequest() is { } cur_info &&
                !cur_info.IsValueEqual(textInfo)) {
                // auto correct coming from stale info, do replacement w/ fresh data
                int actual_next_idx = cur_info.SelectionEndIdx;
                string extra_text = string.Empty;
                if(actual_next_idx > next_idx) {
                    extra_text = cur_info.Text.Substring(next_idx, actual_next_idx - next_idx);
                }

                if(!string.IsNullOrEmpty(extra_text)) {
                    // append all fresh text present after correctoin and before fresh sel end
                    completionText += extra_text;
                    // adjust replace range for extra
                    next_idx = startIdx + orig_text.Length + extra_text.Length;

                    needs_trailing_space = false;
                }
            }

            string output_text = completionText + (needs_trailing_space ? KeyConstants.SPACE_STR : string.Empty);

            InputConnection.OnReplaceText(startIdx, next_idx, output_text);

            if(isAutoCorrect || is_override_auto_correct) {
                // make last info match auto-corrected info
                LastAutoCorrectRange = (startIdx, orig_text, completionText);
            }

            if(!needs_trailing_space && IsNormalTextCompletion) {
                // when trailing space not part of replacement move insert
                // 1 past its extent or some editors will treat input as another replacement
                // (i think since replace uses composingRegion on android at least)
                InputConnection.OnNavigate(1, 0);
            }
            KeyboardViewModel.UnsupressCursorChange();
        }
        public override double GetCompletionTextFontSize(string text, int itemIdx, out string formatted_text) {
            formatted_text = text;
            double omit_offset = OmittableItemIdx == itemIdx ? -2 : 0;
            int def_char_count =
                KeyboardViewModel.IsShiftOnLock ?
                    5 : // caps lock
                    KeyboardViewModel.IsShiftOnTemp ?
                        7 : // title case
                        9; // lower case
            double fs = 0;
            if(string.IsNullOrEmpty(text) ||
                text.Length < def_char_count) {
                fs = KeyboardLayoutConstants.TextCompletionFontSize1 + omit_offset;
            } else {
                int diff = text.Length - def_char_count;
                if(diff < def_char_count) {
                    fs = KeyboardLayoutConstants.TextCompletionFontSize2 + omit_offset;
                } else {
                    formatted_text = text.Substring(0, def_char_count) + Environment.NewLine + text.Substring(def_char_count, text.Length - def_char_count);
                    fs = KeyboardLayoutConstants.TextCompletionFontSize3 + omit_offset;
                }
            }
            return fs * KeyboardViewModel.FloatEmojiScale;
        }
        public double GetCompletionTextFontSize2(string text, int itemIdx, out string formatted_text) {
            formatted_text = text;

            double max_fs = KeyboardLayoutConstants.TextCompletionFontSize1;
            double min_fs = KeyboardLayoutConstants.TextCompletionFontSize3;
            double fs = max_fs;
            double fs_step = 1;
            double def_pad = 5;
            double pad = OmittableItemIdx == itemIdx ? OmitButtonWidth : def_pad;
            double max_width = AutoCompleteItemWidth - pad;

            if(InputConnection.TextTools is not { } tm) {
                return min_fs;
            }
            List<string> lines = [text];
            while(true) {
                bool fits = true;
                for(int i = 0; i < lines.Count; i++) {
                    var text_rect = tm.MeasureText(text, fs, out _, out _);
                    if(text_rect.Width > max_width) {
                        fits = false;
                        break;
                    }
                }
                if(fits) {
                    formatted_text = lines.Count > 1 ? string.Join(Environment.NewLine, lines) : text;
                    return fs;
                }
                fs -= fs_step;
                if(fs < min_fs) {
                    fs = min_fs;

                    int splits = lines.Count + 1;
                    int split_len = (int)(text.Length / splits);
                    lines.Clear();
                    for(int i = 0; i < splits; i++) {
                        int sidx = lines.Sum(x => x.Length);
                        int eidx = Math.Min(sidx + split_len, text.Length - 1);
                        lines.Add(text.Substring(sidx, eidx - sidx));
                    }
                }
            }
        }
        public override double GetCompletionTextFontSize(string text, int itemIdx) =>
            GetCompletionTextFontSize(text, itemIdx, out _);
        #endregion

        #region Protected Methods
        protected override async Task OmitItemAsync(string item_text) {
            await WordDb.OmitWordAsync(item_text);
        }
        protected override Point TranslateContainerPoint(Point loc) {
            double x = loc.X - AutoCompleteRect.Left;
            double y = loc.Y - AutoCompleteRect.Top;
            return new Point(x, y);
        }
        protected override void StartCompletionRequest(string input, int max, CancellationToken ct) {
            MpConsole.WriteLine($"Compl Request started. Input: '{input}'", level: MpLogLevel.Verbose);

            if(string.IsNullOrEmpty(input)) {
                if(KeyboardViewModel.IsNextWordCompletionEnabled) {
                    SetCompletionItems(WordDb.TakeSomeDefaultWords(max));
                }
                return;
            }
            CurSearchDepth = 0;
            StartBusyAnimation();
            WorkingComparisions = [];

            InputConnection.MainThread.Post(async () => {
                HasCompletionMatch = false;

                while(true) {
                    var level_comps = await WordDb.GetWordsByRankLevelAsync(input, max, CurSearchDepth, ct);
                    if(ct.IsCancellationRequested) {
                        // new input
                        break;
                    }
                    if(level_comps == null) {
                        // no more possible results
                        break;
                    }
                    if(CurSearchDepth == 0 &&
                        level_comps.FirstOrDefault(x => x.WordText.ToLower() == input.ToLower()) is { } exact_match) {
                        // input has exact match so won't attempt autocorrect
                        HasCompletionMatch = true;
                        //level_comps.Remove(exact_match);
                    }

                    // merge rank range results
                    MergeComparisionsIntoWorkingSet(level_comps, max, CurSearchDepth, false);

                    if(KeyboardViewModel.IsShowEmojiTextCompletionEnabled &&
                        CurSearchDepth == 0 &&
                        level_comps.Any() &&
                        Parent.EmojiPagesViewModel.EmojiSearchViewModel.EmojiAutoCompleteViewModel is { } emacvm) {
                        var emoji_comps = emacvm.GetEmojiComparisions(input.ToLower(), Math.Max(max - level_comps.Count, (int)(max / 3)));
                        if(emoji_comps.Any()) {
                            // merge emoji results
                            MergeComparisionsIntoWorkingSet(emoji_comps, max, CurSearchDepth, true);
                        }
                    }


                    if(WorkingComparisions.Select(x => x.WordText).ToList() is { } wcl) {
                        if(!HasCompletionMatch &&
                            KeyboardViewModel.IsConfirmIgnoreAutoCorrectEnabled) {
                            // add checkmark to front of compl
                            wcl.Insert(0, ConfirmIgnoreAutoCorrectCompletionText);
                        }
                        SetCompletionItems(wcl.Take(max));
                    }

                    if(CurSearchDepth == 0 && level_comps.Any()) {
                        // halt if any starts with completions
                        break;
                    }
                    CurSearchDepth++;
                }
                StopBusyAnimation();
            });

        }
        void MergeComparisionsIntoWorkingSet(IEnumerable<WordComparision> level_comps, int max, int level, bool isEmojis) {
            if(level == 0 && !isEmojis) {
                WorkingComparisions =
                        WorkingComparisions
                            .Union(level_comps)
                            .OrderByDescending(x => x.Uses)
                            .ThenByDescending(x => x.Rank)
                            .ThenBy(x => x.EditDistance)
                            .Take(max);
                return;
            }
            WorkingComparisions =
                        WorkingComparisions
                            .Union(level_comps)
                            .OrderBy(x => x.EditDistance)
                            .ThenByDescending(x => x.Uses)
                            .ThenByDescending(x => x.Rank)
                            .Take(max);
        }
        protected override string GetCompletionInput(SelectableTextRange textInfo, out bool isAtWordTerminator) {
            if(TextRangeTools.GetWordAtCaret(textInfo, out _, out _, out isAtWordTerminator) is not { } input) {
                return string.Empty;
            }
            return input;
        }
        #endregion

        #region Private Methods



        #endregion

        #region Commands
        #endregion
    }
}
