namespace MonkeyBoard.Bridge {
    public interface MpIKeyboardPermissionHelper
    {
        bool IsKeyboardActive();

        bool IsKeyboardEnabled();

        void ShowKeyboardSelector();

        void ShowKeyboardActivator();

        void ShowMicActivator();
    }
}