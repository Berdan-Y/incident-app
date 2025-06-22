namespace Shared.Models.Dtos;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid IncidentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? UserName { get; set; }
    public string? IncidentTitle { get; set; }
}