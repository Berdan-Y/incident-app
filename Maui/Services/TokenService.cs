using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;

namespace Maui.Services;

public class TokenService : ITokenService, IDisposable
{
    private string? _token;
    private bool _isLoggedIn;
    private const string TokenKey = "auth_token";
    private readonly IPreferences _preferences;
    private readonly IDispatcherTimer _tokenValidationTimer;
    private const int TokenValidationIntervalSeconds = 5;

    public event EventHandler? LoggedIn;
    public event EventHandler? LoggedOut;

    public bool IsLoggedIn => _isLoggedIn;
    public bool IsInitializing { get; private set; } = true;

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
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(TokenKey);
            if (string.IsNullOrEmpty(token))
            {
                // Try fallback storage
                token = _preferences.Get<string>(TokenKey, null);
            }

            if (!string.IsNullOrEmpty(token) && !IsTokenExpired(token))
            {
                _token = token;
                _isLoggedIn = true;
                _tokenValidationTimer.Start();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
            // Try fallback storage
            var token = _preferences.Get<string>(TokenKey, null);
            if (!string.IsNullOrEmpty(token) && !IsTokenExpired(token))
            {
                _token = token;
                _isLoggedIn = true;
                _tokenValidationTimer.Start();
            }
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

            _isLoggedIn = true;
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

    public Task LogoutAsync()
    {
        _token = null;
        _isLoggedIn = false;
        _tokenValidationTimer.Stop();

        try
        {
            SecureStorage.Default.Remove(TokenKey);
        }
        catch
        {
            // Ignore secure storage errors during logout
        }
        _preferences.Remove(TokenKey);
        LoggedOut?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
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
            return jwtToken.Subject;
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
}