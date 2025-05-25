using Shared.Models.Enums;

namespace Shared.Models.Dtos;

public class IncidentResponseDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public Status Status { get; set; }
    public Priority Priority { get; set; }
    public Guid? ReportedById { get; set; }
    public UserDto? CreatedBy { get; set; }
    public Guid? AssignedToId { get; set; }
    public UserDto? AssignedTo { get; set; }
    public ICollection<IncidentPhotoDto>? Photos { get; set; } = new List<IncidentPhotoDto>();
} 