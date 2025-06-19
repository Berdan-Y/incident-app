namespace Shared.Models.Dtos;

public class UpdateIncidentDetailsDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ZipCode { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
} 