using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Storage;
using IoTSampleWithWTS.Interfaces;
using IoTSampleWithWTS.Models;
using Newtonsoft.Json;

namespace IoTSampleWithWTS.Helpers
{
    internal class BingSpeechHelper
    {
        private const string INTERACTIVE = "interactive";
        private const string CONVERSATION = "conversation";
        private const string DICTATION = "dictation";

        private readonly string _language = "ko-KR";
        private readonly string _requestUri;
        private IAuthenticationService _authenticationService;

        public BingSpeechHelper()
        {
            //&format=detailed
            _requestUri =
                $@"https://speech.platform.bing.com/speech/recognition/{
                    INTERACTIVE}/cognitiveservices/v1?language={
                        _language}";
        }

        public async Task<string> GetTextFromAudioAsync(string recordedFilename)
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(recordedFilename);

            using (var fileStream = new FileStream(file.Path, FileMode.Open, FileAccess.Read))
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                    client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/xml"));
                    client.DefaultRequestHeaders.TransferEncoding.Add(TransferCodingHeaderValue.Parse("chunked"));
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "API키를 입력하세요");

                    var content = new StreamContent(fileStream);
                    content.Headers.Add("ContentType", new[] {"audio/wav", "codec=audio/pcm", "samplerate=16000"});

                    try
                    {
                        var response = await client.PostAsync(_requestUri, content);
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var speechResults = JsonConvert.DeserializeObject<BinSpeechResult>(responseContent);

                        content.Dispose();

                        return speechResults.DisplayText;
                    }
                    catch (Exception e)
                    {
                        content.Dispose();
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
        }
    }
}
