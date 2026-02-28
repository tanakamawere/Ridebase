using Ridebase.Models;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class MockUserBootstrapService : IUserBootstrapService
{
    private readonly IUserSessionService userSessionService;

    public MockUserBootstrapService(IUserSessionService _userSessionService)
    {
        userSessionService = _userSessionService;
    }

    public async Task<UserBootstrapState> ResolveAfterLoginAsync(string userId)
    {
        var state = await userSessionService.GetStateAsync();
        state.UserId = userId;

        if (string.IsNullOrWhiteSpace(state.UserId))
        {
            state.UserId = Guid.NewGuid().ToString("N");
        }

        if (state.Role == AppUserRole.Driver && !state.IsOnboarded)
        {
            state.IsDriverSubscribed = false;
        }

        return state;
    }
}
