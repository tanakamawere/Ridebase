namespace Ridebase.Services.Interfaces;

public interface IStorageService
{
    Task<string> GetAuthTokenAsync();
    Task<string> GetUserIdAsync();

    Task SetAuthTokenAsync(string authToken);
    Task SetUserIdAsync(string userId);
}
