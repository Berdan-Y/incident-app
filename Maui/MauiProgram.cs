using Microsoft.Extensions.Logging;
using Refit;
using Shared.Api;

namespace Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Configure API Services
        var baseAddress = "http://localhost:5007"; // Replace with your actual API base URL
        
        builder.Services
            .AddRefitClient<IIncidentApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));
            
        builder.Services
            .AddRefitClient<IUserApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));
            
        builder.Services
            .AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}