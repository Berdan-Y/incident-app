using Blazor.Components;
using Blazor.Components.Services;
using Blazor.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Authentication
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add ThemeService as scoped
builder.Services.AddScoped<ThemeService>();

// Add AuthService
builder.Services.AddScoped<AuthService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);
// Add AuthResponseHandler
builder.Services.AddScoped<AuthResponseHandler>();

// Add ApiClient
builder.Services.AddScoped<IApiClient, Blazor.Services.ApiClient>();

// Add HttpClient configurations
var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5007/";

// Configure the named client for AuthService
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<AuthHttpMessageHandler>();

// Configure the typed client for ApiClient
builder.Services.AddHttpClient<IApiClient, Blazor.Services.ApiClient>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<AuthHttpMessageHandler>();

// Add AuthHttpMessageHandler
builder.Services.AddScoped<AuthHttpMessageHandler>();

// Configure Data Protection
builder.Services.AddDataProtection()
    .SetApplicationName("IncidentApp")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")));

// Configure Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "IncidentApp.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();