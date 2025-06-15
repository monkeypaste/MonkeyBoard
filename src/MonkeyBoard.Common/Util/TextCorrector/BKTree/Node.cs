using System.Diagnostics.Contracts;

namespace MonkeyBoard.Common {
    public class Node {
        // stores the word of the current Node
        public WordEntry Entry { get; set; } = new();
        //public string Word { get; set; }
        public string Word =>
            Entry == null ? string.Empty : Entry.Word;

        // links to other Node in the tree
        public int[] Next { get; set; }

        public Node(int rank, string word) {
            Entry = new WordEntry(rank, word);
            //Word = x;
            // initializing next[i] = 0
            Next = new int[2 * BKTree.MAX_WORD_LEN];
            for (int i = 0; i < 2 * BKTree.MAX_WORD_LEN; i++)
                Next[i] = 0;
        }

        public Node(int rank) {
            Entry = new WordEntry(rank);
        }
        public Node() { }
    }
}




