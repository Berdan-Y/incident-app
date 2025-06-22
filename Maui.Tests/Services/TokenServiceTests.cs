using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Maui.Core.Services;
using Xunit;

namespace Maui.Tests.Services;

// Test doubles for MAUI-specific types
public interface IPreferences
{
    void Clear();
    void Set<T>(string key, T value);
    T Get<T>(string key, T defaultValue);
}

public interface ISecureStorage
{
    Task<string> GetAsync(string key);
    Task SetAsync(string key, string value);
    void RemoveAll();
}

public interface IDispatcherTimer
{
    TimeSpan Interval { get; set; }
    void Start();
    void Stop();
    event EventHandler Tick;
}

public interface IDispatcher
{
    IDispatcherTimer CreateTimer();
}

public interface IApplication
{
    IDispatcher Dispatcher { get; }
}

public class TestPreferences : IPreferences
{
    private readonly Dictionary<string, object> _storage = new();

    public void Clear() => _storage.Clear();

    public T Get<T>(string key, T defaultValue)
    {
        if (_storage.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _storage[key] = value;
    }
}

public class TestSecureStorage : ISecureStorage
{
    private readonly Dictionary<string, string> _storage = new();

    public Task<string> GetAsync(string key)
    {
        return Task.FromResult(_storage.TryGetValue(key, out var value) ? value : null);
    }

    public Task SetAsync(string key, string value)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public void RemoveAll() => _storage.Clear();
}

public class TestDispatcherTimer : IDispatcherTimer
{
    public TimeSpan Interval { get; set; }
    public event EventHandler Tick;

    public void Start() { }
    public void Stop() { }

    public void SimulateTick()
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }
}

public class TestDispatcher : IDispatcher
{
    private readonly TestDispatcherTimer _timer = new();
    public IDispatcherTimer CreateTimer() => _timer;
}

public class TestApplication : IApplication
{
    public IDispatcher Dispatcher { get; } = new TestDispatcher();
}

public class TokenServiceTests
{
    private readonly string _validToken;
    private readonly string _expiredToken;
    private readonly string _validTokenWithClaims;

    public TokenServiceTests()
    {
        // Create a valid token that expires in 1 hour
        var validHandler = new JwtSecurityTokenHandler();
        var validToken = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(1),
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user-id"),
                new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
                new Claim("unique_name", "testuser")
            }
        );
        _validToken = validHandler.WriteToken(validToken);

        // Create an expired token
        var expiredHandler = new JwtSecurityTokenHandler();
        var expiredToken = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(-1)
        );
        _expiredToken = expiredHandler.WriteToken(expiredToken);

        // Create a valid token with all claims
        var claimsHandler = new JwtSecurityTokenHandler();
        var tokenWithClaims = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(1),
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user123"),
                new Claim("unique_name", "johndoe"),
                new Claim("email", "john@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User")
            }
        );
        _validTokenWithClaims = claimsHandler.WriteToken(tokenWithClaims);
    }

    [Fact]
    public async Task SetTokenAsync_WithValidToken_SetsTokenAndUpdatesState()
    {
        // Arrange
        var service = new TokenService();
        var propertyChangedFired = false;
        var loggedInFired = false;

        service.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TokenService.IsLoggedIn))
                propertyChangedFired = true;
        };

        service.LoggedIn += (s, e) => loggedInFired = true;

        // Act
        await service.SetTokenAsync(_validToken);

        // Assert
        Assert.True(service.IsLoggedIn);
        Assert.True(propertyChangedFired);
        Assert.True(loggedInFired);
        Assert.Equal(_validToken, service.GetToken());
    }

    [Fact]
    public async Task SetTokenAsync_WithExpiredToken_ThrowsException()
    {
        // Arrange
        var service = new TokenService();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.SetTokenAsync(_expiredToken));
        Assert.False(service.IsLoggedIn);
        Assert.Null(service.GetToken());
    }

    [Fact]
    public async Task SetTokenAsync_WithNullOrEmptyToken_LogsOut()
    {
        // Arrange
        var service = new TokenService();
        await service.SetTokenAsync(_validToken); // First set a valid token
        Assert.True(service.IsLoggedIn);

        var loggedOutFired = false;
        service.LoggedOut += (s, e) => loggedOutFired = true;

        // Act
        await service.SetTokenAsync(null);

        // Assert
        Assert.False(service.IsLoggedIn);
        Assert.True(loggedOutFired);
        Assert.Null(service.GetToken());
    }

    [Fact]
    public async Task LogoutAsync_ClearsTokenAndUpdatesState()
    {
        // Arrange
        var service = new TokenService();
        await service.SetTokenAsync(_validToken);
        Assert.True(service.IsLoggedIn);

        var loggedOutFired = false;
        service.LoggedOut += (s, e) => loggedOutFired = true;

        // Act
        await service.LogoutAsync();

        // Assert
        Assert.False(service.IsLoggedIn);
        Assert.True(loggedOutFired);
        Assert.Null(service.GetToken());
        Assert.Empty(service.GetRoles());
    }

    [Fact]
    public async Task SetRolesAsync_UpdatesRolesAndNotifiesChange()
    {
        // Arrange
        var service = new TokenService();
        var propertyChangedFired = false;

        service.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TokenService.Roles))
                propertyChangedFired = true;
        };

        // Act
        await service.SetRolesAsync("Admin,User");

        // Assert
        Assert.True(propertyChangedFired);
        Assert.Equal(2, service.GetRoles().Count);
        Assert.Contains("Admin", service.GetRoles());
        Assert.Contains("User", service.GetRoles());
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("User", true)]
    [InlineData("SuperAdmin", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public async Task HasRole_ReturnsExpectedResult(string role, bool expected)
    {
        // Arrange
        var service = new TokenService();
        await service.SetRolesAsync("Admin,User");

        // Act
        var result = service.HasRole(role);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetUserId_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var service = new TokenService();
        await service.SetTokenAsync(_validTokenWithClaims);

        // Act
        var userId = service.GetUserId();

        // Assert
        Assert.Equal("user123", userId);
    }

    [Fact]
    public async Task GetUserName_WithValidToken_ReturnsUserName()
    {
        // Arrange
        var service = new TokenService();
        await service.SetTokenAsync(_validTokenWithClaims);

        // Act
        var userName = service.GetUserName();

        // Assert
        Assert.Equal("johndoe", userName);
    }

    [Fact]
    public async Task GetEmail_WithValidToken_ReturnsEmail()
    {
        // Arrange
        var service = new TokenService();
        await service.SetTokenAsync(_validTokenWithClaims);

        // Act
        var email = service.GetEmail();

        // Assert
        Assert.Equal("john@example.com", email);
    }

    [Fact]
    public void Dispose_StopsTokenValidationTimer()
    {
        // Arrange
        var service = new TokenService();

        // Act & Assert - No exception should be thrown
        service.Dispose();
    }

    [Fact]
    public async Task GetUserId_WithNoToken_ReturnsNull()
    {
        // Arrange
        var service = new TokenService();

        // Act
        var userId = service.GetUserId();

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public async Task GetUserName_WithNoToken_ReturnsNull()
    {
        // Arrange
        var service = new TokenService();

        // Act
        var userName = service.GetUserName();

        // Assert
        Assert.Null(userName);
    }

    [Fact]
    public async Task GetEmail_WithNoToken_ReturnsNull()
    {
        // Arrange
        var service = new TokenService();

        // Act
        var email = service.GetEmail();

        // Assert
        Assert.Null(email);
    }

    [Fact]
    public async Task GetToken_WithNoToken_ReturnsNull()
    {
        // Arrange
        var service = new TokenService();

        // Act
        var token = service.GetToken();

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task GetRoles_WithNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var service = new TokenService();

        // Act
        var roles = service.GetRoles();

        // Assert
        Assert.NotNull(roles);
        Assert.Empty(roles);
    }
} 