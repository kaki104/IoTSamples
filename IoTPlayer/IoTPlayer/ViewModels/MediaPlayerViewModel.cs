using System;
using System.Collections.Generic;
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
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Threading;
using IoTPlayer.Helpers;
using IoTPlayer.Services;
using FileAttributes = Windows.Storage.FileAttributes;

namespace IoTPlayer.ViewModels
{
    public class MediaPlayerViewModel : ViewModelBase
    {
        private const string SRGS_FILE_NAME = "SRGS.xml";

        /// <summary>
        ///     음성 인식
        /// </summary>
        private SpeechRecognizer _speechRecognizer;


        //// TODO WTS: Specify your video default and image here
        //private const string DefaultSource = "https://sec.ch9.ms/ch9/db15/43c9fbed-535e-4013-8a4a-a74cc00adb15/C9L12WinTemplateStudio_high.mp4";

        //// The poster image is displayed until the video is started
        //private const string DefaultPoster = "https://sec.ch9.ms/ch9/db15/43c9fbed-535e-4013-8a4a-a74cc00adb15/C9L12WinTemplateStudio_960.jpg";

        private IMediaPlaybackSource _source;

        public IMediaPlaybackSource Source
        {
            get { return _source; }
            set { Set(ref _source, value); }
        }

        private ImageSource _posterSource;
        private string _title;
        private string _album;
        private string _artist;
        private IReadOnlyList<StorageFile> _songList;
        /// <summary>
        /// 현재 재생 중인 파일
        /// </summary>
        private StorageFile _currentSong;

        private MediaPlayerState _currentPlaybackState;

        private string _firstFolderToken;

        /// <summary>
        /// 포스터 소스
        /// </summary>
        public ImageSource PosterSource
        {
            get { return _posterSource; }
            set { Set(ref _posterSource, value); }
        }

        public MediaPlayerViewModel()
        {
            //Source = MediaSource.CreateFromUri(new Uri(DefaultSource));
            //var musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            //PosterSource = DefaultPoster;

            Init();
        }

        /// <summary>
        /// 곡 제목
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { Set(ref _title ,value); }
        }
        /// <summary>
        /// 앨범 
        /// </summary>
        public string Album
        {
            get { return _album; }
            set { Set(ref _album ,value); }
        }
        /// <summary>
        /// 아티스트
        /// </summary>
        public string Artist
        {
            get { return _artist; }
            set { Set(ref _artist ,value); }
        }
        /// <summary>
        /// 초기화
        /// </summary>
        private void Init()
        {
            SimpleIoc.Default.GetInstance<NavigationServiceEx>().Navigated += Navigation_Navigated;

            PropertyChanged += MediaPlayerViewModel_PropertyChanged;
        }

        /// <summary>
        /// 프로퍼티 체인지 확인
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MediaPlayerViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentPlaybackState):
                    //todo : 할일이 있으면 하세요
                    Debug.WriteLine("CurrentPlaybackState {0}", CurrentPlaybackState);
                    break;
            }
        }

        private async void Navigation_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (Source != null) return;

            await InitPlaylistAsync();

            var supportedLanguages = SpeechRecognizer.SupportedGrammarLanguages;
            var enUS = supportedLanguages.FirstOrDefault(p => p.LanguageTag == "en-US")
                       ?? SpeechRecognizer.SystemSpeechLanguage;

            await InitializeRecognizerAsync(enUS);


        }
        /// <summary>
        /// 외장 드라이브에서 첫번째 폴더에서 mp3 파일 목록을 가지고 오도록...
        /// </summary>
        /// <returns></returns>
        private async Task<bool> InitPlaylistAsync()
        {
            var queryOption = new QueryOptions
                (CommonFileQuery.OrderByTitle, new[] {".mp3"});

            var q = KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOption);
            var c = await q.GetItemCountAsync();
            _songList = await q.GetFilesAsync(0, c);
            return true;
        }

        /// <summary>
        /// 플레이어 현재 상태
        /// </summary>
        public MediaPlayerState CurrentPlaybackState
        {
            get { return _currentPlaybackState; }
            set { Set(ref _currentPlaybackState ,value); }
        }

        /// <summary>
        /// 음악 재생
        /// </summary>
        /// <returns></returns>
        private async Task<bool> BeginPlaybackAsync()
        {
            if (_songList == null
                || _songList.Any() == false) return false;

            _currentSong = _songList.First();
            var musicInfo = await _currentSong.Properties.GetMusicPropertiesAsync();
            Title = musicInfo.Title;
            Album = musicInfo.Album;
            Artist = musicInfo.Artist;
            //앨범 포스터
            var thumb = await _currentSong.GetThumbnailAsync(ThumbnailMode.SingleItem);
            var bi = new BitmapImage();
            bi.SetSource(thumb);
            PosterSource = bi;

            Source = MediaSource.CreateFromStorageFile(_currentSong);
            return true;
        }

        /// <summary>
        /// 클린업
        /// </summary>
        public override void Cleanup()
        {
            SimpleIoc.Default.GetInstance<NavigationServiceEx>().Navigated -= Navigation_Navigated;

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

            var filter = args.Result.SemanticInterpretation.Properties.FirstOrDefault(p => p.Key == "FILTER");
            if (string.IsNullOrEmpty(filter.Key) == false)
            {

            }

            var command = action.Value.FirstOrDefault();
            Action executeAction = null;
            switch (command)
            {
                case "BEGIN":
                    executeAction = async () =>
                    {
                        var resunt = await BeginPlaybackAsync();
                        if (resunt == false)
                        {
                            //에러 메시지 출력?
                        }

                    };
                    break;
                case "PAUSE":
                    executeAction = async () =>
                    {
                        await CommonHelper.ShowMessageAsync("PAUSE playback");
                    };
                    break;
                case "STOP":
                    executeAction = async () =>
                    {
                        await CommonHelper.ShowMessageAsync("STOP playback");
                    };
                    break;
                case "NEXT":
                    executeAction = async () =>
                    {
                        await CommonHelper.ShowMessageAsync("NEXT playback");
                    };
                    break;
                case "PREVIEW":
                    executeAction = async () =>
                    {
                        await CommonHelper.ShowMessageAsync("PREVIEW playback");
                    };
                    break;
            }

            if (executeAction == null) return;

            await DispatcherHelper.RunAsync(executeAction);
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
