using Android.Content;
using Java.Interop;
using MonkeyBoard.Bridge;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using MonkeyPaste.Common.Avalonia;
using MonkeyPaste.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Android.Content.ClipboardManager;
using ClipboardManager = Android.Content.ClipboardManager;

namespace MonkeyPaste.Keyboard.Android {
    public class MpAdKbClipboardListener : MpAvClipboardWatcher {
        #region Private Variables
        private Context _context;
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties
        MpAdPrimaryClipChangedListener PrimaryClipChangedListener { get; }
        List<MpPortableDataObject> PendingItems { get; } = [];

        ClipboardManager _clipboardManager;
        ClipboardManager ClipboardManager {
            get {
                if(_clipboardManager is null &&
                    _context.GetSystemService(Context.ClipboardService) is ClipboardManager cms) {
                    _clipboardManager = cms;
                }
                return _clipboardManager;
            }
        }
        bool IsListening { get; set; }

        #endregion

        #region Events


        public override event EventHandler<MpPortableDataObject> OnClipboardChanged {
            add {
                bool needs_pop = _onClipboardChanged is null;
                _onClipboardChanged += value;

                if(needs_pop) {
                    PopAllPendingItemsAsync().FireAndForgetSafeAsync();
                }
            }
            remove {
                _onClipboardChanged -= value;
            }
        }
        #endregion

        #region Constructors
        public MpAdKbClipboardListener(Context context) {
            _context = context;
            PrimaryClipChangedListener = new();
            ClipboardManager.AddPrimaryClipChangedListener(PrimaryClipChangedListener);
            PrimaryClipChangedListener.OnPrimaryChanged += PrimaryClipChangedListener_OnPrimaryChanged;
        }

        #endregion

        #region Public Methods
        public override void StartMonitor(bool ignoreCurrentState) {
            IsListening = true;
            base.StartMonitor(ignoreCurrentState);
        }

        public override void StopMonitor() {
            IsListening = false;
            base.StopMonitor();
        }

        #endregion

        #region Protected Methods

        protected override async Task<MpPortableDataObject> ReadPlatformClipboardAsync() {
            await Task.Delay(1);
            return GetPrimaryDataObject();
        }
        protected override async Task<MpPortableDataObject> ApplyClipboardPluginsAsync(MpPortableDataObject mpdo) {
            if(Mp.Services is not { } mps ||
                mps.DataObjectTools is not { } dot) {
                return mpdo;
            }
            var result = await dot.ReadDataObjectAsync(mpdo, MpDataObjectSourceType.ClipboardWatcher);
            return result as MpPortableDataObject;
        }
        protected override void TriggerClipboardChange(MpPortableDataObject mpdo) {
            if(!MpAvMainWindowViewModel.Exists) {
                PendingItems.Add(mpdo);
            }
            base.TriggerClipboardChange(mpdo);
        }
        #endregion

        #region Private Methods

        private void PrimaryClipChangedListener_OnPrimaryChanged(object sender, EventArgs e) {
            if(IsMonitoring) {
                // handled by base timer
                return;
            }
            TriggerClipboardChange(GetPrimaryDataObject());
        }
        async Task PopAllPendingItemsAsync() {
            while(true) {
                if(_onClipboardChanged is null) {
                    // not sure if -= resets eventHandler to null but shouldn't clear pending if nothings there
                    return;
                }
                if(Mp.Services is { } mps &&
                    mps.StartupState is { } ss &&
                    ss.IsReady) {
                    break;
                }
                await Task.Delay(100);
            }

            PendingItems.ForEach(x => TriggerClipboardChange(x));
            PendingItems.Clear();
        }

        MpAvDataObject GetPrimaryDataObject() {
            if(_context.GetSystemService(Context.ClipboardService) is not ClipboardManager cm ||
                cm.PrimaryClip is not { } pc ||
                cm.PrimaryClipDescription is not { } pcd) {
                return null;
            }
            var mime_types = Enumerable.Range(0, pcd.MimeTypeCount).Select(x => pcd.GetMimeType(x)).ToList();
            var items = Enumerable.Range(0, pc.ItemCount).Select(x => pc.GetItemAt(x)).ToList();

            //MpDebug.Assert(pc.ItemCount == pcd.MimeTypeCount, $"Item/mime count mismatch");

            var avdo = new MpAvDataObject();

            //MpConsole.WriteLine($"Cb Formats: {string.Join(" ", mime_types)}");
            foreach(var mime_type in mime_types) {
                if(mime_type.ToLower().Contains("uri")) {

                }
                object data = null;
                switch(mime_type) {
                    case MpPortableDataFormats.MimeHtml:
                        if(items.Select(x => x.CoerceToHtmlText(_context).ToStringOrEmpty()).OrderByDescending(x => x.Length).FirstOrDefault() is { } html) {
                            data = html;
                        }
                        break;
                    case MpPortableDataFormats.MimeText:
                        if(items.Select(x => x.CoerceToText(_context).ToStringOrEmpty()).OrderByDescending(x => x.Length).FirstOrDefault() is { } text) {
                            data = text;
                        }
                        break;
                }
                if(data is null ||
                    (data is string dataStr && string.IsNullOrEmpty(dataStr))) {
                    continue;
                }
                avdo.SetData(mime_type, data);
            }
            avdo.SetData(MpPortableDataFormats.INTERNAL_CONTENT_UTC_TIMESTAMP_FORMAT, DateTime.UtcNow.ToString());

            if(_context is AdInputMethodService ims &&
                ims.ProcWatcher is { } pw &&
                pw.ActiveInfo is { } active_pi) {
                avdo.SetData(MpPortableDataFormats.INTERNAL_PROCESS_INFO_FORMAT, active_pi);
            }
            return avdo;
        }


        #endregion

        #region Commands
        #endregion

        internal class MpAdPrimaryClipChangedListener : Java.Lang.Object, IOnPrimaryClipChangedListener {
            internal event EventHandler OnPrimaryChanged;

            public void OnPrimaryClipChanged() {
                OnPrimaryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
