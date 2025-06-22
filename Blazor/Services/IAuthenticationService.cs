using Shared.Models.Dtos;
using Shared.Models.Enums;

namespace Blazor.Services;

public interface IAuthenticationService
{
    bool IsAuthenticated { get; }
    string? UserEmail { get; }
    Role? UserRole { get; }
    event Action? AuthenticationStateChanged;

    Task<bool> LoginAsync(LoginDto loginDto);
    Task<bool> RegisterAsync(RegisterDto registerDto);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetCurrentUserIdAsync();
    Task<string?> GetCurrentUserRoleAsync();
    bool IsInRole(string role);
    bool IsAdmin();
    void RequireAuthentication();
    void RequireAdmin();
}