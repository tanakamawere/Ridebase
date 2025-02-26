using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

internal class StorageService : IStorageService
{
    public async Task<string> GetAuthTokenAsync()
    {
        return await SecureStorage.GetAsync("auth_token");
    }

    public async Task<string> GetUserIdAsync()
    {
        return await SecureStorage.GetAsync("user_id");
    }

    public async Task<bool> IsLoggedInAsync()
    {
        return !string.IsNullOrEmpty(await SecureStorage.GetAsync("auth_token"));
    }

    public async Task SetAuthTokenAsync(string authToken)
    {
        await SecureStorage.SetAsync("auth_token", authToken);
    }

    public async Task SetUserIdAsync(string userId)
    {
        await SecureStorage.SetAsync("user_id", userId);
    }
}
