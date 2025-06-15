using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using Android.Widget;
using AndroidX.Core.App;
using Avalonia.Controls;
using Java.Util;
using MonkeyPaste.Avalonia;
using MonkeyPaste.Common;
using MonkeyBoard.Common;
using System;
using System.Linq;
//using Xamarin.Essentials;

namespace MonkeyBoard.Android {
    public class SpeechToText : Java.Lang.Object, IRecognitionListener, ISpeechToTextConnection {
        #region Private Variables
        int RecordAudioRequestCode = 1;
        #endregion

        #region Constants
        public const string SPEECH_TO_TEXT_PERM_REQ = "speechToText";
        #endregion

        #region Statics
        #endregion

        #region Interfaces

        #region ISpeechToTextConnection Implementation

        public event EventHandler OnReady;
        public event EventHandler OnSpeechBegin;
        public event EventHandler<string> OnPartialText;
        public event EventHandler<string> OnText;
        public event EventHandler OnSpeechEnd;
        public event EventHandler<double> OnVolumeChanged;
        public event EventHandler<string> OnError;
        #endregion

        #region IRecognitionListener Implementation

        void IRecognitionListener.OnBeginningOfSpeech() {
            OnSpeechBegin?.Invoke(this, EventArgs.Empty);
        }


        void IRecognitionListener.OnEndOfSpeech() {
            OnSpeechEnd?.Invoke(this, EventArgs.Empty);
        }

        void IRecognitionListener.OnError(SpeechRecognizerError error) {
            OnError?.Invoke(this, error.ToString());
        }

        void IRecognitionListener.OnPartialResults(Bundle partialResults) {
            OnPartialText?.Invoke(this, GetBundleText(partialResults));
        }

        void IRecognitionListener.OnReadyForSpeech(Bundle @params) {
            OnReady?.Invoke(this, EventArgs.Empty);
        }

        void IRecognitionListener.OnResults(Bundle bundle) {
            OnText?.Invoke(this, GetBundleText(bundle));
        }

        void IRecognitionListener.OnRmsChanged(float rmsdB) {
            OnVolumeChanged?.Invoke(this, (double)rmsdB);
        }
        // unimplemented

        void IRecognitionListener.OnBufferReceived(byte[] buffer) {

        }
        void IRecognitionListener.OnEvent(int eventType, Bundle @params) {

        }
        #endregion
        #endregion

        #region Properties
        Context Context { get; set; }
        SpeechRecognizer SpeechRecognizer { get; set; }
        #endregion

        #region Events
        #endregion

        #region Constructors
        public SpeechToText(Context ctx) {
            Context = ctx;
        }
        #endregion

        #region Public Methods
        public void Init() {
            if(!CanRecord()) {
                RequestPermission();
            }

            SpeechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Context);
            SpeechRecognizer.SetRecognitionListener(this);
        }
        public void StartListening() {
            Intent sri = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            sri.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            sri.PutExtra(RecognizerIntent.ExtraLanguage, CultureManager.CurrentUiCulture);
            //sri.PutExtra(RecognizerIntent.EXTRA_LANGUAGE, "en-US”)

            SpeechRecognizer.StartListening(sri);
        }

        public void StopListening() {
            SpeechRecognizer?.StopListening();
        }

        public bool CanRecord() {
            return Context.CheckSelfPermission(Manifest.Permission.RecordAudio) == Permission.Granted;
        }


        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        void RequestPermission() {
            if(Context is not Activity activity) {
                if(Context != null &&
                    //!Context.IsDestroyed &&
                    MpPlatformKeyboardServices.KeyboardPermissionHelper is { } kph) {
                    kph.ShowMicActivator();
                    return;
                }
                Intent mainIntent = new Intent(Context, Context.GetType()); //typeof(MainActivity));
                mainIntent.AddFlags(ActivityFlags.NewTask);
                mainIntent.PutExtra(SPEECH_TO_TEXT_PERM_REQ, true);
                Context.StartActivity(mainIntent);
                return;
            }


            if(Build.VERSION.SdkInt >= BuildVersionCodes.M) {
                ActivityCompat.RequestPermissions(activity, new string[] { Manifest.Permission.RecordAudio }, RecordAudioRequestCode);
            }
        }

        string GetBundleText(Bundle bundle) {
            var data = bundle.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if(data.FirstOrDefault() is not { } result) {
                return string.Empty;
            }
            return result;
        }
        #endregion

        #region Commands
        #endregion


    }
}
