using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Shared.Api;
using Shared.Models.Dtos;
using System.Text.Json;

namespace Maui.ViewModels;

public partial class RegistrationViewModel : ObservableObject
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
    private string? _firstName;

    [ObservableProperty]
    private string? _lastName;

    [ObservableProperty]
    private string? _errorMessage;

    public RegistrationViewModel(IAuthApi authApi, ITokenService tokenService)
    {
        _authApi = authApi;
        _tokenService = tokenService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsLoading) return;

        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) ||
            string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please fill in all fields", "OK");
            return;
        }

        if (!Behaviors.EmailValidationBehavior.ValidateEmail(Email))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid email address", "OK");
            return;
        }

        try
        {
            IsLoading = true;

            var registerDto = new RegisterDto
            {
                Email = Email,
                Password = Password,
                FirstName = FirstName,
                LastName = LastName
            };

            var response = await _authApi.RegisterAsync(registerDto);

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                // Reset fields
                Email = string.Empty;
                Password = string.Empty;
                FirstName = string.Empty;
                LastName = string.Empty;

                await Application.Current.MainPage.DisplayAlert(
                    "Success",
                    "Registration successful! Please login.",
                    "OK"
                );

                // Navigate to login page
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                var errorMessage = response.Error?.Content ?? "Unknown error occurred";
                await Application.Current.MainPage.DisplayAlert(
                    "Registration Failed",
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