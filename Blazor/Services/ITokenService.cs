namespace Blazor.Services;

public interface ITokenService
{
    bool IsLoggedIn { get; }
    event Action? LoggedIn;
    event Action? LoggedOut;
    
    string? GetToken();
    void SetToken(string token);
    Task SetTokenAsync(string token);
    void RemoveToken();
    bool HasToken();
    Task LogoutAsync();
    
    Task SetRolesAsync(string roles);
    List<string> GetRoles();
    bool HasRole(string role);
} 