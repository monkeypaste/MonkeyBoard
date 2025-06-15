using AuthenticationServices;
using CoreGraphics;
using Foundation;
using MonkeyPaste.Keyboard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UIKit;

namespace MonkeyBoard.iOS.KeyboardExt {
    public class iosKeyboardContainerView : UIStackView {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #endregion

        #region Properties

        #region Members

        #endregion

        #region Views
        public iosKeyboardView KeyboardView { get; set; }
        #endregion

        #region View Models
        #endregion

        #region Appearance
        #endregion

        #region Layout
        #endregion

        #region State
        int ActivateCount { get; set; }
        nfloat OriginalHeight { get; set; }
        nfloat? CollapsedHeight { get; set; }
        public bool IsCollapsed =>
            CollapsedHeight != null;
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public iosKeyboardContainerView() {
            BackgroundColor = UIColor.Clear;
            Axis = UILayoutConstraintAxis.Vertical;
            MultipleTouchEnabled = true;
            UserInteractionEnabled = true;
            TranslatesAutoresizingMaskIntoConstraints = false;

            KeyboardView = new iosKeyboardView().SetDefaultProps(true);
            this.AddArrangedSubview(KeyboardView);
        }
        #endregion

        #region Public Methods
        
        public void Init(iosKeyboardViewController vc) {
            KeyboardView.Init(vc);
            this.Frame = KeyboardView.Frame;
        }
        public void ActivateConstraints() {
            if(iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.RootView is not { } RootView) {
                return;
            }
            ActivateCount++;
            if(ActivateCount >= 500) {
                ActivateCount = 0;
            }
            // some info: https://stackoverflow.com/q/24167909/105028
            float priority = 500 + ActivateCount;
            this.ClearConstraints();
            KeyboardView.ClearConstraints();

            NSLayoutConstraint.ActivateConstraints([
                    this.WidthAnchor.ConstraintEqualTo(this.Frame.Width).WithPriority(priority),
                    this.HeightAnchor.ConstraintEqualTo(this.Frame.Height).WithPriority(priority),

                    this.TopAnchor.ConstraintEqualTo(RootView.TopAnchor).WithPriority(priority),
                    this.BottomAnchor.ConstraintEqualTo(RootView.BottomAnchor).WithPriority(priority),
                    this.LeftAnchor.ConstraintEqualTo(RootView.LeftAnchor).WithPriority(priority),
                    this.RightAnchor.ConstraintEqualTo(RootView.RightAnchor).WithPriority(priority)
                    ]);

            NSLayoutConstraint.ActivateConstraints([
                KeyboardView.WidthAnchor.ConstraintEqualTo(KeyboardView.Frame.Width).WithPriority(priority),
                KeyboardView.HeightAnchor.ConstraintEqualTo(KeyboardView.Frame.Height).WithPriority(priority),
                KeyboardView.BottomAnchor.ConstraintEqualTo(this.BottomAnchor).WithPriority(priority)
                ]);

            KeyboardView.RenderFrame(true);                        
            RootView.NeedsUpdateConstraints();            
            RootView.Redraw(true);
        }
        public void Unload() {
            KeyboardView.Unload();
        }
        public void AdjustHeight(nfloat dh) {
            if (iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.RootView is not { } RootView) {
                return;
            }
            this.Frame = RootView.Frame.Resize(RootView.Frame.Width,RootView.Frame.Height + dh);

            ActivateConstraints();
        }
        public void ToggleExpanded() {
            if(IsCollapsed) {
                Expand();
            } else {
                Collapse();
            }
            KeyboardView.FooterView.SetDismissImage(IsCollapsed);
        }
        public void Move(CGPoint screenPos) {
            if(iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.RootView is not { } RootView) {
                return;
            }
            var size = RootView.Bounds.Size;

            RootView.ClearConstraints();
            RootView.TranslatesAutoresizingMaskIntoConstraints = false;
            float priority = 600;
            NSLayoutConstraint.ActivateConstraints([
                    RootView.WidthAnchor.ConstraintEqualTo(size.Width).WithPriority(priority),
                    RootView.HeightAnchor.ConstraintEqualTo(size.Height).WithPriority(priority),

                    RootView.LeftAnchor.ConstraintEqualTo(RootView.LeftAnchor,screenPos.X).WithPriority(priority),
                    RootView.TopAnchor.ConstraintEqualTo(RootView.TopAnchor,screenPos.Y).WithPriority(priority),
                    this.RightAnchor.ConstraintEqualTo(RootView.RightAnchor, screenPos.X + size.Width).WithPriority(priority),
                    this.BottomAnchor.ConstraintEqualTo(RootView.BottomAnchor, screenPos.Y + size.Height).WithPriority(priority),
                    ]);
            ActivateConstraints();
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        void Collapse() {
            if (IsCollapsed) {
                return;
            }
            OriginalHeight = this.Frame.Height;
            CollapsedHeight = this.Frame.Height - KeyboardView.FooterView.Frame.Height;
            MpConsole.WriteLine($"Collapsing height by: {CollapsedHeight}");
            AdjustHeight(-CollapsedHeight.Value);
        }
        void Expand() {
            MpConsole.WriteLine($"Expand by {CollapsedHeight}");

            AdjustHeight(CollapsedHeight ?? 0);
            CollapsedHeight = null;
        }
        #endregion


        #region Touch
        void ProcessTouches(NSSet touches, TouchEventType eventType) {
            if(iosKeyboardViewController.Instance is not { } kbvc ||
                kbvc.KeyboardView is not { } kbv) {
                return;
            }

            for (int i = 0; i < (int)touches.Count; i++) {
                if(touches.ElementAt(i) is not UITouch touch) {
                    continue;
                }
                kbv.TriggerTouchEvent(touch.LocationInView(kbv), eventType);
            }
            if(eventType == TouchEventType.Release) {
                iosHelpers.DoGC();
            }
        }
        public override void TouchesBegan(NSSet touches, UIEvent evt) {
            ProcessTouches(touches, TouchEventType.Press);
        }
        public override void TouchesMoved(NSSet touches, UIEvent evt) {
            ProcessTouches(touches, TouchEventType.Move);
        }
        public override void TouchesEnded(NSSet touches, UIEvent evt) {
            ProcessTouches(touches, TouchEventType.Release);
        }
        #endregion
    }
}