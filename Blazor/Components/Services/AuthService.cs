using Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using Blazor.Services;
using Shared.Models.Dtos;

namespace Blazor.Components.Services;

public class AuthService
{
    private readonly IApiClient _apiClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApiClient apiClient,
        IJSRuntime jsRuntime,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigationManager,
        ILogger<AuthService> logger)
    {
        _apiClient = apiClient;
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    private bool CanAccessJavaScript()
    {
        return _jsRuntime.GetType().Name != "UnsupportedJavaScriptRuntime";
    }

    public async Task<LoginResult> Login(string email, string password)
    {
        try
        {
            _logger.LogInformation("Starting login process for user: {Email}", email);
            
            var loginDto = new LoginDto { Email = email, Password = password };
            var response = await _apiClient.LoginAsync(loginDto);
            _logger.LogInformation("Login request sent, received response: {HasToken}", !string.IsNullOrEmpty(response?.Token));

            if (response?.Token != null)
            {
                _logger.LogInformation("Login successful for user: {Email}, Token received: {HasToken}", email, !string.IsNullOrEmpty(response.Token));
                
                // Convert API response to internal LoginResult format
                var result = new LoginResult
                {
                    Token = response.Token,
                    UserId = response.UserId,
                    Roles = response.Roles,
                    Message = response.Message
                };
                
                _logger.LogInformation("Received token: {Token}", result.Token);
                
                if (result.Roles != null && !result.Roles.Contains("Admin"))
                {
                    _logger.LogWarning("Access denied for user: {Email} - Admin privileges required", email);
                    await Logout();
                    throw new Exception("Access denied. Admin privileges required.");
                }
                
                _logger.LogInformation("Storing authentication data for user: {Email}", email);
                
                // Only try to store in localStorage if JavaScript is available
                if (CanAccessJavaScript())
                {
                    try
                    {
                        // Store the token and user info in local storage
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
                        _logger.LogInformation("Successfully stored token in localStorage: {Token}", result.Token);
                        
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", result.UserId.ToString());
                        _logger.LogInformation("Successfully stored userId in localStorage");
                        
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userRoles", System.Text.Json.JsonSerializer.Serialize(result.Roles));
                        _logger.LogInformation("Successfully stored userRoles in localStorage");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
                    {
                        _logger.LogWarning("Cannot access localStorage during prerendering, skipping storage");
                        // Continue anyway, the state will be updated
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to store authentication data in localStorage");
                        throw;
                    }
                }
                else
                {
                    _logger.LogInformation("JavaScript not available, skipping localStorage storage");
                }

                // Update authentication state
                var authState = (CustomAuthStateProvider)_authStateProvider;
                await authState.UpdateAuthenticationState(result);
                _logger.LogInformation("Successfully updated authentication state");
                
                return result;
            }

            _logger.LogWarning("Login failed for user: {Email} - No token received", email);
            throw new Exception("Invalid email or password");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during login for user: {Email}", email);
            throw new Exception("Invalid email or password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Email}", email);
            throw;
        }
    }

    public async Task Logout()
    {
        try
        {
            _logger.LogInformation("Attempting logout");
            
            await _apiClient.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Ignore errors during logout
        }
        finally
        {
            _logger.LogInformation("Clearing authentication data");
            
            // Clear local storage only if JavaScript is available
            if (CanAccessJavaScript())
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userRoles");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
                {
                    _logger.LogWarning("Cannot access localStorage during prerendering for logout");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing localStorage during logout");
                }
            }

            // Update authentication state
            var authState = (CustomAuthStateProvider)_authStateProvider;
            await authState.Logout();

            // Only navigate if NavigationManager is available
            try
            {
                _navigationManager.NavigateTo("/login");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("has not been initialized"))
            {
                _logger.LogWarning("NavigationManager not initialized during logout");
            }
        }
    }

    public async Task<LoginResult?> GetStoredAuthState()
    {
        try
        {
            _logger.LogInformation("Retrieving stored authentication state");
            
            // Only try to access localStorage if JavaScript is available
            if (!CanAccessJavaScript())
            {
                _logger.LogInformation("JavaScript not available, cannot retrieve stored auth state");
                return null;
            }
            
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            var userId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");
            var rolesJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userRoles");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("No stored authentication state found");
                return null;
            }

            var roles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(rolesJson ?? "[]");
            _logger.LogInformation("Retrieved stored authentication state for user: {UserId}", userId);

            return new LoginResult
            {
                Token = token,
                UserId = Guid.Parse(userId),
                Roles = roles ?? new List<string>()
            };
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
        {
            _logger.LogInformation("Cannot access localStorage during prerendering");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stored authentication state");
            return null;
        }
    }
}