namespace Shared.Models.Dtos;

public class CreateIncidentDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
} 