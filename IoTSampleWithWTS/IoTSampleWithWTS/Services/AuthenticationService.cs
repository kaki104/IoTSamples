using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IoTSampleWithWTS.Interfaces;

namespace IoTSampleWithWTS.Services
{
    internal class AuthenticationService : IAuthenticationService
    {
        private readonly string _baseUri;
        private readonly int _refreshTokenMinutes;
        private string _subscriptionKey;

        private AuthenticationService()
        {
            _baseUri = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";
            _refreshTokenMinutes = 9;
        }

        public AuthenticationService(string subscriptionKey)
            : this()
        {
            InitializeService(subscriptionKey);
        }

        public string Token { get; private set; }

        public void InitializeService(string subscriptionKey)
        {
            this._subscriptionKey = subscriptionKey;
            var interval = TimeSpan.FromMinutes(_refreshTokenMinutes);

            RenewAccessTokenAsync(OnExpire, interval, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private async Task RenewAccessTokenAsync(Action onExpire, TimeSpan interval,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                onExpire?.Invoke();

                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, cancellationToken);
            }
        }

        private void OnExpire()
        {
            var task = Task.Run(() => FetchToken(_baseUri));

            task.Wait();

            Token = task.Result;
        }

        private async Task<string> FetchToken(string fetchUri)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key",
                    _subscriptionKey);

                var result = await client.PostAsync(fetchUri, null);

                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}
