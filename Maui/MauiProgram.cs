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

namespace Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
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

        builder.Services
            .AddRefitClient<IIncidentApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseAddress);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => httpClientHandler);

        builder.Services
            .AddRefitClient<IUserApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseAddress);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
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

        // Register ViewModels
        builder.Services.AddSingleton<LogoutViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegistrationViewModel>();
        builder.Services.AddTransient<ReportIncidentViewModel>();

        // Register Shell and Pages
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegistrationPage>();
        builder.Services.AddTransient<LogoutPage>();
        builder.Services.AddTransient<ReportIncidentPage>();

        // Register Maps
        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<Microsoft.Maui.Maps.IMap>(serviceProvider => new Microsoft.Maui.Controls.Maps.Map());

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}