using Ridebase.Models;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ridebase.Services.ApiClients;

public class AuthenticationApiClient : IAuthenticationClient
{
    private readonly IApiClient apiClient;
    public AuthenticationApiClient(IApiClient _apiClient)
    {
        apiClient = _apiClient;
    }

    public async Task<ApiResponse<User>> GetUserInfo(string userId)
    {
        var response = await apiClient.GetAsync<User>("user/registration");

        return response;
    }

    public async Task<ApiResponse<string>> SendToken(string token)
    {
        var response = await apiClient.PostAsync<string>("login_registration", "");

        return response;
    }
}
