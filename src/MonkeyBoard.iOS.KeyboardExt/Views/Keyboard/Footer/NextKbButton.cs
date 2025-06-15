using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Linq;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class NextKbButton : UIControl {

        public override void TouchesBegan(NSSet touches, UIEvent evt) {
            if (touches.FirstOrDefault() is not UITouch t) {
                return;
            }
            //MpConsole.WriteLine("Next down");
            if(iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardView is not { } kbv ||
                kbvc is not IKeyboardInputConnection kic) {
                return;
            }
            kbv.TriggerTouchEvent(t.LocationInView(kbv), TouchEventType.Press);
            kbvc.HandleInputModeList(this, evt);

        }
        public override void TouchesMoved(NSSet touches, UIEvent evt) {
            if (touches.FirstOrDefault() is not UITouch t ||
                iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardView is not { } kbv ||
                kbvc is not IKeyboardInputConnection kic) {
                return;
            }
            kbv.TriggerTouchEvent(t.LocationInView(kbv), TouchEventType.Move);
            //if (this.Bounds.Contains(t.LocationInView(this))) {
            //    kbvc.HandleInputModeList(this, evt);
            //}
        }
        public override void TouchesEnded(NSSet touches, UIEvent evt) {
            if (touches.FirstOrDefault() is not UITouch t ||
                iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardView is not { } kbv ||
                kbvc is not IKeyboardInputConnection kic) {
                return;
            }
            kbv.TriggerTouchEvent(t.LocationInView(kbv), TouchEventType.Release);           
            if(this.Bounds.Contains(t.LocationInView(this))) {
                kbvc.HandleInputModeList(this, evt);
            }
        }

    }
}