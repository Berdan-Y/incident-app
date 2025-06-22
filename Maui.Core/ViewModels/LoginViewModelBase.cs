using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Api;
using Shared.Models.Dtos;
using System.Net.Http;
using Refit;

namespace Maui.Core.ViewModels;

public abstract partial class LoginViewModelBase : ObservableObject
{
    private readonly IAuthApi _authApi;
    private readonly ITokenService _tokenService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private string? _errorMessage;

    protected LoginViewModelBase(IAuthApi authApi, ITokenService tokenService)
    {
        _authApi = authApi;
        _tokenService = tokenService;
    }

    public async Task<LoginResult> LoginAsync()
    {
        if (IsLoading) return new LoginResult { Success = false, Message = "Already processing a login request" };

        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            return new LoginResult { Success = false, Message = "Please enter both email and password" };
        }

        try
        {
            IsLoading = true;

            var loginDto = new LoginDto
            {
                Email = Email,
                Password = Password
            };

            var response = await _authApi.LoginAsync(loginDto);

            if (response.IsSuccessStatusCode)
            {
                if (response.Content == null)
                {
                    return new LoginResult { Success = false, Message = "Login response did not contain a valid token" };
                }

                var loginResponse = response.Content;
                if (loginResponse.Token != null)
                {
                    await _tokenService.SetTokenAsync(loginResponse.Token);

                    var roles = loginResponse.Roles != null && loginResponse.Roles.Count > 0
                        ? string.Join(",", loginResponse.Roles)
                        : "";

                    await _tokenService.SetRolesAsync(roles);

                    // Reset fields
                    Email = string.Empty;
                    Password = string.Empty;

                    return new LoginResult { Success = true, Message = "Login successful! Welcome back." };
                }
                
                return new LoginResult { Success = false, Message = "Login response did not contain a valid token" };
            }
            
            var errorMessage = response.Error?.Content ?? "Unknown error occurred";
            return new LoginResult { Success = false, Message = errorMessage };
        }
        catch (Exception ex)
        {
            return new LoginResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public interface ITokenService
{
    Task SetTokenAsync(string token);
    Task SetRolesAsync(string roles);
}

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
} 