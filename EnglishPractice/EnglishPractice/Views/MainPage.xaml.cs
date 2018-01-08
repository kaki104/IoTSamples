using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using EnglishPractice.Helpers;
using EnglishPractice.Models;
using ResourceManager = System.Resources.ResourceManager;

namespace EnglishPractice.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private SpeechRecognizer _speechRecognizer;
        private IAsyncOperation<SpeechRecognitionResult> _recognitionOperation;
        private IList<Case> _caseList;
        private int _currentIndex;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //네비게이트
            bool permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained)
            {
                // Enable the recognition buttons.
                //btnRecognizeWithUI.IsEnabled = true;
                //btnRecognizeWithoutUI.IsEnabled = true;

                var speechLanguage = SpeechRecognizer.SystemSpeechLanguage;
                var langTag = speechLanguage.LanguageTag;
                var speechContext = ResourceContext.GetForCurrentView();
                speechContext.Languages = new[] { langTag };

                var supportedLanguages = SpeechRecognizer.SupportedGrammarLanguages;
                var enUS = supportedLanguages.FirstOrDefault(p => p.LanguageTag == "en-US");
                if (enUS == null)
                {
                    enUS = SpeechRecognizer.SystemSpeechLanguage;
                }
                await InitializeRecognizerAsync(enUS);
            }
            else
            {
                //resultTextBlock.Visibility = Visibility.Visible;
                //resultTextBlock.Text = "Permission to access capture resources was not given by the user; please set the application setting in Settings->Privacy->Microphone.";
                //btnRecognizeWithUI.IsEnabled = false;
                //btnRecognizeWithoutUI.IsEnabled = false;
                //cbLanguageSelection.IsEnabled = false;
            }
        }


        private void PopulateLanguageDropdown()

        {
            // disable the callback so we don't accidentally trigger initialization of the recognizer
            // while initialization is already in progress.
            //isPopulatingLanguages = true;
            var defaultLanguage = SpeechRecognizer.SystemSpeechLanguage;
            var supportedLanguages = SpeechRecognizer.SupportedGrammarLanguages;

            //isPopulatingLanguages = false;
        }

        private async Task InitializeRecognizerAsync(Language recognizerLanguage)

        {

            if (_speechRecognizer != null)
            {

                // cleanup prior to re-initializing this scenario.
                _speechRecognizer.StateChanged -= _speechRecognizer_StateChanged;
                _speechRecognizer.Dispose();
                _speechRecognizer = null;
            }

            try
            {
                // Create an instance of SpeechRecognizer.
                _speechRecognizer = new SpeechRecognizer(recognizerLanguage);

                // Provide feedback to the user about the state of the recognizer.
                _speechRecognizer.StateChanged += _speechRecognizer_StateChanged;

                var shell = (ShellPage) Window.Current.Content;
                _caseList = shell.CaseList;
                foreach (var @case in _caseList)
                {
                    _speechRecognizer.Constraints.Add(
                        new SpeechRecognitionListConstraint(
                            new [] { @case.SpeakText }, @case.ShowText));
                }

                // RecognizeWithUIAsync allows developers to customize the prompts.
                _speechRecognizer.UIOptions.ExampleText = "drink coffee";

                // Compile the constraint.
                var compilationResult = await _speechRecognizer.CompileConstraintsAsync();

                // Check to make sure that the constraints were in a proper format and the recognizer was able to compile it.
                if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    //// Disable the recognition buttons.
                    //btnRecognizeWithUI.IsEnabled = false;
                    //btnRecognizeWithoutUI.IsEnabled = false;

                    //// Let the user know that the grammar didn't compile properly.
                    //resultTextBlock.Visibility = Visibility.Visible;
                    //resultTextBlock.Text = "Unable to compile grammar.";
                }
                else
                {
                    //btnRecognizeWithUI.IsEnabled = true;
                    //btnRecognizeWithoutUI.IsEnabled = true;

                    //resultTextBlock.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == ShellPage.HResultRecognizerNotFound)
                {
                    //btnRecognizeWithUI.IsEnabled = false;
                    //btnRecognizeWithoutUI.IsEnabled = false;

                    //resultTextBlock.Visibility = Visibility.Visible;
                    //resultTextBlock.Text = "Speech Language pack for selected language not installed.";
                }
                else
                {
                    await CommonHelper.ShowMessageAsync(ex.Message, "Exception");
                }

            }

        }

        private async void _speechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SpeechRecognizerState.Text = "Speech recognizer state: " + args.State;

            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ShowTextBlock.Text = _caseList[_currentIndex].ShowText;

            // Disable the UI while recognition is occurring, and provide feedback to the user about current state.
            NextButton.IsEnabled = false;
            WaitTextBlock.Visibility = Visibility.Visible;

            // Start recognition.
            try
            {
                // Save the recognition operation so we can cancel it (as it does not provide a blocking
                // UI, unlike RecognizeWithAsync()
                _recognitionOperation = _speechRecognizer.RecognizeAsync();
                var speechRecognitionResult = await _recognitionOperation;

                // If successful, display the recognition result. A cancelled task should do nothing.
                if (speechRecognitionResult.Status == SpeechRecognitionResultStatus.Success)
                {
                    string tag = "unknown";
                    if (speechRecognitionResult.Constraint != null)
                    {
                        // Only attempt to retreive the tag if we didn't hit the garbage rule.
                        tag = speechRecognitionResult.Constraint.Tag;
                    }
                    ResultTextBlock.Text =
                        string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})",
                            speechRecognitionResult.Text,
                            tag,
                            speechRecognitionResult.Confidence.ToString());
                    //ShowTextBlock.Text
                    ResultDetailTextBlock.Text = ShowTextBlock.Text == tag
                        ? "성공"
                        : "실패 [" + _caseList[_currentIndex].SpeakText + "]";

                    _currentIndex++;
                }
                else
                {
                    ResultTextBlock.Text =
                        string.Format("Speech Recognition Failed, Status: {0}",
                            speechRecognitionResult.Status.ToString());
                }
            }
            catch (TaskCanceledException exception)
            {
                // TaskCanceledException will be thrown if you exit the scenario while the recognizer is actively
                // processing speech. Since this happens here when we navigate out of the scenario, don't try to 
                // show a message dialog for this exception.
                System.Diagnostics.Debug.WriteLine("TaskCanceledException caught while recognition in progress (can be ignored):");
                System.Diagnostics.Debug.WriteLine(exception.ToString());
            }
            catch (Exception exception)
            {
                // Handle the speech privacy policy error.
                if ((uint)exception.HResult == ShellPage.HResultPrivacyStatementDeclined)
                {
                    ResultTextBlock.Text = "The privacy statement was declined.";
                }
                else
                {
                    await CommonHelper.ShowMessageAsync(exception.Message, "Exception");
                }
            }
            // Reset UI state.
            NextButton.IsEnabled = true;
            WaitTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
