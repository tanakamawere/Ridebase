namespace Ridebase.Services.Interfaces;

public interface IStorageService
{
    Task<string> GetAuthTokenAsync();
    Task<string> GetRefreshTokenAsync();
    Task<string> GetUserIdAsync();

    Task SetAuthTokenAsync(string authToken);
    Task SetRefreshTokenAsync(string refreshToken);
    Task SetUserIdAsync(string userId);

    //Check if user is logged in
    Task<bool> IsLoggedInAsync();
}
