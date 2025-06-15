using MonkeyPaste.Common;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class KbDebugLogger {

        StringBuilder LogStringBuilder { get; set; }
        SharedPrefWrapper PrefService { get; }
        IKeyboardInputConnection InputConnection { get; }
        public KbDebugLogger(SharedPrefWrapper prefService, IKeyboardInputConnection ic) {
            PrefService = prefService;
            InputConnection = ic;
        }
        public void InitLogger() {
            void MpConsole_ConsoleLineAdded(object sender, string e) {
                if (LogStringBuilder == null) {
                    LogStringBuilder = new StringBuilder();
                }
                LogStringBuilder.Append(e);
            }
            if (PrefService.GetPrefValue<bool>(PrefKeys.DO_LOGGING)) {
                MpConsole.ConsoleLineAdded += MpConsole_ConsoleLineAdded;
            } else {
                MpConsole.ConsoleLineAdded -= MpConsole_ConsoleLineAdded;
            }
        }
        public void ClearLog() {
            if (LogStringBuilder is not { } lsb) {
                return;
            }
            lsb.Clear();

            Task.Run(async () => {
                await Task.Delay(500);
                InputConnection.OnAlert(string.Empty, ResourceStrings.U["LogClearedMessage"].value);
            });
        }
        public void CopyLogToClipboard() {
            if (LogStringBuilder is not { } lsb ||
                InputConnection is not { } ic ||
                ic.ClipboardHelper is not { } cbh) {
                return;
            }

            Task.Run(async () => {
                await cbh.SetTextAsync(lsb.ToString());
                InputConnection.OnAlert(string.Empty, ResourceStrings.U["LogOnClipboardMessage"].value);
            });
        }
    }
}
