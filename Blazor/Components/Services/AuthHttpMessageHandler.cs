using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace Blazor.Components.Services;

public class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<AuthHttpMessageHandler> _logger;
    private string? _cachedToken;
    private bool _hasTriedTokenRetrieval = false;

    public AuthHttpMessageHandler(IJSRuntime jsRuntime, NavigationManager navigationManager, ILogger<AuthHttpMessageHandler> logger)
    {
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
        _logger = logger;
        _logger.LogInformation("AuthHttpMessageHandler initialized. JSRuntime type: {JSRuntimeType}", _jsRuntime.GetType().Name);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing request: {Method} {Uri}", request.Method, request.RequestUri);

        // Skip token for login requests
        if (request.RequestUri?.PathAndQuery.Contains("/api/Auth/login") == true)
        {
            _logger.LogInformation("Skipping token for login request");
            return await base.SendAsync(request, cancellationToken);
        }

        // Try to add authorization header
        await TryAddAuthorizationHeader(request);

        var response = await base.SendAsync(request, cancellationToken);

        // Handle unauthorized responses
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized response from {Uri}", request.RequestUri);
            await HandleUnauthorizedResponse();
        }

        return response;
    }

    private async Task TryAddAuthorizationHeader(HttpRequestMessage request)
    {
        try
        {
            // Check if we can access JavaScript interop
            if (!CanAccessJavaScript())
            {
                _logger.LogInformation("JavaScript interop not available, skipping token retrieval");
                return;
            }

            _logger.LogInformation("Attempting to retrieve token from localStorage");
            
            // Try to get token from cache first, or if we haven't tried yet
            if (string.IsNullOrEmpty(_cachedToken) && !_hasTriedTokenRetrieval)
            {
                try
                {
                    _cachedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                    _hasTriedTokenRetrieval = true;
                    _logger.LogInformation("Successfully retrieved token from localStorage");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
                {
                    _logger.LogInformation("Cannot access localStorage during prerendering, skipping token retrieval");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving token from localStorage");
                    _hasTriedTokenRetrieval = true;
                    return;
                }
            }

            if (!string.IsNullOrEmpty(_cachedToken))
            {
                _logger.LogInformation("Retrieved token from localStorage: {TokenPreview}...", _cachedToken.Substring(0, Math.Min(20, _cachedToken.Length)));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cachedToken);
                _logger.LogInformation("Added Authorization header with token: Bearer {TokenPreview}...", _cachedToken.Substring(0, Math.Min(20, _cachedToken.Length)));
            }
            else
            {
                _logger.LogWarning("No token found in localStorage");
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
        {
            _logger.LogInformation("Skipping token retrieval during prerendering: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token from localStorage");
        }
    }

    private bool CanAccessJavaScript()
    {
        // In Blazor Server, we can access JavaScript if it's not an UnsupportedJavaScriptRuntime
        // In Blazor WebAssembly, we can always access JavaScript
        return _jsRuntime.GetType().Name != "UnsupportedJavaScriptRuntime";
    }

    private bool IsNavigationManagerInitialized()
    {
        try
        {
            // Try to access the Uri property to check if NavigationManager is initialized
            _ = _navigationManager.Uri;
            return true;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("has not been initialized"))
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task HandleUnauthorizedResponse()
    {
        try
        {
            _logger.LogInformation("Handling unauthorized response - clearing cached token");
            
            // Clear cached token
            _cachedToken = null;
            _hasTriedTokenRetrieval = false;
            
            // Try to clear localStorage if possible
            if (CanAccessJavaScript())
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userRoles");
                    _logger.LogInformation("Cleared localStorage items");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
                {
                    _logger.LogInformation("Cannot clear localStorage during prerendering");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing localStorage");
                }
            }
            
            // Only redirect if NavigationManager is initialized and we're not already on login page
            if (IsNavigationManagerInitialized())
            {
                var currentPath = new Uri(_navigationManager.Uri).AbsolutePath;
                if (!currentPath.Equals("/login", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Redirecting to login page");
                    _navigationManager.NavigateTo("/login", true);
                }
            }
            else
            {
                _logger.LogInformation("NavigationManager not initialized, skipping redirect");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling unauthorized response");
        }
    }
} 