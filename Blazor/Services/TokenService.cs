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
            _token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);

            var roles = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", RolesKey);

            if (!string.IsNullOrEmpty(roles))
            {
                _roles = roles.Split(',').Select(r => r.Trim()).ToList();
            }

            if (!string.IsNullOrEmpty(_token))
            {
                if (!IsTokenExpired(_token))
                {
                    LoggedIn?.Invoke();
                }
                else
                {
                    await LogoutAsync();
                }
            }
            else
            {
                await LogoutAsync();
            }
        }
        catch (Exception ex)
        {
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
        _token = null;
        _roles.Clear();
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RolesKey);
        LoggedOut?.Invoke();
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
        if (string.IsNullOrEmpty(roles))
        {
            _roles.Clear();
        }
        else
        {
            _roles = roles.Split(',').Select(r => r.Trim()).ToList();
        }
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RolesKey, roles);
    }

    public List<string> GetRoles() => _roles;

    public bool HasRole(string role)
    {
        var hasRole = _roles.Contains(role, StringComparer.OrdinalIgnoreCase);
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