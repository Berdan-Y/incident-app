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
            Debug.WriteLine("Token expired during validation check");
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
        Debug.WriteLine("TokenService.InitializeAsync called");
        try
        {
            // Try to get token from secure storage first
            var token = await SecureStorage.Default.GetAsync(TokenKey);
            var roles = await SecureStorage.Default.GetAsync(RolesKey);

            Debug.WriteLine($"Retrieved from SecureStorage - Token: {(token != null ? "exists" : "null")}, Roles: {roles}");

            // If not found in secure storage, try preferences
            if (token == null)
            {
                token = _preferences.Get<string>(TokenKey, null);
                roles = _preferences.Get<string>(RolesKey, null);
                Debug.WriteLine($"Retrieved from Preferences - Token: {(token != null ? "exists" : "null")}, Roles: {roles}");
            }

            if (!string.IsNullOrEmpty(token))
            {
                _token = token;
                IsLoggedIn = true;
                LoggedIn?.Invoke(this, EventArgs.Empty);

                if (!string.IsNullOrEmpty(roles))
                {
                    Roles = roles.Split(',').Select(r => r.Trim()).ToList();
                    Debug.WriteLine($"Set roles from storage: {string.Join(", ", Roles)}");
                }
            }
            else
            {
                IsLoggedIn = false;
                Roles = new List<string>();
                Debug.WriteLine("No token found, user is not logged in");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
            IsLoggedIn = false;
            Roles = new List<string>();
        }
        finally
        {
            IsInitializing = false;
            Debug.WriteLine($"InitializeAsync completed. IsLoggedIn: {IsLoggedIn}, Roles: {string.Join(", ", Roles)}");
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
            Debug.WriteLine($"Attempting to validate and store token: {token.Substring(0, Math.Min(50, token.Length))}...");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Debug.WriteLine($"Token validation results:");
            Debug.WriteLine($"Valid From: {jwtToken.ValidFrom}");
            Debug.WriteLine($"Valid To: {jwtToken.ValidTo}");
            Debug.WriteLine($"Current UTC Time: {DateTime.UtcNow}");

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                Debug.WriteLine("Token validation failed: Token is expired");
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
                Debug.WriteLine($"SecureStorage failed, using fallback: {ex.Message}");
                // Fallback to preferences if secure storage fails
                _preferences.Set(TokenKey, token);
            }

            IsLoggedIn = true;
            _tokenValidationTimer.Start();
            LoggedIn?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine("Token successfully stored and validated");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in SetTokenAsync: {ex.Message}");
            throw;
        }
    }

    public string? GetToken() => _token;

    private void ClearAllStorage()
    {
        Debug.WriteLine("Clearing all storage...");
        try
        {
            // Clear SecureStorage
            SecureStorage.Default.RemoveAll();
            Debug.WriteLine("SecureStorage cleared");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error clearing SecureStorage: {ex.Message}");
        }

        // Clear Preferences
        try
        {
            _preferences.Clear();
            Debug.WriteLine("Preferences cleared");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error clearing Preferences: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        Debug.WriteLine("LogoutAsync called");
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
        
        Debug.WriteLine("Logout completed");
        await Task.CompletedTask;
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var isExpired = jwtToken.ValidTo < DateTime.UtcNow;
            Debug.WriteLine($"Token expiry check - ValidTo: {jwtToken.ValidTo}, Current UTC: {DateTime.UtcNow}, IsExpired: {isExpired}");
            return isExpired;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking token expiration: {ex.Message}");
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
            Debug.WriteLine($"Error getting user ID from token: {ex.Message}");
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
            Debug.WriteLine($"Error getting username from token: {ex.Message}");
            return null;
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
            Debug.WriteLine($"Error getting email from token: {ex.Message}");
            return null;
        }
    }

    public async Task SetRolesAsync(string roles)
    {
        Debug.WriteLine($"TokenService.SetRolesAsync called with roles: {roles}");
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
            Debug.WriteLine($"Roles successfully stored. Current roles: {string.Join(", ", Roles)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error storing roles in SecureStorage: {ex.Message}");
            // Ensure roles are at least stored in preferences
            _preferences.Set(RolesKey, roles ?? string.Empty);
            Roles = string.IsNullOrEmpty(roles)
                ? new List<string>()
                : roles.Split(',').Select(r => r.Trim()).ToList();
        }
    }

    public List<string> GetRoles()
    {
        Debug.WriteLine("GetRoles called");
        Debug.WriteLine($"Returning cached roles: {string.Join(", ", _roles)}");
        return _roles;
    }

    public bool HasRole(string role)
    {
        if (string.IsNullOrEmpty(role)) return false;

        var roles = GetRoles();
        var hasRole = roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        Debug.WriteLine($"HasRole check - Role: {role}, User roles: {string.Join(", ", roles)}, Has role: {hasRole}");
        return hasRole;
    }
}