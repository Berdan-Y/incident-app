using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;

namespace Maui.ViewModels;

public partial class LogoutViewModel : ObservableObject
{
    private readonly ITokenService _tokenService;

    [ObservableProperty]
    private bool isLoggedIn;

    public LogoutViewModel(ITokenService tokenService)
    {
        _tokenService = tokenService;
        isLoggedIn = _tokenService.IsLoggedIn;

        _tokenService.LoggedIn += OnLoginStateChanged;
        _tokenService.LoggedOut += OnLoginStateChanged;
    }
    
    private void OnLoginStateChanged(object sender, EventArgs e)
    {
        IsLoggedIn = _tokenService.IsLoggedIn;
    }
    
    [RelayCommand]
    private async Task LogoutAsync()
    {
        var shouldLogout = await Application.Current.MainPage.DisplayAlert(
            "Confirm Logout",
            "Are you sure you want to logout?",
            "Yes",
            "No"
        );

        if (shouldLogout)
        {
            await _tokenService.LogoutAsync();
            IsLoggedIn = false;
        }
    }
} 