namespace Shared.Models.Dtos;

public class UpdateIncidentDetailsDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
} 