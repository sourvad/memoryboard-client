using System.Net.Http;

namespace Memoryboard
{
    public static class HttpClientSingleton
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://memoryboard-api.fly.dev/api/")
        };

        public static HttpClient Instance => _httpClient; // Access the singleton HttpClient
    }
}
