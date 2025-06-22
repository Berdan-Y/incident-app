using Shared.Models.Dtos;
using Shared.Models.Classes;

namespace API.Repositories;

public interface IIncidentRepository
{
    Task<IEnumerable<Incident>> GetAllAsync();
    Task<Incident?> GetByIdAsync(Guid id);
    Task<IEnumerable<Incident>> GetByReporterIdAsync(Guid reporterId);
    Task<Incident> CreateAsync(Incident incident);
    Task<Incident> UpdateAsync(Incident incident);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Incident>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Incident>> GetAssignedToUserAsync(Guid userId);
    Task<IEnumerable<Incident>> GetFilteredIncidentsAsync(IncidentFilterDto filter);
    Task<IncidentPhoto> AddPhotoAsync(IncidentPhoto photo);
    Task<IncidentPhoto?> GetPhotoByIdAsync(Guid photoId);
    Task DeletePhotoAsync(Guid photoId);
    Task<IEnumerable<IncidentPhoto>> GetIncidentPhotosAsync(Guid incidentId);
    Task<bool> UserExistsAsync(Guid userId);
}