using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Blazor.Components.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Blazor.Components;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal? _authenticatedUser;
    private readonly IJSRuntime _jsRuntime;
    private bool _isInitialized;

    public CustomAuthStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }
        return new AuthenticationState(_authenticatedUser ?? _anonymous);
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Check if JavaScript is available (not during prerendering)
            if (!CanAccessJavaScript())
            {
                _isInitialized = true;
                return;
            }

            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            var userId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");
            var rolesJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userRoles");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId))
            {
                var roles = JsonSerializer.Deserialize<List<string>>(rolesJson ?? "[]") ?? new List<string>();
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userId)
                };

                // Add all roles as claims
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var identity = new ClaimsIdentity(claims, "CustomAuth");
                _authenticatedUser = new ClaimsPrincipal(identity);
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
        {
            // This is expected during prerendering, just continue
        }
        catch (Exception)
        {
            // If we can't access localStorage (e.g., during server-side rendering), just continue
        }
        finally
        {
            _isInitialized = true;
        }
    }

    private bool CanAccessJavaScript()
    {
        // In Blazor Server, we can access JavaScript if it's not an UnsupportedJavaScriptRuntime
        // In Blazor WebAssembly, we can always access JavaScript
        return _jsRuntime.GetType().Name != "UnsupportedJavaScriptRuntime";
    }

    public Task UpdateAuthenticationState(LoginResult loginResult)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, loginResult.UserId.ToString()),
            new Claim(ClaimTypes.Name, loginResult.UserId.ToString())
        };

        // Add all roles as claims
        claims.AddRange(loginResult.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, "CustomAuth");
        _authenticatedUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }

    public async Task Logout()
    {
        _authenticatedUser = null;
        
        // Clear localStorage if JavaScript is available
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
                // This is expected during prerendering, just continue
            }
            catch (Exception)
            {
                // Ignore errors when clearing localStorage
            }
        }
        
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}