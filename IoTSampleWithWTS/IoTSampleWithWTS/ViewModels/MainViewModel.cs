using System.Windows.Input;
using IoTSampleWithWTS.Helpers;

namespace IoTSampleWithWTS.ViewModels
{
    /// <summary>
    ///     C:\Users\MunChan Park\AppData\Local\Packages\2737D7A1-4344-4312-8073-17D872FC0E37_v0qv1p8057pc0\LocalState
    /// </summary>
    public class MainViewModel : Observable
    {
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

        private void Init()
        {
            StartRecodingCommand = new RelayCommand(async () =>
            {
                await Singleton<MicrophoneHelper>.Instance.StartRecordingAsync("test.wav");
            });

            StopRecodingCommand = new RelayCommand(async () =>
            {
                await Singleton<MicrophoneHelper>.Instance.StopRecordingAsync();

                //var result = await Singleton<BingSpeechHelper>.Instance.GetTextResultAsync("test.wav");
                var result = await Singleton<AiOpenHelper>.Instance.GetTextResultAsync("test.wav");
                if (result == null) return;
                ResponseText = result;
            });
        }
    }
}
