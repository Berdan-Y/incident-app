using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;

namespace Maui.Services;

public interface ITokenService : INotifyPropertyChanged
{
    bool IsLoggedIn { get; }
    bool IsInitializing { get; }
    List<string> Roles { get; }
    event EventHandler? LoggedIn;
    event EventHandler? LoggedOut;
    Task SetTokenAsync(string token);
    string? GetToken();
    Task LogoutAsync();
    string? GetUserId();
    string? GetUserName();
    string? GetEmail();
    Task SetRolesAsync(string roles);
    List<string> GetRoles();
    bool HasRole(string role);
}