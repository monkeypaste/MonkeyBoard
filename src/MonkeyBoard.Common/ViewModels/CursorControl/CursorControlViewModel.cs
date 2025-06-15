using Avalonia;
using Avalonia.Media;
using MonkeyPaste.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class CursorControlViewModel : FrameViewModelBase {
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
        public new KeyboardViewModel Parent { get; set; }
        #endregion

        #region Appearance
        public double CursorControlOpacity =>
             IsCursorControlActive ? 1 : 0;

        public string TitleText =>
            ResourceStrings.U["CcLabel"].value;
        public string SelectAllText =>
            ResourceStrings.U["CcSelectAllTitle"].value;
        public object TitleIconSourceObj =>
            "👆";

        public string BgHexColor =>
            KeyboardPalette.P[PaletteColorType.CursorControlBg];
        public string TitleFgHexColor =>
            IsWordSelect ? "#FFFF0000" : KeyboardPalette.P[PaletteColorType.CursorControlFg];
        public string SelectAllBgHexColor =>
            IsCursorControlSelectAllHovering ?
                KeyboardPalette.P[PaletteColorType.CursorControlSelectAllOverBg] :
                KeyboardPalette.P[PaletteColorType.CursorControlSelectAllBg];

        public string SelectAllFgHexColor =>
            IsCursorControlSelectAllHovering ?
                KeyboardPalette.P[PaletteColorType.CursorControlSelectAllOverFg] :
                KeyboardPalette.P[PaletteColorType.CursorControlSelectAllFg];
        public double TitleFontSize => 18;
        public double SelectAllFontSize => 24;
        #endregion

        #region Layout

        public Rect CursorControlRect { get; private set; }
        public Rect SelectAllRect { get; private set; }
        public Point SelectAllTextLoc { get; private set; }
        public CornerRadius SelectAllCornerRadius => 
            KeyboardViewModel.CommonCornerRadius;

        public Rect TitleTextRect { get; private set; }
        public Rect TitleIconRect { get; private set; }
        public Point TitleTextLoc { get; private set; }
        public Point TitleIconLoc { get; private set; }

        Touch ActiveTouch { get; set; }
        #endregion

        #region State
        public override bool IsVisible =>
            IsCursorControlActive;
        public bool IsSelectAllVisible { get; private set; }
        bool IsWordSelect { get; set; }        
        double MinCursorControlDragDist => 10;
        public bool IsCursorControlActive { get; private set; }
        bool IsCursorControlSelectAllHovering { get; set; }
        Size GlyphSize { get; set; }
        #endregion

        #region Model
        #endregion

        #endregion

        #region Events
        public event EventHandler OnShowCursorControl;
        public event EventHandler OnHideCursorControl;
        #endregion

        #region Constructors
        public CursorControlViewModel(KeyboardViewModel parent) {
            Parent = parent;
        }
        #endregion

        #region Public Methods
        public void InitLayout() {
            if(Parent.InputConnection.TextTools is not { } tm) {
                return;
            }
            // cntr
            double w = Parent.TotalRect.Width;
            double h = Parent.TotalRect.Height;
            CursorControlRect = new Rect(0, 0, w, h);

            var sel_all_size = tm.MeasureText(SelectAllText, SelectAllFontSize, out _, out _);
            var title_size = tm.MeasureText(TitleText, TitleFontSize, out _, out _);

            double y_pad = 15;
            double text_x = w / 2;            

            // title in middle
            double title_y = (h / 2) + y_pad;
            TitleTextLoc = new Point(text_x, title_y);
            TitleTextRect = new Rect(new(0,title_y), title_size);

            double title_icon_y = title_y + title_size.Height + y_pad;
            TitleIconLoc = new Point(text_x, title_icon_y);
            TitleIconRect = new Rect(new(0,title_icon_y), title_size);

            // select all near top
            double select_all_y = (title_y / 2) + y_pad;
            SelectAllTextLoc = new Point(text_x, select_all_y);

            double ccsar_w = w / 2;
            double ccsar_h = h / 5;
            double ccsar_x = SelectAllTextLoc.X - (ccsar_w / 2d);
            double ccsar_y = SelectAllTextLoc.Y - (ccsar_h / 2d) - (sel_all_size.Height/2d);
            SelectAllRect = new Rect(ccsar_x, ccsar_y, ccsar_w, ccsar_h);
        }
        public void ResetState() {
            IsWordSelect = false;
            ActiveTouch = null;
            IsCursorControlActive = false;
            IsCursorControlSelectAllHovering = false;
        }

        public bool CheckCanCursorControlBeEnabled(Touch touch, KeyViewModel kvm, bool isHold) {
            if (kvm == null ||
                !kvm.IsSpaceBar ||
                kvm.IsSoftPressed ||
                //!touch.IsOwner(kvm) ||
                kvm.TouchId != touch.Id ||
                !KeyboardViewModel.CanCursorControlBeEnabled ||
                IsCursorControlActive) {
                return false;
            }
            if (isHold) {
                return true;
            }
            return Touches.Dist(touch.Location, touch.PressLocation) >= MinCursorControlDragDist;
        }
        public void StartCursorControl(Touch touch) {
            if(OperatingSystem.IsIOS() && 
                KeyboardViewModel.CurTextInfo is { } cti &&
                cti.Text.Contains('\n')) {
                IsSelectAllVisible = false;
            } else {
                IsSelectAllVisible = true;// InputConnection.IsReceivingCursorUpdates;
            }
            

            IsWordSelect =
                KeyboardViewModel.IsDoubleTapCursorControlEnabled &&
                InputConnection.IsReceivingCursorUpdates &&
                KeyboardViewModel.SpacebarKey.LastPressDt - KeyboardViewModel.SpacebarKey.LastReleaseDt <= TimeSpan.FromMilliseconds(KeyboardViewModel.MaxDoubleTapSpaceForPeriodMs);
            if(IsWordSelect) {
                KeyboardViewModel.UndoLeadingWithText(InputConnection.OnTextRangeInfoRequest(), Parent.LastSpaceReplacedText, KeyConstants.SPACE_STR);
            }
            IsCursorControlActive = true;
            StartTouchTimer(touch);
            OnShowCursorControl?.Invoke(this, EventArgs.Empty);
            Renderer.RenderFrame(true);
        }
        public void StopCursorControl(Touch touch) {
            if(IsCursorControlSelectAllHovering) {
                KeyboardViewModel.DoSelectAll();
            } else if(IsWordSelect) {
                KeyboardViewModel.DoSelectCaretWord();
            }
            ResetState();
            if (Parent.SpacebarKey is { } sb_kvm) {
                sb_kvm.SetPressed(false, touch);
            }
            OnHideCursorControl?.Invoke(this, EventArgs.Empty);
            Renderer.RenderFrame(true);
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods


        void StartTouchTimer(Touch touch) {
            if(touch == null) {
                return;
            }
            ActiveTouch = touch;
            if(GlyphSize == default && InputConnection.TextTools is { } tt) {
                // measure size of 12 point character to gauge insert delta
                GlyphSize = tt.MeasureText("A", 12, out _, out _);
            }
            DateTime? last_update_dt = null;
            TimeSpan update_span = TimeSpan.FromMilliseconds(150);
            if (ActiveTouch == null) {
                return;
            }
            Point last_update_loc = ActiveTouch.Location;

            // sensitivity is val 1-100 w/ default at 50 so balance so 50 to be actual glyph size
            double multiplier = (KeyboardViewModel.CursorControlSensitivity + 50) / 100d;
            double min_dist = new Point(GlyphSize.Width, GlyphSize.Height).Length() / (multiplier * multiplier);

            InputConnection.MainThread.Post(async () => {
                
                while (ActiveTouch != null) {
                    if (last_update_dt is { } last_dt && DateTime.Now - last_dt < update_span) {
                        await Task.Delay(10);
                        continue;
                    }
                    last_update_dt = DateTime.Now;

                    CheckForSelectAll(ActiveTouch);

                    int dx = 0;
                    int dy = 0;

                    double cur_dist = ActiveTouch.Location.Distance(last_update_loc);
                    if (cur_dist < min_dist) {
                        // NOTE max_extent_dist is kinda arbitrary,
                        // my S9 has a beveled side so you can't
                        // really get to the edge but 
                        double max_extent_dist = min_dist * 3;
                        // check for extent auto scroll

                        double x = ActiveTouch.Location.X;
                        double l = CursorControlRect.Left;
                        double r = CursorControlRect.Right;
                        if (x - l <= max_extent_dist) {
                            dx = -1;
                        } else if (r - x <= max_extent_dist) {
                            dx = 1;
                        } else {
                            continue;
                        }                        
                    } else {
                        int factor = Math.Max(1, (int)(cur_dist / min_dist));
                        switch (ActiveTouch.Location.GetDirection(last_update_loc)) {
                            case VectorDirection.Up:
                                dy = -factor;
                                break;
                            case VectorDirection.Down:
                                dy = factor;
                                break;
                            case VectorDirection.Left:
                                dx = -factor;
                                break;
                            case VectorDirection.Right:
                                dx = factor;
                                break;
                            default:
                                continue;
                        }

                    }
                    last_update_loc = ActiveTouch.Location;

                    InputConnection.OnNavigate(dx, dy);
                }
            });
        }

        void StopTouchTimer() {
            ActiveTouch = null;
        }

        void CheckForSelectAll(Touch touch) {
            if(touch == null || !IsSelectAllVisible) {
                return;
            }
            bool was_sa_hovering = IsCursorControlSelectAllHovering;
            IsCursorControlSelectAllHovering = SelectAllRect.Contains(touch.Location);
            if (IsCursorControlSelectAllHovering != was_sa_hovering) {
                Renderer.PaintFrame(true);
            }
        }
        #endregion

        #region Commands
        #endregion
    }
}
