using FuzzySharp;
using MonkeyPaste.Common;
using SQLite;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public static class WordDb {
        #region Private Variables
        static IKeyboardInputConnection InputConnection { get; set; }
        #endregion

        #region Constants

        const long MAX_DEFAULT_RANK = 23135851162;
        const int DEFAULT_WORD_COUNT = 20;

        #endregion

        #region Statics
        static bool LOG_QUERIES { get; set; } = false;
        static bool IS_DB_ENABLED => true;
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        static bool CanAsyncQuery =>
            AsyncConn != null && IS_DB_ENABLED;
        static bool CanSyncQuery =>
            SyncConn != null && IS_DB_ENABLED;
        static WordRankType RankType { get; set; } = WordRankType.RowId;
        static string DbPath { get; set; }
        static SQLiteAsyncConnection AsyncConn { get; set; }
        static SQLiteConnection SyncConn { get; set; }
        static bool IsLoading { get; set; }
        public static bool IsLoaded { get; set; }
        public static WordDbStatistics Stats { get; private set; } = new(0, 0, 0, 0);
        static int MIN_COUNT_DIFF_FOR_REANALYSIS => 5_000;
        static int InitCount { get; set; }
        static string LastWordRankedCulture { get; set; }
        static TimeSpan TARGET_COMPL_SPAN => TimeSpan.FromSeconds(0.1);
        static int BaseMinRank =>
            Ranks.FirstOrDefault();

        static int[] Ranks { get; set; } = [];
        static int MaxInputLen { get; set; } = 15;

        static IEnumerable<string> DefaultWords { get; set; } = [];
        #endregion

        #region Events
        #endregion

        #region Constructors
        #endregion

        #region Life Cycle
        public static async Task LoadDbAsync(IKeyboardInputConnection ic, bool isReset) {
            var sw = Stopwatch.StartNew();
            if(!CultureManager.IsLoaded) {
                CultureManager.Init(ic);
            }
            if(IsLoading || (IsLoaded && LastWordRankedCulture == CultureManager.CurrentKbCulture)) {
                return;
            }
            IsLoading = true;
            MpConsole.WriteLine("kb Db loading..");
            InputConnection = ic;

            if(!IS_DB_ENABLED) {
                IsLoading = false;
                IsLoaded = true;
                return;
            }

            Batteries_V2.Init();

            string db_name = $"words_{CultureManager.CurrentKbCulture}.db";
            string db_pass = "test";
            DbPath = Path.Combine(CultureManager.CurrentKbCultureDir, db_name);

            try {
                var conn_str = new SQLiteConnectionString(
                    databasePath: DbPath,
                    key: db_pass,
                    storeDateTimeAsTicks: true,
                    openFlags: SQLiteOpenFlags.ReadWrite |
                                SQLiteOpenFlags.Create |
                                //SQLiteOpenFlags.SharedCache |
                                //SQLiteOpenFlags.Memory |
                                SQLiteOpenFlags.FullMutex
                                );
                AsyncConn = new SQLiteAsyncConnection(conn_str);
                SyncConn = new SQLiteConnection(conn_str);
            }
            catch(Exception ex) {
                ex.Dump();
                await FinishInitAsync(sw);
                return;
            }


            if(CultureManager.CurrentKbCulture.StartsWith("en")) {
                //raw.sqlite3_create_function(AsyncConn.GetConnection().Handle, "EDIT_DIST", 2, null, FuzzRatio);
                raw.sqlite3_create_function(AsyncConn.GetConnection().Handle, "EDIT_DIST", 2, null, EditDistanceSqlFunc);
            } else {
                //NOTE fuzz is only made for english so fallback to dist otherwise
                //from https://github.com/JakeBayer/FuzzySharp#fuzzysharp-in-different-languages
                raw.sqlite3_create_function(AsyncConn.GetConnection().Handle, "EDIT_DIST", 2, null, EditDistanceSqlFunc);
            }
            await AsyncConn.CreateTableAsync<Word>();

            InitCount = await GetTotalWordCountAsync();
            DefaultWords = await GetDefaultsWordsAsync(DEFAULT_WORD_COUNT);

            if(isReset) {
                // o
                await FinishInitAsync(sw);
            } else {
                // load fast don't wait
                FinishInitAsync(sw).FireAndForgetSafeAsync();
            }
        }

        public static async Task ResetDbAsync_query(IKeyboardInputConnection ic) {
            if(!CanAsyncQuery) {
                return;
            }
            // remove user words
            await AsyncConn.ExecuteAsync($"DELETE FROM Word WHERE UserWord=1");

            // reset corpus
            await AsyncConn.ExecuteAsync($"UPDATE Word SET Uses=0, Omitted=0");

            await LoadDbAsync(ic, true);
        }
        public static async Task ResetDbAsync_file(IKeyboardInputConnection ic) {
            if(!CanAsyncQuery) {
                return;
            }
            await UnloadAsync();

            //delete all db files
            DeleteDb();

            KbAssetMover.MoveAssets(true);

            await LoadDbAsync(ic, true);
        }
        #endregion

        #region Queries

        #region Runtime

        public static async Task<int> RemoveDupsAsync() {
            if(!CanAsyncQuery) {
                return 0;
            }
            string find_query = @"
SELECT * FROM Word WHERE LOWER(WordText) IN(
SELECT wt FROM (
SELECT LOWER(WordText) wt, COUNT(*) c FROM Word GROUP BY LOWER(WordText) HAVING c > 1
)
)
";
            var results = await AsyncConn.QueryAsync<Word>(find_query);
            var to_remove = results.GroupBy(x => x.WordText.ToLower());
            List<int> ids_to_remove = [];
            foreach(var rmv_group in to_remove) {
                ids_to_remove.AddRange(rmv_group.OrderByDescending(x => x.Rank).Skip(1).Select(x => x.Id));
            }

            await AsyncConn.ExecuteAsync($"DELETE FROM Word WHERE Id IN ({string.Join(",", ids_to_remove)})");

            return ids_to_remove.Count;
        }
        #endregion

        #region Omit

        public static async Task OmitWordAsync(string word) {
            if(!CanAsyncQuery) {
                return;
            }
            await AsyncConn.ExecuteAsync($"UPDATE Word SET Omitted=1 WHERE LOWER(WordText)=?", word.ToLower());
        }
        public static async Task<int> GetOmittedWordCountAsync() {
            if(!CanAsyncQuery) {
                return 0;
            }
            var result = await AsyncConn.QueryScalarsAsync<int>($"SELECT COUNT(*) FROM Word WHERE Omitted=1");
            if(result.FirstOrDefault() is int count) {
                return count;
            }
            return 0;
        }
        #endregion

        #region Words (Scalar)

        public static bool IsBeginningOfWord(string input) {
            if(!CanSyncQuery) {
                return false;
            }
            string mv = input.ToLower();
            string lv = $"{mv}%";
            string query = $@"
SELECT 
    Id
FROM Word 
WHERE LOWER(WordText) LIKE ? And Omitted=0
LIMIT 1";

            //MpConsole.WriteLine($"StartsWith query: ");
            //MpConsole.WriteLine(query.GetParameterizedQueryString(new object[] { mv, lv, maxResults }));
            var result = SyncConn.QueryScalars<int>(query, lv);
            return result.Any();
        }

        public static IEnumerable<string> TakeSomeDefaultWords(int maxResults) {
            if(DefaultWords == null || !DefaultWords.Any()) {
                return [];
            }
            DefaultWords = DefaultWords.Randomize();
            return DefaultWords.Take(maxResults);
        }
        static async Task<IEnumerable<string>> GetDefaultsWordsAsync(int maxResults) {
            if(!CanAsyncQuery) {
                return [];
            }
            var result = await AsyncConn.QueryScalarsAsync<string>($"SELECT WordText FROM Word WHERE Omitted=0 ORDER BY Rank DESC LIMIT ?", maxResults);
            result = result.Randomize().Take(maxResults).ToList();
            return result;
        }

        #endregion

        #region Words (Query)
        public static async Task<IEnumerable<Word>> GetMostCommonWordsAsync(int maxResults) {
            if(!CanAsyncQuery) {
                return [];
            }
            string query = $"SELECT * FROM Word WHERE Omitted=0 ORDER BY Rank DESC LIMIT ?";
            var result = await AsyncConn.QueryAsync<Word>(query, maxResults);
            return result;
        }
        public static async Task<Word> GetWordAsync(string text) {
            if(!CanAsyncQuery) {
                return null;
            }
            string query =
@"SELECT * FROM Word WHERE LOWER(WordText)=? LIMIT 1";
            var result = await AsyncConn.QueryAsync<Word>(query, text.ToLower());
            return result.FirstOrDefault();
        }
        public static async Task<List<string>> GetBestWordFromListAsync(string[] words, CancellationToken ct) {
            if(!CanAsyncQuery) {
                return [];
            }
            // NOTE presumes words is lower case
            List<string> result = null;
            string words_mask = string.Join(",", Enumerable.Repeat("?", words.Length));
            string query = @$"
SELECT LOWER(WordText) FROM Word 
WHERE Omitted=0 AND LOWER(WordText) IN ({words_mask})";
            result = await AsyncConn.CancellableQueryScalarsAsync<string>(query, ct, words.ToArray());
            if(LOG_QUERIES) {
                MpConsole.WriteLine($"Best word query:");
                MpConsole.WriteLine(query.GetParameterizedQueryString(words));
            }
            return result;
        }
        public static async Task<List<WordComparision>> GetWordsByRankLevelAsync(string input, int maxResults, int level, CancellationToken ct) {
            if(!CanAsyncQuery) {
                return [];
            }
            // NOTE returns NULL when canceled or no more possible results
            while(!IsLoaded) {
                if(ct.IsCancellationRequested) {
                    return null;
                }
                //MpConsole.WriteLine($"Waiting for TextCorrector to load...");
                await Task.Delay(50);
            }
            if(level == 0) {
                var starts_with_results = await GetStartsWithWordsAsync(input, maxResults, ct);
                return starts_with_results;
            }
            level = level - 1;
            if(level >= Ranks.Length) {
                // too high
                return null;
            }
            int min_rank = Ranks[level];
            int max_rank = level == 0 ? int.MaxValue : Ranks[level - 1] - 1;
            if(max_rank < 0) {
                // level too high
                return null;
            }
            var result = await GetSimilarWordsInRankRangeAsync(input, maxResults, min_rank, max_rank, ct);
            return result;
        }

        static async Task<List<WordComparision>> GetSimilarWordsInRankRangeAsync(string input, int maxResults, int minRank, int maxRank, CancellationToken ct) {
            string rank_clause =
                RankType == WordRankType.Frequency ?
                    $"WHERE Rank >= ? And Rank <= ?" :
                    $"WHERE rowid >= ? And rowid <= ?";

            string query = @$"
SELECT 
    *,
    EDIT_DIST(LOWER(SUBSTR(WordText,0,?)), ?) AS EditDistance
FROM Word
{rank_clause} And LOWER(WordText)!=? AND LENGTH(WordText)>=? And LOWER(WordText) NOT LIKE ? And Omitted=0
ORDER BY EditDistance ASC, Uses DESC, Rank DESC
LIMIT ?
";

            if(input.Length > MaxInputLen) {
                input = input.Substring(0, MaxInputLen);
            }
            string mv = input.ToLower();
            string lv = $"{mv}%";
            int max_len_diff = mv.Length > 3 ? 2 : mv.Length == 3 ? 1 : 0;
            int len = mv.Length - max_len_diff;

            if(LOG_QUERIES) {
                MpConsole.WriteLine($"Rank query: ");
                MpConsole.WriteLine(query.GetParameterizedQueryString(new object[] { MaxInputLen, mv, minRank, maxRank, mv, len, lv, maxResults }));
            }

            var result = await AsyncConn.CancellableQueryAsync<WordComparision>(query, ct, MaxInputLen, mv, minRank, maxRank, mv, len, lv, maxResults);
            return result;
        }
        static async Task<List<WordComparision>> GetStartsWithWordsAsync(string input, int maxResults, CancellationToken ct) {
            int len = input.Length;
            string mv = input.ToLower();
            string lv = $"{mv}%";
            string query = $@"
SELECT 
    *,
    LENGTH(WordText) - ? AS EditDistance
FROM Word 
WHERE Omitted=0 AND LENGTH(WordText)>=? AND LOWER(WordText) LIKE ?
ORDER BY Uses DESC, Rank DESC 
LIMIT ?";
            if(LOG_QUERIES) {
                MpConsole.WriteLine($"StartsWith query: ");
                MpConsole.WriteLine(query.GetParameterizedQueryString(new object[] { mv, lv, maxResults }));
            }
            var result = await AsyncConn.CancellableQueryAsync<WordComparision>(query, ct, len, len, lv, maxResults);
            return result;
        }
        #endregion        

        #region Update
        public static async Task<bool> UpdateWordUseAsync(Dictionary<string, int> usedWords, bool allowInsert) {
            if(!CanAsyncQuery) {
                return false;
            }
            // NOTE presumes all words are lower cased already and none have any spaces
            // NOTE2 no repeated words

            try {
                if(!IsLoaded) {
                    return false;
                }
                string words_mask = string.Join(",", Enumerable.Repeat("?", usedWords.Count));

                // find existing words and their cur Use counts

                var found_words = await AsyncConn.QueryAsync<Word>($"SELECT * FROM Word WHERE LOWER(WordText) IN ({words_mask})", usedWords.Select(x => x.Key).ToArray());
                // get new words (words not found in db)
                var new_words = usedWords.Where(x => found_words.All(y => y.WordText.ToLower() != x.Key)).ToList();
                if(!allowInsert) {
                    new_words.Clear();
                } else {
                    // filter new words, don't add less than len 3
                    new_words = new_words.Where(x => x.Key.Length >= 2).ToList();
                }

                // add new words and update existing word counts
                await Task.WhenAll(
                    found_words
                    .Select(x =>
                    AsyncConn.ExecuteAsync($"UPDATE Word SET Uses=? WHERE WordText=?", x.Uses + usedWords[x.WordText.ToLower()], x.WordText))
                    .Union(new_words.Select(x =>
                    // NOTE new words added so Uses is -1, then they will only be used (since default is 0) when found at least TWICE
                    AsyncConn.ExecuteAsync($"INSERT INTO Word(WordText,Rank,Uses,Culture,UserWord) VALUES(?,?,?,?,?)", x.Key, 0, x.Value, LastWordRankedCulture, 1))));


                //MpConsole.WriteLine($"New SearchWords: {(allowInsert ? string.Empty : "IGNORED")}");
                //new_words.ForEach(x => MpConsole.WriteLine($"'{x.Key}': {x.Value}"));
                //MpConsole.WriteLine($"Updated SearchWords:");
                //found_words.ForEach(x => MpConsole.WriteLine($"'{x.WordText}': {x.Uses + usedWords[x.WordText]}"));
                return true;
            }
            catch(Exception ex) {
                // NOTE this sometimes fails when the keyboard is deactivated
                MpConsole.WriteLine(ex.ToString());
            }
            return false;
        }
        #endregion

        #region Analysis
        static async Task AnalyzeWordSetAsync(Stopwatch sw) {
            var sps = InputConnection.SharedPrefService;
            string cur_culture = CultureManager.CurrentKbCulture;
            string last_culture = sps.GetPrefValue<string>(PrefKeys.LAST_WORD_RANKED_CULTURE);
            int last_count = sps.GetPrefValue<int>(PrefKeys.LAST_WORD_RANKED_COUNT);
            int avg_len = sps.GetPrefValue<int>(PrefKeys.AVG_WORD_LEN);
            string rank_csv = sps.GetPrefValue<string>(PrefKeys.WORD_RANK_CSV);
            string rank_type_str = sps.GetPrefValue<string>(PrefKeys.WORD_RANK_TYPE);

            bool needs_analysis = false;
            if(last_count < 0 || InitCount - last_count >= MIN_COUNT_DIFF_FOR_REANALYSIS) {
                needs_analysis = true;
            } else if(cur_culture != last_culture) {
                needs_analysis = true;
            }

            if(needs_analysis) {
                MpConsole.WriteLine($"Beginning analysis of {InitCount} words");

                // get avg word len
                avg_len = await GetAverageWordLengthAsync();
                // clamp comparisions to 2x avg len
                var avg_words = await AsyncConn.QueryScalarsAsync<string>($"SELECT WordText FROM Word WHERE LENGTH(WordText)=? LIMIT 1", avg_len);

                int freq_count = await AsyncConn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Word WHERE Rank>1");

                var rank_type = freq_count > 0 ? WordRankType.Frequency : WordRankType.RowId;
                rank_type_str = rank_type.ToString();

                if(avg_words.FirstOrDefault() is { } avg_word) {
                    // notes
                    // goal dur: 0.2s
                    // total en count: 165,643
                    // avg en len: 9
                    // max rank: 222
                    // sub ranks: 5 (arbitrary)
                    // total EDIT_DIST query dur: 10.18s
                    // words comp per second: 16,271
                    // goal rank clamp count: 1*goal_dur * words_comp_per_second == 3,254
                    // rank steps: (total-clamp_count)/sub_ranks == 32,478
                    // goals:
                    // 1. min rank where avg word query is like 0.2s
                    // 2. rank step proportional to total words

                    var sw2 = Stopwatch.StartNew();
                    await DoAnalysisEditDistQueryAsync(avg_word);
                    sw2.Stop();
                    double tt = sw.Elapsed.TotalSeconds;
                    double words_per_second = InitCount / tt;
                    double goal_t = TARGET_COMPL_SPAN.TotalSeconds;
                    double goal_count = goal_t * words_per_second;
                    int goal_sub_ranks = 5;
                    int sub_rank_step = (int)((InitCount - goal_count) / (double)goal_sub_ranks);

                    var ranks = new List<int>();
                    int count = 0;
                    while(true) {
                        int cur_goal = count == 0 ? (int)goal_count : (int)(sub_rank_step * count);
                        int cur_rank = rank_type == WordRankType.Frequency ? await GetMinRankForWordCountAsync(cur_goal) : cur_goal;
                        ranks.Add(cur_rank);
                        if(cur_rank <= 0) {
                            break;
                        }
                        count++;
                    }
                    rank_csv = string.Join(",", ranks);

                    sps.SetPrefValue(PrefKeys.LAST_WORD_RANKED_CULTURE, cur_culture);
                    sps.SetPrefValue(PrefKeys.LAST_WORD_RANKED_COUNT, InitCount);
                    sps.SetPrefValue(PrefKeys.AVG_WORD_LEN, avg_len);
                    sps.SetPrefValue(PrefKeys.WORD_RANK_CSV, rank_csv);
                    sps.SetPrefValue(PrefKeys.WORD_RANK_TYPE, rank_type.ToString());
                }
            }
            if(Enum.TryParse(typeof(WordRankType), rank_type_str, out var wrtObj) &&
                wrtObj is WordRankType wrt) {
                RankType = wrt;
            }
            LastWordRankedCulture = cur_culture;
            MaxInputLen = avg_len * 2;
            Ranks = rank_csv.SplitNoEmpty(",").Select(x => int.Parse(x)).ToArray();
            Stats = new(avg_len, Ranks.Any() ? Ranks.FirstOrDefault() : 0, Ranks.Length, InitCount);

            //int omit_count = await GetOmittedWordCountAsync();
            sw.Stop();
            MpConsole.WriteLine($"Db loaded {sw.ElapsedMilliseconds}ms | Count: {InitCount} | Max Input Length: {MaxInputLen} | Ranks: {rank_csv} |");

        }
        #endregion

        #endregion

        #region Private Methods
        static async Task FinishInitAsync(Stopwatch sw) {
            await AnalyzeWordSetAsync(sw);
            IsLoading = false;
            IsLoaded = true;
        }
        static async Task UnloadAsync() {
            if(AsyncConn != null) {
                await AsyncConn.CloseAsync();
            }
            if(SyncConn != null) {
                SyncConn.Close();
            }
        }
        static async Task<List<T>> CancellableQueryScalarsAsync<T>(this SQLiteAsyncConnection conn, string query, CancellationToken ct, params object[] args) {
            try {
                var result = await Task.Factory.StartNew(delegate {
                    SQLiteConnectionWithLock connection = conn.GetConnection();
                    using(connection.Lock()) {
                        return connection.QueryScalars<T>(query, args);
                    }
                }, ct);
                return result;
            }
            catch(Exception ex) {
                ex.Dump();
                return [];
            }
        }
        static Task<List<T>> CancellableQueryAsync<T>(this SQLiteAsyncConnection conn, string query, CancellationToken ct, params object[] args) where T : new() {
            //try {
            return Task.Factory.StartNew(delegate {
                SQLiteConnectionWithLock connection = conn.GetConnection();
                using(connection.Lock()) {
                    return connection.Query<T>(query, args);
                }
            }, ct).DefaultIfCanceled(defaultValue: []);
            //return result;
            //} catch {
            //    return [];
            //}
        }
        static Task<T> DefaultIfCanceled<T>(this Task<T> @this, T defaultValue = default(T)) {
            return
              @this.ContinueWith
                (
                  t => {
                      if(t.IsCanceled) return defaultValue;

                      return t.Result;
                  }
                );
        }
        static async Task<int> GetTotalWordCountAsync() {
            var result = await AsyncConn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Word WHERE Omitted=0");
            return result;
        }
        static async Task DoAnalysisEditDistQueryAsync(string sample_word) {
            _ = await AsyncConn.QueryScalarsAsync<string>($"select EDIT_DIST(WordText,?) AS dist from Word order by dist", sample_word);
        }
        static async Task<int> GetAverageWordLengthAsync() {
            var result = await AsyncConn.ExecuteScalarAsync<int>($"SELECT AVG(LENGTH(WordText)) FROM Word WHERE Omitted=0");
            return result;
        }
        static async Task<int> GetMaxRankAsync() {
            var result = await AsyncConn.ExecuteScalarAsync<int>($"SELECT Rank FROM Word WHERE Omitted=0 ORDER BY Rank DESC limit 1");
            return result;
        }
        static async Task<int> GetMinRankForWordCountAsync(int wordCount) {
            var result = await AsyncConn.ExecuteScalarAsync<int>($"SELECT Rank FROM Word WHERE Omitted=0 ORDER BY Rank DESC limit 1 offset ?", wordCount);
            return result;
        }
        public static void FuzzRatioSqlFunc(sqlite3_context ctx, object user_data, sqlite3_value[] args) {
            string a = raw.sqlite3_value_text(args[0]).utf8_to_string();
            string b = raw.sqlite3_value_text(args[1]).utf8_to_string();
            int ratio = Fuzz.Ratio(a, b);
            raw.sqlite3_result_int(ctx, 100 - ratio);
        }
        static void EditDistanceSqlFunc(sqlite3_context ctx, object user_data, sqlite3_value[] args) {
            // NOTE a is always from db b is mv
            string a = raw.sqlite3_value_text(args[0]).utf8_to_string();
            string b = raw.sqlite3_value_text(args[1]).utf8_to_string();
            raw.sqlite3_result_int(ctx, LevenshteinDamerauDist(TrimWordPunctuation(a), b));
        }

        static string TrimWordPunctuation(string word) {
            return word.Replace("'", string.Empty).Replace("-", string.Empty);
        }

        public static int LevenshteinDamerauDist(string a, string b) {
            if(a == b) {
                return 0;
            }
            int len_a = a.Length;
            int len_b = b.Length;
            if(len_a == 0 || len_b == 0) {
                return len_a == 0 ? len_b : len_a;
            }

            var matrix = new int[len_a + 1, len_b + 1];

            for(int i = 1; i <= len_a; i++) {
                matrix[i, 0] = i;
                for(int j = 1; j <= len_b; j++) {
                    int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                    if(i == 1) {
                        matrix[0, j] = j;
                    }

                    var vals = new int[] {
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1,
                        matrix[i - 1, j - 1] + cost
                    };
                    matrix[i, j] = vals.Min();
                    if(i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) {
                        matrix[i, j] = Math.Min(matrix[i, j], matrix[i - 2, j - 2] + cost);
                    }

                }
            }
            return matrix[len_a, len_b];
        }

        public static int LevenshteinDist(string a, string b) {
            if(a == null || b == null) {
                return int.MaxValue; // or handle the case where either string is null
            }

            int m = a.Length;
            int n = b.Length;
            int[,] dp = new int[m + 1, n + 1];

            // filling base cases
            for(int i = 0; i <= m; i++) {
                dp[i, 0] = i;
            }
            for(int j = 0; j <= n; j++) {
                dp[0, j] = j;
            }
            // populating matrix using dp-approach
            for(int i = 1; i <= m; i++) {
                for(int j = 1; j <= n; j++) {
                    if(a[i - 1] != b[j - 1]) {
                        int del = 1 + dp[i - 1, j];
                        int ins = 1 + dp[i, j - 1];
                        int rep = 1 + dp[i - 1, j - 1];
                        dp[i, j] = Math.Min(del, Math.Min(ins, rep));
                    } else {
                        dp[i, j] = dp[i - 1, j - 1];
                    }

                }
            }
            return dp[m, n];
        }

        static void DeleteDb() {
            new string[] { "db", "db-shm", "db-wal" }.
                Select(x => Path.Combine(Path.GetDirectoryName(DbPath), $"{Path.GetFileNameWithoutExtension(DbPath)}.{x}"))
                .Where(x => File.Exists(x))
                .ForEach(x => File.Delete(x));
        }

        #region Unused

        public static async Task AddItemAsync<T>(T item) where T : new() {
            if(!CanAsyncQuery) {
                return;
            }
            await AsyncConn.InsertAsync(item);
        }
        public static async Task UpdateItemAsync<T>(T item) where T : new() {
            if(!CanAsyncQuery) {
                return;
            }
            await AsyncConn.UpdateAsync(item);
        }

        public static async Task RemoveItemAsync<T>(T item) where T : new() {
            if(!CanAsyncQuery) {
                return;
            }
            await AsyncConn.DeleteAsync(item);
        }
        static async Task AddSpellFixExtensionAsync(IAssetLoader loader, string local_storage_path) {
            await AsyncConn.EnableLoadExtensionAsync(true);

            string spellfix_ext = OperatingSystem.IsAndroid() ? "so" : "dll";
            string spellfix_name = $"spellfix.{spellfix_ext}";
            string spellfix_path = Path.Combine(local_storage_path, spellfix_name.Replace(".temp", ".so"));
            if(!File.Exists(spellfix_path)) {
                // TODO need to look at arch and get right spellfix lib here

                // copy over 
                var fs = File.Create(spellfix_path);
                Stream dll_stream = null;
                if(OperatingSystem.IsWindows()) {
                    dll_stream = loader.LoadStream("C:\\Users\\tkefauver\\Source\\Repos\\MonkeyPaste\\Scratch\\MobileKeyboardTest\\iosKeyboardTest\\Assets\\spellfix.dll");
                    dll_stream.Seek(0, SeekOrigin.Begin);
                } else {
                    dll_stream = loader.LoadStream(spellfix_name);
                }
                dll_stream.CopyTo(fs);
                fs.Close();
                dll_stream.Dispose();
                fs.Dispose();
            }

            raw.sqlite3_load_extension(
                AsyncConn.GetConnection().Handle,
                utf8z.FromString(spellfix_path),
                utf8z.FromString("sqlite3_spellfix_init"),
                out _);
            //if (err.utf8_to_string() is { } err_str &&
            //    !string.IsNullOrEmpty(err_str)) {
            //    MpConsole.WriteLine(err_str);
            //}
        }

        static async Task CreateSpellFixTableAsync() {
            await AsyncConn.ExecuteAsync($"CREATE VIRTUAL TABLE demo USING spellfix1;");
            await AsyncConn.ExecuteAsync($"INSERT INTO demo(word,rank) SELECT WordText,Rank FROM Word;");
        }
        #endregion
        #endregion

        #region Helpers

        #endregion
    }
}




