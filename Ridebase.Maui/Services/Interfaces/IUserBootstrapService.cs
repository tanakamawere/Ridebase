using Ridebase.Models;

namespace Ridebase.Services.Interfaces;

public interface IUserBootstrapService
{
    Task<UserBootstrapState> ResolveAfterLoginAsync(string userId, string? accessToken = null);
}
