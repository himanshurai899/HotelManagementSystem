using System.Text.Json;

namespace HotelManagementSystem.Admin.Data
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public ApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiBaseUrl"];
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}{url}");
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseBody);
        }

        public async Task<T> PostAsync<T>(string url, T data)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}{url}", data);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseBody);
        }

        public async Task<T> PutAsync<T>(string url, T data)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}{url}", data);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseBody);
        }

        public async Task DeleteAsync(string url)
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}{url}");
            response.EnsureSuccessStatusCode();
        }
    }
}
