using Android.Content;
using Android.Provider;
using Android.Views;
using Android.Views.InputMethods;
using MonkeyBoard.Bridge;
using MonkeyPaste.Avalonia;
using System;
using System.Linq;
//using Xamarin.Essentials;

namespace MonkeyBoard.Android {
    public class AdPermissionHelper : MpIKeyboardPermissionHelper {
        Context Context { get; set; }
        public AdPermissionHelper(Context context) {
            Context = context;
        }
        public bool IsKeyboardEnabled() {
            if(Context is not { } ma ||
                ma.GetSystemService(Context.InputMethodService) is not InputMethodManager imm) {
                return false;
            }
            return imm.EnabledInputMethodList.Any(x => x.PackageName == ma.PackageName);
        }
        public bool IsKeyboardActive() {
            if(Context is not { } ma ||
                ma.GetSystemService(Context.InputMethodService) is not InputMethodManager imm) {
                return false;
            }

            string result = Settings.Secure.GetString(ma.ContentResolver, Settings.Secure.DefaultInputMethod);
            return result.StartsWith(ma.PackageName);
        }
        public void ShowKeyboardSelector() {
            if(Context is not { } ma ||
                ma.GetSystemService(Context.InputMethodService) is not InputMethodManager imm
                ) {
                return;
            }
            imm.ShowInputMethodPicker();
        }
        public void ShowKeyboardActivator() {
            if(Context is not { } ma ||
                ma.GetSystemService(Context.InputMethodService) is not InputMethodManager imm) {
                return;
            }
            //imm.ShowInputMethodAndSubtypeEnabler(ma.PackageName);
            ma.StartActivity(new Intent(Settings.ActionInputMethodSettings));
        }
        public void ShowMicActivator() {
            if(Context is not { } ma) {
                return;
            }
            var SpeechToText = new SpeechToText(ma);
            SpeechToText.Init();

        }
    }
}
