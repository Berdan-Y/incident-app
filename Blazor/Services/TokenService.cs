using System.IdentityModel.Tokens.Jwt;
using Microsoft.JSInterop;

namespace Blazor.Services;

public class TokenService : ITokenService
{
    private readonly IJSRuntime _jsRuntime;
    private string? _token;
    private const string TokenKey = "auth_token";
    private const string RolesKey = "user_roles";
    private List<string> _roles = new();
    private Task? _initializationTask;

    public event Action? LoggedIn;
    public event Action? LoggedOut;
    public bool IsLoggedIn => !string.IsNullOrEmpty(_token);

    public TokenService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _initializationTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            Console.WriteLine("TokenService: Starting initialization");
            _token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            Console.WriteLine($"TokenService: Retrieved token from storage: {(_token != null ? "Present" : "Not present")}");
            
            var roles = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", RolesKey);
            Console.WriteLine($"TokenService: Retrieved roles from storage: {roles}");

            if (!string.IsNullOrEmpty(roles))
            {
                _roles = roles.Split(',').Select(r => r.Trim()).ToList();
                Console.WriteLine($"TokenService: Parsed roles: {string.Join(", ", _roles)}");
            }

            if (!string.IsNullOrEmpty(_token))
            {
                if (!IsTokenExpired(_token))
                {
                    Console.WriteLine("TokenService: Valid token found, triggering LoggedIn");
                    LoggedIn?.Invoke();
                }
                else
                {
                    Console.WriteLine("TokenService: Token is expired, logging out");
                    await LogoutAsync();
                }
            }
            else
            {
                Console.WriteLine("TokenService: No token found, ensuring logged out state");
                await LogoutAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TokenService: Error during initialization: {ex.Message}");
            await LogoutAsync();
        }
    }

    public async Task EnsureInitialized()
    {
        if (_initializationTask != null)
        {
            await _initializationTask;
            _initializationTask = null;
        }
    }

    public string? GetToken()
    {
        var token = _token;
        Console.WriteLine($"TokenService.GetToken called, returning: {(token != null ? $"{token.Substring(0, Math.Min(20, token.Length))}..." : "null")}");
        return token;
    }

    public async Task SetTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            await LogoutAsync();
            return;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                await LogoutAsync();
                throw new Exception("Token is expired");
            }

            _token = token;
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
            Console.WriteLine("Token set in storage");
            LoggedIn?.Invoke();
        }
        catch
        {
            await LogoutAsync();
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        Console.WriteLine("TokenService.LogoutAsync called");
        _token = null;
        _roles.Clear();
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RolesKey);
        Console.WriteLine("Token and roles cleared from storage");
        LoggedOut?.Invoke();
        Console.WriteLine("LoggedOut event invoked");
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public async Task SetRolesAsync(string roles)
    {
        Console.WriteLine($"Setting roles: {roles}");
        if (string.IsNullOrEmpty(roles))
        {
            _roles.Clear();
            Console.WriteLine("Roles cleared (empty input)");
        }
        else
        {
            _roles = roles.Split(',').Select(r => r.Trim()).ToList();
            Console.WriteLine($"Roles set to: {string.Join(", ", _roles)}");
        }
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RolesKey, roles);
        Console.WriteLine("Roles saved to storage");
    }

    public List<string> GetRoles() => _roles;

    public bool HasRole(string role)
    {
        var hasRole = _roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        Console.WriteLine($"Checking role '{role}' - Result: {hasRole}, Available roles: {string.Join(", ", _roles)}");
        return hasRole;
    }

    public void RemoveToken()
    {
        _token = null;
    }

    public bool HasToken()
    {
        return !string.IsNullOrEmpty(_token);
    }

    public void SetToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            RemoveToken();
            return;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                RemoveToken();
                throw new Exception("Token is expired");
            }

            _token = token;
            Console.WriteLine("Token set in memory");
            LoggedIn?.Invoke();
            
            _ = _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        }
        catch
        {
            RemoveToken();
            throw;
        }
    }
} 