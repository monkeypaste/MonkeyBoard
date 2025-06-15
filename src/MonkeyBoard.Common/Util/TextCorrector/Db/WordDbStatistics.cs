namespace MonkeyBoard.Common {
    public class WordDbStatistics {
        public int WordCount { get; private set; }
        public int AverageWordLength { get; private set; }
        public int MinCommonRank { get; private set; }
        public int MaxDepth { get; private set; }
        public WordDbStatistics(int avgLen, int minRank, int maxDepth, int wordCount) {
            AverageWordLength = avgLen;
            MinCommonRank = minRank;
            MaxDepth = maxDepth;
            WordCount = wordCount;
        }
    }
}




