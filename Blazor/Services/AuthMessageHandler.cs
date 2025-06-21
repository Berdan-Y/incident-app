using System.Net.Http.Headers;

namespace Blazor.Services;

public class AuthMessageHandler : DelegatingHandler
{
    private readonly ITokenService _tokenService;

    public AuthMessageHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"AuthMessageHandler: Processing request to {request.RequestUri}");
        
        var token = _tokenService.GetToken();
        
        Console.WriteLine($"AuthMessageHandler: Token {(string.IsNullOrEmpty(token) ? "not present" : "present")}");
        
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Console.WriteLine("AuthMessageHandler: Added Authorization header");
        }
        else
        {
            Console.WriteLine("AuthMessageHandler: No token available to add to request");
        }

        var response = await base.SendAsync(request, cancellationToken);
        Console.WriteLine($"AuthMessageHandler: Response status code: {response.StatusCode}");
        
        return response;
    }
}
