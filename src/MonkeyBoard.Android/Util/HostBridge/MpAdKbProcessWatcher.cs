using Android.Content;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;

namespace MonkeyPaste.Keyboard.Android {
    public class MpAdKbProcessWatcher : MpIProcessWatcher {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties
        Context Context { get; set; }
        public bool IsWatching { get; private set; }
        public MpPortableProcessInfo LastProcessInfo { get; }
        public MpPortableProcessInfo ThisAppProcessInfo { get; }
        public IEnumerable<MpPortableProcessInfo> AllWindowProcessInfos { get; }

        public MpPortableProcessInfo ActiveInfo {
            get {
                if(Context is not AdInputMethodService ims ||
                ims.HostId is not { } host_id) {
                    return null;
                }
                return GetInfoByPackageName(host_id);
            }
        }
        #endregion

        #region Events

        public event EventHandler<MpPortableProcessInfo> OnAppActivated;
        #endregion

        #region Constructors
        public MpAdKbProcessWatcher(Context ctx) {
            Context = ctx;
            if(Context is not AdInputMethodService ims) {
                return;
            }
            ims.OnHostIdChanged += Ims_OnHostIdChanged;
        }

        #endregion

        #region Public Methods
        public bool IsProcessPathEqual(MpPortableProcessInfo p1, MpPortableProcessInfo p2) {
            if(p1 == p2) {
                return true;
            }
            if(p1 == null || p2 == null) {
                return false;
            }
            return p1.ProcessPath.ToLower() == p2.ProcessPath.ToLower();
        }

        public nint SetActiveProcess(MpPortableProcessInfo p) {
            return 1;// nint.Zero;
        }

        public MpPortableProcessInfo GetProcessInfoFromScreenPoint(MpPoint pixelPoint) => ActiveInfo;

        public MpPortableProcessInfo GetProcessInfoFromHandle(nint handle) => ActiveInfo;

        public MpPortableProcessInfo GetClipboardOwner() => ActiveInfo;

        public void StartWatcher() {
            IsWatching = true;
        }

        public void StopWatcher() {
            IsWatching = false;
        }

        public void RegisterActionComponent(MpIInvokableAction mvm) {
            OnAppActivated += mvm.OnActionInvoked;
            MpConsole.WriteLine($"{nameof(OnAppActivated)} Registered {mvm.Label}");
        }

        public void UnregisterActionComponent(MpIInvokableAction mvm) {
            OnAppActivated -= mvm.OnActionInvoked;
            MpConsole.WriteLine($"{nameof(OnAppActivated)} Unregistered {mvm.Label}");
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        private void Ims_OnHostIdChanged(object sender, string e) {
            if(GetInfoByPackageName(e) is not { } ppi) {
                return;
            }
            TriggerAppActivated(ppi);
        }
        void TriggerAppActivated(MpPortableProcessInfo ppi) {
            if(!IsWatching) {
                return;
            }
            OnAppActivated?.Invoke(this, ppi);
        }
        MpPortableProcessInfo GetInfoByPackageName(string packageName) {
            if(string.IsNullOrEmpty(packageName) ||
                AdHelpers.GetAppPath(Context, packageName) is not { } app_path) {
                return null;
            }
            var ppi = new MpPortableProcessInfo() {
                Handle = 1,
                ProcessPath = app_path,
                ApplicationName = AdHelpers.GetAppName(Context, packageName),
                MainWindowIconBase64 = AdHelpers.GetAppIconBase64(Context, app_path)
            };
            return ppi;
        }
        #endregion

        #region Commands
        #endregion


    }
}
