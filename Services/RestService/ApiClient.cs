using System.Net.Http.Json;

namespace Ridebase.Services.RestService;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

    public async Task<ApiResponse<T>> GetAsync<T>(string url, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (headers.Any())
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        var response = await _httpClient.SendAsync(request);
        return await ProcessResponseAsync<T>(response);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string url, object data, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(data)
        };

        if (headers.Any())
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        var response = await _httpClient.SendAsync(request);
        return await ProcessResponseAsync<T>(response);
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string url, object data)
    {
        var response = await _httpClient.PutAsJsonAsync(url, data);
        return await ProcessResponseAsync<T>(response);
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string uri)
    {
        var response = await _httpClient.DeleteAsync(uri);
        return await ProcessResponseAsync<T>(response);
    }
}
