using Auth0.OidcClient;
using IdentityModel.OidcClient;
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
    private readonly Auth0Client auth0Client;
    public AuthenticationApiClient(IApiClient _apiClient, Auth0Client auth0)
    {
        apiClient = _apiClient;
        auth0Client = auth0;
    }

    public async Task<ApiResponse<User>> GetUserInfo(string userId)
    {
        var response = await apiClient.GetAsync<User>("user/registration");

        return response;
    }

    public async Task<ApiResponse<LoginResult>> LoginAsync()
    {
        var loginResult = await auth0Client.LoginAsync();

        //If successful, return the login result, otherwise API respnse with error
        if (loginResult.IsError)
        {
            return new ApiResponse<LoginResult>
            {
                IsSuccess = false,
                ErrorMessage = loginResult.Error
            };
        }
        else
        {
            return new ApiResponse<LoginResult>()
            {
                Data = loginResult,
                IsSuccess = true
            };
        }
    }

    public Task LogoutAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<string>> SendToken(string token)
    {
        var response = await apiClient.PostAsync<string>("login_registration", "");

        return response;
    }
}
