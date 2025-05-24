namespace API.Models.Classes;

public sealed class Role
{
    public const string Admin = "Admin";
    public const string FieldEmployee = "FieldEmployee";
    public const string Member = "Member";
    public const int AdminId = 1;
    public const int FieldEmployeeId = 2;
    public const int MemberId = 3;
    
    public int Id { get; set; }
    public required string Name { get; set; }
}