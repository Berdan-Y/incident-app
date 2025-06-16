namespace Shared.Models.Dtos;

public class CreateIncidentDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double? Latitude { get; set; } = 0.0;
    public double? Longitude { get; set; } = 0.0;
    public string? Address { get; set; } = null;
    public string? Zipcode { get; set; } = null;
    public string? ReportedBy { get; set; } = null;
}