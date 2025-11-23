// Services/HttpClientExtensions.cs
using System.Text.Json;

namespace ReportService.Services
{
    public static class HttpClientExtensions
    {
        public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}