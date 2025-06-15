using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using MonkeyPaste.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static SQLite.TableMapping;

namespace MonkeyBoard.Common {
    public static class Extensions {
        #region Tree
        public static IEnumerable<TreeViewModelBase> Children(this TreeViewModelBase fvm) {
            return ViewModelBase.All.OfType<TreeViewModelBase>().Where(x => x.Parent == fvm);
        }
        public static IEnumerable<TreeViewModelBase> Descendants(this TreeViewModelBase fvm, bool includeSelf = false) {
            if(includeSelf) {
                yield return fvm;
            }
            foreach(var cfvm in fvm.Children()) {
                yield return cfvm;
                foreach(var dfvm in cfvm.Descendants()) {
                    yield return dfvm;
                }
            }
        }
        public static IEnumerable<TreeViewModelBase> Ancestors(this TreeViewModelBase fvm, bool includeSelf = false) {
            if (includeSelf) {
                yield return fvm;
            }
            var cur_fvm = fvm;
            while(true) {
                if(cur_fvm == null || cur_fvm.Parent is not { } parent_fvm) {
                    yield break;
                }
                yield return parent_fvm;
                cur_fvm = parent_fvm;
            }
        }
        public static TreeViewModelBase Root(this TreeViewModelBase fvm) {
            if(fvm.Parent == null) {
                return fvm;
            }
            return fvm.Ancestors().LastOrDefault();
        }
        #endregion


        #region Linq
        public static bool IsEmpty<T>(this IEnumerable<T> enumerable) {
            return !enumerable.Any();
        }
        #endregion

        #region Enums
        public static IEnumerable<T> GetKeys<T>(this Enum enumType) where T:struct {
            if(Enum.GetNames(enumType.GetType()) is not { } names ||
                names.Select(x=>x.ToEnum<T>()) is not { } keys) {
                return [];
            }
            return keys;
        }
        public static IEnumerable GetKeys(this Type enumType) {
            if(Enum.GetNames(enumType) is not { } names ||
                names.Select(x=>x.ToEnum(enumType)) is not { } keys) {
                return default;
            }
            return keys;
        }
        #endregion

        #region Strings
        public static string ToCodePointStr(this string str) {
            var codePoints = new List<int>(str.Length);
            for (int i = 0; i < str.Length; i++) {
                codePoints.Add(Char.ConvertToUtf32(str, i));
                if (Char.IsHighSurrogate(str[i]))
                    i += 1;
            }

            return string.Join(" ", codePoints.Select(x => x.ToString("X")));
        }

        public static int[] IndexOfAll(this string text, string mv) {
            List<int> indexes = [];
            int i = 0;
            while(i < text.Length) { 
                string remaining = text.Substring(i);
                if(remaining.StartsWith(mv)) {
                    indexes.Add(i);
                    i += Math.Max(mv.Length,1);
                } else {
                    i++;
                }
            }
            return indexes.ToArray();
        }
        public static Dictionary<string, int> GetWordCounts(this string confirmedText) {
            var matches = new Dictionary<string, int>();
            var words = confirmedText.ToWordRanges().ToList();
            words.AddRange(confirmedText.ToCompoundWords(words));
            foreach (string word in words.Select(x=>x.Item2)) {
                if (matches.TryGetValue(word, out int count)) {
                    matches[word]++;
                } else {
                    matches.Add(word, 1);
                }
            }
            // 
            return matches;
        }

        public static string ToTypedString(this Enum enumVal, int count = 1) {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++) {
                sb.Append($"{enumVal.GetType().Name}.{enumVal}");
            }
            return sb.ToString();
        }
        public static IEnumerable<string> ToWords(this string text, bool case_sensitive = false) {
            var mc = Regex.Matches(case_sensitive ? text : text.ToLower(), "\\w+");
            foreach (Match m in mc) {
                foreach (Group mg in m.Groups) {
                    foreach (Capture c in mg.Captures) {
                        yield return c.Value;
                    }
                }
            }
        }
        public static IEnumerable<(int,string)> ToWordRanges(this string text, bool case_sensitive = false) {
            var mc = Regex.Matches(case_sensitive ? text : text.ToLower(), "\\w+");
            foreach (Match m in mc) {
                foreach (Group mg in m.Groups) {
                    foreach (Capture c in mg.Captures) {
                        yield return (c.Index, c.Value);
                    }
                }
            }
        }
        static IEnumerable<(int,string)> ToCompoundWords(this string text, IEnumerable<(int idx,string word)> words, bool case_sensitive = false) {
            text = case_sensitive ? text : text.ToLower();
            var word_idx_lookup =
                words.OrderBy(x => x.idx).ToDictionary(x => x.idx, x => x.word);

            var compound_words = new List<(int,string)>();
            int i = 0;
            while(i < word_idx_lookup.Count - 1) {
                var kvp = word_idx_lookup.ElementAt(i);
                int sidx = kvp.Key;
                int eidx = sidx + kvp.Value.Length - 1;

                int comp_eidx = eidx;
                int j = i + 1;
                while(j < word_idx_lookup.Count) {
                    var next_kvp = word_idx_lookup.ElementAt(j);
                    int next_sidx = next_kvp.Key;
                    if(next_sidx - comp_eidx == 2 && !char.IsWhiteSpace(text[comp_eidx+1])) {
                        // when 2 words are separated percent ONE non-space symbol treat as compound word
                        // like me@email.com or www.coolstuff.com
                        comp_eidx = next_sidx + next_kvp.Value.Length - 1;
                        j++;
                    } else {
                        break;
                    }
                }
                i = j;
                if(comp_eidx == eidx) {
                    // not a compound
                    continue;
                }
                string compound_word = text.Substring(sidx, comp_eidx - sidx + 1);                
                compound_words.Add((sidx,compound_word));
            }
            MpConsole.WriteLine($"Compound words: {(string.Join(",", compound_words.Select(x => $"'{x}'")))}");
            return compound_words;
        }

        public static string GetParameterizedQueryString(this string query, object[] args) {
            if (args == null || args.Length == 0) {
                return query;
            }
            query = query.Replace(Environment.NewLine, " ");
            var argList = new Stack(args.Reverse().ToArray());

            var sb = new StringBuilder();
            for (int i = 0; i < query.Length; i++) {
                if (query[i] == '?') {
                    if (argList.Count == 0) {
                        MpDebug.Break("Param count mismatch");
                    }
                    object arg = argList.Pop();
                    string arg_str = arg.ToString();
                    if (arg is string || arg is char) {
                        arg_str = $"'{arg_str}'";
                    }
                    sb.Append(arg_str);
                } else {
                    sb.Append(query[i]);
                }
            }
            if (argList.Count > 0) {
                MpDebug.Break("Param count mismatch");
            }
            return sb.ToString();
        }


        #endregion

        #region Color
        public static string Lerp(this string fromHex, string toHex, double percent) {
            return fromHex.ToPortableColor().Lerp(toHex.ToPortableColor(), percent).ToHex();
        }
        static MpColor Lerp(this MpColor from, MpColor to, double percent) {
            byte a = from.A.Lerp(to.A, percent);
            byte r = from.R.Lerp(to.R, percent);
            byte g = from.G.Lerp(to.G, percent);
            byte b = from.B.Lerp(to.B, percent);
            return new MpColor(a, r, g, b);
        }
        #endregion

        #region Geometry

        public static void ExpandSides(this Rect rect, out double l, out double t, out double r, out double b) {
            l = rect.Left;
            t = rect.Top;
            r = rect.Right;
            b = rect.Bottom;
        }
        public static void Expand(this Rect rect, out double x, out double y, out double w, out double h) {
            x = rect.X;
            y = rect.Y;
            w = rect.Width;
            h = rect.Height;
        }
        public static double Distance(this Rect rect, Point p) {
            // from https://stackoverflow.com/a/18157551/105028
            double dx = Math.Max(0, Math.Max(rect.Left - p.X, p.X - rect.Right));
            double dy = Math.Max(0, Math.Max(rect.Top - p.Y, p.Y - rect.Bottom));
            return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
        }
        public static Rect Resize(this Rect rect, double nw, double nh) {
            return new Rect(rect.Position, new Size(nw, nh));
        }
        public static Rect Stretch(this Rect rect, double s) {
            return new Rect(rect.Position, new Size(rect.Width + s, rect.Height + s));
        }
        public static Rect Stretch(this Rect rect, double dw, double dh) {
            return new Rect(rect.Position, new Size(rect.Width + dw, rect.Height + dh));
        }
        public static VectorDirection GetDirection(this Point p2, Point p1) {
            if (p2 == p1) {
                return VectorDirection.None;
            }
            switch (p2.Slope(p1)) {
                default:
                    return VectorDirection.Up;
                case 90:
                    return VectorDirection.Left;
                case 180:
                    return VectorDirection.Down;
                case 270:
                    return VectorDirection.Right;
            }
        }
        public static double Angle(this Point p2, Point p1) {
            double angle = double.RadiansToDegrees(Math.Atan2(p1.Y - p2.Y, p2.X - p1.X));
            return angle;
        }
        public static int Slope(this Point p2, Point p1) {
            double angle = p2.Angle(p1);
            if (angle > 45 && angle <= 135)
                // up
                return 0;
            if (angle >= 135 && angle < 180 || angle < -135 && angle > -180)
                // left
                return 90;
            if (angle < -45 && angle >= -135)
                // down
                return 180;
            if (angle > -45 && angle <= 45)
                // right
                return 270;
            return 0;
        }

        public static Point Translate(this FrameViewModelBase sourceFrame, Point p, FrameViewModelBase targetFrame) {
            sourceFrame.Ancestors().OfType<FrameViewModelBase>().ForEach(x => p += x.Frame.Position);
            targetFrame.Ancestors(true).OfType<FrameViewModelBase>().ForEach(x => p -= x.Frame.Position);
            return p;
        }

        public static Rect ToBounds(this Rect rect, Rect relativeTo = default) {
            double x = relativeTo == default ? 0 : rect.X - relativeTo.X;
            double y = relativeTo == default ? 0 : rect.Y - relativeTo.Y;
            return new Rect(x,y,rect.Width,rect.Height);
        }
        public static Rect RoundToInt(this Rect rect) {
            return new Rect(Math.Floor(rect.X), Math.Floor(rect.Y), Math.Ceiling(rect.Width), Math.Ceiling(rect.Height));
        }
        public static byte Lerp(this byte from, byte to, double percent) {
            // https://stackoverflow.com/a/33045266/105028
            return (byte)((double)from * (1d - percent) + (double)to * (double)percent);
        }
        public static double Lerp(this double from, double to, double percent) {
            // https://stackoverflow.com/a/33045266/105028
            return from * (1 - percent) + to * percent;
        }
        public static Point Rotate(this Point pointToRotate, Point centerPoint, double angleInDegrees) {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            double x = (cosTheta * (pointToRotate.X - centerPoint.X) - sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X);
            double y = (sinTheta * (pointToRotate.X - centerPoint.X) + cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y);

            return new Point(x, y);
        }
        public static double Length(this Point p) =>
            p.Distance(new());
        public static double Distance(this Point a, Point b) {
            return Math.Sqrt(Math.Pow(a.X-b.X, 2) + Math.Pow(a.Y-b.Y, 2));
        }
        public static bool IsPopupHit(
            this Rect popupRect, 
            int r, int c, 
            int pivotRow, int pivotCol,
            int rows,int cols, 
            bool isLast, 
            Rect anchorRect, Rect rootRect, 
            Point pressLocation, Point location, Point lastLocation,
            double multiplier = 1d) {

            // find popupRect cntr origin offset
            double pur_origin_x = popupRect.Width * c;
            double pur_origin_y = popupRect.Height * r;
            double origin_offset_x = popupRect.Position.X - pur_origin_x;
            double origin_offset_y = popupRect.Position.Y - pur_origin_y;

            // use origin offset to find pivot rect
            double pivot_x = (pivotCol * popupRect.Width) + origin_offset_x;
            double pivot_y = (pivotRow * popupRect.Height) + origin_offset_y;
            Rect pivot_rect = new Rect(pivot_x, pivot_y, popupRect.Width, popupRect.Height);

            // offset touches to center of pivot (default item)
            Point pivot_to_press_diff = pivot_rect.Center - pressLocation;
            // NOTE (i think) diff can be scaled using a multiplier
            Point cur_pivot_relative_loc = location + pivot_to_press_diff;
            Point last_pivot_relative_loc = lastLocation + pivot_to_press_diff;
            Point pivot_relative_loc = last_pivot_relative_loc + ((cur_pivot_relative_loc - last_pivot_relative_loc) * multiplier);
           

            double px = pivot_relative_loc.X;
            double py = pivot_relative_loc.Y;

            // snap perimeter cells to outer bounds
            double ol = Math.Min(0, px);
            double ot = Math.Min(0, py);
            double or = Math.Max(rootRect.Width, px);
            double ob = Math.Max(rootRect.Height, py);
            double hl = popupRect.Left;
            double ht = popupRect.Top;
            double hr = popupRect.Right;
            double hb = popupRect.Bottom;

            if (r == 0) {
                ht = Math.Min(ht, ot);
            }
            if (r == rows - 1) {
                hb = Math.Max(hb, ob);
            }
            if (c == 0) {
                hl = Math.Min(hl, ol);
            }
            if (c == cols - 1) {
                hr = Math.Max(hr, or);
            }
            if (isLast) {
                hr = Math.Max(hr, or);
                hb = Math.Max(hb, ob);
            }

            var hit_rect = new Rect(hl, ht, Math.Max(0, hr - hl), Math.Max(0, hb - ht));
            var adj_p = new Point(px, py);

            bool is_hit = hit_rect.Contains(adj_p);
            return is_hit;
        }
        public static Rect SquareByWidth(this Rect rect) {
            return rect.Resize(rect.Width, rect.Width);
        }
        public static Rect SquareByHeight(this Rect rect) {
            return rect.Resize(rect.Height, rect.Height);
        }
        public static Rect Align(this Rect rect, Rect containerRect, HorizontalAlignment ha, VerticalAlignment va, bool relative) {
            double cw = containerRect.Width;
            double ch = containerRect.Height;
            double rw = rect.Width;
            double rh = rect.Height;
            double l = relative ? 0 : containerRect.Left;
            double t = relative ? 0 : containerRect.Top;
            double r = relative ? containerRect.Width : containerRect.Right;
            double b = relative ? containerRect.Height : containerRect.Bottom;

            double rx = 0;
            double ry = 0;
            switch (ha) {
                case HorizontalAlignment.Stretch:
                    rx = l;
                    rw = cw;
                    break;
                case HorizontalAlignment.Left:
                    rx = l;
                    break;
                case HorizontalAlignment.Center:
                    rx = l + (cw / 2) - (rw / 2);
                    break;
                case HorizontalAlignment.Right:
                    rx = r - rw;
                    break;
            }
            switch(va) {
                case VerticalAlignment.Stretch:
                    ry = t;
                    rh = ch;
                    break;
                case VerticalAlignment.Top:
                    ry = t;
                    break;
                case VerticalAlignment.Center:
                    ry = t + (ch / 2) - (rh / 2);
                    break;
                case VerticalAlignment.Bottom:
                    ry = b - rh;
                    break;
            }
            return new Rect(rx, ry, rw, rh);
        }
        public static Rect InsetByRatio(this Rect rect, double r) =>
            Inset(rect, rect.Width * r, rect.Height * r);
        public static Rect InsetByRatio(this Rect rect, double rx, double ry) =>
            Inset(rect, rect.Width * rx, rect.Height * ry);
        public static Rect Inset(this Rect rect, double dx, double dy) {
            double hdx = dx / 2;
            double hdy = dy / 2;
            double l = rect.Left + hdx;
            double t = rect.Top + hdy;
            double r = rect.Right - hdx;
            double b = rect.Bottom - hdy;
            return new Rect(l, t, r - l, b - t);
        }
        
        public static Rect Move(this Rect rect, double dx, double dy) {
            return rect.Move(new(dx, dy));
        }
            
        public static Rect Move(this Rect rect, Point originDelta) {
            double x = rect.X + originDelta.X;
            double y = rect.Y + originDelta.Y;
            return new Rect(new Point(x, y), rect.Size);
        }
        public static Rect Place(this Rect rect, Point newOrigin) {
            return new Rect(newOrigin, rect.Size);
        }

        public static Rect Translate(this Rect rect, Rect containerRect) {
            return rect.Move(containerRect.Position);
        }
        public static Point FindAboveTranslationToFit(this Rect rect, Rect anchorRect, Rect outerRect) {
            double hl = rect.Left;
            double ht = rect.Top;
            double hr = rect.Right;
            double hb = rect.Bottom;

            double x_diff = 0;
            double y_diff = ht - outerRect.Top;

            bool contain_frame = true;

            if (contain_frame) {
                if (y_diff < 0) {
                    ht -= y_diff;
                    hb -= y_diff;
                } else {
                    y_diff = 0;
                }
                var this_key_rect = anchorRect;
                var y_adj_rect = new Rect(hl, ht, hr - hl, hb - ht);
                if (y_adj_rect.Intersects(this_key_rect)) {
                    bool is_on_right = anchorRect.Center.X >= outerRect.Width;
                    if (is_on_right) {
                        x_diff = this_key_rect.Left - hr - rect.Width;
                    } else {
                        x_diff = hl - this_key_rect.Right + (rect.Width * 2);
                    }
                }
            } else {
                //if (y_diff < 0) {
                //    y += y_diff;
                //    hb += y_diff;
                //} else {
                //    y_diff = 0;
                //}
            }
            return new Point(x_diff, -y_diff);
        }
        public static Rect PositionAbove(this Rect rect, Rect anchorRect, Rect outerRect, double edge_pad = 5, double y_offset = 5) {
            // NOTES
            // rect should be at origin
            // anchorRect is in outerRect coordinate space

            double x = anchorRect.Center.X - rect.Center.X;
            double y = anchorRect.Top - rect.Height - y_offset;

            double r = x + rect.Width;
            double r_diff = r - outerRect.Right;
            if (r_diff > 0) {
                x = x - r_diff - edge_pad;
                r = r - r_diff - edge_pad;
            }
            double l_diff = outerRect.Left - x;
            if (l_diff > 0) {
                x = x + l_diff + edge_pad;
                r = r + l_diff + edge_pad;
            }
            return new Rect(x, y, rect.Width, rect.Height);
        }
        public static Point ToAvPoint(this MpPoint p) {
            return new Point(p.X, p.Y);
        }
        public static MpPoint ToPortablePoint(this Point p) {
            return new MpPoint(p.X, p.Y);
        }
        public static Rect Inner(this Rect rect) {
            return new Rect(0, 0, rect.Width, rect.Height);
        }
        public static Rect Flate(this Rect rect, double dl,double dt,double dr,double db) {
            double l = rect.Left + dl;
            double t = rect.Top + dt;

            double r = rect.Right + dr;
            double b = rect.Bottom + db;

            l = Math.Min(l, r);
            r = Math.Max(l, r);

            t = Math.Min(t, b);
            b = Math.Max(t, b);

            return new Rect(l, t, r - l, b - t);
        }

        #endregion

        #region Keyboard

        public static bool IsNumpadLayout(this KeyboardLayoutType klt) {
            switch(klt) {
                case KeyboardLayoutType.Pin:
                case KeyboardLayoutType.PhoneNumber:
                case KeyboardLayoutType.Digits:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsSpecialKeyStr(this string str) {
            return str.StartsWith(nameof(SpecialKeyType));
        }
        public static void Repeat(this IMainThread mt, Action keyAction, Func<bool> canRepeat, int delayMs, int repeatMs) {
            mt.Post(async () => {
                keyAction.Invoke();
                await Task.Delay(delayMs);
                while (canRepeat()) {
                    keyAction.Invoke();
                    await Task.Delay(repeatMs);
                }
            });
        }

        public static KeyboardLayoutType ToKeyboardLayoutType(this KeyboardFlags kbf) {
            if (kbf.HasFlag(KeyboardFlags.Normal)) {
                return KeyboardLayoutType.Normal;
            }
            if (kbf.HasFlag(KeyboardFlags.Numbers)) {
                return KeyboardLayoutType.PhoneNumber;
            }
            if (kbf.HasFlag(KeyboardFlags.Digits)) {
                return KeyboardLayoutType.Digits;
            }
            if (kbf.HasFlag(KeyboardFlags.Pin)) {
                return KeyboardLayoutType.Pin;
            }
            if (kbf.HasFlag(KeyboardFlags.Url)) {
                return KeyboardLayoutType.Url;
            }
            if (kbf.HasFlag(KeyboardFlags.Email)) {
                return KeyboardLayoutType.Email;
            }
            return KeyboardLayoutType.None;
        }
        #endregion

        #region Emoji
        public static string ToResxSafeText(this string text) {
            return text;
        }
        #endregion

        #region Color
        public static string AdjustAlpha(this string hex, byte newAlpha) {
            // assumes hex is either 3 or 4 channel and alpha is first channel
            int offset = hex.Length == 9 ? 3 : 1;
            return $"#{newAlpha.ToString("X2", CultureInfo.InvariantCulture)}{hex.Substring(offset, hex.Length - offset)}";
        }
        #endregion
    }
}
