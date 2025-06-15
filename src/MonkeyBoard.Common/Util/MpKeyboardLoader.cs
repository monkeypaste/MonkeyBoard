using MonkeyPaste.Common;
using System;

namespace MonkeyBoard.Common {
    public static class MpKeyboardLoader {
        public static void Load(IKeyboardInputConnection ic, bool isInitialStartup) {
            CultureManager.Init(ic);
            WordDb.LoadDbAsync(ic, false).FireAndForgetSafeAsync();
            ResourceStrings.Init(CultureManager.CurrentUiCulture,CultureManager.CurrentKbCulture);
        }
    }
}
