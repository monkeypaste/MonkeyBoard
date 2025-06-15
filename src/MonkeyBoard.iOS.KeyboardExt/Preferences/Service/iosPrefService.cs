using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace MonkeyBoard.iOS.KeyboardExt {

    public class iosPrefService : IPlatformSharedPref {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IPlatformSharedPref Implementation
        bool IPlatformSharedPref.CanWrite => true;// InputConnection as iosKeyboardViewController);.HasFullAccess;
        public object GetPlatformPrefValue(PrefKeys prefKey) {
            if (SharedPrefs == null ||
                PrefWrapper is not { } pw ||
                !pw.DefPrefValLookup.TryGetValue(prefKey, out var defValObj)) {
                return default;
            }
            if (defValObj is SliderPrefProps spp) {
                defValObj = spp.Default;
            }
            string key = prefKey.ToString();
            object val = default;

            try {
                switch (defValObj.GetType()) {
                    case Type boolType when boolType == typeof(bool):
                        val = SharedPrefs.BoolForKey(key);
                        break;
                    case Type intType when intType == typeof(int):
                        val = SharedPrefs.IntForKey(key);
                        break;
                    case Type strType when strType == typeof(string):
                        val = SharedPrefs.StringForKey(key);
                        break;
                    default:
                        // unhandled
                        //Debugger.Break();
                        //iosFooterView.SetLabel($"Value not found for {prefKey} returning default {defValObj} of type {defValObj.GetType()}",true);
                        break;
                }
                return val;
            }
            catch (Exception ex) {
                ex.Dump();
            }
            return defValObj;
        }

        public void SetPlatformPrefValue(PrefKeys prefKey, object newValObj) {
            if (this.SharedPrefs == null) {
                return;
            }
            //if(!(this as IPlatformSharedPref).CanWrite) {
            //    MpConsole.WriteLine($"Cant write to prefs (no full access)");
            //    return;
            //}
            string key = prefKey.ToString();

            switch (newValObj) {
                case bool boolVal:
                    SharedPrefs.SetBool(boolVal,key);
                    break;
                case int intVal:
                    SharedPrefs.SetInt(intVal,key);
                    break;
                case string strVal:
                    SharedPrefs.SetString(strVal, key);
                    break;

                default:
                    // unhandled
                    Debugger.Break();
                    break;
            }
            if (!SharedPrefs.Synchronize()) {
                //throw new Exception("Sync failed");
                iosFooterView.SetLabel("Sync failed");
            }
        }
        #endregion

        #endregion

        #region Properties

        IKeyboardInputConnection InputConnection { get; set; }

        #region Members
        NSUserDefaults _sharedPrefs;
        public NSUserDefaults SharedPrefs {
            get {
                if (_sharedPrefs == null) {
                    //var ud = new NSUserDefaults(KbStorageHelpers.IOS_SHARED_GROUP_ID, NSUserDefaultsType.SuiteName);
                    var ud = NSUserDefaults.StandardUserDefaults;
                    //ud.Synchronize();
                    _sharedPrefs = ud;
                }
                return _sharedPrefs;
            }
        }
        SharedPrefWrapper PrefWrapper { get; set; }

        #endregion

        #region View Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public iosPrefService(IKeyboardInputConnection ic, SharedPrefWrapper prefWrapper) {
            InputConnection = ic;
            PrefWrapper = prefWrapper;
        }
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion
    }
}