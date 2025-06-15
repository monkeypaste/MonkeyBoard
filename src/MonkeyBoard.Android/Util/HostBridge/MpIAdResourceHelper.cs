namespace MonkeyBoard.Android {
    public interface MpIAdResourceHelper {
        int DefaultFontResourceId { get; }
        int SettingsLayoutId { get; }
        bool IsTablet { get; }
    }

    public abstract class MpAdHostBridgeBase {
        protected static MpAdHostBridgeBase _instance;
        public static MpAdHostBridgeBase Instance =>
            _instance;
        public MpIAdResourceHelper HostResources { get; protected set; }
    }
}
