using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models.Classes;

public class InvalidatedToken
{
    public Guid Id { get; set; }

    public required string Token { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}