using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia;
using MonkeyBoard.Bridge;
using MonkeyBoard.Common;
using Point = Avalonia.Point;

namespace MonkeyBoard.Android {
    public class AdEmojiSearchPopupView : AdCustomViewGroup, ITranslatePoint {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region ITranslatePoint Implementation
        Point ITranslatePoint.TranslatePoint(Point p) {
            return new Point(p.X, p.Y - Bounds.Height());
        }
        #endregion
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.TotalRect.ToRectF().ToBounds();

            base.MeasureFrame(invalidate);
        }
        #endregion

        #region Properties

        #region View Models
        public new EmojiSearchViewModel DC { get; private set; }
        #endregion

        #region Views
        public AdEmojiSearchEditText SearchEditText { get; set; }
        #endregion

        protected override bool RequiresHwAccel => true;

        #endregion

        #region Events
        #endregion

        #region Constructors
        public AdEmojiSearchPopupView(Context context, Paint sharedPaint, EmojiSearchViewModel dc) : base(context, sharedPaint) {
            this.DC = dc;
            this.DC.SetRenderContext(this);

            // BUG this view doesn't unless is
            this.SetLayerType(LayerType.Hardware, sharedPaint);

            SetOnTouchListener(Context as AdInputMethodService);
            var pur = DC.TotalRect.ToRectF();
            Frame = pur.ToBounds();
            float y_offset = pur.Height();
            // search box
            var et = new AdEmojiSearchEditText(Context, sharedPaint, DC).SetDefaultProps();
            et.ClipToOutline = true;
            et.SetMaxLines(1);
            //et.RoundBgCorners(5, KeyboardPalette.P[PaletteColorType.EmojiSearchBgHex.ToAdColor());
            et.SetPadding(
                DC.SearchBoxPadding.Left.UnscaledI(),
                DC.SearchBoxPadding.Top.UnscaledI(),
                DC.SearchBoxPadding.Right.UnscaledI(),
                DC.SearchBoxPadding.Bottom.UnscaledI()
                );

            et.Frame = DC.SearchBoxRect.ToRectF().Move(0, y_offset);
            et.Hint = DC.PlaceholderText;
            et.ImeOptions = ImeAction.Done;
            et.SetTextColor(DC.EmojiSearchBoxFgHexColor.ToAdColor());
            et.Background = null;
            et.SetBackgroundColor(DC.EmojiSearchBoxBgHexColor.ToAdColor());
            if(MpAdHostBridgeBase.Instance is { } hbb &&
                hbb.HostResources is { } hr) {
                et.Typeface = Context.Resources.GetFont(hr.DefaultFontResourceId);//Resource.Font.Nunito_Regular);
            }

            et.TextSize = DC.SearchBoxFontSize.UnscaledF();
            SearchEditText = et;

            // compl view
            var compl = new AdAutoCompleteView(Context, sharedPaint, DC.EmojiAutoCompleteViewModel);
            compl.SetBackgroundColor(DC.EmojiSearchBoxBgHexColor.ToAdColor());
            // inner cntr

            AddView(compl);
            AddView(SearchEditText);
            compl.SetZ(1);
            SearchEditText.SetZ(5);
        }
        #endregion

        #region Public Methods
        public void Unload() {
            this.SetOnTouchListener(null);
            if(SearchEditText != null) {
                SearchEditText.Unload();
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {

            if(!DC.IsVisible) {
                return;
            }

            base.OnDraw(canvas);
            SharedPaint.Color = DC.EmojiSearchBoxBgHexColor.ToAdColor();

            float y_offset = DC.TotalRect.Height.UnscaledF();

            // overlay
            SharedPaint.Color = DC.OverlayBgHexColor.ToAdColor();
            canvas.DrawRect(DC.OverlayRect.ToRectF().Move(0, y_offset), SharedPaint);

        }
        #endregion

        #region Private Methods
        #endregion\

    }
}