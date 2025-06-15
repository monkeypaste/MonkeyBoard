using Android.App.Job;
using Android.Content;
using Android.Util;
using AndroidX.Concurrent.Futures;
using AndroidX.Work;
using Google.Common.Util.Concurrent;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using MonkeyPaste.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MpConsole = MonkeyPaste.Common.MpConsole;

namespace MonkeyBoard.Android {
    public class WordUpdateWorker : ListenableWorker, CallbackToFutureAdapter.IResolver {
        #region Private Variables
        #endregion

        #region Constants
        public const string TAG = "WordUpdateWorker";
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties
        Dictionary<string, int> WordsToUpdate { get; set; } = [];
        bool AllowInsert { get; set; }
        #endregion

        #region Events
        #endregion

        #region Constructors
        public WordUpdateWorker(Context context, WorkerParameters workerParams) : base(context, workerParams) {
            try {
                AllowInsert = workerParams.InputData.GetBoolean("allowInsert",false);
                WordsToUpdate =
                    workerParams.InputData.GetString("words")
                        .SplitByLineBreak()
                        .Select(x => x.SplitNoEmpty(","))
                        .Where(x=>x.Length >= 2)
                        .ToDictionary(x => x[0], x => int.Parse(x[1]));
            } catch(Exception ex) {
                ex.Dump();
                WordsToUpdate = [];
                AllowInsert = false;
            }
        }
        #endregion

        #region Public Methods

        public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer p0) {
            Task.Run(async () => {
                bool success = false;
                var sw = Stopwatch.StartNew();

                success = await WordDb.UpdateWordUseAsync(WordsToUpdate, AllowInsert);
                
                MpConsole.WriteLine($"Word update done. Total SearchWords: {WordsToUpdate.Count} {success.ToTestResultLabel()} Time: {sw.ElapsedMilliseconds}ms");
                if(success) {
                    return p0.Set(Result.InvokeSuccess());
                }
                return p0.Set(Result.InvokeFailure());
            });
            return TAG;
        }

        public override IListenableFuture StartWork() {
            return CallbackToFutureAdapter.GetFuture(this);
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion
    }
    public static class WordUpdateScheduler {
        #region Private Variables
        #endregion

        #region Constants
        #endregion


        public static void AddWords(Context context, Dictionary<string, int> words, bool allowInsert) {           
            Constraints.Builder constraints = new Constraints.Builder()
                //.SetRequiresDeviceIdle(true)
                //.SetRequiresStorageNotLow(true)
                //.SetRequiresBatteryNotLow(true)
                ;

            Data.Builder data = new Data.Builder();
            data.PutBoolean("allowInsert", allowInsert);
            data.PutString("words", string.Join(Environment.NewLine, words.Select(x=>$"{x.Key},{x.Value}")));

            var wordUpdateRequest = new OneTimeWorkRequest.Builder(typeof(WordUpdateWorker))
                .AddTag(WordUpdateWorker.TAG)
                .SetInputData(data.Build())
                .SetConstraints(constraints.Build())
                .Build();

            WorkManager
                .GetInstance(context)
                .BeginUniqueWork(WordUpdateWorker.TAG, ExistingWorkPolicy.Keep, wordUpdateRequest)
                .Enqueue();
        }
    }
}
