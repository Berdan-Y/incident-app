using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace API.Models.Classes;

[Table("users")]
public class User
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("email")]
    public required string Email { get; set; }
    [Column("password")]
    public required string Password { get; set; }
    [Column("first_name")]
    public required string FirstName { get; set; }
    [Column("last_name")]
    public required string LastName { get; set; }
    public string RoleJson { get; set; } = "{\"Name\":\"MEMBER\"}";

    [Column("role")]
    [NotMapped]
    public Role Role
    {
        get => string.IsNullOrEmpty(RoleJson) ? new Role { Name = "MEMBER" } : JsonSerializer.Deserialize<Role>(RoleJson)!;
        set => RoleJson = JsonSerializer.Serialize(value);
    }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}