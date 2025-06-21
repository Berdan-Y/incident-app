using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor;
using MudBlazor.Services;
using Blazor.Services;
using Refit;
using Shared.Api;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration
var apiBaseAddress = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl") 
    ?? throw new InvalidOperationException("API Base URL is not configured");

Console.WriteLine($"API Base URL configured as: {apiBaseAddress}");

// Configure JSON serialization settings
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

var refitSettings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
};

// Register services
builder.Services.AddMudServices();

// Register core services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IGoogleMapsService, GoogleMapsService>();

// Configure HttpClient with auth handler
builder.Services.AddTransient<AuthMessageHandler>();

// Configure the default HttpClient (used by AuthenticationService)
builder.Services.AddHttpClient(string.Empty, client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
    Console.WriteLine($"Configured default HttpClient with base address: {client.BaseAddress}");
}).AddHttpMessageHandler<AuthMessageHandler>();

// Register Refit client for IIncidentApi
builder.Services.AddRefitClient<IIncidentApi>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseAddress))
    .AddHttpMessageHandler<AuthMessageHandler>();

// Register Refit client for IUserApi
builder.Services.AddRefitClient<IUserApi>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseAddress))
    .AddHttpMessageHandler<AuthMessageHandler>();

// Configure Google Maps service
builder.Services.AddHttpClient<IGoogleMapsService, GoogleMapsService>();

await builder.Build().RunAsync();