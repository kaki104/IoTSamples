using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IoTSampleWithWTS.Helpers
{
    internal class AiOpenHelper
    {
        private readonly string _requestUri;

        public AiOpenHelper()
        {
            _requestUri = "http://aiopen.etri.re.kr:8000/WiseASR/Recognition";
        }

        public async Task<string> GetTextResultAsync(string recordedFilename)
        {
            //저장된 파일을 가지고 옴
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(recordedFilename);
            //파일 내용을 byte로 받음
            var bytes = File.ReadAllBytes(file.Path);
            //byte[]을 base64로 변경
            var audioContents = Convert.ToBase64String(bytes);

            using (var client = new HttpClient())
            {
                dynamic argument = new JObject();
                argument.language_code = "korean";
                argument.audio = audioContents;

                dynamic request = new JObject();
                request.access_key = "API 키를 입력하세요";
                request.argument = argument;

                var str = request.ToString();
                var content = new StringContent(str);
                content.Headers.ContentType.CharSet = "UTF-8";
                content.Headers.ContentType.MediaType = "application/json";

                try
                {
                    var response = await client.PostAsync(_requestUri, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var speechResults = JsonConvert.DeserializeObject<RecognitionResult>(responseContent);

                    content.Dispose();

                    return speechResults.ReturnObject.Recognized;
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

    internal class RecognitionResult
    {
        [JsonProperty(PropertyName = "request_id")]
        public int RequestId { get; set; }

        [JsonProperty(PropertyName = "result")]
        public int Result { get; set; }

        [JsonProperty(PropertyName = "return_object")]
        public ReturnObject ReturnObject { get; set; }
    }

    internal class ReturnObject
    {
        [JsonProperty(PropertyName = "recognized")]
        public string Recognized { get; set; }
    }
}
