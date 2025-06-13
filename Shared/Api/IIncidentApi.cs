using Refit;
using Shared.Models;
using Shared.Models.Dtos;

namespace Shared.Api;

public interface IIncidentApi
{
    [Get("/api/incidents")]
    Task<IApiResponse<List<IncidentDto>>> GetIncidentsAsync();

    [Get("/api/incidents/{id}")]
    Task<IApiResponse<IncidentDto>> GetIncidentByIdAsync(int id);

    [Post("/api/incidents")]
    Task<IApiResponse<IncidentDto>> CreateIncidentAsync([Body] CreateIncidentDto incident);

    [Put("/api/incidents/{id}")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentAsync(int id, [Body] UpdateIncidentDto incident);

    [Delete("/api/incidents/{id}")]
    Task<IApiResponse<bool>> DeleteIncidentAsync(int id);

    [Get("/api/incidents/my-incidents")]
    Task<IApiResponse<List<IncidentDto>>> GetMyIncidentsAsync();

    [Get("/api/incidents/assigned-to-me")]
    Task<IApiResponse<List<IncidentDto>>> GetIncidentsAssignedToMeAsync();

    [Patch("/api/incidents/{id}/status")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentStatusAsync(string id, [Body] UpdateIncidentStatusRequest request);

    [Patch("/api/incidents/{id}/priority")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentPriorityAsync(string id, [Body] UpdateIncidentPriorityRequest request);

    [Patch("/api/incidents/{id}/assign")]
    Task<IApiResponse<IncidentDto>> AssignIncidentAsync(string id, [Body] AssignIncidentRequest request);

    [Post("/api/incidents/{id}/photos")]
    Task<IApiResponse<List<PhotoDto>>> UploadIncidentPhotosAsync(string id, [Body] UploadPhotosRequest request);

    [Get("/api/incidents/{id}/photos")]
    Task<IApiResponse<List<PhotoDto>>> GetIncidentPhotosAsync(string id);

    [Get("/api/incidents/photos/{id}")]
    Task<IApiResponse<PhotoDto>> GetPhotoByIdAsync(string id);

    [Delete("/api/incidents/photos/{id}")]
    Task<IApiResponse> DeletePhotoAsync(string id);
} 