using MonkeyPaste.Common;

namespace MonkeyBoard.Desktop {
    public class DesktopKbPermissionHelper : MpIKeyboardPermissionHelper {
        public bool IsKeyboardActive() {
            return false;
        }

        public bool IsKeyboardEnabled() {
            return true;
        }

        public void ShowKeyboardSelector() {
        }

        public void ShowKeyboardActivator() {
        }

        public void ShowMicActivator() {
        }
    }
}