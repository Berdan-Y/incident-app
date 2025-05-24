namespace API.Models.Classes;

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public int RoleId { get; set; }
    public required Role Role { get; set; }
    public required User User { get; set; }
}