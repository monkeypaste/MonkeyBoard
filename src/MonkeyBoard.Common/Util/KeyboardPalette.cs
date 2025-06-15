using MonkeyPaste.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MonkeyBoard.Common {
    public enum PaletteColorType {
        None = 0,
        Bg,
        InvalidBg,
        ValidBg,
        DefaultKeyBg,
        Fg,
        Fg2,
        HoldBg,
        HoldFocusBg,
        HoldFg,
        SpecialBg,
        SpecialSymbolBg,
        SpecialShiftBg,
        SpecialBackspaceBg,
        SpecialPrimaryBg,
        CursorControlSelectAllBg,
        CursorControlSelectAllOverBg,
        CursorControlSelectAllFg,
        CursorControlSelectAllOverFg,
        CursorControlBg,
        CursorControlFg,
        MenuBg,
        MenuFg,
        MenuItemPressedBg,
        MenuItemSelectedBg,
        EmojiMenuItemBg,
        EmojiMenuItem2Bg,
        EmojiSearchBg,
        EmojiSearchTextFg,
        EmojiSearchTextCaret,
        EmojiSearchTextSel,
        CustomBgColor,
        CustomBgImgBase64,
        KeyShadowBg,
        DragHandleBg,
        FloatBorderBg,
        FloatBorderPressedBg
    };

    public static class KeyboardPalette {

        #region Properties
        public static bool IsDark { get; private set; }

        static Dictionary<PaletteColorType, (string light, string dark)> _fullPalette;
        static Dictionary<PaletteColorType, string> _p;
        public static Dictionary<PaletteColorType, string> P {
            get {
                if (_fullPalette == null) {
                    _fullPalette = new() {
                        {
                            PaletteColorType.Bg,
                            ($"#{BG_A}FEEFFE", $"#{BG_A}121212")
                        },
                        {
                            PaletteColorType.CursorControlBg,
                            ($"#{CC_BG_A}141414", $"#{CC_BG_A}141414")
                        },
                        {
                            PaletteColorType.CursorControlFg,
                            ($"#{FG_A}FEEFFE", $"#{FG_A}FEEFFE")
                        },
                        {
                            PaletteColorType.CursorControlSelectAllBg,
                            ($"#00FAFAD2", $"#00FFD700")
                        },
                        {
                            PaletteColorType.CursorControlSelectAllFg,
                            ($"#{FG_A}FEEFFE", $"#{FG_A}FEEFFE")
                        },
                        {
                            PaletteColorType.CursorControlSelectAllOverBg,
                            ($"#{FG_A}FFA500", $"#{FG_A}FFA500")
                        },
                        {
                            PaletteColorType.CursorControlSelectAllOverFg,
                            ($"#{FG_A}122112", $"#{FG_A}121212")
                        },
                        {
                            PaletteColorType.DefaultKeyBg,
                            ($"#{FG_BG_A}E0E0E0", $"#{FG_BG_A}313131")
                        },
                        {
                            PaletteColorType.EmojiMenuItemBg,
                            ($"#{FG_BG_A}FFFFF0", $"#{FG_BG_A}434343")
                        },
                        {
                            PaletteColorType.EmojiMenuItem2Bg,
                            ($"#{FG_BG_A}DEEDDE", $"#{FG_BG_A}211221")
                        },
                        {
                            PaletteColorType.EmojiSearchBg,
                            ($"#{BG_A}F5F5F5", $"#{BG_A}011001")
                        },
                        {
                            PaletteColorType.EmojiSearchTextFg,
                            ($"#{FG_A}122112", $"#{FG_A}FEEFFE")
                        },
                        {
                            PaletteColorType.EmojiSearchTextCaret,
                            ($"#{FG_A}122112", $"#{FG_A}FEEFFE")
                        },
                        {
                            PaletteColorType.EmojiSearchTextSel,
                            ($"#640000FF", $"#64ADD8E6")
                        },
                        {
                            PaletteColorType.Fg,
                            ($"#{FG_A}122112", $"#{FG_A}FEEFFE")
                        },
                        {
                            PaletteColorType.Fg2,
                            ($"#{FG_A}696969", $"#{FG_A}DCDCDC")
                        },
                        {
                            PaletteColorType.DragHandleBg,
                            ($"#{FG_A}696969", $"#{FG_A}DCDCDC")
                        },
                        {
                            PaletteColorType.FloatBorderBg,
                            ($"#{BG_A}00FFFF", $"#{BG_A}FF00FF")
                        },
                        {
                            PaletteColorType.FloatBorderPressedBg,
                            ($"#{BG_A}FF00FF", $"#{BG_A}00FFFF")
                        },
                        {
                            PaletteColorType.HoldBg,
                            //($"#{PU_BG_A}FAFAD2", $"#{PU_BG_A}FFD700")
                            ($"#{PU_BG_A}FFD700", $"#{PU_BG_A}FFD700")
                        },
                        {
                            PaletteColorType.HoldFg,
                            //($"#{FG_A}122112", $"#{FG_A}121212")
                            ($"#{FG_A}121212", $"#{FG_A}121212")
                        },
                        {
                            PaletteColorType.HoldFocusBg,
                            //($"#{PU_BG_A}F0E68C", $"#{PU_BG_A}FFA500")
                            ($"#{PU_BG_A}FFA500", $"#{PU_BG_A}FFA500")
                        },
                        {
                            PaletteColorType.InvalidBg,
                            ($"#FFFF7F7F", $"#FF7F3F3F")
                        },
                        {
                            PaletteColorType.MenuBg,
                            ($"#{BG_A}FFFFF0", $"#{BG_A}211221")
                        },
                        {
                            PaletteColorType.MenuFg,
                            ($"#{FG_A}122112", $"#{FG_A}FEEFFE")
                        },
                        {
                            PaletteColorType.MenuItemPressedBg,
                            ($"#{FG_BG_A}DCDCDC", $"#{FG_BG_A}808080")
                        },
                        {
                            PaletteColorType.MenuItemSelectedBg,
                            ($"#{FG_BG_A}C9C9C9", $"#{FG_BG_A}433443")
                        },
                        {
                            PaletteColorType.SpecialBackspaceBg,
                            ($"#{FG_BG_A}FF7F7F", MpColorHelpers.GetDarkerHexColor($"#{FG_BG_A}FF7F7F"))
                        },
                        {
                            PaletteColorType.SpecialPrimaryBg,
                            ($"#{FG_BG_A}7FFF8E", MpColorHelpers.GetDarkerHexColor($"#{FG_BG_A}7FFF8E"))
                        },
                        {
                            PaletteColorType.SpecialShiftBg,
                            ($"#{FG_BG_A}7F92FF", MpColorHelpers.GetDarkerHexColor($"#{FG_BG_A}7F92FF"))
                        },
                        {
                            PaletteColorType.SpecialSymbolBg,
                            ($"#{FG_BG_A}FFE97F", MpColorHelpers.GetDarkerHexColor($"#{FG_BG_A}FFE97F"))
                        },
                        {
                            PaletteColorType.SpecialBg,
                            ($"#{FG_BG_A}B0B0B0", $"#{FG_BG_A}303030")
                        },
                        {
                            PaletteColorType.ValidBg,
                            ($"#FF7FFF8E", $"#FF3F7F47")
                        },
                    };

                    if (IsDark) {
                        _p = _fullPalette.ToDictionary(x => x.Key, x => x.Value.dark);
                    } else {
                        _p = _fullPalette.ToDictionary(x => x.Key, x => x.Value.light);
                    }

                    if (_p.TryGetValue(PaletteColorType.DefaultKeyBg, out var key_bg_hex)) {
                        string shadow_hex = IsDark ? MpColorHelpers.GetLighterHexColor(key_bg_hex) : MpColorHelpers.GetDarkerHexColor(key_bg_hex);
                        shadow_hex = shadow_hex.AdjustAlpha(SHADOW_ALPHA);
                        _p.AddOrReplace(PaletteColorType.KeyShadowBg, shadow_hex);
                    }


                    if (CustomBgHex is { } bg_hex && bg_hex.StartsWith("#")) {
                        _p.AddOrReplace(PaletteColorType.CustomBgColor, bg_hex.AdjustAlpha(ACTUAL_BG_ALPHA));
                    }

                    if (CustomBgImgPath is { } img_path &&
                        img_path.IsFile()) {
                        _p.AddOrReplace(PaletteColorType.CustomBgImgBase64, MpFileIo.ReadBytesFromFile(img_path).ToBase64String());
                        if (!_p.ContainsKey(PaletteColorType.CustomBgColor) &&
                            _p.TryGetValue(PaletteColorType.Bg, out var def_bg_hex)) {
                            // use default bg when no special color set
                            _p.AddOrReplace(PaletteColorType.CustomBgColor, def_bg_hex.AdjustAlpha(ACTUAL_BG_ALPHA));
                        }
                    }
                }
                return _p;
            }
        }

        public static string[] PalettePresets { get; } = new string[] {
            // from https://loading.io/color/feature/Spectral-8
            "#FFD53E4F",
            "#FFF46D43",
            "#FFFDAE61",
            "#FFFDAE61",
            "#FFE6F598",
            "#FFABDDA4",
            "#FF66C2A5",
            "#FF3288BD"
        };

        #region Common

        public static string Transparent { get; set; } = "#00000000";

        const byte DEF_BG_ALPHA = 255;
        const byte DEF_PU_BG_ALPHA = 255;
        const byte DEF_CC_BG_ALPHA = 200;
        const byte DEF_FG_BG_ALPHA = 255;
        const byte DEF_FG_ALPHA = 255;

        const byte SHADOW_ALPHA_FACTOR = 0;//105;

        static byte ACTUAL_BG_ALPHA { get; set; } = DEF_BG_ALPHA;
        static byte BG_ALPHA { get; set; } = DEF_BG_ALPHA;
        static byte PU_BG_ALPHA { get; set; } = DEF_PU_BG_ALPHA;
        static byte CC_BG_ALPHA { get; set; } = DEF_CC_BG_ALPHA;
        static byte FG_BG_ALPHA { get; set; } = DEF_FG_BG_ALPHA;
        static byte FG_ALPHA { get; set; } = DEF_FG_ALPHA;
        static byte SHADOW_ALPHA { get; set; } = default;

        static string CustomBgImgPath { get; set; }
        static string CustomBgHex { get; set; }

        static string BG_A => BG_ALPHA.ToString("X2");
        static string PU_BG_A => PU_BG_ALPHA.ToString("X2");
        static string FG_BG_A => FG_BG_ALPHA.ToString("X2");
        static string FG_A => FG_ALPHA.ToString("X2");
        static string CC_BG_A => CC_BG_ALPHA.ToString("X2");

        #endregion

        #endregion
        public static void SetTheme(
            string bgImgPath,
            string bgHex,
            bool isDark,
            byte bga,
            byte fga,
            byte fgbga) {
            string old_bg_path = CustomBgImgPath;
            string old_bg_hex = CustomBgHex;
            bool was_dark = IsDark;
            var old_bga = BG_ALPHA;
            var old_fga = FG_ALPHA;
            var old_fg_bg_a = FG_BG_ALPHA;
            var old_shadow_a = SHADOW_ALPHA; ;

            CustomBgImgPath = bgImgPath;
            CustomBgHex = bgHex;
            IsDark = isDark;

            // make bg 'barely visible' when custom color/img is being used
            double custom_bg_factor =
                CustomBgImgPath == default && CustomBgHex == default ?
                    1 : 0.1;
            ACTUAL_BG_ALPHA = bga;
            BG_ALPHA = (byte)(bga * custom_bg_factor);
            FG_BG_ALPHA = fgbga;
            FG_ALPHA = fga;

            // clamp alphas if user adjusted
            CC_BG_ALPHA = Math.Min(CC_BG_ALPHA, BG_ALPHA);
            SHADOW_ALPHA = (byte)Math.Max(0, FG_BG_ALPHA - SHADOW_ALPHA_FACTOR);

            bool needs_update =
                old_bg_path != CustomBgImgPath ||
                old_bg_hex != CustomBgHex ||
                was_dark != IsDark ||
                old_bga != BG_ALPHA ||
                old_fga != FG_ALPHA ||
                old_fg_bg_a != FG_BG_ALPHA ||
                old_shadow_a != SHADOW_ALPHA;
            if (!needs_update) {
                return;
            }
            _fullPalette = null;
            _ = P;

            //string suffix = IsDark ? "_dark" : "_light";
            //var props = typeof(KeyboardPalette).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            //var sb = new StringBuilder();
            //foreach (var src_prop in props) {
            //    if (src_prop.GetValue(null) is not string hex ||
            //        !src_prop.Name.EndsWith(suffix) ||
            //        src_prop.Name.Replace(suffix, string.Empty) is not { } trg_name ||
            //        props.FirstOrDefault(x => x.Name == trg_name) is not { } trg_prop) {
            //        if (src_prop.Name.EndsWith(suffix)) {
            //            // name is wrong or missing working prop
            //            Debugger.Break();
            //        }
            //        continue;
            //    }
            //    trg_prop.SetValue(null, hex);

            //    var pal_key = trg_prop.Name.Replace("Hex", string.Empty).ToEnum<PaletteColorType>();
            //    if(pal_key == PaletteColorType.None) {
            //        continue;
            //    }
            //    var pal_val = IsDark ? _fullPalette[pal_key].dark : _fullPalette[pal_key].light;
            //    if(pal_val != hex) {
            //        sb.AppendLine($"{pal_key}: {hex}");
            //    }
            //}
            //string errors = sb.ToString();

        }

        public static string Get(PaletteColorType colorType, bool isPressed, bool flip = false) {
            double factor = !isPressed ? 0 : IsDark && !flip ? 0.1 : -0.1;
            return MpColorHelpers.ChangeHexBrightness(P[colorType], factor, false);
        }

        public static void PrintPalette() {
            var sb = new StringBuilder();
            foreach (var prop in typeof(KeyboardPalette).GetProperties()) {
                if (prop.GetValue(null) is not string hex) {
                    continue;
                }
                sb.AppendLine($"public string {prop.Name.Replace("Brush", "Hex")} {{ get; }} = \"{hex}\";");
            }
            Debug.Write(sb.ToString());
        }
    }
}
