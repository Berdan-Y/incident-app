using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;
using System.Security.Claims;
using System.ComponentModel;
using Microsoft.Maui.Controls;

namespace Maui.Services;

public class TokenService : ITokenService, IDisposable, INotifyPropertyChanged
{
    private string? _token;
    private bool _isLoggedIn;
    private const string TokenKey = "auth_token";
    private const string RolesKey = "user_roles";
    private readonly IPreferences _preferences;
    private readonly IDispatcherTimer _tokenValidationTimer;
    private const int TokenValidationIntervalSeconds = 5;
    private List<string> _roles = new();

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoggedIn)));
            }
        }
    }
    public bool IsInitializing { get; private set; } = true;

    public List<string> Roles
    {
        get => _roles;
        private set
        {
            _roles = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Roles)));
        }
    }

    public TokenService()
    {
        _preferences = Preferences.Default;

        // Initialize token validation timer
        _tokenValidationTimer = Application.Current.Dispatcher.CreateTimer();
        _tokenValidationTimer.Interval = TimeSpan.FromSeconds(TokenValidationIntervalSeconds);
        _tokenValidationTimer.Tick += TokenValidationTimer_Tick;

        _ = InitializeAsync();
    }

    private async void TokenValidationTimer_Tick(object sender, EventArgs e)
    {
        if (!_isLoggedIn || string.IsNullOrEmpty(_token))
        {
            _tokenValidationTimer.Stop();
            return;
        }

        if (IsTokenExpired(_token))
        {
            await LogoutAsync();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.DisplayAlert("Session Expired", "Your session has expired. Please log in again.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            });
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Try to get token from secure storage first
            var token = await SecureStorage.Default.GetAsync(TokenKey);
            var roles = await SecureStorage.Default.GetAsync(RolesKey);

            // If not found in secure storage, try preferences
            if (token == null)
            {
                token = _preferences.Get<string>(TokenKey, null);
                roles = _preferences.Get<string>(RolesKey, null);
            }

            if (!string.IsNullOrEmpty(token))
            {
                _token = token;
                IsLoggedIn = true;
                LoggedIn?.Invoke(this, EventArgs.Empty);

                if (!string.IsNullOrEmpty(roles))
                {
                    Roles = roles.Split(',').Select(r => r.Trim()).ToList();
                }
            }
            else
            {
                IsLoggedIn = false;
                Roles = new List<string>();
            }
        }
        catch (Exception ex)
        {
            IsLoggedIn = false;
            Roles = new List<string>();
        }
        finally
        {
            IsInitializing = false;
        }
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
                throw new Exception($"Token is expired. Token expiry: {jwtToken.ValidTo}, Current time: {DateTime.UtcNow}");
            }

            _token = token;
            try
            {
                await SecureStorage.Default.SetAsync(TokenKey, token);
            }
            catch (Exception ex)
            {
                // Fallback to preferences if secure storage fails
                _preferences.Set(TokenKey, token);
            }

            IsLoggedIn = true;
            _tokenValidationTimer.Start();
            LoggedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public string? GetToken() => _token;

    private void ClearAllStorage()
    {
        try
        {
            // Clear SecureStorage
            SecureStorage.Default.RemoveAll();
        }
        catch (Exception ex)
        {
        }

        // Clear Preferences
        try
        {
            _preferences.Clear();
        }
        catch (Exception ex)
        {
        }
    }

    public async Task LogoutAsync()
    {
        _tokenValidationTimer.Stop();

        // Clear the token and roles
        _token = null;
        Roles.Clear();

        // Clear all storage
        ClearAllStorage();

        // Update state
        IsLoggedIn = false;

        // Notify listeners
        LoggedOut?.Invoke(this, EventArgs.Empty);

        await Task.CompletedTask;
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var isExpired = jwtToken.ValidTo < DateTime.UtcNow;
            return isExpired;
        }
        catch (Exception ex)
        {
            return true;
        }
    }

    public void Dispose()
    {
        _tokenValidationTimer.Stop();
        _tokenValidationTimer.Tick -= TokenValidationTimer_Tick;
    }

    public string? GetUserId()
    {
        if (string.IsNullOrEmpty(_token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(_token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public string? GetUserName()
    {
        if (string.IsNullOrEmpty(_token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(_token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;
        }
        catch (Exception ex)
        {
        }
    }

    public string? GetEmail()
    {
        if (string.IsNullOrEmpty(_token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(_token);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "emails")?.Value
                       ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "upn")?.Value;
            return email;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task SetRolesAsync(string roles)
    {
        try
        {
            if (string.IsNullOrEmpty(roles))
            {
                await SecureStorage.Default.SetAsync(RolesKey, string.Empty);
                _preferences.Set(RolesKey, string.Empty);
                Roles = new List<string>();
            }
            else
            {
                await SecureStorage.Default.SetAsync(RolesKey, roles);
                // Fallback storage
                _preferences.Set(RolesKey, roles);
                Roles = roles.Split(',').Select(r => r.Trim()).ToList();
            }
        }
        catch (Exception ex)
        {
            // Ensure roles are at least stored in preferences
            _preferences.Set(RolesKey, roles ?? string.Empty);
            Roles = string.IsNullOrEmpty(roles)
                ? new List<string>()
                : roles.Split(',').Select(r => r.Trim()).ToList();
        }
    }

    public List<string> GetRoles()
    {
        return _roles;
    }

    public bool HasRole(string role)
    {
        if (string.IsNullOrEmpty(role)) return false;

        var roles = GetRoles();
        var hasRole = roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        return hasRole;
    }
}