using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Maui.Core.Services;

public class TokenService : INotifyPropertyChanged, IDisposable
{
    private string? _token;
    private HashSet<string> _roles = new();
    private bool _isLoggedIn;
    private readonly Timer _tokenValidationTimer;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LoggedIn;
    public event EventHandler? LoggedOut;

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set
        {
            if (_isLoggedIn != value)
            {
                _isLoggedIn = value;
                OnPropertyChanged(nameof(IsLoggedIn));
            }
        }
    }

    public IReadOnlyCollection<string> Roles => _roles;

    public TokenService()
    {
        _tokenValidationTimer = new Timer(ValidateToken, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public async Task SetTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            await LogoutAsync();
            return;
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        if (jwtToken == null || jwtToken.ValidTo <= DateTime.UtcNow)
        {
            throw new Exception("Invalid or expired token");
        }

        _token = token;
        IsLoggedIn = true;
        OnLoggedIn();
    }

    public async Task LogoutAsync()
    {
        _token = null;
        _roles.Clear();
        IsLoggedIn = false;
        OnLoggedOut();
        await Task.CompletedTask;
    }

    public string? GetToken() => _token;

    public async Task SetRolesAsync(string roles)
    {
        _roles = new HashSet<string>(roles.Split(',', StringSplitOptions.RemoveEmptyEntries));
        OnPropertyChanged(nameof(Roles));
        await Task.CompletedTask;
    }

    public bool HasRole(string? role)
    {
        if (string.IsNullOrEmpty(role)) return false;
        return _roles.Contains(role);
    }

    public IReadOnlyCollection<string> GetRoles() => _roles;

    public string? GetUserId()
    {
        if (string.IsNullOrEmpty(_token)) return null;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(_token) as JwtSecurityToken;
        return jwtToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetUserName()
    {
        if (string.IsNullOrEmpty(_token)) return null;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(_token) as JwtSecurityToken;
        return jwtToken?.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;
    }

    public string? GetEmail()
    {
        if (string.IsNullOrEmpty(_token)) return null;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(_token) as JwtSecurityToken;
        return jwtToken?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
    }

    private void ValidateToken(object? state)
    {
        if (string.IsNullOrEmpty(_token)) return;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(_token) as JwtSecurityToken;

        if (jwtToken?.ValidTo <= DateTime.UtcNow)
        {
            LogoutAsync().Wait();
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnLoggedIn()
    {
        LoggedIn?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnLoggedOut()
    {
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _tokenValidationTimer?.Dispose();
    }
} 