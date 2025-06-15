using System.IdentityModel.Tokens.Jwt;

namespace Maui.Services;

public interface ITokenService
{
    bool IsLoggedIn { get; }
    bool IsInitializing { get; }
    event EventHandler? LoggedIn;
    event EventHandler? LoggedOut;
    Task SetTokenAsync(string token);
    string? GetToken();
    Task LogoutAsync();
    string? GetUserId();
    string? GetUserName();
    string? GetEmail();
}