using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Shared.Models.Classes;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string RoleJson { get; set; } = "{\"Name\": \"Member\"}";

    [NotMapped]
    public Role Role
    {
        get => string.IsNullOrEmpty(RoleJson) ? new Role { Name = "MEMBER" } : JsonSerializer.Deserialize<Role>(RoleJson)!;
        set => RoleJson = JsonSerializer.Serialize(value);
    }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}