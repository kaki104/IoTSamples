using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.AllJoyn;
using Windows.Globalization;
using GalaSoft.MvvmLight;

using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.Storage.Search;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Threading;
using IoTPlayer.Helpers;
using IoTPlayer.Services;

namespace IoTPlayer.ViewModels
{
    public class MediaPlayerViewModel : ViewModelBase
    {
        private const string SRGS_FILE_NAME = "SRGS.xml";

        /// <summary>
        ///     음성 인식
        /// </summary>
        private SpeechRecognizer _speechRecognizer;


        // TODO WTS: Specify your video default and image here
        private const string DefaultSource = "https://sec.ch9.ms/ch9/db15/43c9fbed-535e-4013-8a4a-a74cc00adb15/C9L12WinTemplateStudio_high.mp4";

        // The poster image is displayed until the video is started
        private const string DefaultPoster = "https://sec.ch9.ms/ch9/db15/43c9fbed-535e-4013-8a4a-a74cc00adb15/C9L12WinTemplateStudio_960.jpg";

        private IMediaPlaybackSource _source;

        public IMediaPlaybackSource Source
        {
            get { return _source; }
            set { Set(ref _source, value); }
        }

        private string _posterSource;

        public string PosterSource
        {
            get { return _posterSource; }
            set { Set(ref _posterSource, value); }
        }

        public MediaPlayerViewModel()
        {
            //Source = MediaSource.CreateFromUri(new Uri(DefaultSource));
            //var musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            PosterSource = DefaultPoster;

            Init();
        }

        public NavigationServiceEx Navigation
            => SimpleIoc.Default.GetInstance<NavigationServiceEx>();

        private void Init()
        {
            Navigation.Navigated += Navigation_Navigated            ;
        }

        private async void Navigation_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (Source != null) return;

            //await BeginPlayback();

            var supportedLanguages = SpeechRecognizer.SupportedGrammarLanguages;
            var enUS = supportedLanguages.FirstOrDefault(p => p.LanguageTag == "en-US")
                       ?? SpeechRecognizer.SystemSpeechLanguage;

            await InitializeRecognizerAsync(enUS);

        }
        /// <summary>
        /// 음악 재생
        /// </summary>
        /// <returns></returns>
        private async Task<bool> BeginPlaybackAsync()
        {
            var musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            //첫번째 폴더
            var folder = musicLibrary.Folders.FirstOrDefault();
            if (folder == null) return false;
            var files = await folder.GetFilesAsync(CommonFileQuery.OrderByName);
            var song = files.FirstOrDefault(p => Path.GetExtension(p.Name) == ".mp3");
            Source = MediaSource.CreateFromStorageFile(song);
            return true;
        }

        /// <summary>
        /// 클린업
        /// </summary>
        public override void Cleanup()
        {
            if (_speechRecognizer == null) return;
            // cleanup prior to re-initializing this scenario.
            _speechRecognizer.StateChanged -= _speechRecognizer_StateChanged;
            _speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
            _speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
            _speechRecognizer.Dispose();
            _speechRecognizer = null;
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

                var grammarContentFile = await Package.Current.InstalledLocation.GetFileAsync(SRGS_FILE_NAME);
                if (grammarContentFile == null)
                    throw new NullReferenceException("SRGS 파일이 존재하지 않습니다.");

                // Create an instance of SpeechRecognizer.
                _speechRecognizer = new SpeechRecognizer(recognizerLanguage);

                // Provide feedback to the user about the state of the recognizer.
                _speechRecognizer.StateChanged += _speechRecognizer_StateChanged;
                _speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
                _speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;

                //SRGS 읽어서 조건에 추가
                var grammarConstraint = new SpeechRecognitionGrammarFileConstraint(grammarContentFile);
                _speechRecognizer.Constraints.Add(grammarConstraint);

                // Compile the constraint.
                var compilationResult = await _speechRecognizer.CompileConstraintsAsync();

                // Check to make sure that the constraints were in a proper format and the recognizer was able to compile it.
                if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    await CommonHelper.ShowMessageAsync("Error SpeechRecognizer.CompileConstraints");
                }

                //음성 인식 시작
                await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == CommonHelper.HResultRecognizerNotFound)
                    await CommonHelper.ShowMessageAsync("Speech Language pack for selected language not installed.");
                else
                    await CommonHelper.ShowMessageAsync(ex.Message, "Exception");
            }
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if(args.Result.Confidence == SpeechRecognitionConfidence.Low
               || args.Result.Confidence == SpeechRecognitionConfidence.Rejected) return;

            if (args.Result.SemanticInterpretation.Properties.Count == 0) return;

            var action = args.Result.SemanticInterpretation.Properties.FirstOrDefault(p => p.Key == "ACTION");
            if (string.IsNullOrEmpty(action.Key)) return;

            var command = action.Value.FirstOrDefault();
            switch (command)
            {
                case "begin playback":
                    await DispatcherHelper.RunAsync(async () =>
                    {
                        var resunt = await BeginPlaybackAsync();
                        if (resunt == false)
                        {
                            //에러 메시지 출력?
                        }

                    });
                    break;
            }
        }

        private void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void _speechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            
        }
    }
}
