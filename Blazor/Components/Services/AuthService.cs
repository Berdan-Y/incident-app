using System.Net.Http.Json;
using Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Blazor.Components.Services;

public class AuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        IJSRuntime jsRuntime,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigationManager)
    {
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
    }

    public async Task<LoginResult> Login(string email, string password)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.PostAsJsonAsync("api/Auth/login", new { email, password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                if (result?.Roles != null && !result.Roles.Contains("Admin"))
                {
                    await Logout();
                    throw new Exception("Access denied. Admin privileges required.");
                }
                if (result?.Token != null)
                {
                    // Store the token and user info in local storage
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", result.UserId.ToString());
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userRoles", System.Text.Json.JsonSerializer.Serialize(result.Roles));

                    // Update authentication state
                    var authState = (CustomAuthStateProvider)_authStateProvider;
                    await authState.UpdateAuthenticationState(result);

                    return result;
                }
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new Exception(error?.Message ?? "Invalid email or password");
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task Logout()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("API");
            await client.PostAsync("api/Auth/logout", null);
        }
        catch
        {
            // Ignore errors during logout
        }
        finally
        {
            // Clear local storage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userRoles");

            // Update authentication state
            var authState = (CustomAuthStateProvider)_authStateProvider;
            await authState.Logout();

            _navigationManager.NavigateTo("/login");
        }
    }

    public async Task<LoginResult?> GetStoredAuthState()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            var userId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");
            var rolesJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userRoles");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return null;

            var roles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(rolesJson ?? "[]");

            return new LoginResult
            {
                Token = token,
                UserId = Guid.Parse(userId),
                Roles = roles ?? new List<string>()
            };
        }
        catch
        {
            return null;
        }
    }
}