using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Avalonia;
using MonkeyPaste.Common;
using MonkeyBoard.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Android.Views.View;
using AvRect = Avalonia.Rect;
using GPaint = Android.Graphics.Paint;
using Rect = Android.Graphics.Rect;

namespace MonkeyBoard.Android {
    public class AdKeyboardView : AdCustomViewGroup,
        IOnCreateContextMenuListener,
        IMenuItemOnMenuItemClickListener,
        IFrameRenderer,
        ITextTools {
        #region Private Variables
        #endregion

        #region Constants

        #endregion

        #region Statics
        public static int DEFAULT_FONT_RES_ID => 0;// MpAdHostBridgeBase.Instance.HostResources.DefaultFontResourceId; //Resource.Font.Nunito_Regular;

        #endregion

        #region Interfaces

        #region IMenuItemOnMenuItemClickListener Implementation
        bool IMenuItemOnMenuItemClickListener.OnMenuItemClick(IMenuItem item) {
            bool handled = false;
            if(Context.GetSystemService(Context.ClipboardService) is not ClipboardManager cbm ||
                AdEmojiSearchPopupWindow.ContainerView is not { } cv ||
                cv.Descendants().OfType<AdEmojiSearchEditText>().FirstOrDefault() is not { } et) {
                return handled;
            }

            if(item.TitleFormatted.ToString() == ResourceStrings.U["CutLabel"].value) {
                cbm.PrimaryClip = ClipData.NewPlainText("search_text", et.SelectedText);
                et.SelectedText = string.Empty;
            } else if(item.TitleFormatted.ToString() == ResourceStrings.U["CopyLabel"].value) {
                cbm.PrimaryClip = ClipData.NewPlainText("search_text", et.SelectedText);
            } else if(item.TitleFormatted.ToString() == ResourceStrings.U["PasteLabel"].value &&
                      cbm.Text is { } paste_text) {
                et.SelectedText = paste_text;
            } else {
                handled = false;
            }
            return handled;
        }
        #endregion

        #region IOnCreateContextMenuListener Implementation

        void IOnCreateContextMenuListener.OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo) {
            string test = menu.GetType().ToString();

            menu.ClearHeader();
            menu.Clear();
            //menu.SetHeaderTitle("TEST MENU");

            IMenuItem cut_item = menu.Add(ResourceStrings.U["CutLabel"].value);
            IMenuItem copy_item = menu.Add(ResourceStrings.U["CopyLabel"].value);
            IMenuItem paste_item = menu.Add(ResourceStrings.U["PasteLabel"].value);

            cut_item.SetOnMenuItemClickListener(this);
            copy_item.SetOnMenuItemClickListener(this);
            paste_item.SetOnMenuItemClickListener(this);
        }
        #endregion

        #region ITextTools Implementation
        bool ITextTools.CanRender(string text) =>
            AdHelpers.CanShowEmoji(text, SharedPaint);
        Size ITextTools.MeasureText(string text, double scaledFontSize, out double ascent, out double descent) {
            /*
            maybe this can help with this madness? (fontMetric part)
            from https://stackoverflow.com/a/36771539/105028
            width = textView.getPaint().measureText(text);
            metrics = textView.getPaint().getFontMetrics();
            height = metrics.bottom - metrics.top;
            */

            var tb = new Rect();
            text = text ?? string.Empty;
            SharedPaint.TextAlign = GPaint.Align.Center;
            SharedPaint.TextSize = scaledFontSize.UnscaledF();
            SharedPaint.GetTextBounds(text, 0, text.Length, tb);
            ascent = SharedPaint.Ascent().ScaledD();
            descent = SharedPaint.Descent().ScaledD();
            return tb.ToAvRect().Size;

        }
        #endregion

        #region IKeyboardRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            //this.Visibility = (!DC.FloatContainerViewModel.IsPressed).ToViewState();
            RefreshLayers();
            base.LayoutFrame(invalidate);
        }

        public override void MeasureFrame(bool invalidate) {
            this.Frame = DC.TotalRect.ToRectF().Place(0, ContainerOffsetY);
            if(LastBgBmpScaledFrameSize is { } lfs &&
                ((int)lfs.Width != BgAnchorRect.Width() || (int)lfs.Height != BgAnchorRect.Height()) &&
                LastBgBmpBase64 is { } bg_base64) {
                // adjust bg for orientation change
                SetupCustomBgBmp(bg_base64);
            }
            base.MeasureFrame(invalidate);
        }
        public override void PaintFrame(bool invalidate) {
            if(KeyboardPalette.P.TryGetValue(PaletteColorType.CustomBgImgBase64, out var bg_base64) &&
                    LastBgBmpBase64 != bg_base64) {
                SetupCustomBgBmp(bg_base64);
            } else if(!KeyboardPalette.P.ContainsKey(PaletteColorType.CustomBgImgBase64) &&
                LastBgBmpBase64 != null) {
                // remove bg
                SetupCustomBgBmp(null);
            }
            base.PaintFrame(invalidate);
        }
        #endregion

        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        public new KeyboardViewModel DC { get; set; }
        FrameViewModelBase LastRefreshedPageViewModel { get; set; }
        #endregion

        #region Layout
        RectF BgAnchorRect => this.Frame;
        public float ContainerOffsetY { get; set; } = 0;
        #endregion

        #region Appearance
        #endregion
        #region State
        public bool IsFloating =>
            Parent is AdFloatInnerContainerView;
        string LastBgBmpBase64 { get; set; }
        MpSize LastBgBmpScaledFrameSize { get; set; }
        #endregion

        #region Views
        Bitmap CustomBgBmp { get; set; }
        PointF CustomBgBmpLoc { get; set; }
        public AdMenuView MenuView { get; set; }
        AdSpeechView SpeechView { get; set; }
        AdEmojiPagesView EmojiPagesView { get; set; }
        public AdKeyGridView KeyGridView { get; set; }
        AdCursorControlView CursorControlView { get; set; }
        AdFooterView FooterView { get; set; }
        public EditText HiddenEditTextForContextMenu { get; private set; }
        #endregion


        #endregion

        #region Events
        #endregion

        #region Constructors
        public AdKeyboardView(Context context) : base(context) {
            SharedPaint = SetupPaint(context);
        }

        #endregion

        #region Public Methods

        public void Init(IKeyboardInputConnection conn) {
            var kbs = KeyboardViewModel.GetTotalSizeByScreenSize(AdDeviceInfo.ScaledSize, AdDeviceInfo.IsPortrait);
            double scale = AdDeviceInfo.Scaling;
            DC = new KeyboardViewModel(conn, kbs / scale, scale, scale);
            DC.OnKeyLayoutChanged += DC_OnKeyLayoutChanged;

            if(Context is IOnTouchListener otl) {
                this.SetOnTouchListener(otl);
            }
            this.RemoveAllViews();

            MenuView = new AdMenuView(Context, SharedPaint, DC.MenuViewModel).SetDefaultProps("Menu");
            this.AddView(MenuView);

            KeyGridView = new AdKeyGridView(Context, SharedPaint, DC).SetDefaultProps("KeyboardGrid");
            this.AddView(KeyGridView);

            CursorControlView = new AdCursorControlView(Context, SharedPaint, DC.CursorControlViewModel).SetDefaultProps();
            this.AddView(CursorControlView);

            EmojiPagesView = new AdEmojiPagesView(Context, SharedPaint, DC.MenuViewModel.EmojiPagesViewModel).SetDefaultProps();
            this.AddView(EmojiPagesView);

            SpeechView = new AdSpeechView(Context, SharedPaint, DC.MenuViewModel.SpeechPageViewModel).SetDefaultProps();
            this.AddView(SpeechView);

            FooterView = new AdFooterView(Context, SharedPaint, DC.FooterViewModel);
            this.AddView(FooterView);

            HiddenEditTextForContextMenu = new EditText(Context);
            HiddenEditTextForContextMenu.Visibility = ViewStates.Invisible;
            HiddenEditTextForContextMenu.SetOnCreateContextMenuListener(this);
            this.AddView(HiddenEditTextForContextMenu);

            RemapRenderers();
        }


        public void Unload() {
            this.SetOnTouchListener(null);
            AdEmojiSearchPopupWindow.Hide();
            KeyGridView?.Unload();
            EmojiPagesView?.Unload();
            CursorControlView?.Unload();
            if(DC != null) {
                DC.OnKeyLayoutChanged -= DC_OnKeyLayoutChanged;
            }

        }
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {

            if(!DC.IsVisible) {
                canvas.DrawColor(Color.Transparent, BlendMode.Multiply);
                return;
            }
            //

            if(KeyboardPalette.P.TryGetValue(PaletteColorType.CustomBgColor, out var bg_hex)) {
                // custom color
                SharedPaint.Color = bg_hex.ToAdColor();
                canvas.DrawRect(this.BgAnchorRect, SharedPaint);
            }
            if(CustomBgBmp is { } bg_bmp) {
                // custom img
                SharedPaint.SetTint(null);
                canvas.DrawBitmap(bg_bmp, CustomBgBmpLoc.X, CustomBgBmpLoc.Y, SharedPaint);
            }
        }

        #endregion

        #region Private Methods

        private void DC_OnKeyLayoutChanged(object sender, EventArgs e) {
            MpConsole.WriteLine($"kb Layout changed");
            Handler.Post(() => {
                RemapRenderers();
            });

        }

        public void RemapRenderers() {
            // NOTE when flags change keys are recreated and renderers need to be re-assigned
            DC.SetRenderContext(this);
            KeyGridView.ResetRenderer();
            MenuView.ResetRenderer();
            EmojiPagesView.ResetRenderer();

            RenderFrame(true);
        }
        public static GPaint SetupPaint(Context context) {
            var paint = new GPaint();
            paint.TextAlign = GPaint.Align.Left;
            //paint.FakeBoldText = true;
            //paint.ElegantTextHeight = true;
            paint.AntiAlias = true;
            //paint.SetTypeface(context.Resources.GetFont(DEFAULT_FONT_RES_ID));
            return paint;
        }

        void RefreshLayers() {
            this.SetZ(-1);
            float max_z = 100;
            float top_z = 10;

            bool is_speech_vis = DC.MenuViewModel.SpeechPageViewModel.IsVisible;
            float speech_z = SpeechView.GetZ();

            bool is_emoji_vis = DC.MenuViewModel.EmojiPagesViewModel.IsVisible;
            bool is_emoji_sb_vis = DC.MenuViewModel.EmojiPagesViewModel.EmojiSearchViewModel.IsVisible;
            float emoji_page_z = EmojiPagesView.GetZ();

            float new_speech_z = is_speech_vis ? top_z : 0;
            float new_emoji_page_z = is_emoji_vis ? top_z : 0;
            float new_kb_page_z = is_emoji_vis || is_speech_vis ? 0 : top_z;
            bool is_kb_vis = new_kb_page_z != 0;

            bool is_loading = DC.IsBusy;
            bool is_change = is_loading || emoji_page_z != new_emoji_page_z || speech_z != new_speech_z;

            KeyGridView.SetZ(new_kb_page_z);
            CursorControlView.SetZ(new_kb_page_z);
            EmojiPagesView.SetZ(new_emoji_page_z);
            SpeechView.SetZ(new_speech_z);

            MenuView.SetZ(max_z);

            if(LastRefreshedPageViewModel != DC.VisiblePageViewModel) {
                //LastRefreshedPageViewModel = DC.VisiblePageViewModel;
                //RemapRenderers();
                SpeechView.Visibility = is_speech_vis.ToViewState();
                EmojiPagesView.Visibility = is_emoji_vis.ToViewState();
                KeyGridView.Visibility = is_kb_vis.ToViewState();

                SpeechView.Redraw(true);
                EmojiPagesView.Redraw(true);
                KeyGridView.Redraw(true);
            }
        }
        void SetupCustomBgBmp(string bg_base64) {
            LastBgBmpBase64 = bg_base64;
            if(LastBgBmpBase64 == null) {
                LastBgBmpScaledFrameSize = null;
                CustomBgBmp = null;
                return;
            }
            var raw_bmp = AdHelpers.LoadBitmap(bg_base64.ToBytesFromBase64String());

            var view_size = new MpSize(BgAnchorRect.Width(), BgAnchorRect.Height());
            var bmp_size = new MpSize(raw_bmp.Width, raw_bmp.Height);
            var scaled_size = bmp_size.ResizeKeepAspect(view_size.Width, view_size.Height);

            double bmp_x = BgAnchorRect.Left + (view_size.Width / 2) - (scaled_size.Width / 2);
            double bmp_y = BgAnchorRect.Top + (view_size.Height / 2) - (scaled_size.Height / 2);
            CustomBgBmpLoc = new PointF((float)bmp_x, (float)bmp_y);
            CustomBgBmp = raw_bmp.Scale((int)scaled_size.Width, (int)scaled_size.Height, false);

            LastBgBmpScaledFrameSize = view_size;
        }
        #endregion


    }
}