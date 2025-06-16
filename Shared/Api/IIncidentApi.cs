using Refit;
using Shared.Models;
using Shared.Models.Dtos;
using System.Net.Http;

namespace Shared.Api;

public interface IIncidentApi
{
    [Get("/api/incident")]
    Task<IApiResponse<List<IncidentDto>>> GetIncidentsAsync();

    [Get("/api/incident/{id}")]
    Task<IApiResponse<IncidentDto>> GetIncidentByIdAsync(int id);

    [Multipart]
    [Post("/api/incident")]
    Task<IApiResponse<IncidentDto>> CreateIncidentAsync([AliasAs("incident")] IncidentCreateDto incident);

    [Put("/api/incident/{id}")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentAsync(int id, [Body] UpdateIncidentDto incident);

    [Delete("/api/incident/{id}")]
    Task<IApiResponse<bool>> DeleteIncidentAsync(int id);

    [Get("/api/incident/my-incidents")]
    Task<IApiResponse<List<IncidentDto>>> GetMyIncidentsAsync();

    [Get("/api/incident/assigned-to-me")]
    Task<IApiResponse<List<IncidentDto>>> GetIncidentsAssignedToMeAsync();

    [Patch("/api/incident/{id}/status")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentStatusAsync(string id, [Body] UpdateIncidentStatusRequest request);

    [Patch("/api/incident/{id}/priority")]
    Task<IApiResponse<IncidentDto>> UpdateIncidentPriorityAsync(string id, [Body] UpdateIncidentPriorityRequest request);

    [Patch("/api/incident/{id}/assign")]
    Task<IApiResponse<IncidentDto>> AssignIncidentAsync(string id, [Body] AssignIncidentRequest request);

    [Post("/api/incident/{id}/photos")]
    Task<IApiResponse<List<PhotoDto>>> UploadIncidentPhotosAsync(string id, [Body] UploadPhotosRequest request);

    [Get("/api/incident/{id}/photos")]
    Task<IApiResponse<List<PhotoDto>>> GetIncidentPhotosAsync(string id);

    [Get("/api/incident/photos/{id}")]
    Task<IApiResponse<PhotoDto>> GetPhotoByIdAsync(string id);

    [Delete("/api/incident/photos/{id}")]
    Task<IApiResponse> DeletePhotoAsync(string id);
}