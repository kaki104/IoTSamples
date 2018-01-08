using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using EnglishPractice.Helpers;
using EnglishPractice.Models;

namespace EnglishPractice.Views
{
    public sealed partial class PredefindedPage : Page, INotifyPropertyChanged
    {
        private SpeechRecognizer _speechRecognizer;
        private IAsyncOperation<SpeechRecognitionResult> _recognitionOperation;
        private IList<Case> _caseList;
        private int _currentIndex;
        private bool _isListening;
        private DispatcherTimer _timer;
        private Case _currentCase;


        public PredefindedPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //마이크로 폰 권한 체크
            bool permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained)
            {
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

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _timer.Tick += _timer_Tick;
        }

        /// <summary>
        /// 다음 케이스 출력
        /// </summary>
        private void NextCase()
        {
            _currentCase = _caseList[_currentIndex];
            ShowTextBlock.Text = _currentCase.ShowText;
            _currentIndex++;
            if (_currentIndex == _caseList.Count) _currentIndex = 0;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _timer.Stop();
            _timer.Tick -= _timer_Tick;

            Cleanup();
        }

        private void _timer_Tick(object sender, object e)
        {
            NextCase();
        }

        private void Cleanup()
        {
            if (_speechRecognizer == null) return;
            // cleanup prior to re-initializing this scenario.
            _speechRecognizer.StateChanged -= _speechRecognizer_StateChanged;
            _speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
            _speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
            _speechRecognizer.HypothesisGenerated -= _speechRecognizer_HypothesisGenerated;
            _speechRecognizer.Dispose();
            _speechRecognizer = null;
        }

        private async void _speechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SpeechRecognizerState.Text = "Speech recognizer state: " + args.State;

            });

        }

        private async Task InitializeRecognizerAsync(Language recognizerLanguage)
        {
            Cleanup();

            try
            {
                // Create an instance of SpeechRecognizer.
                _speechRecognizer = new SpeechRecognizer(recognizerLanguage);

                // Provide feedback to the user about the state of the recognizer.
                _speechRecognizer.StateChanged += _speechRecognizer_StateChanged;

                var shell = (ShellPage)Window.Current.Content;
                _caseList = shell.CaseList;

                //var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.WebSearch, "dictation");
                //_speechRecognizer.Constraints.Add(dictationConstraint);

                foreach (var @case in _caseList)
                {
                    _speechRecognizer.Constraints.Add(
                        new SpeechRecognitionListConstraint(
                            new[] { @case.SpeakText }, @case.ShowText));
                }

                // RecognizeWithUIAsync allows developers to customize the prompts.
                //_speechRecognizer.UIOptions.ExampleText = "drink coffee";


                // Compile the constraint.
                var compilationResult = await _speechRecognizer.CompileConstraintsAsync();

                // Check to make sure that the constraints were in a proper format and the recognizer was able to compile it.
                if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    await CommonHelper.ShowMessageAsync("Error SpeechRecognizer.CompileConstraints");
                    return;
                }
                // Handle continuous recognition events. Completed fires when various error states occur. ResultGenerated fires when
                // some recognized phrases occur, or the garbage rule is hit. HypothesisGenerated fires during recognition, and
                // allows us to provide incremental feedback based on what the user's currently saying.
                _speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
                _speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
                //_speechRecognizer.HypothesisGenerated += _speechRecognizer_HypothesisGenerated;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == ShellPage.HResultRecognizerNotFound)
                {
                    await CommonHelper.ShowMessageAsync("Speech Language pack for selected language not installed.");
                }
                else
                {
                    await CommonHelper.ShowMessageAsync(ex.Message, "Exception");
                }

            }
        }

        private async void _speechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            var hypothesis = args.Hypothesis.Text;

            // Update the textbox with the currently confirmed text, and the hypothesis combined.
            var textboxContent = hypothesis + " ...";
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Debug.WriteLine(textboxContent);
            });
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // We may choose to discard content that has low confidence, as that could indicate that we're picking up
            // noise via the microphone, or someone could be talking out of earshot.
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                args.Result.Confidence == SpeechRecognitionConfidence.High
                && _currentCase.SpeakText == args.Result.Text)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ResultTextBlock.Text = args.Result.Text;
                    ResultDetailTextBlock.Text = "성공";
                });
            }
            else
            {
                // In some scenarios, a developer may choose to ignore giving the user feedback in this case, if speech
                // is not the primary input mechanism for the application.
                // Here, just remove any hypothesis text by resetting it to the last known good.
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ResultTextBlock.Text = "Discarded due to low/rejected Confidence: " + args.Result.Text;
                    ResultDetailTextBlock.Text = "실패 [" + _currentCase.SpeakText + "]";
                });
            }
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                // If TimeoutExceeded occurs, the user has been silent for too long. We can use this to 
                // cancel recognition if the user in dictation mode and walks away from their device, etc.
                // In a global-command type scenario, this timeout won't apply automatically.
                // With dictation (no grammar in place) modes, the default timeout is 20 seconds.
                if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SpeechRecognizerState.Text = "Automatic Time Out of Dictation";
                        _isListening = false;
                    });
                }
                else
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SpeechRecognizerState.Text = "Continuous Recognition Completed: " + args.Status;
                        _isListening = false;
                    });
                }
            }
        }

        #region PropertyChanged

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

        #endregion

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (_speechRecognizer == null) return;

            if (_isListening == false)
            {
                //false
                // The recognizer can only start listening in a continuous fashion if the recognizer is currently idle.
                // This prevents an exception from occurring.
                if (_speechRecognizer.State == Windows.Media.SpeechRecognition.SpeechRecognizerState.Idle)
                {
                    StartStopButton.Content = "중지";
                    try
                    {
                        _isListening = true;
                        NextCase();
                        _timer.Start();

                        await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        if ((uint)ex.HResult == ShellPage.HResultRecognizerNotFound)
                        {
                            await CommonHelper.ShowMessageAsync("Speech Language pack for selected language not installed.");
                        }

                        if ((uint) ex.HResult == -2147199735)
                        {
                            await CommonHelper.ShowMessageAsync(ex.Message, "Exception");
                        }
                        else
                        {
                            await CommonHelper.ShowMessageAsync(ex.Message, "Exception");
                        }
                        _timer.Stop();
                        _isListening = false;
                        StartStopButton.Content = "시작";
                    }
                }
            }
            else
            {
                //true
                _isListening = false;
                _timer.Stop();
                StartStopButton.Content = "시작";

                if (_speechRecognizer.State != Windows.Media.SpeechRecognition.SpeechRecognizerState.Idle)
                {
                    // Cancelling recognition prevents any currently recognized speech from
                    // generating a ResultGenerated event. StopAsync() will allow the final session to 
                    // complete.
                    try
                    {
                        await _speechRecognizer.ContinuousRecognitionSession.StopAsync();

                        // Ensure we don't leave any hypothesis text behind
                        //dictationTextBox.Text = dictatedTextBuilder.ToString();
                    }
                    catch (Exception exception)
                    {
                        await CommonHelper.ShowMessageAsync(exception.Message, "Exception");
                        _timer.Stop();
                    }
                }
            }
        }

    }
}
