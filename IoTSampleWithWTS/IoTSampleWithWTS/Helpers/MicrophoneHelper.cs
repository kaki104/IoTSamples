using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;

namespace IoTSampleWithWTS.Helpers
{
    internal class MicrophoneHelper
    {
        private const uint CHANNEL = 1;
        private const uint BITS_PER_SAMPLE = 16;
        private const uint SAMPLE_RATE = 16000;

        private AudioFileOutputNode _audioFileOutputNode;
        private AudioGraph _audioGraph;
        private string _outputFilename;
        private StorageFile _storageFile;

        public async Task StartRecordingAsync(string recordingFilename)
        {
            _outputFilename = recordingFilename;
            _storageFile = await ApplicationData.Current.LocalFolder
                .CreateFileAsync(_outputFilename, CreationCollisionOption.ReplaceExisting);

            await InitialiseAudioGraph();
            await InitialiseAudioFileOutputNode();
            await InitialiseAudioFeed();

            _audioGraph.Start();
        }

        private async Task InitialiseAudioGraph()
        {
            // Prompt the user for permission to access the microphone. This request will only happen
            // once, it will not re-prompt if the user rejects the permission.
            var permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained == false)
            {
                Debug.WriteLine("마이크 사용 권한을 얻지 못했습니다.");
                return;
            }

            var audioGraphSettings = new AudioGraphSettings(AudioRenderCategory.Media);
            var audioGraphResult = await AudioGraph.CreateAsync(audioGraphSettings);

            if (audioGraphResult.Status != AudioGraphCreationStatus.Success)
                throw new InvalidOperationException("AudioGraph creation error !");

            _audioGraph = audioGraphResult.Graph;
        }

        private async Task InitialiseAudioFileOutputNode()
        {
            var outputProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low);
            outputProfile.Audio = AudioEncodingProperties.CreatePcm(SAMPLE_RATE, CHANNEL, BITS_PER_SAMPLE);

            var outputResult = await _audioGraph.CreateFileOutputNodeAsync(_storageFile, outputProfile);

            if (outputResult.Status != AudioFileNodeCreationStatus.Success)
                throw new InvalidOperationException("AudioFileNode creation error !");

            _audioFileOutputNode = outputResult.FileOutputNode;
        }

        private async Task InitialiseAudioFeed()
        {
            var defaultAudioCaptureId = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default);
            var microphone = await DeviceInformation.CreateFromIdAsync(defaultAudioCaptureId);

            var inputProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
            var inputResult =
                await _audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Media, inputProfile.Audio, microphone);

            if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
                throw new InvalidOperationException("AudioDeviceNode creation error !");

            inputResult.DeviceInputNode.AddOutgoingConnection(_audioFileOutputNode);
        }

        public async Task StopRecordingAsync()
        {
            if (_audioGraph == null)
                throw new NullReferenceException("You have to start recording first !");

            if (_outputFilename == null)
                throw new NullReferenceException("You have to start recording first !");

            _audioGraph.Stop();
            await _audioFileOutputNode.FinalizeAsync();

            _audioGraph.Dispose();
            _audioGraph = null;
        }

        public async Task RemoveRecordingAsync()
        {
            if (_outputFilename == null)
                throw new NullReferenceException("You have to start recording first !");

            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(_outputFilename);
            if (item == null) return;
            await item.DeleteAsync();
            _outputFilename = string.Empty;
        }
    }
}
