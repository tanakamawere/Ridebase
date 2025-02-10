using IdentityModel.OidcClient;
using Ridebase.Models;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Interfaces;

public interface IAuthenticationClient
{
    //Method to send token to API
    public Task<ApiResponse<string>> SendToken(string token);

    // Get user info
    public Task<ApiResponse<User>> GetUserInfo(string userId);
    public Task<ApiResponse<LoginResult>> LoginAsync();
    public Task LogoutAsync();
}
