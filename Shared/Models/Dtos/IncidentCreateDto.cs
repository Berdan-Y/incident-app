using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.Dtos;

public class IncidentCreateDto
{
    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [Required]
    public required string Description { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public string? Address { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    public Guid? ReportedById { get; set; }
}