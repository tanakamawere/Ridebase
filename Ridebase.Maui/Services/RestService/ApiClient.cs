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
        return await ProcessResponseAsync<T>(response);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string url, object data)
    {
        var response = await _httpClient.PostAsJsonAsync(url, data);
        return await ProcessResponseAsync<T>(response);
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string url, object data)
    {
        var response = await _httpClient.PutAsJsonAsync(url, data);
        return await ProcessResponseAsync<T>(response);
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string url)
    {
        var response = await _httpClient.DeleteAsync(url);
        return await ProcessResponseAsync<T>(response);
    }

    private async Task<ApiResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = new ApiResponse<T>
        {
            StatusCode = (int)response.StatusCode,
            IsSuccess = response.IsSuccessStatusCode
        };

        if (response.IsSuccessStatusCode)
        {
            apiResponse.Data = await response.Content.ReadFromJsonAsync<T>();
        }
        else
        {
            apiResponse.ErrorMessage = response.ReasonPhrase;
        }

        return apiResponse;
    }
}
