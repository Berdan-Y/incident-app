using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;
using Status = Shared.Models.Enums.Status;

namespace API.Models.Classes;

public class Incident
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    [StringLength(500)]
    public string Address { get; set; }
    
    [StringLength(20)]
    public string ZipCode { get; set; }
    
    [Required]
    public Status Status { get; set; }
    
    [Required]
    public Priority Priority { get; set; }
    
    [ForeignKey("CreatedBy")]
    public Guid? ReportedById { get; set; }
    public User? CreatedBy { get; set; }
    
    [ForeignKey("AssignedTo")]
    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    
    public ICollection<IncidentPhoto> Photos { get; set; } = new List<IncidentPhoto>();
}