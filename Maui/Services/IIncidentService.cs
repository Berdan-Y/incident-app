using Shared.Models.Dtos;

namespace Maui.Services;

public interface IIncidentService
{
    Task DeleteIncidentAsync(IncidentResponseDto incident);
    Task<List<IncidentResponseDto>> GetMyIncidentsAsync();
    Task<List<IncidentResponseDto>> GetAllIncidentsAsync();
    Task<List<IncidentResponseDto>> GetAssignedIncidentsAsync();
    Task<IncidentResponseDto> GetIncidentByIdAsync(Guid id);
    Task<IncidentResponseDto> UpdateIncidentAsync(Guid id, UpdateIncidentDto updateDto);
    Task<IncidentResponseDto> UpdateIncidentDetailsAsync(Guid id, UpdateIncidentDetailsDto details);
    Task<IncidentResponseDto> UpdateIncidentStatusAsync(Guid id, int status);
    Task<IncidentResponseDto> PatchIncidentAsync(Guid id, IncidentPatchDto patchDto);
}