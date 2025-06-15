using CoreAnimation;
using CoreFoundation;
using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosDisplayLinkHelper : IAnimationTimer {
        Dictionary<object, CADisplayLink> RunningActionLookup { get; set; } = [];
        NSRunLoop DefaultRunLoop =>
            NSRunLoop.Current;
        NSRunLoopMode DefaultRunLoopMode =>
            NSRunLoopMode.Default;
        public void Start(object key, Action action) {
            CADisplayLink displayLink = CADisplayLink.Create(action);  //Update is my refresh method
            RunningActionLookup.AddOrReplace(key, displayLink);
            displayLink.AddToRunLoop(DefaultRunLoop, DefaultRunLoopMode);            
        }

        public void Stop(object key) {
            if(!RunningActionLookup.TryGetValue(key, out var dl)) {
                return;
            }
            dl.Invalidate();
            dl.RemoveFromRunLoop(DefaultRunLoop, DefaultRunLoopMode);
            RunningActionLookup.Remove(key);
        }
    }
}