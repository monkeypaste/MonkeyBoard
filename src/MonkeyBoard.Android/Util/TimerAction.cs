using Android.OS;
using Java.Util;
using System;
//using Xamarin.Essentials;

namespace MonkeyBoard.Android {
    public class TimerAction : TimerTask {
        Handler _handler;
        Action _action;
        public TimerAction(Handler handler, Action action) {
            _handler = handler;
            _action = action;
        }
        public override void Run() {
            _handler?.Post(() => {
                _action?.Invoke();
            });

        }
    }
}
