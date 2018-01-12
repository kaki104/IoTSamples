using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Core;
using Windows.UI.Xaml;
using EnglishPractice2.Helpers;
using EnglishPractice2.Models;
using EnglishPractice2.Services;
using EnglishPractice2.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;

namespace EnglishPractice2.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _showText;
        private string _result;

        /// <summary>
        /// 시작 여부 확인
        /// </summary>
        private bool _hasStart;
        /// <summary>
        /// 난수 생성용
        /// </summary>
        private Random _random;
        /// <summary>
        /// 현재 작업 중인 단문
        /// </summary>
        private Sentence _currentSentence;
        /// <summary>
        /// TTS용 신디사이져
        /// </summary>
        private SpeechSynthesizer _synthesizer;
        /// <summary>
        /// 음성 인식
        /// </summary>
        private SpeechRecognizer _speechRecognizer;

        private VoiceInformation _englishVoice;
        private MediaPlaybackItem _mediaPlaybackItem;
        private SpeechSynthesisStream _speechSynthesisStream;
        private IAsyncOperation<SpeechRecognitionResult> _recognitionOperation;

        /// <summary>
        /// 기본 생성자
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
        /// 네비게이션 서비스
        /// </summary>
        private NavigationServiceEx NavigationService
            => SimpleIoc.Default.GetInstance<NavigationServiceEx>();

        /// <summary>
        /// 초기화
        /// </summary>
        private void Init()
        {
            NavigationService.Navigated += NavigationService_Navigated;

            StartCommand = new RelayCommand(StartCommandExecute);
            StopCommand = new RelayCommand(() =>
            {

            });
            MediaEndedCommand = new RelayCommand(MediaEndedCommandExecute);
        }
        /// <summary>
        /// 음성 출력 종료
        /// </summary>
        private async void MediaEndedCommandExecute()
        {
            //음성 입력 대기
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
                }
                else
                {
                    Result = "Discarded due to low/rejected Confidence: " + speechRecognitionResult.Text;
                }
            }
            else
            {
                Result = string.Format("Speech Recognition Failed, Status: {0}",
                        speechRecognitionResult.Status.ToString());
            }


        }

        /// <summary>
        /// 네비게이트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void NavigationService_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
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
        /// 음성 인식 초기화
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
                {
                    _speechRecognizer.Constraints.Add(
                        new SpeechRecognitionListConstraint(
                            new[] { sentence.SpeakText }, sentence.ShowText));
                }

                // RecognizeWithUIAsync allows developers to customize the prompts.
                _speechRecognizer.UIOptions.ExampleText = "drink coffee";
                _speechRecognizer.UIOptions.ShowConfirmation = false;

                // Compile the constraint.
                var compilationResult = await _speechRecognizer.CompileConstraintsAsync();

                // Check to make sure that the constraints were in a proper format and the recognizer was able to compile it.
                if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    await CommonHelper.ShowMessageAsync("Error SpeechRecognizer.CompileConstraints");
                    return;
                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == CommonHelper.HResultRecognizerNotFound)
                {
                    await CommonHelper.ShowMessageAsync("Speech Language pack for selected language not installed.");
                }
                else
                {
                    await CommonHelper.ShowMessageAsync(ex.Message, "Exception");
                }

            }
        }

        private void _speechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine("Speech recognizer state: " + args.State);
        }


        /// <summary>
        /// 미디어 플레이백 아이템
        /// </summary>
        public MediaPlaybackItem MediaPlaybackItem
        {
            get { return _mediaPlaybackItem; }
            set { Set(ref _mediaPlaybackItem ,value); }
        }
        /// <summary>
        /// 스피치 신디사이져 스트림
        /// </summary>
        public SpeechSynthesisStream SpeechSynthesisStream
        {
            get { return _speechSynthesisStream; }
            set { Set(ref _speechSynthesisStream ,value); }
        }

        /// <summary>
        /// 시작 버튼 클릭시 실행할 메소드
        /// </summary>
        private async void StartCommandExecute()
        {
            //출력할 단문 하나 랜덤 선택
            var sentenceList = Singleton<SentenceHelper>.Instance.SentenceList;
            var randomIndex = _random.Next(sentenceList.Count);
            _currentSentence = sentenceList[randomIndex];

            //화면에 한글 출력 후 보이스 출력
            ShowText = _currentSentence.ShowText;
            //음성 스트림 생성
            SpeechSynthesisStream = await _synthesizer
                .SynthesizeTextToStreamAsync(_currentSentence.ShowText);

            ////미디어 소스로 만들기
            //var mediaSource = MediaSource.CreateFromStream(stream, stream.ContentType);
            ////미디어 플레이백 아이템
            //MediaPlaybackItem = new MediaPlaybackItem(mediaSource);
            ////음성 출력~~
        }

        /// <summary>
        /// 시작
        /// </summary>
        public ICommand StartCommand { get; set; }
        /// <summary>
        /// 종료
        /// </summary>
        public ICommand StopCommand { get; set; }
        /// <summary>
        /// 미디어 종료 커맨드
        /// </summary>
        public ICommand MediaEndedCommand { get; set; }

        /// <summary>
        /// 발음 해야할 단문
        /// </summary>
        public string ShowText
        {
            get { return _showText; }
            set { Set(ref _showText ,value); }
        }
        /// <summary>
        /// 결과
        /// </summary>
        public string Result
        {
            get { return _result; }
            set { Set(ref _result ,value); }
        }

        public override void Cleanup()
        {
            if (_speechRecognizer == null) return;
            // cleanup prior to re-initializing this scenario.
            _speechRecognizer.StateChanged -= _speechRecognizer_StateChanged;
            _speechRecognizer.Dispose();
            _speechRecognizer = null;
        }
    }
}
