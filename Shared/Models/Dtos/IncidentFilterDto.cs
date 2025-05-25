using Shared.Models.Enums;

namespace Shared.Models.Dtos;

public class IncidentFilterDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Status? Status { get; set; }
    public Priority? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid? ReportedById { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? UpdatedFrom { get; set; }
    public DateTime? UpdatedTo { get; set; }
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? DistanceInKm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
} 