using Avalonia;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MonkeyBoard.Common {
    public class FloatContainerViewModel : FrameViewModelBase {
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

        #region View Models
        public new KeyboardViewModel Parent { get; private set; }
        #endregion

        #region Appearance
        public CornerRadius ContainerCornerRadius =>
            Parent.CommonCornerRadius;
        public string FloatBorderHex =>
            IsActive ? KeyboardPalette.P[PaletteColorType.FloatBorderPressedBg] : KeyboardViewModel.FooterViewModel.DragHandleFgHex;//KeyboardPalette.P[PaletteColorType.FloatBorderBg];
        public string FloatBgHex =>
            KeyboardPalette.P[PaletteColorType.MenuBg];

        //string HandleBgHex =>
        //    KeyboardPalette.P[PaletteColorType.FloatBorderBg];
        //string HandlePressedBgHex =>
        //    KeyboardPalette.P[PaletteColorType.FloatBorderPressedBg];
        //byte HandleBgAlpha { get; set; } = 255;
        //public string CurHandleBgHex =>
        //    IsScaling ? HandlePressedBgHex : HandleBgHex.AdjustAlpha(HandleBgAlpha);
        #endregion

        #region Layout
        Size MinSize => new Size(
            Parent.TotalDockedSize.Width * MinFloatScaleX, 
            Parent.TotalDockedSize.Height * MinFloatScaleY);
        Size MaxSize => new Size(
            Parent.TotalDockedSize.Width * MaxFloatScaleX, 
            Parent.TotalDockedSize.Height * MaxFloatScaleY);
        double DefaultFloatScaleX => Parent.IsPortrait ? 0.75 : 0.5;
        double DefaultFloatScaleY => Parent.IsPortrait ? 0.75 : 0.5;
        double MinFloatScaleX => Parent.CanInitiateFloatLayout ? 0.5 : 0.001;
        double MaxFloatScaleX => 1;
        double MinFloatScaleY => Parent.CanInitiateFloatLayout ? 0.5 : 0.001;
        double MaxFloatScaleY => 1.5;
        public Point FloatScale { get; private set; } = new(1, 1);
        // NOTE this pad per side not per dimension
        public double FloatPad => 
            KeyboardViewModel.CanInitiateFloatLayout ? 
                Math.Max(5,ContainerCornerRadius.TopLeft/4) : 
                0;

        public Rect FloatScreenRect =>
            ContainerRect.Move(FloatPosition);
        public Point FloatPosition { get; private set; }
        public Rect ContainerRect {
            get {
                var ar = Parent.TotalRect;

                double w = ar.Width + (FloatPad * 2);
                double h = ar.Height + (FloatPad * 2);
                double x = 0;
                double y = 0;
                return new Rect(x, y, w, h);
            }
        }
        Rect ContainerHitRect {
            get {
                var ar = ContainerRect;
                double w = ar.Width;
                double h = ar.Height;
                double x = -FloatPad;
                double y = -FloatPad;
                return new Rect(x, y, w, h);
            }
        }
        public Rect InnerContainerRect {
            get {
                var ar = ContainerRect;
                double w = ar.Width - (FloatPad*2);
                double h = ar.Height - (FloatPad*2);
                double x = FloatPad;
                double y = FloatPad;
                return new Rect(x, y, w, h);
            }
        }
        public Rect InnerBorderRect {
            get {
                var ar = ContainerRect;
                double hbw = FloatBorderWidth / 2;
                double w = ar.Width - (hbw*2);
                double h = ar.Height - (hbw*2);
                double x = hbw;
                double y = hbw;
                return new Rect(x, y, w, h);
            }
        }
        double FloatBorderWidth =>
            FloatPad;// IsActive ? FloatPad / 1 : FloatPad / 2;
        IEnumerable<Rect> GetHandleRects(double long_side, double short_side) {
            ContainerRect.ToBounds().ExpandSides(out double al, out double at, out double ar, out double ab);
            ContainerRect.ToBounds().Expand(out double ax, out double ay, out double aw, out double ah);

            for (int i = 0; i < 4; i++) {
                double l = 0, t = 0, r = 0, b = 0;
                switch (i) {
                    case 0:
                        // left
                        l = al;
                        t = at + (ah / 2) - (long_side / 2);
                        r = l + short_side;
                        b = t + long_side;
                        break;
                    case 1:
                        // top
                        l = al + (aw / 2) - (long_side / 2);
                        t = at;
                        r = l + long_side;
                        b = t + short_side;
                        break;
                    case 2:
                        // right
                        r = ar;
                        t = at + (ah / 2) - (long_side / 2);
                        l = r - short_side;
                        b = t + long_side;
                        break;
                    case 3:
                        // bottom
                        var drag_rect = KeyboardViewModel.FooterViewModel.DragHandleRect;
                        b = ab;
                        t = b - short_side;

                        // bottom left
                        l = al + (aw / 4) - (long_side / 2);
                        r = l + long_side;
                        yield return new Rect(l, t, r - l, b - t);

                        // bottom right
                        l = ar - (aw / 4) - (long_side / 2);
                        r = l + long_side;
                        break;
                }
                yield return new Rect(l, t, r - l, b - t);
            }
        }
        public IEnumerable<Rect> HandleRects =>
            GetHandleRects(Math.Min(MinSize.Width,MinSize.Height) / 3, FloatPad);

        public IEnumerable<Rect> HandleHitRects =>
            GetHandleRects(Math.Min(MinSize.Width, MinSize.Height), FloatPad * 6).Select(x=>x.Move(-FloatPad, -FloatPad));

        public IEnumerable<CornerRadius> HandleCornerRadii {
            get {
                double cr = Parent.CommonCornerRadius.TopLeft;
                for (int i = 0; i < 5; i++) {
                    double tl = 0, tr = 0, br = 0, bl = 0;
                    switch(i) {
                        case 0:
                            // left
                            tr = br = cr;
                            break;
                        case 1:
                            // top
                            bl = br = cr;
                            break;
                        case 2:
                            // right
                            tl = bl = cr;
                            break;
                        case 3:
                        case 4:
                            // bottom
                            tl = tr = cr;
                            break;
                    }
                    yield return new CornerRadius(tl, tr, br, bl);
                }
            }
        }
        #endregion

        #region State
        int TouchOwnerId { get; set; } = -1;
        string TouchId { get; set; }
        bool IsActive =>
            IsScaling || IsMoving;
        bool IsMoving { get; set; }
        public bool IsScaling { get; private set; }
        public override bool IsVisible =>
            Parent.IsFloatingLayout;
        public bool IsHandlesVisible => 
            !IsMoving && 
            (
            IsHandlePreviewing ||
            TouchId != null
            );
        bool IsHandlePreviewing { get; set; }
        int HandlePreviewMs =>
            1_500;
        Point? CurScaleDelta { get; set; }
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events

        public event EventHandler OnFloatPositionChangeBegin;
        public event EventHandler OnFloatPositionChanged;
        public event EventHandler OnFloatPositionChangeEnd;

        public event EventHandler OnFloatScaleChangeBegin;
        public event EventHandler OnFloatScaleChanged;
        public event EventHandler OnFloatScaleChangeEnd;
        #endregion

        #region Constructors
        public FloatContainerViewModel(KeyboardViewModel parent) {
            Parent = parent;
        }
        #endregion

        #region Input
        public bool HandleTouch(Touch touch, TouchEventType eventType) {
            if(!IsVisible) {
                return false;
            }
            bool handled = false;
            switch(eventType) {
                case TouchEventType.Press:
                    if(TouchId != null ||
                        !IsHandlesVisible ||
                        GetHandleUnderLoc(touch.Location) is not { } handle_idx) {
                        if(IsHandlesVisible) {
                            StartHandlePreview(true);
                        }
                        break;
                    }
                    TouchId = touch.Id;
                    TouchOwnerId = handle_idx;
                    StartFloatScale();
                    handled = true;
                    break;
                case TouchEventType.Move:
                    handled = TouchId == touch.Id;
                    if(!handled) {
                        break;
                    }
                    var diff = touch.RawLocation - touch.LastRawLocation;

                    // only track handle axis
                    if(TouchOwnerId == 0 || TouchOwnerId == 2) {
                        diff = new Point(diff.X, 0);
                    } else {
                        diff = new Point(0, diff.Y);
                    }

                    if (CurScaleDelta is not { } csd) {
                        csd = diff;
                    } else {
                        csd += diff;
                    }
                    CurScaleDelta = csd;

                    if (csd.Length() < 5) {
                        break;
                    }
                    ScaleFloatDimensions(csd.X, csd.Y,TouchOwnerId);
                    CurScaleDelta = null;
                    break;
                case TouchEventType.Release:
                    handled = TouchId == touch.Id;
                    if(!handled) {
                        break;
                    }
                    ResetState();
                    FinishFloatScale();
                    break;
            }

            return handled;
        }

        #endregion

        #region Lifecycle

        public Size ResetLayout(bool invalidate) {
            double new_w = DefaultFloatScaleX * KeyboardViewModel.TotalDockedSize.Width;
            double new_h = DefaultFloatScaleY * KeyboardViewModel.TotalDockedSize.Height;
            var work_rect = InputConnection.ScaledWorkAreaRect;
            double x = work_rect.Left + (work_rect.Width / 2) - (new_w / 2);
            double y = work_rect.Top + (work_rect.Height / 2) - (new_h / 2);

            FloatScale = new Point(DefaultFloatScaleX, DefaultFloatScaleY);
            FloatPosition = new Point(x, y);
            var default_size = new Size(new_w, new_h);
            Parent.SetDesiredSize(default_size, false);

            if(invalidate) {
                OnFloatPositionChanged?.Invoke(this, EventArgs.Empty);
                OnFloatScaleChanged?.Invoke(this, EventArgs.Empty);
                this.Renderer.MeasureFrame(true);
            }
            return default_size;
        }
        public void ResetState() {
            IsMoving = false;
            IsScaling = false;
            TouchOwnerId = -1;
            TouchId = null;
        }

        public void RefreshFloatScale() {
            if (!Parent.IsFloatingLayout) {
                FloatScale = new(1, 1);
                return;
            }
            // BUG not sure why but Y scale ends up being ~0.9 by default and it needs to be accurate
            double sx = FloatScreenRect.Width / Parent.TotalDockedSize.Width;
            double sy = FloatScreenRect.Height / Parent.TotalDockedSize.Height;
            FloatScale = new Point(sx, sy);
        }
        public void ResetFloatProperties() {
            if (Parent.IsFloatingLayout) {
                return;
            }
            FloatScale = new(1, 1);
            FloatPosition = new();
        }

        public Size InitFloatLayout(Size dockedSize, bool isInit) {
            if (Parent.CanInitiateFloatLayout) {
                if (isInit) {
                    return ResetLayout(false);
                }
                return new Size(dockedSize.Width * FloatScale.X, dockedSize.Height * FloatScale.Y);
            }
            double scale_x = InputConnection.MaxScaledSize.Width / dockedSize.Width;
            double scale_y = InputConnection.MaxScaledSize.Height / dockedSize.Height;
            FloatScale = new Point(scale_x, scale_y);
            FloatPosition = new();
            return InputConnection.MaxScaledSize;
        }
        #endregion

        #region Move
        public void StartFloatMove() {
            IsMoving = true;
            this.Renderer.MeasureFrame(true);
            OnFloatPositionChangeBegin?.Invoke(this, EventArgs.Empty);
        }
        public void FinishFloatMove() {
            IsMoving = false;
            StartHandlePreview(false);
            OnFloatPositionChangeEnd?.Invoke(this, EventArgs.Empty);
        }
        public bool MoveFloatLocation(double dx, double dy) {
            var nr = ClampWithin(FloatScreenRect.Move(dx, dy), InputConnection.ScaledWorkAreaRect.ToBounds());

            if (FloatPosition == nr.Position) {
                return false;
            }
            FloatPosition = nr.Position;

            //MpConsole.WriteLine($"[SCREEN] Win Pos: {FloatPosition}");
            OnFloatPositionChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        #endregion

        #region Scale
        void StartHandlePreview(bool fadeOut) {
            IsHandlePreviewing = !fadeOut;
            this.Renderer.MeasureFrame(true);
            //if (!fadeOut) {
            //    IsHandlePreviewing = true;
            //}
            //double start_a = fadeOut ? 255 : 0;
            //double end_a = fadeOut ? 0 : 255;

            //double dur = 500;

            //DateTime st = DateTime.Now;

            //var Timer = InputConnection.AnimationTimer;
            //Timer.Start(this, Update);

            //void Update() {
            //    HandleBgAlpha = (byte)start_a.Lerp(end_a, (DateTime.Now - st).TotalMilliseconds / dur);
            //    this.Renderer.MeasureFrame(true);
            //    if (HandleBgAlpha == (byte)end_a) {
            //        // new touch, cancel
            //        IsHandlePreviewing = !fadeOut;
            //        Timer.Stop(this);
            //        return;
            //    }
            //    MpConsole.WriteLine($"ALPHA:{HandleBgAlpha}");
            //}
        }
        public void StartFloatScale() {
            IsScaling = true;
            this.Renderer.MeasureFrame(true);
            OnFloatScaleChangeBegin?.Invoke(this, EventArgs.Empty);
        }
        public void FinishFloatScale() {
            ResetState();
            StartHandlePreview(true);
            OnFloatScaleChangeEnd?.Invoke(this, EventArgs.Empty);
        }

        bool ScaleFloatDimensions(double dx, double dy, int sideId) {
            // first move by dx, y is pinned during scaling
            var last_scr_rect = FloatScreenRect;
            double x = last_scr_rect.X;
            double y = last_scr_rect.Y;
            double w = last_scr_rect.Width;
            double h = last_scr_rect.Height;

            switch (sideId) {
                case 0:
                    // left
                    x += dx;
                    w -= dx;
                    break;
                case 1:
                    // top
                    y += dy;
                    h -= dy;
                    break;
                case 2:
                    // right
                    w += dx;
                    break;
                case 3:
                case 4:
                    // bottom
                    h += dy;
                    break;
            }

            double clamped_w = Math.Clamp(w, MinSize.Width, MaxSize.Width);
            if (clamped_w != w && sideId == 0) {
                // cancel translate x
                x -= dx;
            }

            double clamped_h = Math.Clamp(h, MinSize.Height, MaxSize.Height);
            if (clamped_h != h && sideId == 1) {
                y -= dy;
            }
            w = clamped_w;
            h = clamped_h;
            var new_scr_rect = new Rect(x, y, w, h);
            new_scr_rect = ClampWithin(new_scr_rect, InputConnection.ScaledWorkAreaRect);
            if (new_scr_rect == last_scr_rect) {
                return false;
            }
            //MpConsole.WriteLine($"SIDE: {sideId} {new_scr_rect}");

            double sx = new_scr_rect.Width / Parent.TotalDockedSize.Width;
            double sy = new_scr_rect.Height / Parent.TotalDockedSize.Height;

            FloatScale = new Point(sx, sy);
            FloatPosition = new_scr_rect.Position;
            Parent.SetDesiredSize(new_scr_rect.Size, false);

            OnFloatPositionChanged?.Invoke(this, EventArgs.Empty);
            OnFloatScaleChanged?.Invoke(this, EventArgs.Empty);
            this.Renderer.MeasureFrame(true);
            return true;
        }
        #endregion

        #region Helpers
        Rect ClampWithin(Rect nr, Rect war) {
            if (nr.Left < war.Left) {
                nr = nr.Move(war.Left - nr.Left, 0);
            }
            if (nr.Top < war.Top) {
                nr = nr.Move(0, war.Top - nr.Top);
            }
            if (nr.Right > war.Right) {
                nr = nr.Move(war.Right - nr.Right, 0);
            }
            if (nr.Bottom > war.Bottom) {
                nr = nr.Move(0, war.Bottom - nr.Bottom);
            }
            return nr;
        }

        int? GetHandleUnderLoc(Point p) {
            var test = HandleHitRects.ToList();
            var ht = HandleHitRects.FirstOrDefault(x => x.Contains(p));
            int idx = HandleHitRects.IndexOf(ht);
            return idx >= 0 ? idx : null;
        }
        #endregion
    }
}
