using System.Net.Http.Json;

namespace Ridebase.Services.RestService;

public static class ApiResponseExtensions
{
    public static async Task<ApiResponse<T>> ToApiResponseAsync<T>(this HttpResponseMessage response)
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

    public static async Task<ApiResponse<string>> ToStringApiResponseAsync(this HttpResponseMessage response, string fallback)
    {
        var body = await response.Content.ReadAsStringAsync();

        return new ApiResponse<string>
        {
            IsSuccess = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode,
            Data = response.IsSuccessStatusCode ? string.IsNullOrWhiteSpace(body) ? fallback : body : string.Empty,
            ErrorMessage = response.IsSuccessStatusCode ? string.Empty : body
        };
    }
}