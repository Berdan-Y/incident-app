using Microsoft.JSInterop;
using MudBlazor;

namespace Blazor.Components;

public class ThemeService
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isDarkMode;
    private MudTheme _currentTheme;

    public event Action? OnChange;

    public bool IsDarkMode => _isDarkMode;
    public MudTheme CurrentTheme => _currentTheme;

    public ThemeService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _currentTheme = LightTheme;
        _isDarkMode = false;
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var jsRuntime = scope.ServiceProvider.GetRequiredService<IJSRuntime>();
            var savedTheme = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "theme");
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
    }

    public async Task ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        _currentTheme = _isDarkMode ? DarkTheme : LightTheme;
        NotifyStateChanged();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var jsRuntime = scope.ServiceProvider.GetRequiredService<IJSRuntime>();
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", _isDarkMode ? "dark" : "light");
        }
        catch (InvalidOperationException)
        {
            // Ignore the error during prerendering
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

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