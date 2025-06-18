using System.Text.Json.Serialization;

namespace Shared.Models.Dtos;

public class LoginResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    public override string ToString()
    {
        return $"Token: {Token}\nUserId: {UserId}\nRoles: {string.Join(", ", Roles ?? new List<string>())}\nMessage: {Message}";
    }
}