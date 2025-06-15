using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Avalonia;
using Avalonia.Controls;
using HarfBuzzSharp;
using IntelliJ.Lang.Annotations;
using MonkeyBoard.Common;
using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static Android.Views.View;
using Canvas = Android.Graphics.Canvas;
using GPaint = Android.Graphics.Paint;
using Matrix = Android.Graphics.Matrix;
using Point = Android.Graphics.Point;
using Rect = Android.Graphics.Rect;

namespace MonkeyBoard.Android {

    public class AdKeyView : 
        AdCustomView, 
        IFrameRenderer {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        static Dictionary<string, Bitmap> ImageLookup { get; set; } = [];

        static bool DRAW_DEBUG = false;


        static GPaint SimplePaint { get; set; }
        static GPaint BlurPaint { get; set; }
        static float outline_r = 1.0f;
        public static void ResetPaints() {
            SimplePaint = null;
            BlurPaint = null;
        }
        static GPaint GetPaintSimple() {
            var paint = new GPaint();
            paint.AntiAlias = true;
            paint.Dither = true;
            paint.Color = Color.Argb(KeyboardPalette.IsDark ? 70 : 200, 255, 255, 255);
            paint.StrokeWidth = outline_r;
            paint.SetStyle(Paint.Style.Stroke);
            paint.StrokeJoin = Paint.Join.Round;
            paint.StrokeCap = Paint.Cap.Round;
            return paint;
        }
        static GPaint GetPaintBlur() {
            var paint = new GPaint(GetPaintSimple());
            //SharedPaint.Color = Color.Argb(235, 74, 138, 255);
            paint.Color = Color.Argb(KeyboardPalette.IsDark ? 40 : 180, 255, 255, 255);
            paint.StrokeWidth = outline_r * 3;
            paint.SetMaskFilter(new BlurMaskFilter(outline_r * 1.5f, BlurMaskFilter.Blur.Normal));
            return paint;
        }
        #endregion

        #region Interfaces

        #region IkeyboardViewRenderer Implementation

        public override void MeasureFrame(bool invalidate) {
            if (!DC.IsVisible) {
                base.MeasureFrame(invalidate);
                return;
            }
            //var last_frame = Frame;

            if(DC.IsPopupKey) {
                Frame = DC.InnerRect.Flate(0, 0, 1, 1).ToRectF();
                KeyPathRect = Frame.ToBounds();
            } else {
                Frame = DC.KeyboardRect.ToRectF();
                KeyPathRect = DC.InnerRect.ToBounds().ToRectF();
            }
            //bool needs_layout = last_frame != Frame;
            KeyPath = DC.CornerRadius.ToPath(KeyPathRect); 

            if (DC.IsPrimaryImage) {
                KeyImage = KeyImage.LoadRescaleOrIgnore(DC.CurrentChar, DC.PrimaryImageRect.ToRectF());
            }
            //if (needs_layout) {
            //    this.UpdateLayout(this.Frame);
            //}
            base.MeasureFrame(invalidate);
        }

        public override void PaintFrame(bool invalidate) {
            if(!DC.IsVisible) {
                return;
            }
            DC.SetBrushes();
            base.PaintFrame(invalidate);
        }
        #endregion

        #endregion

        #region Properties

        #region Members
        Bitmap KeyImage { get; set; }
        #endregion

        #region View Models
        public new KeyViewModel DC { get; private set; }
        #endregion

        #region Appearance

        public Path KeyPath { get; private set; } = new();
        RectF KeyPathRect { get; set; } = new();
        #endregion

        #region Layout
        #endregion

        #region State
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public AdKeyView(KeyViewModel kvm, Context context, Paint paint) : base(context, paint) {
            this.DC = kvm;
            DC.SetRenderContext(this);
            RenderFrame(false);
        }

        #endregion

        #region Public Methods
        
        public override string ToString() {
            return $"'{DC.PrimaryValue}'";
        }
        #endregion

        #region Protected Methods
        protected override void OnDraw(Canvas canvas) {
            if (!DC.IsVisible) {
                return;
            }

            canvas.Save();

            var inner_offset = DC.InnerOffset.ToPointF();
            canvas.Translate(inner_offset.X, inner_offset.Y);

            if (DC.CanHaveShadow) {
                // drop shadow
                SharedPaint.Color = DC.Parent.ShadowHex.ToAdColor();

                var shadow_offset = DC.Parent.ShadowOffset.ToPointF();
                canvas.Save();
                canvas.Translate(shadow_offset.X, shadow_offset.Y);
                canvas.DrawPath(KeyPath, SharedPaint);
                canvas.Restore();
            }

            // key bg
            SharedPaint.Color = DC.BgHex.ToAdColor();
            canvas.DrawPath(KeyPath, SharedPaint);

            if (DC.IsPrimaryImage) {
                if (KeyImage is { } bmp &&
                    DC.PrimaryImageRect.ToRectF() is { } img_rect &&
                    !img_rect.IsEmpty) {
                    // primary img
                    SharedPaint.SetTint(DC.PrimaryHex.ToAdColor());
                    canvas.DrawBitmap(bmp, img_rect.Left, img_rect.Top, SharedPaint);
                    SharedPaint.SetTint(null);
                }
            } else {
                // primary text
                if (!string.IsNullOrEmpty(DC.PrimaryValue) && !DC.IsPulling) {
                    canvas.DrawAlignedText(
                                SharedPaint,
                                KeyPathRect,
                                DC.PrimaryValue,
                                DC.PrimaryFontSize.UnscaledF(),
                                DC.PrimaryHex.ToAdColor(),
                                DC.PrimaryTextHorizontalAlignment,
                                DC.PrimaryTextVerticalAlignment,
                                DC.PrimaryTextOffset.ToPointF());
                }

                // secondary text
                if (DC.IsSecondaryVisible) {
                    canvas.DrawAlignedText(
                                SharedPaint,
                                KeyPathRect,
                                DC.SecondaryValue,
                                DC.SecondaryFontSize.UnscaledF(),
                                DC.SecondaryHex.ToAdColor(),
                                DC.SecondaryTextHorizontalAlignment,
                                DC.SecondaryTextVerticalAlignment,
                                DC.SecondaryTextOffset.ToPointF());
                }
            }
            
            if(DC.Parent.IsShadowsEnabled && DC.CanHaveShadow && DC.IsSpecial) {
                // draw specular highlights on shadow catty-corner
                DrawOutline(canvas);
            }

            if(DRAW_DEBUG) {
                SharedPaint.Color = MpColorHelpers.GetRandomHexColor().AdjustAlpha(150).ToAdColor();
                if(DC.IsPressed) {
                    canvas.DrawRect(DC.TotalHitRect.ToBounds().ToRectF(), SharedPaint);
                } else {
                    canvas.DrawRect(Bounds, SharedPaint);
                }

            }
            canvas.Restore();
        }
        #endregion

        #region Private Methods
        void DrawOutline(Canvas canvas) {
            canvas.Save();
            float scale = 0.92f;
            float inset = Math.Min(KeyPathRect.Width(), KeyPathRect.Height()) * (1.0f - scale);
            float outline_w = KeyPathRect.Width() - inset;
            float outline_h = KeyPathRect.Height() - inset;
            float outline_x = (KeyPathRect.Width() - outline_w) / 2;
            float outline_y = (KeyPathRect.Height() - outline_h) / 2;
            var outline_rect = new RectF(outline_x, outline_y, outline_x + outline_w, outline_y + outline_h);
            var outline_path = DC.CornerRadius.ToPath(outline_rect);

            var shadow_offset = DC.Parent.ShadowOffset.ToPointF();
            var shadow_loc = new PointF(shadow_offset.X * Bounds.Width(), shadow_offset.Y * Bounds.Height());
            var hl_center = new PointF(-shadow_loc.X, -shadow_loc.Y);
            var hl_w = Bounds.Width() / 4;
            var hl_h = Bounds.Height() / 4;

            float hl_x = hl_center.X - (hl_w / 2);
            float hl_y = hl_center.Y - (hl_h / 2);
            var hl_rect = new RectF(hl_x, hl_y, hl_w, hl_h);

            var protect = new Region(hl_rect.ToRect());
            canvas.ClipRect(hl_rect);

            //if(BlurPaint == null) {
            //    BlurPaint = GetPaintBlur();
            //}            
            //canvas.DrawPath(outline_path, BlurPaint);
            if(SimplePaint == null) {
                SimplePaint = GetPaintSimple();
            }            
            canvas.DrawPath(outline_path, SimplePaint);
            canvas.Restore();
        }

        static GPaint GetGradientPaint(float w, float h) {
            var paint = new GPaint();
            paint.Color = Color.Black;
            paint.StrokeWidth = 1;
            paint.SetStyle(GPaint.Style.FillAndStroke);
            paint.SetShader(new RadialGradient(w / 2, h / 2, h / 3, Color.Transparent, Color.Black, Shader.TileMode.Mirror));
            return paint;
        }

        #endregion

    }

}