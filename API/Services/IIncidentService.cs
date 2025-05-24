using API.Dtos;
using API.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace API.Services;

public interface IIncidentService
{
    Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync();
    Task<IncidentResponseDto?> GetIncidentByIdAsync(Guid id);
    Task<IEnumerable<IncidentResponseDto>> GetIncidentsByUserAsync(Guid userId);
    Task<IEnumerable<IncidentResponseDto>> GetIncidentsAssignedToUserAsync(Guid userId);
    Task<IncidentResponseDto> CreateIncidentAsync(IncidentCreateDto incidentDto, Guid? userId = null);
    Task<IncidentResponseDto?> UpdateIncidentAsync(Guid id, IncidentCreateDto incidentDto);
    Task<IncidentResponseDto?> PatchIncidentAsync(Guid id, IncidentPatchDto patchDto);
    Task<IncidentResponseDto?> UpdateIncidentStatusAsync(Guid id, Status status);
    Task<IncidentResponseDto?> UpdateIncidentPriorityAsync(Guid id, Priority priority);
    Task<bool> DeleteIncidentAsync(Guid id);
    Task<IncidentResponseDto?> AssignIncidentAsync(Guid id, Guid assigneeId);
    Task<IEnumerable<IncidentResponseDto>> GetFilteredIncidentsAsync(IncidentFilterDto filter);
    Task<IncidentPhotoDto> AddPhotoToIncidentAsync(Guid incidentId, IFormFile photo);
    Task DeletePhotoAsync(Guid photoId);
    Task<IEnumerable<IncidentPhotoDto>> GetIncidentPhotosAsync(Guid incidentId);
    Task<IncidentPhotoDto?> GetPhotoByIdAsync(Guid photoId);
}