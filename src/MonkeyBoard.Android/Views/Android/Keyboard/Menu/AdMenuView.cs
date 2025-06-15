using Android.Content;
using Android.Graphics;
using Android.Views;
using MonkeyBoard.Common;
using System;
using System.Linq;
using static Android.Views.View;
using Color = Android.Graphics.Color;
using GPaint = Android.Graphics.Paint;
using PointF = Android.Graphics.PointF;
using Rect = Android.Graphics.Rect;
using RectF = Android.Graphics.RectF;

namespace MonkeyBoard.Android {
    public class AdMenuView : AdCustomViewGroup, IFrameRenderer {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region IKeyboardViewRenderer Implementation
        public override void LayoutFrame(bool invalidate) {
            base.LayoutFrame(invalidate); 

            if (AdEmojiSearchPopupWindow.ContainerView is { } cv) {
                cv.LayoutFrame(invalidate);
            }
        }
        public override void MeasureFrame(bool invalidate) {
            Frame = DC.MenuRect.ToRectF();
            MenuPath = DC.MenuCornerRadius.ToPath(Bounds);

            base.MeasureFrame(invalidate);

            if(AdEmojiSearchPopupWindow.ContainerView is { } cv) {
                cv.MeasureFrame(invalidate);
            }
        }
        public override void PaintFrame(bool invalidate) {
            BackButtonBmp = BackButtonBmp.LoadRescaleOrIgnore(DC.BackIconSourceObj.ToString(), DC.BackButtonImageRect.ToRectF());
            OptionsButtonBmp = OptionsButtonBmp.LoadRescaleOrIgnore(DC.OptionsIconSourceObj.ToString(), DC.OptionButtonImageRect.ToRectF());
            base.PaintFrame(invalidate);

            if (AdEmojiSearchPopupWindow.ContainerView is { } cv) {
                cv.PaintFrame(invalidate);
            }
        }
        public override void RenderFrame(bool invalidate) {
            base.RenderFrame(invalidate);

            if (AdEmojiSearchPopupWindow.ContainerView is { } cv) {
                cv.RenderFrame(invalidate);
            }
        }
        #endregion

        #endregion

        #region Properties

        #region View Models
        public new MenuViewModel DC { get; set; }
        #endregion

        #region Views
        Path MenuPath { get; set; }
        Bitmap BackButtonBmp { get; set; }
        Bitmap OptionsButtonBmp { get; set; }
        AdAutoCompleteView TextAutoCompleteView { get; set; }

        #endregion

        #region Appearance
        #endregion

        #region Layout
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors

        public AdMenuView(Context context, GPaint paint, MenuViewModel dc) : base(context, paint) {
            DC = dc;

            TextAutoCompleteView = new AdAutoCompleteView(context, paint, DC.TextAutoCompleteViewModel).SetDefaultProps("AutoComplete View");
            this.AddView(TextAutoCompleteView);
        }

        #endregion

        #region Public Methods
        public void ResetRenderer() {
            DC.SetRenderContext(this);
            DC.MenuStripViewModel.SetRenderContext(this);
            TextAutoCompleteView.ResetRenderer();
        }
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if (!DC.IsVisible) {
                return;
            }
            if(MenuPath != null) {
                canvas.ClipPath(MenuPath);
            }

            // bg
            SharedPaint.Color = DC.MenuBgHexColor.ToAdColor();
            canvas.DrawRect(Bounds, SharedPaint);

            // press bg
            if (DC.TouchOwner != default) {
                if(DC.TouchOwner.ownerType == MenuItemType.BackButton) {
                    SharedPaint.Color = DC.BackButtonBgHexColor.ToAdColor();
                    canvas.DrawRect(DC.BackButtonRect.ToRectF(), SharedPaint);
                } else if(DC.TouchOwner.ownerType == MenuItemType.OptionsButton) {
                    SharedPaint.Color = DC.OptionsButtonBgHexColor.ToAdColor();
                    canvas.DrawRect(DC.OptionsButtonRect.ToRectF(), SharedPaint);
                } 
            }
            if(DC.CurMenuPageType == MenuPageType.TabSelector && DC.MenuStripViewModel.IsVisible) {
                SharedPaint.TextSize = DC.MenuStripViewModel.MenuItemFontSize.UnscaledF();
                // tabs
                foreach(var tab_item in DC.MenuStripViewModel.Items) {
                    var tab_item_rect = tab_item.TabItemRect.ToRectF();

                    // tab bg
                    if (tab_item.IsPressed) {
                        SharedPaint.Color = DC.MenuStripViewModel.PressedTabItemBgHexColor.ToAdColor();
                        canvas.DrawRect(tab_item_rect, SharedPaint);
                    } else if (tab_item.IsSelected) {
                        SharedPaint.Color = DC.MenuStripViewModel.SelectedTabItemBgHexColor.ToAdColor();
                        canvas.DrawRect(tab_item_rect, SharedPaint);
                    }

                    // tab fg
                    if (tab_item.IconSourceObj is string { } icon_text) {
                        // text fg
                        float x = tab_item.IconRect.Left.UnscaledF();
                        float y = tab_item.IconRect.Top.UnscaledF();
                        SharedPaint.Color = Color.White;
                        canvas.DrawText(icon_text, x, y, SharedPaint);
                    } else {
                        // image fg?
                    }
                }
            }

            SharedPaint.SetTint(DC.MenuFgHexColor.ToAdColor());
            if(DC.IsBackButtonVisible && BackButtonBmp != null) {
                var back_img_rect = DC.BackButtonImageRect.ToRectF();
                canvas.DrawBitmap(BackButtonBmp, back_img_rect.Left, back_img_rect.Top, SharedPaint);
            }

            if(OptionsButtonBmp != null) {
                var opt_btn_rect = DC.OptionButtonImageRect.ToRectF();
                canvas.DrawBitmap(OptionsButtonBmp, opt_btn_rect.Left, opt_btn_rect.Top, SharedPaint);
            }          
            
            
            SharedPaint.SetTint(null);
        }

        #endregion

        #region Private Methods
        #endregion
    }
}