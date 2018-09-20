using System;
using System.Windows.Input;
using IoTSampleWithWTS.Helpers;

namespace IoTSampleWithWTS.ViewModels
{
    /// <summary>
    ///     C:\Users\MunChan Park\AppData\Local\Packages\2737D7A1-4344-4312-8073-17D872FC0E37_v0qv1p8057pc0\LocalState
    /// </summary>
    public class MainViewModel : Observable
    {
        private string _fileName;
        private bool _isRecoding;
        private string _responseText;

        public MainViewModel()
        {
            Init();
        }

        public ICommand StartRecodingCommand { get; set; }

        public ICommand StopRecodingCommand { get; set; }

        public string ResponseText
        {
            get => _responseText;
            set => Set(ref _responseText, value);
        }
        /// <summary>
        /// 레코딩
        /// </summary>
        public bool IsRecoding
        {
            get => _isRecoding;
            set => Set(ref _isRecoding, value);
        }

        private void Init()
        {
            StartRecodingCommand = new RelayCommand(async () =>
            {
                _fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".wav";
                await Singleton<MicrophoneHelper>.Instance.StartRecordingAsync(_fileName);
                IsRecoding = true;
            });

            StopRecodingCommand = new RelayCommand(async () =>
            {
                await Singleton<MicrophoneHelper>.Instance.StopRecordingAsync();
                IsRecoding = false;

                var result = await Singleton<BingSpeechHelper>.Instance.GetTextFromAudioAsync(_fileName);
                if (result == null) return;
                ResponseText = result;
            });
        }
    }
}
