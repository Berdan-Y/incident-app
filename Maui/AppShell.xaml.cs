using Maui.Pages;
using Maui.Services;
using Maui.ViewModels;

namespace Maui;

public partial class AppShell : Shell
{
    private readonly AuthService _authService;
    private readonly LogoutViewModel _logoutViewModel;

    public AppShell(AuthService authService, LogoutViewModel logoutViewModel)
    {
        InitializeComponent();

        _authService = authService;
        _logoutViewModel = logoutViewModel;
        BindingContext = _logoutViewModel;

        // Register routes
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
        Routing.RegisterRoute(nameof(LogoutPage), typeof(LogoutPage));
        Routing.RegisterRoute(nameof(ReportIncidentPage), typeof(ReportIncidentPage));
        Routing.RegisterRoute(nameof(MyIncidentsPage), typeof(MyIncidentsPage));
        Routing.RegisterRoute(nameof(IncidentDetailsPage), typeof(IncidentDetailsPage));

        // Subscribe to authentication state changes
        _authService.PropertyChanged += OnAuthStateChanged;

        // Initial visibility update
        UpdateNavigationVisibility();
    }

    private void OnAuthStateChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AuthService.IsAuthenticated))
        {
            MainThread.BeginInvokeOnMainThread(() => UpdateNavigationVisibility());
        }
    }

    public async void UpdateNavigationVisibility()
    {
        var isAuthenticated = _authService.IsAuthenticated;
        _logoutViewModel.IsLoggedIn = isAuthenticated;

        // Update visibility of each tab based on authentication state
        foreach (var item in Items)
        {
            UpdateItemVisibility(item, isAuthenticated);
        }

        // Navigate to appropriate page
        try
        {
            await Shell.Current.GoToAsync(isAuthenticated ? "//MainPage" : "//LoginPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private void UpdateItemVisibility(BaseShellItem item, bool isAuthenticated)
    {
        switch (item)
        {
            case ShellContent content:
                content.IsVisible = content.Route switch
                {
                    "LoginPage" or "RegistrationPage" => !isAuthenticated,
                    "LogoutPage" or "MyIncidentsPage" => isAuthenticated,
                    "MainPage" or "ReportIncidentPage" => true,
                    _ => content.IsVisible
                };
                break;

            case TabBar tabBar:
                foreach (var tabItem in tabBar.Items)
                {
                    UpdateItemVisibility(tabItem, isAuthenticated);
                }
                break;

            case Tab tab:
                foreach (var tabItem in tab.Items)
                {
                    UpdateItemVisibility(tabItem, isAuthenticated);
                }
                break;
        }
    }
}