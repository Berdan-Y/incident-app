using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Shared.Api;
using Shared.Models.Dtos;
using System.Diagnostics;
using Refit;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maui.ViewModels;

public class LoginResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
    
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }
    
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; }

    public override string ToString()
    {
        return $"Token: {Token}\nUserId: {UserId}\nRoles: {string.Join(", ", Roles ?? new List<string>())}\nMessage: {Message}";
    }
}

public partial class LoginViewModel : ObservableObject
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

    public LoginViewModel(IAuthApi authApi, ITokenService tokenService)
    {
        _authApi = authApi;
        _tokenService = tokenService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading) return;

        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please enter both email and password", "OK");
            return;
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
            
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                try
                {
                    // Configure JSON options
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    // Parse the JSON response
                    var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(response.Content, options);
                    
                    if (loginResponse?.Token != null)
                    {
                        await _tokenService.SetTokenAsync(loginResponse.Token);
                        
                        // Reset fields
                        Email = string.Empty;
                        Password = string.Empty;

                        // Show success popup
                        await Application.Current.MainPage.DisplayAlert(
                            "Success",
                            "Login successful! Welcome back.",
                            "OK"
                        );

                        // Update navigation and go to main page
                        if (Application.Current?.MainPage is AppShell appShell)
                        {
                            appShell.UpdateNavigationVisibility();
                        }
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Error",
                            "Login response did not contain a valid token",
                            "OK"
                        );
                    }
                }
                catch (JsonException ex)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        $"Error processing login response: {ex.Message}",
                        "OK"
                    );
                }
            }
            else
            {
                var errorMessage = response.Error?.Content ?? "Unknown error occurred";
                await Application.Current.MainPage.DisplayAlert(
                    "Login Failed",
                    errorMessage,
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"An unexpected error occurred: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            IsLoading = false;
        }
    }
} 