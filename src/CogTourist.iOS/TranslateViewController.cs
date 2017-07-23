// This file has been autogenerated from a class added in the UI designer.

using System;

using Foundation;
using UIKit;
using Plugin.Permissions.Abstractions;
using Plugin.Permissions;
using System.Threading.Tasks;
using Speech;
using AVFoundation;
using CogTourist.Core;
using Microsoft.ProjectOxford.Vision.Contract;
using Plugin.TextToSpeech;
using Plugin.TextToSpeech.Abstractions;

namespace CogTourist
{
    public partial class TranslateViewController : UIViewController, ISFSpeechRecognizerDelegate
    {
        SFSpeechRecognizer speechRecognizer;

        SFSpeechAudioBufferRecognitionRequest recognitionRequest;
        SFSpeechRecognitionTask recognitionTask;
        AVAudioEngine audioEngine = new AVAudioEngine();

        TranslateService translator = new TranslateService();

        public TranslateViewController(IntPtr handle) : base(handle)
        {
        }

        [Export("speechRecognizer:availabilityDidChange:")]
        public void AvailabilityDidChange(SFSpeechRecognizer speechRecognizer, bool available)
        {
            //if (available)
            //    askQuestion.Enabled = true;
            //else
                //askQuestion.Enabled = false;
        }

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            speechRecognizer = new SFSpeechRecognizer(new NSLocale("en-US"));
            speechRecognizer.Delegate = this;

            englishText.Text = "";
            translatedText.Text = "";

            var microphonePermission = await AskForMicrophonePermission();
            var speechPermission = await AskForSpeechPermission();

            if (!microphonePermission || !speechPermission)
            {
                var popup = UIAlertController.Create("Permission Issue", "App doesn't have access the microphone or speech service", UIAlertControllerStyle.Alert);
                var okAction = UIAlertAction.Create("OK", UIAlertActionStyle.Default, null);

                popup.AddAction(okAction);

                PresentViewController(popup, true, null);

                askQuestion.Enabled = false;
            }

            askQuestion.TouchUpInside += AskQuestion_TouchUpInside;
        }

        async void AskQuestion_TouchUpInside(object sender, EventArgs e)
        {
            if (audioEngine.Running)
            {
                audioEngine.Stop();
                recognitionRequest.EndAudio();
                askQuestion.SetTitle("Ask Question", UIControlState.Normal);

                var frenchLocale = new CrossLocale
                {
                    Language = "fr-FR"
                };

                await CrossTextToSpeech.Current.Speak(translatedText.Text, frenchLocale);
            }
            else
            {
                askQuestion.SetTitle("Finished With Question", UIControlState.Normal);
                StartRecording();
            }
        }

        void StartRecording()
        {
            if (recognitionTask != null)
            {
                recognitionTask.Cancel();
                recognitionTask = null;
            }

            var audioSession = AVAudioSession.SharedInstance();

            try
            {
                NSError err;
                audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord);
                audioSession.SetMode(AVFoundation.AVAudioSession.ModeMeasurement, out err);
                audioSession.SetActive(true);
            }
            catch (Exception ex)
            {
                var s = ex.ToString();
            }

            recognitionRequest = new SFSpeechAudioBufferRecognitionRequest();

            var inputNode = audioEngine.InputNode;

            recognitionRequest.ShouldReportPartialResults = true;

            recognitionTask = speechRecognizer.GetRecognitionTask(recognitionRequest, (arg1, arg2) =>
            {
                var isFinal = false;

                if (arg1 != null)
                {
                    var inputtedText = arg1.BestTranscription.FormattedString;
                    englishText.Text = inputtedText;

                    translator.TranslateText(inputtedText, LanguageCodes.English, LanguageCodes.French).ContinueWith(async (arg) =>
                    {
                        var translated = await arg;

                        InvokeOnMainThread(() => translatedText.Text = translated);
                    });


                    isFinal = arg1.Final;
                }

                if (arg2 != null || isFinal)
                {
                    audioEngine.Stop();
                    inputNode.RemoveTapOnBus(0);
                    recognitionRequest = null;
                    recognitionTask = null;

                    askQuestion.Enabled = true;
                }
            });

            var recordingFormat = inputNode.GetBusOutputFormat(0);
            inputNode.InstallTapOnBus(0, 1024, recordingFormat, (buffer, when) =>
                        {
                            recognitionRequest?.Append(buffer);
                        });

            audioEngine.Prepare();

            try
            {
                NSError err = new NSError();
                audioEngine.StartAndReturnError(out err);

                englishText.Text = "OK, here we go!";
                translatedText.Text = "";
            }
            catch (Exception ex)
            {

            }
        }

        async Task<bool> AskForMicrophonePermission()
        {
            var microphoneStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Microphone);

            if (microphoneStatus != PermissionStatus.Granted)
            {
                var grantedPermissions = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Microphone);

                if (grantedPermissions.ContainsKey(Permission.Microphone))
                    microphoneStatus = grantedPermissions[Permission.Microphone];
            }

            return microphoneStatus == PermissionStatus.Granted;
        }

        async Task<bool> AskForSpeechPermission()
        {
            var speechStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Speech);

            if (speechStatus != PermissionStatus.Granted)
            {
                var grantedPermissions = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Speech);

                if (grantedPermissions.ContainsKey(Permission.Speech))
                    speechStatus = grantedPermissions[Permission.Speech];
            }

            return speechStatus == PermissionStatus.Granted;
        }
    }
}
