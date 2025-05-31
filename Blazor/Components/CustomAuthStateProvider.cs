using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Blazor.Components.Models;

namespace Blazor.Components;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal? _authenticatedUser;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_authenticatedUser ?? _anonymous));
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

    public Task Logout()
    {
        _authenticatedUser = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }
}