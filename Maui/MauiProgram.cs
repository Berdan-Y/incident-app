using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Refit;
using Shared.Api;
using Maui.Services;
using Maui.ViewModels;
using Maui.Pages;
using Maui.Converters;
using System.Net.Http;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;

namespace Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        // Add configuration
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "GoogleMaps:ApiKey", "AIzaSyDsQd3FtHLZnQYThk_PsGiCCpfDksexAoE" }
        });

        // Register App itself
        builder.Services.AddSingleton<App>();

        // Register Converters
        builder.Services.AddSingleton<InverseBoolConverter>();

        // Configure API Services
        var baseAddress = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5007"  // Android emulator special DNS
            : "http://localhost:5007"; // iOS and other platforms

        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        // Add authorization message handler
        builder.Services.AddTransient<AuthorizationMessageHandler>();

        // Register HttpClient for geocoding
        builder.Services.AddHttpClient();

        // Register Geocoding Service
        builder.Services.AddSingleton<IGeocodingService, GoogleGeocodingService>();

        builder.Services
            .AddRefitClient<IIncidentApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseAddress);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthorizationMessageHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => httpClientHandler);

        builder.Services
            .AddRefitClient<IUserApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseAddress);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthorizationMessageHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => httpClientHandler);

        builder.Services
            .AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseAddress);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => httpClientHandler);

        // Register Services
        builder.Services.AddSingleton<ITokenService, TokenService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<IDirectionsService, GoogleDirectionsService>();
        builder.Services.AddSingleton<IIncidentService, IncidentService>();

        // Register ViewModels
        builder.Services.AddSingleton<LogoutViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegistrationViewModel>();
        builder.Services.AddTransient<ReportIncidentViewModel>();
        builder.Services.AddTransient<MyIncidentsViewModel>();
        builder.Services.AddTransient<IncidentDetailsViewModel>();
        builder.Services.AddTransient<AllIncidentsViewModel>();
        builder.Services.AddTransient<AssignedIncidentsViewModel>();
        builder.Services.AddTransient<EditIncidentViewModel>();
        builder.Services.AddTransient<NotificationsViewModel>();

        // Register Shell and Pages
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegistrationPage>();
        builder.Services.AddTransient<LogoutPage>();
        builder.Services.AddTransient<ReportIncidentPage>();
        builder.Services.AddTransient<MyIncidentsPage>();
        builder.Services.AddTransient<IncidentDetailsPage>();
        builder.Services.AddTransient<AllIncidentsPage>();
        builder.Services.AddTransient<AssignedIncidentsPage>();
        builder.Services.AddTransient<EditIncidentPage>();
        builder.Services.AddTransient<NotificationsPage>();

        // Register Maps
        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<Microsoft.Maui.Maps.IMap>(serviceProvider => new Microsoft.Maui.Controls.Maps.Map());

        // Register platform services
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);

        // Register converters in resources
        builder.Services.AddSingleton<ResourceDictionary>(new ResourceDictionary
        {
            { "BoolToBackgroundColorConverter", new BoolToBackgroundColorConverter() },
            { "BoolToReadUnreadConverter", new BoolToReadUnreadConverter() }
        });

        // Register API clients
        builder.Services.AddRefitClient<INotificationApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress))
            .AddHttpMessageHandler<AuthorizationMessageHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => httpClientHandler);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}