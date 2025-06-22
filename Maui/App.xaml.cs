using Microsoft.Maui.Controls;
using Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maui;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();

        // Ensure TokenService is available for converters
        Resources["TokenService"] = services.GetRequiredService<ITokenService>();

        MainPage = services.GetRequiredService<AppShell>();
    }
}