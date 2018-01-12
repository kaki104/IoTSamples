using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Media.Playback;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml.Navigation;
using EnglishPractice2.Helpers;
using EnglishPractice2.Models;
using EnglishPractice2.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;

namespace EnglishPractice2.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        ///     현재 작업 중인 단문
        /// </summary>
        private Sentence _currentSentence;

        private VoiceInformation _englishVoice;

        private bool _hasStart;

        /// <summary>
        ///     한국어
        /// </summary>
        private bool _isKorean;

        /// <summary>
        ///     난수 생성용
        /// </summary>
        private Random _random;

        private IAsyncOperation<SpeechRecognitionResult> _recognitionOperation;
        private string _result;
        private string _showText;

        /// <summary>
        ///     음성 인식
        /// </summary>
        private SpeechRecognizer _speechRecognizer;

        private SpeechSynthesisStream _speechSynthesisStream;

        /// <summary>
        ///     TTS용 신디사이져
        /// </summary>
        private SpeechSynthesizer _synthesizer;

        /// <summary>
        ///     기본 생성자
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                ShowText = "내 커피를 만들어라";
                Result = "실패!! 잘 듣고 다시 따라 하세요";
            }
            else
            {
                Init();
            }
        }

        /// <summary>
        ///     시작 여부 확인
        /// </summary>
        public bool HasStart
        {
            get => _hasStart;
            set => Set(ref _hasStart, value);
        }

        /// <summary>
        ///     네비게이션 서비스
        /// </summary>
        private NavigationServiceEx NavigationService
            => SimpleIoc.Default.GetInstance<NavigationServiceEx>();

        /// <summary>
        ///     스피치 신디사이져 스트림
        /// </summary>
        public SpeechSynthesisStream SpeechSynthesisStream
        {
            get => _speechSynthesisStream;
            set => Set(ref _speechSynthesisStream, value);
        }

        /// <summary>
        ///     시작
        /// </summary>
        public ICommand StartCommand { get; set; }

        /// <summary>
        ///     종료
        /// </summary>
        public ICommand StopCommand { get; set; }

        /// <summary>
        ///     미디어 종료 커맨드
        /// </summary>
        public ICommand MediaEndedCommand { get; set; }

        /// <summary>
        ///     발음 해야할 단문
        /// </summary>
        public string ShowText
        {
            get => _showText;
            set => Set(ref _showText, value);
        }

        /// <summary>
        ///     결과
        /// </summary>
        public string Result
        {
            get => _result;
            set => Set(ref _result, value);
        }

        /// <summary>
        ///     초기화
        /// </summary>
        private void Init()
        {
            NavigationService.Navigated += NavigationService_Navigated;

            StartCommand = new RelayCommand(() =>
            {
                HasStart = true;
                SelectSentence();
            });

            StopCommand = new RelayCommand(() =>
                {
                    HasStart = false;
                    _recognitionOperation?.Cancel();
                },
                () => _speechRecognizer.State == SpeechRecognizerState.Idle);

            MediaEndedCommand = new RelayCommand(
                async () =>
                {
                    if(HasStart) await StartSpeechRecognizeAsync();
                });
        }

        /// <summary>
        ///     받아 쓰기 시작
        /// </summary>
        /// <returns></returns>
        private async Task StartSpeechRecognizeAsync()
        {
            _recognitionOperation = _speechRecognizer.RecognizeWithUIAsync();
            var speechRecognitionResult = await _recognitionOperation;

            // If successful, display the recognition result. A cancelled task should do nothing.
            if (speechRecognitionResult.Status == SpeechRecognitionResultStatus.Success)
            {
                if (speechRecognitionResult.Confidence == SpeechRecognitionConfidence.Medium ||
                    speechRecognitionResult.Confidence == SpeechRecognitionConfidence.High
                    && speechRecognitionResult.Text == _currentSentence.SpeakText)
                {
                    Result = "성공";
                    if(HasStart) SelectSentence();
                }
                else
                {
                    Result = "Discarded due to low/rejected Confidence: " + speechRecognitionResult.Text;
                    if(HasStart) await CreateSynthesisStreamAsync(_currentSentence.SpeakText, false);
                }
            }
            else
            {
                Result = string.Format("Speech Recognition Failed, Status: {0}",
                    speechRecognitionResult.Status.ToString());
                //연습 종료
                HasStart = false;
            }
        }

        /// <summary>
        ///     네비게이트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            //랜덤 초기화
            _random = new Random();
            //신디사이져 초기화
            _synthesizer = new SpeechSynthesizer();

            // Get all of the installed voices.
            var voices = SpeechSynthesizer.AllVoices;
            var voice = voices.FirstOrDefault(p => p.Language == "ko-KR");
            _synthesizer.Voice = voice
                                 ?? throw new NullReferenceException("한국어 음성을 찾을 수 없습니다.");

            _englishVoice = voices.FirstOrDefault(p => p.Language == "en-US"
                                                       && p.Gender == VoiceGender.Female);

            var supportedLanguages = SpeechRecognizer.SupportedGrammarLanguages;
            var enUS = supportedLanguages.FirstOrDefault(p => p.LanguageTag == "en-US")
                       ?? SpeechRecognizer.SystemSpeechLanguage;

            await InitializeRecognizerAsync(enUS);
        }

        /// <summary>
        ///     음성 인식 초기화
        /// </summary>
        /// <param name="recognizerLanguage"></param>
        /// <returns></returns>
        private async Task InitializeRecognizerAsync(Language recognizerLanguage)
        {
            Cleanup();

            try
            {
                // Create an instance of SpeechRecognizer.
                _speechRecognizer = new SpeechRecognizer(recognizerLanguage);

                // Provide feedback to the user about the state of the recognizer.
                _speechRecognizer.StateChanged += _speechRecognizer_StateChanged;

                var sentenceList = Singleton<SentenceHelper>.Instance.SentenceList;
                foreach (var sentence in sentenceList)
                    _speechRecognizer.Constraints.Add(
                        new SpeechRecognitionListConstraint(
                            new[] {sentence.SpeakText}, sentence.ShowText));

                // RecognizeWithUIAsync allows developers to customize the prompts.
                _speechRecognizer.UIOptions.ExampleText = "drink coffee";
                _speechRecognizer.UIOptions.ShowConfirmation = false;
                _speechRecognizer.UIOptions.IsReadBackEnabled = false;

                // Compile the constraint.
                var compilationResult = await _speechRecognizer.CompileConstraintsAsync();

                // Check to make sure that the constraints were in a proper format and the recognizer was able to compile it.
                if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    await CommonHelper.ShowMessageAsync("Error SpeechRecognizer.CompileConstraints");
                }
            }
            catch (Exception ex)
            {
                if ((uint) ex.HResult == CommonHelper.HResultRecognizerNotFound)
                    await CommonHelper.ShowMessageAsync("Speech Language pack for selected language not installed.");
                else
                    await CommonHelper.ShowMessageAsync(ex.Message, "Exception");
            }
        }

        private void _speechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine("Speech recognizer state: " + args.State);
        }

        /// <summary>
        ///     단문 선택 후 한글 보이스 출력
        /// </summary>
        private async void SelectSentence()
        {
            //출력할 단문 하나 랜덤 선택
            var sentenceList = Singleton<SentenceHelper>.Instance.SentenceList;
            var randomIndex = _random.Next(sentenceList.Count);
            _currentSentence = sentenceList[randomIndex];

            //화면에 한글 출력 후 보이스 출력
            ShowText = _currentSentence.ShowText;
            await CreateSynthesisStreamAsync(_currentSentence.ShowText);

            ////미디어 소스로 만들기
            //var mediaSource = MediaSource.CreateFromStream(stream, stream.ContentType);
            ////미디어 플레이백 아이템
            //MediaPlaybackItem = new MediaPlaybackItem(mediaSource);
            ////음성 출력~~
        }

        /// <summary>
        ///     음성 스트림 생성
        /// </summary>
        /// <returns></returns>
        private async Task CreateSynthesisStreamAsync(string speechText, bool isKorean = true)
        {
            _isKorean = isKorean;
            if (_isKorean)
            {
                SpeechSynthesisStream = await _synthesizer
                    .SynthesizeTextToStreamAsync(speechText);
            }
            else
            {
                var ssml = MakeSSML(speechText);
                SpeechSynthesisStream = await _synthesizer
                    .SynthesizeSsmlToStreamAsync(ssml);
            }
        }

        /// <summary>
        /// 클린업
        /// </summary>
        public override void Cleanup()
        {
            if (_speechRecognizer == null) return;
            // cleanup prior to re-initializing this scenario.
            _speechRecognizer.StateChanged -= _speechRecognizer_StateChanged;
            _speechRecognizer.Dispose();
            _speechRecognizer = null;
        }

        /// <summary>
        ///     SSML 생성
        /// </summary>
        /// <param name="englishText"></param>
        /// <returns></returns>
        private string MakeSSML(string englishText)
        {
            var ssml = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\"\r\n";
            ssml += "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\r\n";
            ssml +=
                "xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis  http://www.w3.org/TR/speech-synthesis/synthesis.xsd\"\r\n";
            ssml += "xml:lang=\"en-US\">\r\n";
            ssml += $"  <voice name=\"{_englishVoice.DisplayName}\"><break time=\"1000ms\"/>{englishText}</voice>\r\n";
            ssml += "</speak>";
            return ssml;
        }
    }
}
