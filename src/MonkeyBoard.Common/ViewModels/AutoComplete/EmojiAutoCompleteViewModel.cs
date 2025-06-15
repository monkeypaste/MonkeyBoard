using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class EmojiAutoCompleteViewModel : AutoCompleteViewModelBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        public new EmojiSearchViewModel Parent { get; private set; }

        #region Members
        #endregion

        #region View Models
        #endregion

        #region Appearance
        public override CornerRadius ContainerCornerRadius =>
            new CornerRadius(KeyboardViewModel.CommonCornerRadius.TopLeft, KeyboardViewModel.CommonCornerRadius.TopRight, 0, 0);
        #endregion

        #region Layout
        public override double FrameOffsetY =>
            KeyboardViewModel.CanShowPopupWindows ?
                Parent.TotalRect.Height :
               0// AutoCompleteRect.Top
            ;
        public override Rect AutoCompleteRect {
            get {
                double w = Parent.TotalWidth;
                double h = AutoCompleteHeight;
                double x = 0;
                double y = KeyboardViewModel.CanShowPopupWindows ? KeyboardViewModel.MenuRect.Top - Parent.InnerContainerHeight : 0;
                return new Rect(x, y, w, h);
            }
        }

        public double AutoCompleteHeight =>
            KeyboardViewModel.MenuHeight;
        protected override int MaxVisibleCompletionItems => 5;
        #endregion

        #region State
        protected override int MaxCompletionItemCount =>
            20;
        public override MenuTabItemType TabType => MenuTabItemType.Emoji;
        protected override bool IsPasswordProtected => false;
        public override MenuItemType CompletionType =>
            MenuItemType.EmojiCompletionItem;
        int MaxEmojiCompletionItemCount =>
            KeyboardViewModel.MaxTextCompletionResults;

        public override bool IsVisible =>
            !KeyboardViewModel.IsNumPadLayout &&
            Parent.IsVisible
            //MenuViewModel.CurMenuPageType == MenuPageType.TextCompletions 
            //&&
            //CompletionItems.Count > 0
            ;
        #endregion

        #region Models

        List<string> OmittedEmojis { get; set; } = [];
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public EmojiAutoCompleteViewModel(EmojiSearchViewModel parent) : base(parent) {
            Parent = parent;
        }
        #endregion

        #region Public Methods
        public override void Init() {
            base.Init();

            OmittedEmojis.Clear();
            if(InputConnection is { } ic &&
                ic.SharedPrefService is { } sps &&
                sps.GetPrefValue<string>(PrefKeys.OMITTED_EMOJIS_CSV) is { } omitted_emojis_csv_str &&
                omitted_emojis_csv_str.SplitNoEmpty(",") is { } oel) {
                OmittedEmojis.AddRange(oel);
            }
        }
        public override void DoCompletion(SelectableTextRange textInfo, string completionText, bool isAutoCorrect = false) {
            Parent.Parent.Parent.DoEmojiText(completionText);
        }
        public override double GetCompletionTextFontSize(string text, int itemIdx) =>
            GetCompletionTextFontSize(text, itemIdx, out _);
        public override double GetCompletionTextFontSize(string text, int itemIdx, out string formatted_text) {
            formatted_text = text;
            if(itemIdx == OmittableItemIdx) {
                return KeyboardLayoutConstants.TextCompletionFontSize3;
            }
            return KeyboardLayoutConstants.EmojiFontSize;
        }

        #endregion

        #region Protected Methods
        protected override async Task OmitItemAsync(string item_text) {
            if(OmittedEmojis.Contains(item_text)) {
                // already omitted
                return;
            }
            await Task.Delay(1);
            OmittedEmojis.Add(item_text);
            if(InputConnection is not { } ic ||
                ic.SharedPrefService is not { } sps) {
                return;
            }
            sps.SetPrefValue(PrefKeys.OMITTED_EMOJIS_CSV, string.Join(",", OmittedEmojis));

            ShowCompletion(null, true);
        }
        protected override Point TranslateContainerPoint(Point loc) {
            double x = loc.X - Parent.TotalRect.Left - Parent.CloseButtonRect.Right;//AutoCompleteRect.Left;
            double y = loc.Y + Parent.InnerContainerRect.Height;
            if(!KeyboardViewModel.CanShowPopupWindows) {
                y = loc.Y + Parent.TotalRect.Height;
            }
            return new Point(x, y);
        }
        protected override string GetCompletionInput(SelectableTextRange textInfo, out bool isAtWordTerminator) {
            isAtWordTerminator = false;
            if(textInfo is not { } ti ||
                ti.Text is not { } input) {
                return string.Empty;
            }
            return input;
        }
        public IEnumerable<EmojiComparision> GetEmojiComparisions(string lowerCaseInput, int max) {
            if(string.IsNullOrEmpty(lowerCaseInput) ||
                lowerCaseInput.Length < 2) {
                return [];
            }
            return
                    GetEmojiComparisions_internal([lowerCaseInput], max, true)
                    .Where(x => x.Item2 >= 0)
                        .OrderBy(x => x.Item2)
                        .Select(x => new EmojiComparision(x.Item1.EmojiModel, x.Item1.PrimaryValue, x.Item2))
                        .Take(max);
        }

        IEnumerable<(EmojiKeyViewModel, int)> GetEmojiComparisions_internal(IEnumerable<string> words, int max, bool doDistanceScore) {
            return Parent.Parent.Parent.EmojiPages
                        .SelectMany(x => x.EmojiKeys)
                        .Where(x => x.Items.All(x => !OmittedEmojis.Contains(x)))
                        .Select(x => (x, GetCompletionScore(x, words, doDistanceScore)))
                        .Where(x => (x.Item2 >= 0));
        }
        protected override void StartCompletionRequest(string input, int max, CancellationToken ct) {
            if(!KeyboardViewModel.CanShowPopupWindows &&
                    string.IsNullOrEmpty(input) &&
                    Parent.Parent.Parent.EmojiPages.FirstOrDefault(x => x.EmojiPageType == EmojiPageType.Recents) is { } recent_epvm) {
                SetCompletionItems(recent_epvm.EmojiKeys.Select(x => x.PrimaryValue).Take(max));
                return;
            }
            var words = input.ToWords();
            IEnumerable<string> results =
                GetEmojiComparisions_internal(words, max, false)
                    .OrderByDescending(x => x.Item2)
                    .Select(x => x.Item1.PrimaryValue)
                    .Take(max);
            SetCompletionItems(results);
        }
        #endregion

        #region Private Methods
        int GetCompletionScore(EmojiKeyViewModel em, IEnumerable<string> words, bool doDistanceScore) {
            if(doDistanceScore && words.FirstOrDefault() is { } word) {
                // distance score should be ONE word only
                if(em.ProcessedSearchText.Contains(word.ToLower()) &&
                    em.SearchWords.FirstOrDefault(x => x.StartsWith(word)) is { } search_starts_with) {
                    // when word (or part of it) is in search text
                    // highest score is full search text match
                    //int score = (int)((1 - (search_starts_with.Length - word.Length)) / em.SearchWords.Length);
                    //int score = em.ProcessedSearchText.Length - word.Length;
                    int score = (em.SearchWords.Length - 1) + (search_starts_with.Length - word.Length);//em.ProcessedSearchText.Length - word.Length;
                    return Math.Max(0, score);
                }
                return -1;
            }
            return words.Where(y => em.ProcessedSearchText.Contains(y)).Count();
        }
        #endregion

        #region Commands
        #endregion
    }
}
