using Maui.Pages;
using Maui.Services;
using Maui.ViewModels;
using System.ComponentModel;
using System.Diagnostics;

namespace Maui;

public partial class AppShell : Shell, INotifyPropertyChanged
{
    private readonly AuthService _authService;
    private readonly LogoutViewModel _logoutViewModel;
    private readonly ITokenService _tokenService;

    public ITokenService TokenService
    {
        get
        {
            return _tokenService;
        }
    }

    public bool IsLoggedIn => _tokenService.IsLoggedIn;

    public AppShell(AuthService authService, LogoutViewModel logoutViewModel, ITokenService tokenService)
    {
        InitializeComponent();

        _authService = authService;
        _logoutViewModel = logoutViewModel;
        _tokenService = tokenService;

        // Subscribe to TokenService property changes
        _tokenService.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TokenService));
            OnPropertyChanged(nameof(IsLoggedIn));
        };

        BindingContext = this;

        // Register routes
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
        Routing.RegisterRoute(nameof(LogoutPage), typeof(LogoutPage));
        Routing.RegisterRoute(nameof(ReportIncidentPage), typeof(ReportIncidentPage));
        Routing.RegisterRoute(nameof(MyIncidentsPage), typeof(MyIncidentsPage));
        Routing.RegisterRoute(nameof(IncidentDetailsPage), typeof(IncidentDetailsPage));
        Routing.RegisterRoute(nameof(AllIncidentsPage), typeof(AllIncidentsPage));
        Routing.RegisterRoute(nameof(AssignedIncidentsPage), typeof(AssignedIncidentsPage));
        Routing.RegisterRoute(nameof(EditIncidentPage), typeof(EditIncidentPage));
        Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));
        Routing.RegisterRoute("/FieldEmployeeEditPage", typeof(FieldEmployeeEditPage));

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
        }
    }

    private void UpdateItemVisibility(BaseShellItem item, bool isAuthenticated)
    {

        switch (item)
        {
            case ShellContent content:
                // Skip visibility update for pages with role-based visibility
                if (content.Route is "AllIncidentsPage" or "AssignedIncidentsPage")
                {
                    break;
                }

                var newVisibility = content.Route switch
                {
                    "LoginPage" or "RegistrationPage" => !isAuthenticated,
                    "LogoutPage" or "MyIncidentsPage" or "NotificationsPage" => isAuthenticated,
                    "MainPage" or "ReportIncidentPage" => true,
                    _ => content.IsVisible
                };
                content.IsVisible = newVisibility;
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