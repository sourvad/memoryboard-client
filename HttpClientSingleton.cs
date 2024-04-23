using System.Net.Http;

namespace Memoryboard
{
    public static class HttpClientSingleton
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://localhost:7193/api/")
        }; 

        public static HttpClient Instance => _httpClient; // Access the singleton HttpClient
    }
}
