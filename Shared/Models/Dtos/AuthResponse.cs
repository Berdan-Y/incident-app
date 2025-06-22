using Shared.Models.Enums;
using System.Text.Json.Serialization;

namespace Shared.Models;

public class AuthResponse
{
    public string? Message { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
}