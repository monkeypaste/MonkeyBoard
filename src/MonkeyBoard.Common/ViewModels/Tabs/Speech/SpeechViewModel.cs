using Avalonia;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {

    public class SpeechViewModel : MenuTabViewModelBase {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        #endregion

        #region View Models
        #endregion

        #region Appearance
        protected override object TabIconSourceObj => "🎙️";
        public double SpeechTextSize => 42;

        public object IconSourceObj { get; private set; }

        string IconError => "⚠️";
        string IconLoading => "🙈";
        string IconReady => "🙉";
        string IconListening => "🐵";
        string IconVolQuiet => "👂";
        string IconVolMedium => "👂👂";
        string IconVolLoud => "👂👂👂";
        #endregion

        #region Layout

        #endregion

        #region State
        bool IsStarted { get; set; }
        bool IsListening { get; set; }
        public string SpeechText { get; private set; } = string.Empty;
        #endregion

        #region Models
        protected override MenuTabItemType TabItemType => MenuTabItemType.Speech;
        #endregion

        #endregion

        #region Events
        #endregion

        #region Constructors
        public SpeechViewModel(MenuViewModel parent) : base(parent) {
        }
        #endregion

        #region Public Methods

        public void StartSpeech() {
            AttachEvents();
            IconSourceObj = IconLoading;
            this.Renderer.RenderFrame(true);

            StartListening();
            IsStarted = true;
        }
        public void StopSpeech() {
            if(!IsStarted) {
                return;
            }
            StopListening();
            DetachEvents();
            IsStarted = false;
        }
        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        void StartListening() {
            if(InputConnection.SpeechToTextService is not ISpeechToTextConnection sttc) {
                return;
            }
            sttc.StartListening();
            IsListening = true;
        }
        void StopListening() {
            if(InputConnection.SpeechToTextService is not ISpeechToTextConnection sttc) {
                return;
            }
            sttc.StopListening();
            IsListening = false;
        }
        #region Speech Event Handlers

        void SpeechConnection_OnReady(object sender, System.EventArgs e) {
            IconSourceObj = IconReady;
            SpeechText = ResourceStrings.U["SpeechReadyLabel"].value;
            this.Renderer.RenderFrame(true);
        }
        void SpeechConnection_OnSpeechBegin(object sender, System.EventArgs e) {
            IconSourceObj = IconListening;
            SpeechText = ResourceStrings.U["SpeechListenLabel"].value;
            this.Renderer.RenderFrame(true);
        }
        void SpeechConnection_OnSpeechEnd(object sender, System.EventArgs e) {
            //LeftButtonIconSourceObj = IconReady;
            //SpeechText = ResourceStrings.D["SpeechReadyLabel"].value;


            this.Renderer.RenderFrame(true);
            InputConnection.MainThread.Post(async () => {
                await Task.Delay(2_000);
                StartListening();
            });
        }
        void SpeechConnection_OnText(object sender, string e) {
            if(InputConnection is not IKeyboardInputConnection ic ||
                ic.MainThread is not { } mt) {
                return;
            }
            ic.OnText(e);
            SpeechText = e;

            this.Renderer.RenderFrame(true);
        }

        void SpeechConnection_OnPartialText(object sender, string e) {
            SpeechText = e ?? string.Empty;
            this.Renderer.RenderFrame(true);
        }

        void SpeechConnection_OnVolumeChanged(object sender, double e) {
            //double quiet_max = 25f;
            //double medium_max = 65f;
            //object last_icon = LeftButtonIconSourceObj;

            //if (e < quiet_max) {
            //    // quiet
            //    LeftButtonIconSourceObj = IconVolQuiet;
            //} else if (e >= quiet_max && e < medium_max) {
            //    // medium
            //    LeftButtonIconSourceObj = IconVolMedium;
            //} else {
            //    // loud
            //    LeftButtonIconSourceObj = IconVolLoud;
            //}

            //if(LeftButtonIconSourceObj != last_icon) {
            //    this.Renderer.RenderFrame(true);
            //}

        }

        void SpeechConnection_OnError(object sender, string e) {
            //LeftButtonIconSourceObj = IconError;
            //SpeechText = e;
            //this.Renderer.RenderFrame(true);
        }
        #endregion

        void AttachEvents() {
            if(InputConnection.SpeechToTextService is not ISpeechToTextConnection SpeechConnection) {
                return;
            }
            SpeechConnection.OnReady += SpeechConnection_OnReady;
            SpeechConnection.OnSpeechBegin += SpeechConnection_OnSpeechBegin;
            SpeechConnection.OnSpeechEnd += SpeechConnection_OnSpeechEnd;
            SpeechConnection.OnText += SpeechConnection_OnText;
            SpeechConnection.OnPartialText += SpeechConnection_OnPartialText;
            SpeechConnection.OnVolumeChanged += SpeechConnection_OnVolumeChanged;
            SpeechConnection.OnError += SpeechConnection_OnError;
        }


        void DetachEvents() {
            if(InputConnection.SpeechToTextService is not ISpeechToTextConnection SpeechConnection) {
                return;
            }
            SpeechConnection.OnReady -= SpeechConnection_OnReady;
            SpeechConnection.OnSpeechBegin -= SpeechConnection_OnSpeechBegin;
            SpeechConnection.OnSpeechEnd -= SpeechConnection_OnSpeechEnd;
            SpeechConnection.OnText -= SpeechConnection_OnText;
            SpeechConnection.OnPartialText -= SpeechConnection_OnPartialText;
            SpeechConnection.OnVolumeChanged -= SpeechConnection_OnVolumeChanged;
            SpeechConnection.OnError -= SpeechConnection_OnError;
        }

        #endregion

        #region Commands
        #endregion
    }
}
