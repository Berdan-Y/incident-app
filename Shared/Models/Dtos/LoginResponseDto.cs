namespace Shared.Models.Dtos;

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public Guid UserId { get; set; }
    public List<string> Roles { get; set; } = new();
    public string Message { get; set; } = "";
} 