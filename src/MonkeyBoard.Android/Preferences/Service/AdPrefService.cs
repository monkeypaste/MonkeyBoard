using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.Content;
using AndroidX.Preference;
using Java.Interop;
using Java.Util;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MonkeyBoard.Android {

    public class AdPrefService : IPlatformSharedPref {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region ISharedPrefService Implementation
        bool IPlatformSharedPref.CanWrite => true;
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
                        val = SharedPrefs.GetBoolean(key, (bool)defValObj);
                        break;
                    case Type intType when intType == typeof(int):
                        val = SharedPrefs.GetInt(key, (int)defValObj);
                        break;
                    case Type strType when strType == typeof(string):
                        val = SharedPrefs.GetString(key, (string)defValObj);
                        break;
                    default:
                        // unhandled
                        Debugger.Break();
                        break;
                }
                return val;
            }
            catch (Exception ex) {
                ex.Dump();
            }
            return null;
        }

        public void SetPlatformPrefValue(PrefKeys prefKey, object newValObj) {
            if (SharedPrefs == null) {
                return;
            }
            string key = prefKey.ToString();
            var editor = SharedPrefs.Edit();

            switch (newValObj) {
                case bool boolVal:
                    editor.PutBoolean(key, boolVal);
                    break;
                case int intVal:
                    editor.PutInt(key, intVal);
                    break;
                case string strVal:
                    editor.PutString(key, strVal);
                    break;

                default:
                    // unhandled
                    Debugger.Break();
                    break;
            }
            editor.Commit();
            editor.Apply();
        }
        #endregion

        #endregion

        #region Properties
        ISharedPreferences SharedPrefs { get; set; }
        SharedPrefWrapper PrefWrapper { get; set; }
        #endregion

        #region Events
        #endregion

        #region Constructors
        public AdPrefService(Context context, SharedPrefWrapper prefWrapper) {
            SharedPrefs = PreferenceManager.GetDefaultSharedPreferences(context);
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
