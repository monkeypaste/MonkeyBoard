using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MonkeyBoard.Common {
    public class TextRangeChange {
        public int StartIdx { get; private set; }
        public int NewEndIdx =>
            StartIdx + NewText.Length;
        public string OldText { get; private set; }
        public string NewText { get; private set; }
        public TextRangeChange(int sidx, string oldText, string newText) {
            StartIdx = sidx;
            OldText = oldText;
            NewText = newText;
        }

    }
    public class SelectableTextRange {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties
        public int SelectionStartIdx { get; private set; }
        public int SelectionLength { get; private set; }
        public int SelectionEndIdx =>
            SelectionStartIdx + SelectionLength;

        public string Text { get; private set; } = string.Empty;
        public string LeadingText =>
            SelectionStartIdx < Text.Length ?
                Text.Substring(0, SelectionStartIdx) :
                Text;
        public string TrailingText =>
            SelectionEndIdx < Text.Length ?
                Text.Substring(SelectionEndIdx, Math.Max(0, Text.Length - SelectionEndIdx)) :
                string.Empty;
        public string SelectedText {
            get => SelectionStartIdx < Text.Length ? Text.Substring(SelectionStartIdx, Math.Min(SelectionLength, Text.Length-SelectionStartIdx)) : string.Empty;
            set {
                string new_sel_text = value ?? string.Empty;
                string new_text = LeadingText + new_sel_text + TrailingText;
                Text = new_text;
                SelectionLength = new_sel_text.Length;
            }
        }

        #endregion

        #region Events
        #endregion

        #region Constructors
        public SelectableTextRange() : this(string.Empty, 0, 0) { }

        public SelectableTextRange(string text, int sidx, int len) {
            Text = text ?? string.Empty;
            SelectionStartIdx = sidx;
            SelectionLength = len;
        }
        #endregion

        #region Public Methods

        public void Select(int sidx, int len) {
            int last_start = SelectionStartIdx;
            SelectionStartIdx = Math.Clamp(sidx, 0, Math.Max(0, Text.Length));
            //ActualSelectionStartIdx = Math.Max(0, ActualSelectionStartIdx - SelectionStartIdx - last_start);
            SelectionLength = Math.Clamp(len, 0, Math.Max(0, Text.Length - SelectionStartIdx));
            //MpConsole.WriteLine($"Actual: {ActualSelectionStartIdx} Virtual: {SelectionStartIdx}");
        }

        public void SetText(string text, int sidx = 0, int len = 0) {
            Text = text ?? string.Empty;
            Select(sidx, len);
        }
        public bool IsValueEqual(SelectableTextRange otherRange) {
            if (otherRange == null) {
                return false;
            }
            return
                SelectionStartIdx == otherRange.SelectionStartIdx &&
                SelectionEndIdx == otherRange.SelectionEndIdx &&
                Text == otherRange.Text;
        }
        public override string ToString() {
            return $"[{SelectionStartIdx},{SelectionLength}]'{Text}'";
        }

        public SelectableTextRange Clone() {
            return new SelectableTextRange(Text, SelectionStartIdx, SelectionLength);
        }        
        
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}
