namespace Shared.Models.Classes;

public class UserRole
{
    public Guid UserId { get; set; }
    public int RoleId { get; set; }
    public required Role Role { get; set; }
    public required User User { get; set; }
}