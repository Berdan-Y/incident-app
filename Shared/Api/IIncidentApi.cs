using Refit;
using Shared.Models;
using Shared.Models.Dtos;
using System.Net.Http;

namespace Shared.Api;

public interface IIncidentApi
{
    [Get("/api/incident")]
    Task<IApiResponse<List<IncidentResponseDto>>> GetIncidentsAsync();

    [Get("/api/incident/{id}")]
    Task<IApiResponse<IncidentResponseDto>> GetIncidentByIdAsync(Guid id);

    [Multipart]
    [Post("/api/incident")]
    Task<IApiResponse<IncidentDto>> CreateIncidentAsync([AliasAs("incident")] IncidentCreateDto incident);

    [Put("/api/incident/{id}")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentAsync(Guid id, [Body] UpdateIncidentDto incident);

    [Delete("/api/incident/{id}")]
    Task<IApiResponse<bool>> DeleteIncidentAsync(Guid id);

    [Get("/api/incident/my-incidents")]
    [Headers("Accept: application/json")]
    Task<IApiResponse<List<IncidentResponseDto>>> GetMyIncidentsAsync();

    [Get("/api/incident/assigned-to-me")]
    Task<IApiResponse<List<IncidentResponseDto>>> GetIncidentsAssignedToMeAsync();

    [Patch("/api/incident/{id}/status")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentStatusAsync(Guid id, [Body] UpdateIncidentStatusRequest request);

    [Patch("/api/incident/{id}/priority")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentPriorityAsync(Guid id, [Body] UpdateIncidentPriorityRequest request);

    [Patch("/api/incident/{id}/assign")]
    Task<IApiResponse<IncidentDto>> AssignIncidentAsync(Guid id, [Body] AssignIncidentRequest request);

    [Post("/api/incident/{id}/photos")]
    Task<IApiResponse<List<PhotoDto>>> UploadIncidentPhotosAsync(Guid id, [Body] UploadPhotosRequest request);

    [Get("/api/incident/{id}/photos")]
    Task<IApiResponse<List<PhotoDto>>> GetIncidentPhotosAsync(Guid id);

    [Get("/api/incident/photos/{id}")]
    Task<IApiResponse<PhotoDto>> GetPhotoByIdAsync(Guid id);

    [Delete("/api/incident/photos/{id}")]
    Task<IApiResponse> DeletePhotoAsync(Guid id);
}