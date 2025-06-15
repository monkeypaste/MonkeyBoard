using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using CoreText;
using Foundation;
using MonkeyPaste.Common;
using MonkeyPaste.Keyboard.Common;
using MonoTouch.Dialog;
using ObjCRuntime;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UIKit;
using AvPoint = Avalonia.Point;
using AvRect = Avalonia.Rect;

namespace MonkeyBoard.iOS.KeyboardExt {

    public static class iosHelpers {
        static NFloat Scaling =>
            (NFloat)iosDeviceInfo.Scaling;

        #region Memory
        public static void DoGC() {
            GC.Collect();
        }
        #endregion

        #region Geometery
        public static double Dist(this CGPoint a, CGPoint b) {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }
        public static CGRect RoundToInt(this CGRect rect) {
            return new CGRect(Math.Round(rect.X), Math.Round(rect.Y), Math.Round(rect.Width), Math.Round(rect.Height));
        }
        public static CGRect Flate(this CGRect rect, NFloat dl, NFloat dt, NFloat dr, NFloat db) {
            NFloat l = rect.Left + dl;
            NFloat t = rect.Top + dt;

            NFloat r = rect.Right + dr;
            NFloat b = rect.Bottom + db;

            l = (NFloat)Math.Min(l, r);
            r = (NFloat)Math.Max(l, r);

            t = (NFloat)Math.Min(t, b);
            b = (NFloat)Math.Max(t, b);

            return new CGRect(l, t, r - l, b - t);
        }
        public static CGRect ToCGRect(this AvRect av_rect) {
            return new CGRect(
                (NFloat)av_rect.X * Scaling,
                (NFloat)av_rect.Y * Scaling,
                (NFloat)av_rect.Width * Scaling,
                (NFloat)av_rect.Height * Scaling);
        }
        public static CGRect Place(this CGRect rect, NFloat ox, NFloat oy) {
            NFloat w = rect.Width;
            NFloat h = rect.Height;
            NFloat l = ox;
            NFloat t = oy;
            return new CGRect(l, t, w, h);
        }
        public static CGRect Resize(this CGRect rect, NFloat w, NFloat h) {
            NFloat l = rect.Left;
            NFloat t = rect.Top;
            return new CGRect(l, t, w, h);
        }

        public static CGRect Move(this CGRect rect, NFloat dx, NFloat dy) {
            NFloat w = rect.Width;
            NFloat h = rect.Height;
            NFloat l = rect.Left + dx;
            NFloat t = rect.Top + dy;
            return new CGRect(l, t, w, h);
        }
        public static Rect ToScaledRect(this CGRect rect) {
            double x = rect.X / Scaling;
            double y = rect.Y / Scaling;
            double w = rect.Width / Scaling;
            double h = rect.Height / Scaling;
            return new Rect(x, y, w, h);
        }
        public static CGRect ToBounds(this CGRect rect) {
            return rect.Place(0, 0);
        }
        public static CGRect ToBounds(this CGRect rect, CGRect outer_rect) {
            NFloat w = rect.Width;
            NFloat h = rect.Height;
            NFloat l = rect.Left - outer_rect.Left;
            NFloat t = rect.Top - outer_rect.Top;
            return new CGRect(l, t, w, h);
        }

        public static NFloat UnscaledF(this double d) {
            return (NFloat)(d * Scaling);
        }
        public static int UnscaledI(this double d) {
            return (int)(d * Scaling);
        }

        public static double ScaledD(this NFloat f) {
            return (double)((double)f / (double)Scaling);
        }
        public static double ScaledD(this int i) {
            return (double)((double)i / (double)Scaling);
        }
        public static int ScaledI(this int i) {
            return (int)(i / Scaling);
        }
        public static CGSize ToCGSize(this CGPoint p) {
            return new CGSize(Math.Abs(p.X), Math.Abs(p.Y));
        }
        public static CGPoint ToCGPoint(this AvPoint p) {
            return new CGPoint((NFloat)p.X * Scaling, (NFloat)p.Y * Scaling);
        }
        public static CGPoint Move(this CGPoint p, NFloat dx, NFloat dy) {
            return new CGPoint(p.X + dx, p.Y + dy);
        }
        #endregion

        #region Images
        public static UIImage LoadBitmap(string img_name, CGSize size = default) {
            string img_path = System.IO.Path.Combine(KbStorageHelpers.ImgRootDir, img_name);
            if(MpFileIo.ReadBytesFromFile(img_path) is { } bytes &&
                NSData.FromArray(bytes) is { } byteData &&
                UIImage.LoadFromData(byteData)
                is { } img) {
                if(size != default) {
                    return img.Resize(size, true);
                }
                return img;
            }
            return null;
        }

        public static UIImage Resize(this UIImage source, CGSize newSize, bool recycleSource) {
            UIGraphics.BeginImageContextWithOptions(newSize, false, 0);
            source.Draw(new CGRect(0, 0, newSize.Width, newSize.Height));
            var newImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            if(recycleSource) {
                source.Dispose();
                source = null;
            }
            return newImage;
        }

        #endregion

        #region Alerts
        public static void Alert(UIViewController vc, string title = "", string msg = "", bool isDarkTheme = false) {
            bool done = false;
            var root = new ThemedRootElement(title) {
                new Section(title) {
                    new StyledMultilineElement(msg) {
                        BackgroundColor = UIColor.Clear,
                        TextColor = KeyboardPalette.P[PaletteColorType.Fg].ToUIColor(),
                        Alignment = UITextAlignment.Center
                    }
                },
                new Section() {
                    new StyledStringElement(ResourceStrings.U["OkText"].value, () => {
                        done = true;
                    }) {
                        Alignment = UITextAlignment.Center,
                        TextColor = UIColor.Green
                    }
                }
            };
            var alert_vc = new CustomDialogViewController(root, false, isDarkTheme);
            vc.PresentViewController(alert_vc, true, null);
            iosKeyboardViewController.Instance.Post(async () => {
                while(!done) {
                    await Task.Delay(100);
                }
                alert_vc.DismissViewController(true, null);
            });
        }

        public static async Task<bool> AlertYesNoAsync(UIViewController vc, string title = "", string msg = "", bool isDarkTheme = false) {
            bool? result = null;

            var root = new ThemedRootElement(title) {
                new Section(title) {
                    new StyledMultilineElement(msg) {
                        BackgroundColor = UIColor.Clear,
                        TextColor = KeyboardPalette.P[PaletteColorType.Fg].ToUIColor(),
                        Alignment = UITextAlignment.Center
                    }
                },
                new Section() {
                    new StyledStringElement(ResourceStrings.U["YesText"].value, () => {
                        result = true;
                    }) {
                        Alignment = UITextAlignment.Center,
                        TextColor = UIColor.Blue
                    },
                    new StyledStringElement(ResourceStrings.U["NoText"].value, () => {
                        result = false;
                    }) {
                        Alignment = UITextAlignment.Center,
                        TextColor = UIColor.Red
                    },
                }
            };
            var alert_vc = new CustomDialogViewController(root, true, isDarkTheme);
            vc.PresentViewController(alert_vc, true, null);

            while(result == null) {
                await Task.Delay(100);
            }
            alert_vc.DismissViewController(true, null);
            return result.Value;
        }
        #endregion

        #region Views
        public static UITextAlignment ToIosAlignment(this HorizontalAlignment ha) {
            switch(ha) {
                case HorizontalAlignment.Left:
                    return UITextAlignment.Left;
                case HorizontalAlignment.Center:
                    return UITextAlignment.Center;
                case HorizontalAlignment.Right:
                    return UITextAlignment.Right;
                default:
                    throw new NotSupportedException($"{ha} alignment not supported");
            }
        }
        public static UIControlContentVerticalAlignment ToIosAlignment(this VerticalAlignment va) {
            switch(va) {
                case VerticalAlignment.Top:
                    return UIControlContentVerticalAlignment.Top;
                case VerticalAlignment.Center:
                    return UIControlContentVerticalAlignment.Center;
                case VerticalAlignment.Bottom:
                    return UIControlContentVerticalAlignment.Bottom;
                default:
                    throw new NotSupportedException($"{va} alignment not supported");
            }
        }
        public static void RemoveAndDispose(this UIView v) {
            if(v == null) {
                return;
            }
            if(v.Superview != null) {
                if(v.Superview is UIStackView sv) {
                    sv.RemoveArrangedSubview(v);
                }
                v.RemoveFromSuperview();
            }
            v.Dispose();
        }
        public static NSLayoutConstraint WithPriority(this NSLayoutConstraint lc, float priority) {
            lc.Priority = priority;
            return lc;
        }
        public static void ClearConstraints(this UIView v) {
            for(int i = 0; i < v.Constraints.Length; i++) {
                v.RemoveConstraint(v.Constraints[i]);
            }
        }
        public static void Post(Action action) {
            DispatchQueue.MainQueue.DispatchAsync(() => {
                action.Invoke();
            });
        }
        public static CGSize MeasureText(
            string text,
            string fontName,
            nfloat fontSize,
            out nfloat ascent, out nfloat descent) {
            ascent = 0;
            descent = 0;
            if(string.IsNullOrEmpty(text)) {
                return default;
            }
            var font = new CTFont(fontName, fontSize);
            ascent = font.AscentMetric;
            descent = font.DescentMetric;

            var nstext = new NSAttributedString(text,
                new CTStringAttributes {
                    Font = font
                });

            var result = nstext.Size;
            nstext.Dispose();
            font.Dispose();
            return result;
        }
        public static void DrawText(
                this CGContext context,
                CGRect rect,
                string emojiText,
                nfloat fontSize,
                string fontFamily,
                UIColor fgColor,
                UITextAlignment horizontalAlignment,
                UIControlContentVerticalAlignment verticalAlignment,
                CGPoint offset,
                bool italics = false) =>
            DrawText(context, rect, emojiText, fontSize, fontFamily, fgColor, horizontalAlignment, verticalAlignment, offset.X, offset.Y, italics);

        public static void DrawText(
                this CGContext context,
                CGRect rect,
                string text,
                nfloat fontSize,
                string fontFamily,
                UIColor fgColor,
                UITextAlignment horizontalAlignment = UITextAlignment.Center,
                UIControlContentVerticalAlignment verticalAlignment = UIControlContentVerticalAlignment.Center,
                nfloat ox = default,
                nfloat oy = default,
                bool italics = false
            ) {
            text = text ?? string.Empty;

            var font = UIFont.FromName(fontFamily, fontSize);

            NSMutableParagraphStyle par_style = new NSMutableParagraphStyle() {
                Alignment = horizontalAlignment,
                ParagraphSpacing = 0,
                ParagraphSpacingBefore = 0,
                LineSpacing = 0,
                //LineSpacing = 8,
                //LineBreakMode = UILineBreakMode.TailTruncation
                //LineBreakMode = UILineBreakMode.CharacterWrap
            };
            switch(verticalAlignment) {
                case UIControlContentVerticalAlignment.Center:
                    par_style.MinimumLineHeight = (rect.Height + font.LineHeight) / 2;
                    break;
                case UIControlContentVerticalAlignment.Bottom:
                    par_style.MinimumLineHeight = rect.Height;// - font.LineHeight;
                    break;
                case UIControlContentVerticalAlignment.Top:
                    par_style.MinimumLineHeight = 0;// font.LineHeight;
                    break;

            }
            var attr = NSMutableDictionary.FromObjectsAndKeys([par_style, font, fgColor], [UIStringAttributeKey.ParagraphStyle, UIStringAttributeKey.Font, UIStringAttributeKey.ForegroundColor]);
            //var italic_lvl = new NSNumber(italics ? 1 : 0);
            //attr.Add(italic_lvl, UIStringAttributeKey.Obliqueness);
            var nsstr = new NSString(text);
            nsstr.WeakDrawString(rect.Move(ox, oy), attr);

            nsstr.Dispose();
            attr.Dispose();
            par_style.Dispose();
            font.Dispose();
            //italic_lvl.Dispose();
        }
        public static void DrawText_CT(
            this CGContext context,
            CGRect rect,
            string text,
            nfloat fontSize,
            string fontFamily,
            UIColor fgColor,
            nfloat ox = default, nfloat oy = default,
            bool isItalics = false,
            CTTextAlignment alignment = CTTextAlignment.Center) {
            text = text ?? string.Empty;
            // BUG fix fuzzy text
            rect = rect.RoundToInt();
            ox = (nfloat)Math.Round(ox);
            oy = (nfloat)Math.Round(oy);

            // from https://stackoverflow.com/a/44215442/105028
            var font = new CTFont(fontFamily, fontSize);

            var attr_str = new NSAttributedString(text,
                new CTStringAttributes {
                    ForegroundColor = fgColor.CGColor,
                    Font = font,
                    ParagraphStyle = new CTParagraphStyle(new() {
                        Alignment = alignment
                    })
                });

            var framesetter = new CTFramesetter(attr_str);

            // left column form
            var leftColumnPath = new CGPath();
            leftColumnPath.AddRect(new CGRect(
                rect.X,
                y: -rect.Y,
                width: rect.Size.Width,
                height: rect.Size.Height));

            // left column frame
            var translateAmount = rect.Size.Height;

            if(rect.Width != rect.Height) {
                // BUG for some reason if rect is not square the text is drawn top aligned
                var text_size = MeasureText(text, fontFamily, fontSize, out _, out _);
                oy += (rect.Height / 2f) - (text_size.Height / 2f);
            }

            context.SaveState();
            context.TextMatrix = CGAffineTransform.MakeIdentity();
            if(isItalics) {
                context.TextMatrix = new CGAffineTransform(1, 0, 0.5f, 1, 0, 0);
            }
            context.TranslateCTM(0 + ox, translateAmount + oy);
            context.ScaleCTM(1, -1);
            var leftFrame = framesetter.GetFrame(new NSRange(0, 0), leftColumnPath, null);
            leftFrame.Draw(context);
            context.RestoreState();

            leftFrame.Dispose();
            leftColumnPath.Dispose();
            framesetter.Dispose();
            attr_str.Dispose();
            font.Dispose();
        }

        public static UIBezierPath ToPath(this CornerRadius cr, CGRect bounds) {
            var tlr = new CGSize(cr.TopLeft, cr.TopLeft);
            var trr = new CGSize(cr.TopRight, cr.TopRight);
            var brr = new CGSize(cr.BottomRight, cr.BottomRight);
            var blr = new CGSize(cr.BottomLeft, cr.BottomLeft);
            var maskPath = new UIBezierPathExt(bounds, tlr, trr, brr, blr);
            return maskPath;
        }
        public static void RoundCorners(this UIView view, CornerRadius cr) {
            var scale = UIScreen.MainScreen.Scale;
            if(UIDevice.CurrentDevice.CheckSystemVersion(11, 0)) {
                // from https://stackoverflow.com/a/71329483/105028
                UIRectCorner corner_mask = (UIRectCorner)0;
                double radius = 0;
                if(cr.TopLeft > 0) {
                    corner_mask |= UIRectCorner.TopLeft;
                    radius = cr.TopLeft * scale;
                }
                if(cr.TopRight > 0) {
                    corner_mask |= UIRectCorner.TopRight;
                    radius = cr.TopRight * scale;
                }
                if(cr.BottomRight > 0) {
                    corner_mask |= UIRectCorner.BottomRight;
                    radius = cr.BottomRight * scale;
                }
                if(cr.BottomLeft > 0) {
                    corner_mask |= UIRectCorner.BottomLeft;
                    radius = cr.BottomLeft * scale;
                }
                view.ClipsToBounds = true;
                view.Layer.CornerRadius = (NFloat)radius;
                view.Layer.MaskedCorners = (CACornerMask)corner_mask;
            } else {
                var tlr = new CGSize(cr.TopLeft, cr.TopLeft);
                var trr = new CGSize(cr.TopRight, cr.TopRight);
                var brr = new CGSize(cr.BottomRight, cr.BottomRight);
                var blr = new CGSize(cr.BottomLeft, cr.BottomLeft);
                var maskPath = new UIBezierPathExt(view.Bounds, tlr, trr, brr, blr);
                var shape = new CAShapeLayer() {
                    Path = maskPath.CGPath
                };
                view.Layer.Mask = shape;
            }
        }

        public static void Redraw(this UIView v, bool layout = false) {
            v.Layer.SetNeedsDisplay();
            if(layout) {
                v.Layer.SetNeedsLayout();
            }
            v.Layer.DisplayIfNeeded();
        }
        public static T SetDefaultProps<T>(this T uiv, bool allowInteraction = false) where T : UIView {
            uiv.ClearsContextBeforeDrawing = true;
            uiv.TranslatesAutoresizingMaskIntoConstraints = false;
            uiv.UserInteractionEnabled = false;// allowInteraction;
            //uiv.ClipsToBounds = false;
            return uiv;
        }
        #endregion

        #region Color
        public static UIColor ToUIColor(this string hex) {
            System.Drawing.Color color = System.Drawing.ColorTranslator.FromHtml(hex);
            //return UIColor.FromRGBA(color.A, color.R, color.G, color.B);
            return UIColor.FromRGBA(color.R, color.G, color.B, color.A);
        }
        #endregion

        #region Text


        #endregion

        #region Type
        #endregion
    }
}