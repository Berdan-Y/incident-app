using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Blazor.Components.Services;

public class AuthResponseHandler
{
    private readonly NavigationManager _navigationManager;
    private readonly ISnackbar _snackbar;

    public AuthResponseHandler(NavigationManager navigationManager, ISnackbar snackbar)
    {
        _navigationManager = navigationManager;
        _snackbar = snackbar;
    }

    public void HandleUnauthorized()
    {
        _snackbar.Add("Please log in to continue", Severity.Warning);
        _navigationManager.NavigateTo("/login");
    }
} 