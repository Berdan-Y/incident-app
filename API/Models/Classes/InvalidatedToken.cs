using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models.Classes;

[Table("invalidated_tokens")]
public class InvalidatedToken
{
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("token")]
    public required string Token { get; set; }
    
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
} 