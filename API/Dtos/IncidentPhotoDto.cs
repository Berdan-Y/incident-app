namespace API.Dtos;

public class IncidentPhotoDto
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string FilePath { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid IncidentId { get; set; }
} 