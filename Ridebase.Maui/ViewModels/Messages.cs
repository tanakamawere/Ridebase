using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Ridebase.ViewModels;

public sealed class LoginSuccessData
{
    public required string UserId { get; init; }
    public required string AccessToken { get; init; }
    public required string DisplayName { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PictureUrl { get; init; } = string.Empty;
}

public class LoginSuccessMessage : ValueChangedMessage<LoginSuccessData>
{
    public LoginSuccessMessage(LoginSuccessData data) : base(data) { }
}

public class SignUpSuccessMessage : ValueChangedMessage<string>
{
    public SignUpSuccessMessage(string message) : base(message) { }
}
