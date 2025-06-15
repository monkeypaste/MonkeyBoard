using Foundation;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using System;
using System.Diagnostics;
using UIKit;

namespace MonkeyBoard.iOS {
    public class iosPermissionHelper_old : MpIKeyboardPermissionHelper {
        bool _isKeyboardActive;
        //event EventHandler<NSNotificationEventArgs> CurrentInputModeDidChangeHandler;
        public iosPermissionHelper_old() {
            //UITextInputMode.Notifications.ObserveCurrentInputModeDidChange(CurrentInputModeDidChangeHandler);
            //CurrentInputModeDidChangeHandler += (s, e) => {
            //    //UITextInputMode.CurrentInputMode.
            //    MpConsole.WriteLine($"Input mode changed!");
            //};

            ///*
            //[[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(keyboardDidChange:) name:UITextInputCurrentInputModeDidChangeNotification object:nil];
            //*/
            NSNotificationCenter.DefaultCenter.AddObserver(UITextInputMode.CurrentInputModeDidChangeNotification, (ntf) => {
                // from https://stackoverflow.com/q/31096696/105028
                /*
                NSString *inputMethod = [[UITextInputMode currentInputMode] primaryLanguage];
                 NSLog(@"inputMethod=%@",inputMethod);
                */
                //UITextInputMode.CurrentInputMode.
                _isKeyboardActive = true;
            });
        }
        public bool IsKeyboardActive() {
            return _isKeyboardActive;

        }

        public bool IsKeyboardEnabled() {
            // from https://stackoverflow.com/a/25786928/105028
            string bundleId = "com.MonkeyBoard.iOS.KeyboardExt";
            var kbl = NSUserDefaults.StandardUserDefaults.GetDictionaryOfValuesFromKeys([new NSString("AppleKeyboards")]);
            foreach(var kb in kbl) {
                if(kb.Value is NSString kbid && kbid.ToStringOrEmpty() == bundleId) {
                    return true;
                }
            }
            return false;
        }

        public void ShowKeyboardSelector() {
            UIApplication.SharedApplication.OpenUrl(NSUrl.FromString("app-settings:root=General&path=Keyboard/KEYBOARDS"), new UIApplicationOpenUrlOptions(), (success) => { });
        }

        public void ShowKeyboardActivator() {
            // from https://stackoverflow.com/a/41973450/105028

            UIApplication.SharedApplication.OpenUrl(NSUrl.FromString("app-settings:root=General&path=Keyboard/KEYBOARDS"), new UIApplicationOpenUrlOptions(), (success) => { });
            //UIApplication.SharedApplication.OpenUrl(NSUrl.FromString("prefs:root=General&path=Keyboard"), new UIApplicationOpenUrlOptions(), (success) => { });
        }

        public void ShowMicActivator() {

        }
    }

}

