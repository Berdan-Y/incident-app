using Microsoft.Extensions.DependencyInjection;

namespace Maui;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        MainPage = services.GetRequiredService<AppShell>();
    }
}