using Microsoft.JSInterop;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazor.Components;

public class ThemeService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode;
    private MudTheme _currentTheme;
    private bool _isInitialized;

    public event Action? OnChange;

    public bool IsDarkMode => _isDarkMode;
    public MudTheme CurrentTheme => _currentTheme;

    public ThemeService(IServiceProvider serviceProvider, NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _serviceProvider = serviceProvider;
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
        _currentTheme = LightTheme;
        _isDarkMode = false;
        _isInitialized = false;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "theme");
            if (savedTheme != null)
            {
                _isDarkMode = savedTheme == "dark";
                _currentTheme = _isDarkMode ? DarkTheme : LightTheme;
                NotifyStateChanged();
            }
        }
        catch (InvalidOperationException)
        {
            // Ignore the error during prerendering
            // The theme will be initialized after the component is rendered
        }
        finally
        {
            _isInitialized = true;
        }
    }

    public async Task ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        _currentTheme = _isDarkMode ? DarkTheme : LightTheme;
        NotifyStateChanged();

        if (!_isInitialized)
            return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", _isDarkMode ? "dark" : "light");
        }
        catch (InvalidOperationException)
        {
            // Ignore the error during prerendering
        }
    }

    private void NotifyStateChanged()
    {
        _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: false);
        OnChange?.Invoke();
    }

    public static MudTheme LightTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Green.Accent4,
            AppbarBackground = Colors.Blue.Default,
            Background = Colors.Shades.White,
            DrawerBackground = Colors.Shades.White,
            DrawerText = "rgba(0,0,0, 0.7)",
            DrawerIcon = "rgba(0,0,0, 0.7)",
            Success = Colors.Green.Accent4
        }
    };

    public static MudTheme DarkTheme => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Blue.Lighten1,
            Secondary = Colors.Green.Accent4,
            AppbarBackground = Colors.Blue.Darken4,
            Background = Colors.Shades.Black,
            DrawerBackground = Colors.Shades.Black,
            DrawerText = "rgba(255,255,255, 0.7)",
            DrawerIcon = "rgba(255,255,255, 0.7)",
            Success = Colors.Green.Accent4
        }
    };
} 