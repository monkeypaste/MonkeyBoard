using Avalonia;
using Avalonia.Layout;

using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public abstract class AutoCompleteViewModelBase :
        FrameViewModelBase,
        IInertiaScroll {

        #region Private Variables

        object _completionLock = new();

        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IInertiaScroll Implementation
        bool IInertiaScroll.CanScroll => true;
        #endregion

        #endregion

        #region Properties

        #region Members
        public InertiaScrollerBase Scroller { get; private set; }
        protected CancellationTokenSource ComplCts { get; private set; }
        protected CancellationTokenSource CorrectionCts { get; private set; }

        #endregion

        #region View Models
        public MenuViewModel MenuViewModel =>
            KeyboardViewModel.MenuViewModel;

        public ObservableCollection<string> CompletionItems { get; set; } = [];
        public IEnumerable<string> CompletionDisplayValues {
            get {
                if(InputConnection is not { } ic) {
                    yield break;
                }
                var ti = ic.OnTextRangeInfoRequest();
                // get caret word to detect its case (and compound prefix if enabled)

                string whole_word = TextRangeTools.GetWordAtCaret(
                    textRange: ti,
                    rangeStartIdx: out int ti_idx,
                    hasTrailingSpace: out _,
                    isAtWordTerminator: out _,
                    breakOnCompoundWords: true);
                int i = 0;
                var temp_items = CompletionItems.ToList();
                foreach(string comp_val in temp_items) {
                    if(IsCompletionAllowed) {
                        if(i == OmittableItemIdx && IsOmitLabelVisible) {
                            // blink 'Forget'
                            yield return OmitLabel;
                        } else {
                            yield return GetCompletionDisplayValue(whole_word, comp_val);
                        }
                    } else {
                        // hide completion from password fields
                        yield return PasswordAutoCorrectCompletionText;
                    }
                    i++;
                }
            }
        }
        #endregion

        #region Appearance
        public object OmitCancelIconSourceObj => "delete.png";
        public object OmitConfirmIconSourceObj => "checkround.png";
        public string CancelOmitButtonFgHexColor =>
            KeyboardPalette.P[PaletteColorType.ValidBg];
        public string ConfirmOmitButtonFgHexColor =>
            KeyboardPalette.P[PaletteColorType.InvalidBg];

        public string OmitButtonDefaultFgHexColor =>
            KeyboardPalette.P[PaletteColorType.InvalidBg];
        public virtual string AutoCompleteBgHexColor =>
            KeyboardPalette.P[PaletteColorType.MenuBg];
        public string PressedItemBgHexColor =>
            KeyboardPalette.P[PaletteColorType.MenuItemPressedBg];
        public string InvalidAutoCompletePressedBgHexColor =>
            KeyboardPalette.P[PaletteColorType.InvalidBg];
        public string FgHexColor =>
            KeyboardPalette.P[PaletteColorType.MenuFg];

        string _autoCompleteDefaultSepHexColor;
        protected string DefSeparatorHexColor {
            get {
                if(_autoCompleteDefaultSepHexColor == null) {
                    string hex_suffix = new string(KeyboardPalette.P[PaletteColorType.MenuFg].Skip(3).ToArray());
                    _autoCompleteDefaultSepHexColor = $"#{MaxSeparatorAlpha.ToString("X2", CultureInfo.InvariantCulture)}" + hex_suffix;
                }
                return _autoCompleteDefaultSepHexColor;
            }
        }
        string _autoCompleteSepHexColor;
        public string SeparatorHexColor {
            get => _autoCompleteSepHexColor ?? DefSeparatorHexColor;
            protected set => _autoCompleteSepHexColor = value;
        }
        protected byte SeparatorAlpha { get; set; } = 255;
        protected byte MaxSeparatorAlpha => 225;
        protected byte MinSeparatorAlpha => 50;
        protected string ConfirmIgnoreAutoCorrectCompletionText => "✓";
        protected string PasswordAutoCorrectCompletionText => "🔒";
        public IEnumerable<string> CompletionItemBgHexColors {
            get {
                for(int i = 0; i < CompletionItems.Count; i++) {
                    if(i == PressedCompletionItemIdx && !IsScrolling && i != OmittableItemIdx) {
                        if(IsCompletionAllowed) {
                            yield return PressedItemBgHexColor;
                        } else {
                            yield return InvalidAutoCompletePressedBgHexColor;
                        }
                    } else {
                        yield return null;
                    }
                }
            }
        }


        public string OmitLabel {
            get {
                string label = ResourceStrings.U["ConfirmOmitLabel"].value;
                //if(WorkingComparisions.ElementAtOrDefault(OmittableItemIdx) is { } omit_comp) {
                //    label += Environment.NewLine +
                //        $"{omit_comp.Uses} | {omit_comp.Rank}";
                //}
                return label;
            }
        }
        #endregion

        #region Layout
        public virtual CornerRadius ContainerCornerRadius => new();
        public virtual double FrameOffsetY { get; } = 0;
        public abstract Rect AutoCompleteRect { get; }
        public Rect CompletionItemsRect {
            get {
                double w = CompletionItems.Count * AutoCompleteItemWidth;
                double h = AutoCompleteRect.Height;
                double x = 0;
                double y = 0;
                return new Rect(x, y, w, h);
            }
        }
        public double AutoCompleteWidth =>
            AutoCompleteRect.Width;
        protected abstract int MaxVisibleCompletionItems { get; }

        public double OmitButtonWidth =>
            AutoCompleteRect.Height;
        public Rect ConfirmOmitButtonHitRect {
            get {
                if(!IsOmitButtonsVisible) {
                    return new();
                }
                // TODO this should probably show on the right for RTL languages
                double w = OmitButtonWidth;
                double h = w;
                double x = (OmittableItemIdx * AutoCompleteItemWidth) - CompletionScrollOffset + AutoCompleteItemWidth - w;
                double y = 0;
                return new Rect(x, y, w, h);
            }
        }
        public Rect CancelOmitButtonHitRect {
            get {
                if(!IsOmitButtonsVisible) {
                    return new();
                }
                // TODO this should probably show on the right for RTL languages
                double w = OmitButtonWidth;
                double h = w;
                //double x = ((OmittableItemIdx + 1) * AutoCompleteItemWidth) - CompletionScrollOffset - w;
                double x = (OmittableItemIdx * AutoCompleteItemWidth) - CompletionScrollOffset;
                double y = 0;
                return new Rect(x, y, w, h);
            }
        }

        Rect[] _maxCompletionItemRects;
        public Rect[] CompletionItemRects {
            get {
                if(_maxCompletionItemRects == null ||
                    _maxCompletionItemRects.Length != MaxCompletionItemCount) {
                    double w = AutoCompleteRect.Width / MaxVisibleCompletionItems;
                    double h = AutoCompleteRect.Height;
                    _maxCompletionItemRects = new Rect[MaxCompletionItemCount];
                    for(int i = 0; i < _maxCompletionItemRects.Length; i++) {
                        double x = i * w;
                        double y = 0;
                        _maxCompletionItemRects[i] = new Rect(x, y, w, h);
                    }
                }
                // NOTE only provide rects for available completions
                return
                    _maxCompletionItemRects
                    .Take(CompletionItems.Count)
                    .Select(x => new Rect(x.X - CompletionScrollOffset, x.Y, x.Width, x.Height))
                    .ToArray();
            }
        }
        public double AutoCompleteItemWidth =>
            AutoCompleteRect.Width / MaxVisibleCompletionItems;
        public IEnumerable<Point> CompletionItemTextLocs {
            get {
                int i = 0;
                foreach(string comp_disp_val in CompletionDisplayValues) {
                    //if (InputConnection == null || InputConnection.TextTools == null) {
                    //    yield return new();
                    //    continue;
                    //}
                    var text_rect =
                        InputConnection.TextTools
                        .MeasureText(comp_disp_val, GetCompletionTextFontSize(comp_disp_val, i), out double ascent, out double descent);
                    if(i >= CompletionItemRects.Length) {
                        yield break;
                    }
                    var comp_item_rect = CompletionItemRects[i++];
                    double cix = comp_item_rect.Center.X;
                    double ciy = comp_item_rect.Center.Y - ((ascent + descent) / 2);
                    yield return new Point(cix, ciy);
                }
            }
        }
        public HorizontalAlignment CompletionHorizontalTextAlignment =>
            HorizontalAlignment.Center;
        public VerticalAlignment CompletionVerticalTextAlignment =>
            VerticalAlignment.Center;
        public double CompletionScrollOffset =>
            Scroller.ScrollOffset.X;

        public double SeparatorHeight =>
            AutoCompleteRect.Height / KeyConstants.PHI;
        #endregion

        #region State

        protected IEnumerable<WordComparision> WorkingComparisions { get; set; } = [];
        protected int CurSearchDepth { get; set; }
        public bool HasCompletionMatch { get; protected set; }
        public bool IsBusy { get; protected set; }
        public bool IsOmitLabelVisible { get; protected set; }
        bool IsOmitButtonPressed { get; set; }
        public int OmittableItemIdx { get; private set; } = -1;
        public bool IsOmitButtonsVisible =>
            OmittableItemIdx >= 0;

        public bool IsCompletionAllowed =>
            !IsCompletionInvalid;
        bool IsCompletionInvalid =>
            false;// KeyboardViewModel.IsPassword && IsPasswordProtected;
        protected virtual bool IsPasswordProtected => true;
        protected abstract int MaxCompletionItemCount { get; }

        protected bool IsNormalTextCompletion =>
            // NOTE omitting url cause most url fields also allow for search
            !KeyboardViewModel.IsEmailLayout
            //&& !KeyboardViewModel.IsUrlLayout
            ;

        public abstract MenuItemType CompletionType { get; }
        public abstract MenuTabItemType TabType { get; }
        bool IsTouchOwner =>
            MenuViewModel.TouchOwner != default &&
            MenuViewModel.TouchOwner.ownerType == CompletionType;

        public string LastInput { get; private set; }
        public SelectableTextRange LastTextInfo { get; set; }
        public bool IsScrolling => Scroller.IsUserScrolling;

        public int PressedCompletionItemIdx { get; set; } = -1;

        protected IEnumerable<Word> CommonWords { get; set; } = [];
        protected Dictionary<char, List<(char, Rect)>> InputNeighborLookup { get; } = [];
        protected (int sidx, string oldText, string newText)? LastAutoCorrectRange { get; set; }
        protected List<string> AutoCorrectUndoneWords { get; set; } = [];
        IEnumerable<string> LettersAboveSpaceBar { get; set; } = [];
        int MinAutoCorrectLength => 2;
        int MaxAutoCorrectLength => 10;
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public AutoCompleteViewModelBase(FrameViewModelBase parent) {
            Parent = parent;
            Scroller = InertiaScrollerBase.Create(this, InputConnection);

        }
        #endregion

        #region Public Methods
        public virtual void Init() {
            ResetState();
        }
        public bool CanScroll(Touch touch) {
            return IsScrolling || Math.Abs(touch.PressLocation.X - touch.Location.X) >= InertiaScrollerBase.MIN_SCROLL_DISPLACEMENT;
        }

        public virtual void ResetState() {
            _autoCompleteSepHexColor = null;
            _autoCompleteDefaultSepHexColor = null;
            _maxCompletionItemRects = null;
            LastAutoCorrectRange = null;
            LastInput = string.Empty;
            LastTextInfo = null;
            OmittableItemIdx = -1;
            PressedCompletionItemIdx = -1;
        }

        #region Omit
        public void SetOmit(int itemIdx) {
            OmittableItemIdx = itemIdx;

            //this.Renderer.RenderFrame(true);
            if(InputConnection.MainThread is { } mt) {
                InputConnection.OnFeedback(KeyboardViewModel.FeedbackInvalid);
                mt.Post(async () => {
                    if(WorkingComparisions.ElementAtOrDefault(OmittableItemIdx) is { } omit_comp) {
                        // show summary in footer
                        KeyboardViewModel.FooterViewModel.SetLabelText(omit_comp.Summary);
                    }
                    // do blink blink
                    int cur_idx = itemIdx;
                    DateTime? dt = null;
                    while(true) {
                        if(OmittableItemIdx != cur_idx) {
                            KeyboardViewModel.FooterViewModel.SetLabelText(string.Empty);
                            return;
                        }
                        if(dt == null || DateTime.Now - dt >= TimeSpan.FromMilliseconds(1_000)) {
                            dt = DateTime.Now;
                            IsOmitLabelVisible = !IsOmitLabelVisible;
                            this.Renderer.RenderFrame(true);
                        }
                        await Task.Delay(25);
                    }
                });
            }

        }
        public void ClearOmit() {
            OmittableItemIdx = -1;
            this.Renderer.RenderFrame(true);
        }

        async Task DoOmitAsync() {
            if(OmittableItemIdx < 0 ||
                OmittableItemIdx >= CompletionItems.Count) {
                return;
            }
            int itemIdx = OmittableItemIdx;
            bool is_emoji = false;
            if(WorkingComparisions.ElementAtOrDefault(OmittableItemIdx) is { } omit_comp &&
                omit_comp is EmojiComparision) {
                is_emoji = true;
            }
            string item_text = CompletionItems[itemIdx];
            CompletionItems.Remove(item_text);
            PressedCompletionItemIdx = -1;
            MpConsole.WriteLine($"Omitting '{item_text}'");
            ClearOmit();

            if(is_emoji &&
                this is not EmojiAutoCompleteViewModel &&
                KeyboardViewModel.MenuViewModel.EmojiPagesViewModel.EmojiSearchViewModel.EmojiAutoCompleteViewModel is { } eacvm) {
                // omit emoji from text completion
                await eacvm.OmitItemAsync(item_text);
            } else {
                await OmitItemAsync(item_text);
            }


            ShowCompletion(InputConnection.OnTextRangeInfoRequest(), true);
        }
        #endregion

        bool EnsureCanShowCompletion(string input) {
            if(string.IsNullOrEmpty(input) && !KeyboardViewModel.IsNextWordCompletionEnabled) {
                // don't do beginning of word
                MenuViewModel.SetMenuPage(MenuPageType.TabSelector);
                return false;
            }

            if(MenuViewModel.EmojiPagesViewModel.EmojiSearchViewModel.IsVisible) {
                MenuViewModel.SetMenuPage(MenuPageType.TextCompletions, MenuTabItemType.Emoji);
            } else {
                MenuViewModel.SetMenuPage(MenuPageType.TextCompletions, TabType);
            }
            return true;
        }
        public void ShowCompletion(SelectableTextRange textInfo, bool allowDup) {
            try {
                // NOTE lock ensures completion count changes between insert changes
                // don't throw off the renderers references
                if(!IsVisible) {
                    return;
                }
                if(textInfo is null && InputConnection.OnTextRangeInfoRequest() is { } newest_info) {
                    textInfo = newest_info;
                }
                if((textInfo.IsValueEqual(LastTextInfo) && !allowDup) ||
                    GetCompletionInput(textInfo, out bool isAtWordTerminator) is not { } input) {
                    return;
                }
                MpConsole.WriteLine($"Text to complete: '{input}' AtWordTerminator: {isAtWordTerminator} ", level: MpLogLevel.Verbose);

                if(!EnsureCanShowCompletion(input)) {
                    return;
                }

                bool? is_insert_change = null;
                bool? is_del_change = null;
                bool? is_sel_change = null;
                if(KeyboardViewModel.CanAutoCorrect) {
                    is_insert_change = TextRangeTools.WasLastChangeInsert(textInfo, LastTextInfo);
                    is_del_change = TextRangeTools.WasBackspace(textInfo, LastTextInfo);
                    is_sel_change = !is_insert_change.Value && !is_del_change.Value;
                    if(!is_insert_change.Value) {
                        // avoid autocorrect when user not continuing to type
                        // the working ranges could become wrong (w/o full textPointer tracking)
                        CancelPriorCorrection();
                    }
                    if(!is_sel_change.Value &&
                        LastAutoCorrectRange is { } lacr &&
                        textInfo.SelectionEndIdx < lacr.sidx) {
                        // when change is BEFORE lacr it will corrupt the range, clear it
                        LastAutoCorrectRange = null;
                    }
                }

                if(input != LastInput || allowDup) {
                    Scroller.ScrollToHome();

                    if(KeyboardViewModel.CanAutoCorrect) {
                        // NOTE since auto correct occurs AFTER an insert change
                        // where insert was word terminator, auto correct should be
                        // ignored if current info is 1 insert AFTER lacr
                        bool was_in_lacr_range = is_insert_change.Value &&
                            LastAutoCorrectRange is { } lacr &&
                                textInfo.SelectionLength == 0 &&
                                textInfo.SelectionStartIdx >= lacr.sidx &&
                                textInfo.SelectionStartIdx <= lacr.sidx + lacr.newText.Length;
                        if(isAtWordTerminator &&
                            !HasCompletionMatch &&
                            !IsOmitButtonsVisible &&
                            LastInput.Length >= MinAutoCorrectLength &&
                            LastInput.Length <= MaxAutoCorrectLength &&
                            !string.IsNullOrWhiteSpace(LastInput) &&
                            is_insert_change.Value &&
                            !was_in_lacr_range &&
                            !AutoCorrectUndoneWords.Contains(LastInput.ToLower().Trim())) {
                            // this implies that LastInput is a newly typed word

                            CancelPriorCorrection();
                            DoAutoCorrect(textInfo, LastInput, CorrectionCts.Token);
                        } else if(KeyboardViewModel.IsBackspaceUndoLastAutoCorrectEnabled &&
                                    !isAtWordTerminator &&
                                    //!is_sel_change.Value &&
                                    is_del_change.Value &&
                                    LastAutoCorrectRange is { } lacr2) {
                            // NOTE this checks if insert is WITHIN lacr, only undo ac 
                            bool in_lacr_range = textInfo.SelectionLength == 0 &&
                                    textInfo.SelectionStartIdx >= lacr2.sidx &&
                                    textInfo.SelectionStartIdx < lacr2.sidx + lacr2.newText.Length;
                            if(in_lacr_range) {
                                // undo auto correct
                                // this will be false when check mark clicked
                                bool can_undo_auto_correct = lacr2.oldText.Trim().ToLower() != lacr2.newText.Trim().ToLower();
                                if(can_undo_auto_correct) {
                                    DoCompletion(textInfo, lacr2.oldText, false);
                                    if(!AutoCorrectUndoneWords.Contains(lacr2.oldText.ToLower().Trim())) {
                                        AutoCorrectUndoneWords.Add(lacr2.oldText.ToLower().Trim());
                                    }
                                    LastAutoCorrectRange = null;
                                }

                            }
                        }
                    }
                    CancelPriorCompletions();
                    ClearCompletions();
                    StartCompletionRequest(input, MaxCompletionItemCount, ComplCts.Token);
                }
                if(isAtWordTerminator) {
                    KeyboardViewModel.InputReleaseHistory.Clear();
                }
                LastInput = input;
                LastTextInfo = textInfo.Clone();
                //MpConsole.WriteLine($"Completion Count: {CompletionItems.Count}");
            }
            catch(Exception ex) {
                ex.Dump();
            }
        }

        void HandleAutoCorrect(SelectableTextRange textInfo, string input) {
            if(!KeyboardViewModel.CanAutoCorrect) {
                return;
            }

        }

        public int? GetItemIdxUnderPoint(Point loc) {
            var text_comp_loc = TranslateContainerPoint(loc);
            if(this is EmojiAutoCompleteViewModel) {
                MpConsole.WriteLine($"EC actual touch: {loc} translated touch: {text_comp_loc} rects: {(string.Join("|", CompletionItemRects))}");
            }
            for(int i = 0; i < CompletionItemRects.Length; i++) {
                if(CompletionItemRects[i].Contains(text_comp_loc)) {
                    return i;
                }
            }
            return null;
        }
        public bool IsAnyOmitButtonUnderPoint(Point loc) {
            return IsCancelOmitButtonUnderPoint(loc) || IsConfirmOmitButtonUnderPoint(loc);
        }
        public bool IsCancelOmitButtonUnderPoint(Point loc) {
            if(!IsOmitButtonsVisible) {
                return false;
            }
            var comp_loc = TranslateContainerPoint(loc);
            bool htt = CancelOmitButtonHitRect.Contains(comp_loc);
            return htt;
        }
        public bool IsConfirmOmitButtonUnderPoint(Point loc) {
            if(!IsOmitButtonsVisible) {
                return false;
            }
            var comp_loc = TranslateContainerPoint(loc);
            bool htt = ConfirmOmitButtonHitRect.Contains(comp_loc);
            return htt;
        }
        public void ClearCompletions() {
            ClearOmit();
            bool needs_render = CompletionItems.Any();
            CompletionItems.Clear();
            if(needs_render) {
                this.Renderer.RenderFrame(true);
            }
        }

        public bool CanPerformCompletionAction(Touch touch) {
            if(IsScrolling || !IsCompletionAllowed) {
                return false;
            }
            return true;
        }

        public void HandleCompletion(int itemIdx, Touch touch) {
            if(IsOmitButtonsVisible) {
                if(GetItemIdxUnderPoint(touch.Location) is { } htt_idx &&
                    htt_idx == itemIdx &&
                    htt_idx == OmittableItemIdx) {
                    // tap over omit item (not necessarily omit btn)
                    if(IsConfirmOmitButtonUnderPoint(touch.Location) &&
                        InputConnection.MainThread is { } mt) {
                        // perform omit
                        mt.Post(async () => {
                            await DoOmitAsync();
                            InputConnection.OnFeedback(KeyboardViewModel.FeedbackDelete);
                        });

                        return;
                    }
                }
                // cancel omit
                ClearOmit();
                InputConnection.OnFeedback(KeyboardViewModel.FeedbackClick);
                return;
            }

            if(InputConnection.OnTextRangeInfoRequest() is not { } ti) {
                return;
            }
            DoCompletion(ti, CompletionDisplayValues.ElementAt(itemIdx));
        }
        public void SetPressed(Touch touch, int itemIdx, bool isPressed) {
            if(IsOmitButtonsVisible && IsAnyOmitButtonUnderPoint(touch.Location)) {
                IsOmitButtonPressed = isPressed;
            } else {
                PressedCompletionItemIdx = isPressed ? itemIdx : -1;
            }

        }
        #endregion

        #region Protected Methods

        protected void SetCompletionItems(IEnumerable<string> newItems) {
            CompletionItems.Clear();
            CompletionItems.AddRange(newItems);

            double max_scroll_x = 0;
            if(CompletionItems.Count > MaxVisibleCompletionItems) {
                int last_idx = CompletionItems.Count - MaxVisibleCompletionItems;
                if(_maxCompletionItemRects == null ||
                    last_idx >= _maxCompletionItemRects.Length) {
                    // repopulate rects
                    _ = CompletionItemRects;
                }
                max_scroll_x = _maxCompletionItemRects[last_idx].Left;
            }
            if(this is EmojiAutoCompleteViewModel) {
                max_scroll_x += KeyboardViewModel.MenuViewModel.EmojiPagesViewModel.EmojiSearchViewModel.CloseButtonRect.Width;
            }
            Scroller.SetExtent(0, 0, max_scroll_x, 0);
            Scroller.SetViewport(AutoCompleteRect);

            if(InputConnection is { } ic &&
                ic.MainThread is { } mt) {
                mt.Post(() => {

                    this.Renderer.RenderFrame(true);
                });
            }

        }

        protected void StartBusyAnimation() {
            if(IsBusy) {
                return;
            }
            IsBusy = true;
            Task.Run(async () => {
                DateTime dt = DateTime.Now;
                double alpha = (double)MaxSeparatorAlpha;
                string hex_suffix = new string(KeyboardPalette.P[PaletteColorType.MenuFg].Skip(3).ToArray());

                while(true) {
                    if(!IsBusy) {
                        break;
                    }
                    double end_alpha = alpha == MaxSeparatorAlpha ? MinSeparatorAlpha : MaxSeparatorAlpha;

                    await alpha.AnimateDoubleAsync(
                        end: end_alpha,
                        tts: 1.5 / (CurSearchDepth + 1),
                        fps: 60d,
                        (new_alpha) => {
                            SeparatorHexColor = $"#{((int)new_alpha).ToString("X2", CultureInfo.InvariantCulture)}{hex_suffix}";
                            InputConnection.MainThread.Post(() => this.Renderer.PaintFrame(true));
                            return !IsBusy;
                        });
                    alpha = end_alpha;
                }
            });
        }
        protected void StopBusyAnimation() {
            IsBusy = false;
            SeparatorHexColor = DefSeparatorHexColor;
            this.Renderer.PaintFrame(true);
        }
        protected abstract Task OmitItemAsync(string item_text);
        protected abstract string GetCompletionInput(SelectableTextRange textInfo, out bool isAtWordTerminator);
        protected abstract Point TranslateContainerPoint(Point loc);
        protected abstract void StartCompletionRequest(string input, int max, CancellationToken ct);
        #endregion

        #region Private Methods
        void CancelPriorCompletions() {
            if(ComplCts != null) {
                ComplCts.Cancel();
                ComplCts.Dispose();
            }
            ComplCts = new CancellationTokenSource();
        }
        void CancelPriorCorrection() {
            if(CorrectionCts != null) {
                CorrectionCts.Cancel();
                CorrectionCts.Dispose();
            }
            CorrectionCts = new CancellationTokenSource();
        }

        #region Completion
        string GetCompletionDisplayValue(string leading_word, string comp_val) {
            string out_val = comp_val;

            if(KeyboardViewModel.IsShiftOnLock || (leading_word.Length > 1 && leading_word.IsAllCaps())) {
                return out_val.ToUpper();
            } else if(KeyboardViewModel.IsShiftOnTemp || leading_word.StartsWithCapitalCaseChar()) {
                return out_val.ToTitleCase();
            }

            return out_val;
        }
        public abstract void DoCompletion(SelectableTextRange textInfo, string completionText, bool isAutoCorrect = false);

        public abstract double GetCompletionTextFontSize(string text, int itemIdx);
        public abstract double GetCompletionTextFontSize(string text, int itemIdx, out string formatted_text);

        #endregion

        #region Auto-Correct
        void FinishAutoCorrect(SelectableTextRange acInfo, string resp_text, string leading_word) {
            if(resp_text == null || leading_word == null) {
                return;
            }
            var auto_correct_info = acInfo.Clone();
            auto_correct_info.Select(auto_correct_info.SelectionStartIdx - 1, 0);
            string compl_text = GetCompletionDisplayValue(leading_word, resp_text);
            DoCompletion(auto_correct_info, compl_text, isAutoCorrect: true);
        }

        void DoAutoCorrect(SelectableTextRange textInfo, string leading_word, CancellationToken ct) {
            // only do autocorrect if:
            // 1. There was no exact starts with match
            // 2. The current text info change was not on an existing auto correct component
            // 3. There is an available word_matches
            // 4. The available word_matches is only 1 character off from actual 
            // * probably need to play with heuristic from #3&4 based on text length and/or knowing a common rank (something from initial analysis) or use count

            InputConnection.MainThread.Post(async () => {
                string lc_leading_word = leading_word.ToLower();

                string history_str = string.Join(string.Empty, KeyboardViewModel.InputReleaseHistory.Select(x => x.Item1.ToLower()));
                bool is_history_valid = history_str == lc_leading_word;
                var sb = new StringBuilder(lc_leading_word);

                // possible_words is list of neighbor substitutions per character along with the distance
                // the touch release was from that neighbor
                var possible_words = new List<(string, double)>();
                for(int i = 0; i < lc_leading_word.Length; i++) {
                    char cur_char = lc_leading_word[i];
                    if(!InputNeighborLookup.TryGetValue(cur_char, out var neighbor_chars)) {
                        continue;
                    }
                    Point? touch_loc = null;
                    if(is_history_valid && i < KeyboardViewModel.InputReleaseHistory.Count &&
                        !string.IsNullOrEmpty(KeyboardViewModel.InputReleaseHistory[i].Item1)) {
                        touch_loc = KeyboardViewModel.InputReleaseHistory[i].Item2;
                    }
                    foreach(var neighbor_char in neighbor_chars) {
                        sb.Remove(i, 1);
                        sb.Insert(i, neighbor_char.Item1);
                        double dist = touch_loc == null ? double.MaxValue : neighbor_char.Item2.Distance(touch_loc.Value);
                        possible_words.Add((sb.ToString(), dist));
                    }
                    sb.Remove(i, 1);
                    sb.Insert(i, cur_char);
                }
                // BUG when typing quickly HasComparisonMatch is either reset or not set in time when AutoCorrect
                // gets triggered. It should probably be FIXED instead of doing this but its more important to see if
                // this works so the input is added to the check with 0 distance, if its a word it'll win so no need to auto correct
                possible_words.Add((lc_leading_word, 0));

                var word_matches = await WordDb.GetBestWordFromListAsync(possible_words.Select(x => x.Item1).ToArray(), ct);
                if(ct.IsCancellationRequested ||
                    word_matches.IsEmpty()) {
                    // canceled, non-insert cursor change occured
                    return;
                }
                var best_match_text = possible_words
                    .Where(x => word_matches.Contains(x.Item1))
                    .OrderBy(x => x.Item2)
                    .Select(x => x.Item1)
                    .FirstOrDefault();

                if(best_match_text == default || best_match_text == lc_leading_word) {
                    // BUG (above) case
                    return;
                }

                FinishAutoCorrect(textInfo, best_match_text, leading_word);
            });
        }
        bool CheckForMisspelledWord(SelectableTextRange textInfo, string leading_word) {
            string lc_input = leading_word.ToLower();
            if(CommonWords
                .Select(x => (x, WordDb.LevenshteinDamerauDist(lc_input, x.WordText.ToLower())))
                .OrderBy(x => x.Item2)
                .FirstOrDefault() is { } best_kvp &&
                IsWorthyAutoCorrect(best_kvp.x, best_kvp.Item2, lc_input)) {
                FinishAutoCorrect(textInfo, best_kvp.x.WordText, leading_word);
                return true;
            }
            return false;
        }
        bool CheckForApostrophe(SelectableTextRange textInfo, string leading_word) {
            // NOTE not implemented on non 'en' cultures
            if(!CultureManager.CurrentKbCulture.ToLower().StartsWith("en") ||
                leading_word.Contains("'") ||
                leading_word.Length < 2) {
                return false;
            }
            string lc_input = leading_word.ToLower();
            char[] apos_suffixs = ['s', 't'];
            foreach(char apos_suffix in apos_suffixs) {
                if(lc_input.EndsWith(apos_suffix)) {
                    string lead_w_apos = leading_word.Insert(leading_word.Length - 2, apos_suffix.ToString());
                    if(CommonWords.Any(x => x.WordText == lead_w_apos.ToLower())) {
                        FinishAutoCorrect(textInfo, lead_w_apos, leading_word);
                        return true;
                    }
                }
            }
            return false;
        }
        bool CheckForTwoWords(SelectableTextRange textInfo, string leading_word) {
            if(!LettersAboveSpaceBar.Any() && GetKeysAboveSpaceBar().Select(x => x.PrimaryValue.ToLower()) is { } ks) {
                LettersAboveSpaceBar = ks;
            }
            if(!LettersAboveSpaceBar.Any() ||
                leading_word.Length < 3) {
                return false;
            }
            string lc_input = leading_word.ToLower();
            var lasb = LettersAboveSpaceBar.ToList();
            string check_str = lc_input.Substring(1, leading_word.Length - 2);
            foreach(string match_key in LettersAboveSpaceBar.Where(x => check_str.Contains(x))) {
                int match_idx = check_str.IndexOf(match_key);
                string pre = leading_word.Substring(0, match_idx + 1);
                string post = leading_word.Substring(pre.Length + 1, leading_word.Length - pre.Length - 1);
                if(CommonWords.Any(x => x.WordText == pre.ToLower()) && CommonWords.Any(x => x.WordText == post.ToLower())) {
                    FinishAutoCorrect(textInfo, $"{pre} {post}", leading_word);
                    return true;
                }
            }
            return false;
        }

        protected IEnumerable<KeyViewModel> GetKeysAboveSpaceBar() {
            return KeyboardViewModel.Keys.Where(x => x.Row == KeyboardViewModel.SpacebarKey.Row - 1 && x.IsInput);
        }
        bool IsWorthyAutoCorrect(Word best_wc, double dist, string leading_word) {
            int max_ed = Math.Max(1, (int)Math.Floor(leading_word.Length / 3d));
            return dist <= max_ed;

            ////if (dist <= 1) {
            ////    return true;
            ////}
            //// TODO maybe add use counts here
            //double reasonably_long_word_len = 4;// WordDb.Stats.AverageWordLength - (int)Math.Floor(WordDb.Stats.AverageWordLength / 3d);
            //int max_reasonable_dist = (int)Math.Ceiling(leading_word.Length / reasonably_long_word_len);
            //max_reasonable_dist = 1;
            ////if (dist <= 2 && best_wc.WordText.Length >= reasonably_long_word_len) {
            ////    return true;
            ////}
            ////return false;
            //return dist <= max_reasonable_dist;
        }
        //WordComparision FindBestMatch(int curDepth, IEnumerable<WordComparision> comparisions, string text_to_correct) {
        //    var results = comparisions.Select(x => (x, GetAutoCorrectScore(curDepth, x, text_to_correct))).OrderByDescending(x => x.Item2);

        //    results.ForEach(x => MpConsole.WriteLine($"'{x.x.WordText}' => {x.Item2}"));
        //    if(results.FirstOrDefault() is { } best_kvp &&
        //        best_kvp.Item2 > 0.7d) {
        //        return best_kvp.x;
        //    }
        //    return null;
        //    //return 
        //    //    comparisions
        //    //    .Where(x => GetAutoCorrectScore(curDepth, x, text_to_correct))
        //    //    .OrderByDescending(x => x.Uses)
        //    //    .ThenByDescending(x => x.Rank)
        //    //    .FirstOrDefault();
        //}
        //double GetAutoCorrectScore(int curDepth, WordComparision best_wc, string text_to_correct) {
        //    // this is just a number for avg number of characters could type per single mistake (+1 edit distance)
        //    //int reasonably_long_word_len = 4;//WordDb.Stats.AverageWordLength - (int)Math.Floor(WordDb.Stats.AverageWordLength / 3d);

        //    double rank_weight = best_wc.Rank / WordDb.Stats.MinCommonRank;
        //    double use_weight = best_wc.Uses > 0 ? 1:0;
        //    double ed_weight = 1/best_wc.EditDistance;// / (text_to_correct.Length / reasonably_long_word_len);
        //    double depth_weight = curDepth / WordDb.Stats.MaxDepth;

        //    //if (best_wc.EditDistance <= 1) {
        //    //    return true;
        //    //}
        //    //// TODO maybe add use counts here
        //    //if (best_wc.EditDistance <= 2 && text_to_correct.Length >= reasonably_long_word_len) {
        //    //    return true;
        //    //}
        //    //return false;
        //    return (rank_weight + use_weight + ed_weight + depth_weight) / 4d;
        //}

        #endregion

        #endregion
    }
}
