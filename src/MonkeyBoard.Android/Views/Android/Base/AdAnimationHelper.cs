using Android.Animation;
using Android.Views.Animations;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;

namespace MonkeyBoard.Android {
    public class AdAnimationHelper : IAnimationTimer {
        Dictionary<object, AdTimeAnimator> RunningActionLookup { get; set; } = [];
        public void Start(object key, Action action) {
            var anim = new AdTimeAnimator(action);
            RunningActionLookup.AddOrReplace(key, anim);
            anim.Start();
        }

        public void Stop(object key) {
            if (!RunningActionLookup.TryGetValue(key, out var dl)) {
                return;
            }
            dl.Stop();
            RunningActionLookup.Remove(key);
        }
    }

    public class AdTimeAnimator : Java.Lang.Object, ValueAnimator.IAnimatorUpdateListener {
        Action Action { get; set; }
        ValueAnimator Animator { get; set; }
        public AdTimeAnimator(Action action) {
            Action = action;
            Animator = ValueAnimator.OfFloat(0,10);
            Animator.SetDuration(10_000);
            Animator.SetInterpolator(new LinearInterpolator());            
            Animator.AddUpdateListener(this);            
        }
        public void Start() {
            Animator.Start();
        }
        public void Stop() {
            Animator.Cancel();
            Animator.RemoveAllListeners();
            //Animator.Dispose();
            //Animator = null;
        }
        public void OnAnimationUpdate(ValueAnimator animation) {
            Action.Invoke();
        }
    }
}