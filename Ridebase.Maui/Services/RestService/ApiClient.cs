using System.Net.Http.Json;

namespace Ridebase.Services.RestService;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);
        return await response.ToApiResponseAsync<T>();
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string url, object data)
    {
        var response = await _httpClient.PostAsJsonAsync(url, data);
        return await response.ToApiResponseAsync<T>();
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string url, object data)
    {
        var response = await _httpClient.PutAsJsonAsync(url, data);
        return await response.ToApiResponseAsync<T>();
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string url)
    {
        var response = await _httpClient.DeleteAsync(url);
        return await response.ToApiResponseAsync<T>();
    }
}
