using API.Models.Enums;

namespace API.Dtos;

public class IncidentPatchDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public string? ZipCode { get; set; }
    public Status? Status { get; set; }
    public Priority? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
} 