namespace Shared.Models.Dtos;

public class RoleDto
{
    public required string Name { get; set; }
    public required string[] Permissions { get; set; }
}