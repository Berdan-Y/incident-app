using Shared.Models.Enums;

namespace Shared.Models.Dtos;

public class IncidentUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Status? Status { get; set; }
    public Priority? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
}