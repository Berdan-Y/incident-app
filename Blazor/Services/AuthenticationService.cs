using System;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Shared.Models.Dtos;
using Shared.Models.Enums;
using Shared.Models;

namespace Blazor.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private readonly ITokenService _tokenService;
    private string? _userEmail;

    public event Action? AuthenticationStateChanged;

    public bool IsAuthenticated => _tokenService.IsLoggedIn;
    public string? UserEmail => _userEmail;
    public Role? UserRole { get; private set; }

    public AuthenticationService(HttpClient httpClient, NavigationManager navigationManager, ITokenService tokenService)
    {
        _httpClient = httpClient;
        _navigationManager = navigationManager;
        _tokenService = tokenService;
        _tokenService.LoggedIn += OnAuthenticationStateChanged;
        _tokenService.LoggedOut += OnAuthenticationStateChanged;
    }

    public async Task<bool> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var request = new LoginRequest { Email = loginDto.Email, Password = loginDto.Password };
            return await Login(request);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var request = new RegisterRequest
            {
                Email = registerDto.Email,
                Password = registerDto.Password,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Role = registerDto.Role
            };
            return await Register(request);
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await Logout();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        await Task.CompletedTask; // Ensure method is truly async
        return IsAuthenticated;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        if (!IsAuthenticated) return null;
        return await Task.FromResult(_userEmail);
    }

    public async Task<string?> GetCurrentUserRoleAsync()
    {
        if (!IsAuthenticated) return null;
        return await Task.FromResult(UserRole?.ToString());
    }

    public bool IsInRole(string role)
    {
        return IsAuthenticated && UserRole?.ToString().ToLower() == role.ToLower();
    }

    public bool IsAdmin()
    {
        return IsInRole(Role.Admin.ToString());
    }

    public void RequireAuthentication()
    {
        if (!IsAuthenticated)
        {
            _navigationManager.NavigateTo("/login");
        }
    }

    public void RequireAdmin()
    {
        if (!IsAuthenticated || !_tokenService.HasRole("Admin"))
        {
            throw new UnauthorizedAccessException("Admin access required");
        }
    }

    public async Task<bool> Login(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse?.Token == null)
            {
                return false;
            }

            // Set token first
            await _tokenService.SetTokenAsync(authResponse.Token);

            // Get roles from the response and set them
            var roles = string.Join(",", authResponse.Roles);
            if (!string.IsNullOrEmpty(roles))
            {
                await _tokenService.SetRolesAsync(roles);
            }

            // Set user info
            _userEmail = authResponse.UserName;
            UserRole = authResponse.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ? Role.Admin : Role.Member;

            // Notify state change after everything is set
            NotifyAuthenticationStateChanged();

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> Register(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return false;
            }

            var responseContent = await response.Content.ReadFromJsonAsync<AuthResponse>();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task Logout()
    {
        await _tokenService.LogoutAsync();
        _userEmail = null;
        UserRole = null;
        NotifyAuthenticationStateChanged();
    }

    private void NotifyAuthenticationStateChanged()
    {
        AuthenticationStateChanged?.Invoke();
        // Navigate to home page after login/logout
        _navigationManager.NavigateTo("/", forceLoad: true);
    }

    private void OnAuthenticationStateChanged()
    {
        // Force UI refresh
        _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: false);
    }
}