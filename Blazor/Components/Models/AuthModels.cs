namespace Blazor.Components.Models;

public class LoginResult
{
    public string Token { get; set; } = "";
    public Guid UserId { get; set; }
    public List<string> Roles { get; set; } = new();
    public string Message { get; set; } = "";
}

public class ErrorResponse
{
    public string Message { get; set; } = "";
} 