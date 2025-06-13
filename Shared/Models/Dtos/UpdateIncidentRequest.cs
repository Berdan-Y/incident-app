namespace Shared.Models;

public class UpdateIncidentRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? AssigneeId { get; set; }
    public List<PhotoDto>? Photos { get; set; }
} 