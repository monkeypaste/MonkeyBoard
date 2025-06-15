using Avalonia;
using Avalonia.Layout;
using MonkeyPaste.Common;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class FooterViewModel : FrameViewModelBase {
        #region Private Variables
        #endregion

        #region Constants
        const int LEFT_BUTTON_ID = 1;
        const int RIGHT_BUTTON_ID = 2;
        const int DRAG_HANDLE_ID = 3;
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new KeyboardViewModel Parent { get; private set; }
        #endregion

        #region Appearance
        public string BgHex =>
            Parent.MenuViewModel.MenuBgHexColor;

        public string FooterFgHex =>
            KeyboardPalette.P[PaletteColorType.Fg];
        #region Left Button
        public string LeftButtonBgHex =>
            IsLeftButtonPressed ?
                KeyboardPalette.P[PaletteColorType.MenuItemPressedBg] : null;

        public object LeftButtonIconSourceObj =>
            KeyboardViewModel.CanInitiateFloatLayout ? "reset.png" : "globe.png";
        #endregion

        #region Right Button
        public string RightButtonBgHex =>
            IsRightButtonPressed ? KeyboardPalette.P[PaletteColorType.MenuItemPressedBg] : null;

        public object RightButtonIconSourceObj =>
            KeyboardViewModel.CanInitiateFloatLayout ? "open.png" : "edgearrowleft.png";
        #endregion

        #region Drag Handle
        public string DragHandleFgHex =>
            KeyboardPalette.Get(PaletteColorType.DragHandleBg, IsDragHandlePressed, true);//.AdjustAlpha(150);
        public CornerRadius DragHandleCornerRadius =>
            KeyboardViewModel.CommonCornerRadius;
        public object DragHandleIconSourceObj => "dots_2x10.png";

        #endregion

        #endregion

        #region Layout
        public Rect FooterRect =>
            new Rect(0, Parent.KeyGridRect.Bottom, Parent.KeyboardWidth, Parent.FooterHeight);

        public CornerRadius FooterCornerRadius =>
            KeyboardViewModel.IsFloatingLayout ?
                new CornerRadius(0, 0, KeyboardViewModel.CommonCornerRadius.BottomRight, KeyboardViewModel.CommonCornerRadius.BottomLeft) :
                new CornerRadius();
        public CornerRadius FooterButtonCornerRadius =>
            KeyboardViewModel.CommonCornerRadius;
        double FooterButtonMargin => 1.5;
        double FooterButtonWidthRatio => 2;
        double FooterButtonImageRatio => 0.75;

        #region Left
        public Rect LeftButtonRect {
            get {
                double w = (FooterRect.Height * FooterButtonWidthRatio) - (FooterButtonMargin * 2);
                double h = (w / 2) - (FooterButtonMargin * 2);
                double x = FooterButtonMargin;
                double y = (FooterRect.Height / 2) - (h / 2);
                return new Rect(x, y, w, h);
            }
        }
        public Rect LeftButtonHitRect =>
            LeftButtonRect.Move(0, FooterRect.Top);
        public Rect LeftButtonRelativeImageRect {
            get {
                double w = LeftButtonRect.Height * FooterButtonImageRatio;
                double h = w;
                double x = (LeftButtonRect.Width / 2) - (w / 2);
                double y = (LeftButtonRect.Height / 2) - (h / 2);
                return new Rect(x, y, w, h);
            }
        }
        public Rect LeftButtonImageRect {
            get {
                double w = LeftButtonRect.Height * FooterButtonImageRatio;
                double h = w;
                double x = LeftButtonRect.Left + (LeftButtonRect.Width / 2) - (w / 2);
                double y = LeftButtonRect.Top + (LeftButtonRect.Height / 2) - (h / 2);
                return new Rect(x, y, w, h);
            }
        }
        #endregion

        #region Right
        public Rect RightButtonRect {
            get {
                double w = (FooterRect.Height * FooterButtonWidthRatio) - (FooterButtonMargin * 2);
                double h = (w / 2) - (FooterButtonMargin * 2);
                double x = FooterRect.Right - FooterButtonMargin - w;
                double y = (FooterRect.Height / 2) - (h / 2);
                return new Rect(x, y, w, h);
            }
        }
        Rect RightButtonHitRect =>
            RightButtonRect.Move(0, FooterRect.Top);
        public Rect RightButtonRelativeImageRect {
            get {
                double w = RightButtonRect.Height * FooterButtonImageRatio;
                double h = w;
                double x = (RightButtonRect.Width / 2) - (w / 2);
                double y = (RightButtonRect.Height / 2) - (h / 2);
                return new Rect(x, y, w, h);
            }
        }
        public Rect RightButtonImageRect {
            get {
                double w = RightButtonRect.Height * FooterButtonImageRatio;
                double h = w;
                double x = RightButtonRect.Left + (RightButtonRect.Width / 2) - (w / 2);
                double y = RightButtonRect.Top + (RightButtonRect.Height / 2) - (h / 2);
                return new Rect(x, y, w, h);
            }
        }

        #endregion

        #region Label
        public string LabelText { get; private set; } = string.Empty;
        public HorizontalAlignment LabelHorizontalAlignment =>
            HorizontalAlignment.Center;
        public VerticalAlignment LabelVerticalAlignment =>
            VerticalAlignment.Center;
        public double LabelFontSize =>
            (KeyboardLayoutConstants.DefaultFontSize / 1.5) * KeyboardViewModel.FloatFontScale;
        #endregion

        #region Drag Handle

        public Rect DragHandleRect {
            get {
                // NOTE dots_2x10 is 1152x576
                var ar = FooterRect;
                double w = 50d;
                double h = w * (576d / 1152d);
                double x = (ar.Width / 2) - (w / 2);
                double y = (ar.Height / 2) - (h / 2);
                return new Rect(x, y, w, h);
            }
        }
        Rect DragHandleHitRect =>
            DragHandleRect.Move(0, FooterRect.Top);
        #endregion

        #endregion

        #region State
        public bool IsDragging =>
            TouchOwnerId == DRAG_HANDLE_ID;
        Point? CurScaleDelta { get; set; }
        bool CanScale =>
            KeyboardViewModel.CanInitiateFloatLayout && KeyboardViewModel.IsFloatingLayout;
        bool CanCollapse =>
            !KeyboardViewModel.CanInitiateFloatLayout;
        string TouchId { get; set; }
        int TouchOwnerId { get; set; }
        public override bool IsVisible =>
            //KeyboardViewModel.IsVisible && 
            Parent.FooterHeight > 0;
        bool IsLeftButtonPressed =>
            !string.IsNullOrEmpty(TouchId) && TouchOwnerId == LEFT_BUTTON_ID;
        bool IsRightButtonPressed =>
            !string.IsNullOrEmpty(TouchId) && TouchOwnerId == RIGHT_BUTTON_ID;
        bool IsDragHandlePressed =>
            !string.IsNullOrEmpty(TouchId) && TouchOwnerId == DRAG_HANDLE_ID;
        public bool IsDragHandleVisible =>
            KeyboardViewModel.IsFloatingLayout &&
            KeyboardViewModel.CanInitiateFloatLayout;
        public bool IsLeftButtonVisible =>
            (KeyboardViewModel.CanInitiateFloatLayout && KeyboardViewModel.IsFloatingLayout && CanScale) ||
            (!KeyboardViewModel.CanInitiateFloatLayout && InputConnection.NeedsInputModeSwitchKey);

        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public FooterViewModel(KeyboardViewModel parent) {
            Parent = parent;
        }
        #endregion

        #region Public Methods
        public bool HandleTouch(Touch touch, TouchEventType touchType) {
            if(!IsVisible ||
                KeyboardViewModel is not { } kbvm ||
                kbvm.FloatContainerViewModel is not { } fcvm) {
                return false;
            }

            bool handled = false;
            switch(touchType) {
                case TouchEventType.Press:
                    if(IsLeftButtonVisible && LeftButtonHitRect.Contains(touch.Location)) {
                        TouchOwnerId = LEFT_BUTTON_ID;
                    } else if(RightButtonHitRect.Contains(touch.Location)) {
                        TouchOwnerId = RIGHT_BUTTON_ID;
                    } else if(IsDragHandleVisible && DragHandleHitRect.Contains(touch.Location)) {
                        TouchOwnerId = DRAG_HANDLE_ID;
                        fcvm.StartFloatMove();
                    } else {
                        TouchOwnerId = 0;
                    }
                    handled = TouchOwnerId != 0;
                    if(!handled) {
                        break;
                    }
                    TouchId = touch.Id;

                    if(TouchOwnerId == RIGHT_BUTTON_ID && CanCollapse) {
                        InputConnection.MainThread.Post(async () => {
                            await Task.Delay(kbvm.MinDelayForHoldPopupMs);
                            if(TouchOwnerId == RIGHT_BUTTON_ID &&
                                RightButtonHitRect.Contains(touch.Location) &&
                                !kbvm.CanInitiateFloatLayout) {
                                TouchOwnerId = 0;
                                TouchId = null;
                                InputConnection.OnCollapse(true);
                                this.Renderer.PaintFrame(true);
                            }
                        });
                    }
                    break;
                case TouchEventType.Move:
                    handled = TouchId == touch.Id;
                    if(IsDragHandlePressed) {
                        var diff = touch.RawLocation - touch.LastRawLocation;
                        fcvm.MoveFloatLocation(diff.X, diff.Y);
                    } else if(IsLeftButtonPressed && CanScale) {

                    }
                    break;
                case TouchEventType.Release:
                    handled = TouchId == touch.Id;
                    int owner_id = TouchOwnerId;
                    TouchOwnerId = 0;
                    TouchId = null;
                    if(!handled ||
                        InputConnection.MainThread is not { } mt) {
                        break;
                    }
                    if(owner_id == LEFT_BUTTON_ID &&
                        LeftButtonHitRect.Contains(touch.Location) &&
                        InputConnection is IKeyboardInputConnection ios_ic) {

                        if(CanScale) {
                            KeyboardViewModel.FloatContainerViewModel.ResetLayout(true);
                        } else {
                            mt.Post(() => ios_ic.OnInputModeSwitched());
                        }
                        break;
                    }
                    if(owner_id == RIGHT_BUTTON_ID &&
                        RightButtonHitRect.Contains(touch.Location)) {
                        if(CanCollapse) {
                            InputConnection.OnCollapse(false);
                        } else {
                            InputConnection.OnToggleFloatingWindow();
                        }

                        break;
                    }
                    if(owner_id == DRAG_HANDLE_ID) {
                        fcvm.FinishFloatMove();
                    }
                    break;
            }
            if(handled) {
                this.Renderer.PaintFrame(true);
            } else {
                bool needs_redraw = TouchId != null;
                TouchOwnerId = 0;
                TouchId = null;
                if(needs_redraw) {
                    this.Renderer.PaintFrame(true);
                }
            }
            return handled;
        }
        public void SetLabelText(string text) {
            if(LabelText == text) {
                return;
            }
            LabelText = text;
            this.Renderer?.RenderFrame(true);
        }
        public void ResetState() {
            LabelText = string.Empty;
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        #endregion

        #region Commands
        #endregion
    }
}
