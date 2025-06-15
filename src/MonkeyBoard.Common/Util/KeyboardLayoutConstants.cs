using System;

namespace MonkeyBoard.Common {
    public static class KeyboardLayoutConstants {
        public static double DefaultCornerRadius_mobile => KeyConstants.PHI * 3;
        public static double DefaultCornerRadius_tablet => KeyConstants.PHI * (OperatingSystem.IsAndroid() ? 4:2);
        public static double DefaultFontSize => 16;
        public static double EmojiCompletionFontSize => 32;
        public static double TextCompletionFontSize1 => 16;
        public static double TextCompletionFontSize2 => 12;
        public static double TextCompletionFontSize3 => 8;
        public static double EmojiFontSize => OperatingSystem.IsAndroid() ? 20 : 16;
        public static double MenuTabItemFontSize => 16;
    }
}
