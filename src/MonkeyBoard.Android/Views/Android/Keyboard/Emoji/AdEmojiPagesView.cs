using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.Emoji2.Text;
using Avalonia;
using Avalonia.Layout;
using Java.Lang;
using MonkeyPaste.Common;
using MonkeyBoard.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPaint = Android.Graphics.Paint;

namespace MonkeyBoard.Android {
    public class AdEmojiPagesView : AdCustomView {
        #region Private Variables
        object _createEmojiBmpLock = new object();
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.TotalRect.ToRectF();
            base.MeasureFrame(invalidate);
        }

        public override void RenderFrame(bool invalidate) {
            if(invalidate) {
                this.RequestLayout();
            }
            base.RenderFrame(invalidate);
        }
        #endregion

        #region Properties

        #region Members
        EmojiCompat EmojiHelper { get; set; }
        #endregion

        #region View Models
        public new EmojiPagesViewModel DC { get; private set; }
        #endregion

        #region Views
        Dictionary<EmojiPageType, Bitmap> EmojiPageBitmaps { get; set; } = [];

        List<AdCustomPopupWindow> PopupWindows { get; set; } = [];
        #endregion

        #region Layout
        public float EmojiSearchPopupHeight { get; private set; }
        #endregion

        #region State
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors

        public AdEmojiPagesView(Context context, GPaint paint, EmojiPagesViewModel dc) : base(context, paint) {
            DC = dc;
            DC.SetRenderContext(this);
            EmojiHelper = EmojiCompat.Init(context);

            DC.OnEmojisLoaded += DC_OnEmojisLoaded;

            DC.OnShowEmojiPopup += DC_OnShowPopupKeys;
            DC.OnHideEmojiPopup += DC_OnHideEmojiPopup;

            DC.EmojiSearchViewModel.OnShowEmojiSearch += EmojiSearchViewModel_OnShowEmojiSearch;
            DC.EmojiSearchViewModel.OnHideEmojiSearch += EmojiSearchViewModel_OnHideEmojiSearch;

            DC.KeyboardViewModel.FloatContainerViewModel.OnFloatScaleChangeEnd += FloatContainerViewModel_OnFloatScaleChangeEnd;
        }

        private void FloatContainerViewModel_OnFloatScaleChangeEnd(object sender, EventArgs e) {

            DC.Init();
            //ResetState();
            //if(DC.IsVisible) {
            //    DC.Init();
            //    this.RenderFrame(true);
            //}
        }


        #endregion

        #region Public Methods
        public void ResetRenderer() {
            DC.SetRenderContext(this);
            DC.EmojiFooterMenuViewModel.SetRenderContext(this);
            DC.EmojiFooterMenuViewModel.Items.ToList().ForEach(x => x.SetRenderContext(this));
            DC.EmojiPages.ToList().ForEach(x => x.SetRenderContext(this));

            PopupWindows.ForEach(x => x.Dismiss());
            PopupWindows.Clear();
        }

        public void Unload() {
            PopupWindows.ForEach(x => x.Dismiss());
            PopupWindows.Clear();

            if(DC != null) {
                DC.OnEmojisLoaded -= DC_OnEmojisLoaded;

                DC.OnShowEmojiPopup -= DC_OnShowPopupKeys;
                DC.OnHideEmojiPopup -= DC_OnHideEmojiPopup;

                DC.EmojiSearchViewModel.OnShowEmojiSearch -= EmojiSearchViewModel_OnShowEmojiSearch;
                DC.EmojiSearchViewModel.OnHideEmojiSearch -= EmojiSearchViewModel_OnHideEmojiSearch;
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if(DC == null || !DC.IsVisible) {
                return;
            }
            if(!EmojiPageBitmaps.Any()) {
                CreateEmojiBitmaps();
            }

            SharedPaint.Color = DC.EmojiPagesBgHexColor.ToAdColor();
            canvas.DrawRect(Bounds, SharedPaint);

            SharedPaint.Color = Color.White;
            SharedPaint.TextAlign = GPaint.Align.Center;
            SharedPaint.TextSize = DC.EmojiFontSize.UnscaledF();
            var footer_rect = DC.EmojiFooterMenuViewModel.EmojiFooterRect.ToRectF();
            float scroll_x = DC.ScrollOffsetX.UnscaledF();
            float scroll_y = DC.SelectedEmojiPage == null ? 0 : DC.SelectedEmojiPage.ScrollOffsetY.UnscaledF();

            // emoji pressed bg rect
            if(DC.SelectedEmojiPage != null && DC.SelectedEmojiPage.PressedEmojiKeys.FirstOrDefault() is { } pressed_evm) {
                var evm_rect = pressed_evm.EmojiRect.ToRectF();
                evm_rect = evm_rect.Move(0, -scroll_y);
                SharedPaint.Color = pressed_evm.EmojiBgHexColor.ToAdColor();
                canvas.DrawRect(evm_rect, SharedPaint);
            }

            // emoji page bmp
            var visible_pages = DC.EmojiPages.Where(x => x.IsVisible);
            foreach(var visible_page in visible_pages) {
                float page_scroll_x = scroll_x - visible_page.PageRect.Left.UnscaledF();
                float page_scroll_y = visible_page.ScrollRect.Top.UnscaledF();
                if(!EmojiPageBitmaps.TryGetValue(visible_page.EmojiPageType, out var bmp)) {
                    DrawEmojiPage(canvas, visible_page.SortedEmojiKeys, page_scroll_x, page_scroll_y, Bounds.Top, Bounds.Bottom);
                    // DrawEmojiPage(canvas, visible_page.SortedEmojiKeys,scroll_x, page_scroll_y, Bounds.Top, Bounds.Bottom);
                } else {
                    float clip_h = Bounds.Height() - footer_rect.Height();
                    var bmp_rect = visible_page.PageRect.ToRectF().ToBounds();
                    float sl = bmp_rect.Left;
                    float st = scroll_y;
                    float sr = bmp_rect.Right;
                    float sb = st + clip_h;
                    var src_rect = new RectF(sl, st, sr, sb).ToRect();
                    var scroll_rect = visible_page.ScrollRect.ToRectF();
                    float dl = scroll_rect.Left;
                    float dt = 0;// scroll_rect.Top;
                    float dr = scroll_rect.Right;
                    float db = dt + clip_h;
                    var dst_rect = new RectF(dl, dt, dr, db);
                    SharedPaint.Color = Color.White;

                    canvas.DrawBitmap(bmp, src_rect, dst_rect, SharedPaint);
                }
            }

            // footer bg
            SharedPaint.Color = DC.EmojiFooterMenuViewModel.EmojiFooterMenuBgHexColor.ToAdColor();
            Color fg_color = DC.EmojiFooterMenuViewModel.EmojiFooterMenuFgHexColor.ToAdColor();
            canvas.DrawRect(footer_rect, SharedPaint);

            // footer item bg
            for(int i = 0; i < DC.EmojiFooterMenuViewModel.MenuItemCount; i++) {
                var evm = DC.EmojiFooterMenuViewModel.Items[i];
                if(evm.MenuItemBgHexColor == null) {
                    continue;
                }
                var rect = DC.EmojiFooterMenuViewModel.EmojiFooterItemRects[i].ToRectF().Move(0, footer_rect.Top);
                SharedPaint.Color = evm.MenuItemBgHexColor.ToAdColor();
                canvas.DrawRect(rect, SharedPaint);

            }
            // footer selection bg
            SharedPaint.Color = DC.EmojiFooterMenuViewModel.SelectedMenuItemBgHexColor.ToAdColor();
            var sel_rect = DC.EmojiFooterMenuViewModel.SelectionRect.ToRectF();
            canvas.DrawRect(sel_rect, SharedPaint);

            // footer item fg
            for(int i = 0; i < DC.EmojiFooterMenuViewModel.MenuItemCount; i++) {
                var evm = DC.EmojiFooterMenuViewModel.Items[i];
                if(evm.IconSourceObj is string text) {
                    var loc = DC.EmojiFooterMenuViewModel.GetMenuItemTextLoc(i, text).ToPointF();
                    var rect = DC.EmojiFooterMenuViewModel.EmojiFooterItemRects[i].ToRectF().Move(0, footer_rect.Top);
                    SharedPaint.Color = fg_color;
                    canvas.DrawText(text, loc.X + rect.Left, loc.Y + rect.Top, SharedPaint);
                }

            }
        }
        #endregion

        #region Private Methods

        #region Search

        private void EmojiSearchViewModel_OnShowEmojiSearch(object sender, EventArgs e) {
            Handler.Post(() => AdEmojiSearchPopupWindow.Show(this, SharedPaint, DC.EmojiSearchViewModel));
        }
        private void EmojiSearchViewModel_OnHideEmojiSearch(object sender, EventArgs e) {
            Handler.Post(() => AdEmojiSearchPopupWindow.Hide());
        }


        #endregion

        #region Popups

        private void DC_OnShowPopupKeys(object sender, EmojiKeyViewModel emvm) {
            if(Context is not AdInputMethodService ims ||
                AdCustomPopupWindow.Create(this) is not { } puw) {
                return;
            }

            PopupWindows.Add(puw);
            var puv = new AdEmojiPopupView(Context, SharedPaint, emvm);
            puw.ContentView = puv;
            //puv.MeasureFrame(false);
            puw.Width = (int)puv.Frame.Width();
            puw.Height = (int)puv.Frame.Height();

            var pur = emvm.PopupContainerRect.ToRectF();
            View anchor = this;
            if(DC.KeyboardViewModel.CanInitiateFloatLayout &&
                DC.KeyboardViewModel.IsFloatingLayout &&
                ims.KeyboardContainerView.FloatView is { } fv &&
                fv.FloatWindow is { } fw &&
                fw.AnchorView is { } floatAnchor) {
                anchor = floatAnchor;
                var p = fv.GetTranslatedPopupPosition(pur.Position());
                var float_loc = DC.KeyboardViewModel.FloatContainerViewModel.FloatPosition.ToPointF();
                pur = pur.Place(p.X + float_loc.X, p.Y + float_loc.Y);
            }
            puw.ShowAtLocation(anchor, GravityFlags.NoGravity, (int)pur.Left, (int)pur.Top);
            puw.Update((int)pur.Left, (int)pur.Top, puw.Width, puw.Height, true);
            puv.RenderFrame(true);
        }

        private void DC_OnHideEmojiPopup(object sender, EventArgs e) {
            foreach(var puw in PopupWindows) {
                if(puw.Tag is EmojiKeyViewModel emvm) {
                    emvm.SetRenderContext(this);
                }
                puw.Dismiss();
                puw.Dispose();
            }
            PopupWindows.Clear();
        }
        private void DC_OnEmojisLoaded(object sender, EventArgs e) {
            ResetState();
        }

        void ResetState() {
            Handler.Post(() => {
                EmojiPageBitmaps.Clear();
                CreateEmojiBitmaps();
                ResetRenderer();
                MpConsole.WriteLine($"Emoji bmps reset");
            });
        }
        #endregion

        #region Emoji Bitmaps

        void CreateEmojiBitmaps() {
            if(EmojiPageBitmaps.Any()) {
                return;
            }

            foreach(var pg_vm in DC.EmojiPages.ToList()) {
                if(pg_vm.EmojiPageType == EmojiPageType.Recents ||
                    EmojiPageBitmaps.ContainsKey(pg_vm.EmojiPageType)) {
                    // dont cache recents
                    continue;
                }
                var bmp_rect = pg_vm.PageRect.ToRect().ToBounds();
                var bmp = Bitmap.CreateBitmap(bmp_rect.Width(), bmp_rect.Height(), Bitmap.Config.Argb8888);
                var canvas = new Canvas(bmp);
                DrawEmojiPage(canvas, pg_vm.EmojiKeys, 0, 0, 0, float.MaxValue);
                EmojiPageBitmaps.Add(pg_vm.EmojiPageType, bmp);
            }
        }

        void DrawEmojiPage(Canvas canvas, IEnumerable<EmojiKeyViewModel> emojis, float scroll_x, float scroll_y, float min_y, float max_y) {
            float fontSize = DC.EmojiFontSize.UnscaledF();
            //var tp = new TextPaint(SharedPaint);
            //canvas.Save();
            //canvas.Translate(scroll_x, scroll_y);
            foreach(var evm in emojis) {
                var evm_rect = evm.EmojiRect.ToRectF().Move(-scroll_x, scroll_y);
                if(evm_rect.Bottom < min_y || evm_rect.Top > max_y) {
                    continue;
                }

                if(evm.HasPopup) {
                    SharedPaint.Color = evm.PopupHintBgHexColor.ToAdColor();
                    var p = evm.PopupHintTrianglePoints
                        .Select(x => x.ToPointF())
                        .Select(x => new PointF(x.X + evm_rect.Left, x.Y + evm_rect.Top))
                        .ToArray();

                    Path path = new Path();
                    path.MoveTo(p[0].X, p[0].Y);
                    path.LineTo(p[1].X, p[1].Y);
                    path.LineTo(p[2].X, p[2].Y);
                    path.LineTo(p[0].X, p[0].Y);
                    path.Close();
                    canvas.DrawPath(path, SharedPaint);
                }
                AdHelpers.DrawAlignedText(
                    canvas,
                    SharedPaint,
                    evm_rect,
                    evm.PrimaryValue,
                    fontSize,
                    Color.White,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center);
                //AdHelpers.DrawEmoji(canvas, SharedPaint, evm.PrimaryValue, x, y, fontSize);

            }
            //canvas.Restore();
        }


        #endregion

        #endregion
    }
}
