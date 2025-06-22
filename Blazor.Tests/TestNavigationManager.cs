using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.Tests;

public class TestNavigationManager : NavigationManager
{
    public TestNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        // Handle both absolute and relative URLs
        var newUri = uri.StartsWith("http") ? uri : ToAbsoluteUri(uri).ToString();
        Uri = newUri;
        NotifyLocationChanged(false);
    }

    public static void Register(IServiceCollection services)
    {
        var navigationManager = new TestNavigationManager();
        services.AddSingleton<NavigationManager>(navigationManager);
    }
}