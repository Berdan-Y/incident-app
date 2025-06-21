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
            Console.WriteLine($"Attempting to login with email: {request.Email}");
            Console.WriteLine($"Using HttpClient with base address: {_httpClient.BaseAddress}");
            
            var response = await _httpClient.PostAsJsonAsync("api/Auth/login", request);
            Console.WriteLine($"Login response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode) 
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Login failed with error: {error}");
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse?.Token == null) 
            {
                Console.WriteLine("Login failed: No token received");
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
            Console.WriteLine($"Login exception: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> Register(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Auth/register", request);
            Console.WriteLine($"Register response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Registration failed with error: {error}");
                return false;
            }

            var responseContent = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Console.WriteLine($"Registration success with message: {responseContent?.Message}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration exception: {ex.Message}");
            return false;
        }
    }

    public async Task Logout()
    {
        Console.WriteLine("AuthenticationService.Logout called");
        await _tokenService.LogoutAsync();
        _userEmail = null;
        UserRole = null;
        NotifyAuthenticationStateChanged();
        Console.WriteLine("Logout completed - token cleared and state reset");
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