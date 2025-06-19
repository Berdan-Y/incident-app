using System.Net.Http.Headers;
using System.Diagnostics;

namespace Maui.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ITokenService _tokenService;

    public AuthorizationMessageHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _tokenService.GetToken();
        Debug.WriteLine($"AuthorizationMessageHandler - Token: {token?.Substring(0, Math.Min(50, token?.Length ?? 0))}...");

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Debug.WriteLine($"AuthorizationMessageHandler - Added Authorization header: {request.Headers.Authorization}");
        }
        else
        {
            Debug.WriteLine("AuthorizationMessageHandler - No token available");
        }

        var response = await base.SendAsync(request, cancellationToken);
        Debug.WriteLine($"AuthorizationMessageHandler - Response status: {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"AuthorizationMessageHandler - Error response content: {content}");
        }

        return response;
    }
}