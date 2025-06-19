using Shared.Api;
using Shared.Models.Dtos;

namespace Maui.Services;

public class IncidentService : IIncidentService
{
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;

    public IncidentService(IIncidentApi incidentApi, ITokenService tokenService)
    {
        _incidentApi = incidentApi;
        _tokenService = tokenService;
    }

    public async Task DeleteIncidentAsync(IncidentResponseDto incident)
    {
        var response = await _incidentApi.DeleteIncidentAsync(incident.Id);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(response.Error?.Content ?? "Failed to delete incident");
        }
    }

    public async Task<List<IncidentResponseDto>> GetMyIncidentsAsync()
    {
        var response = await _incidentApi.GetMyIncidentsAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(response.Error?.Content ?? "Failed to load my incidents");
        }
        return response.Content ?? new List<IncidentResponseDto>();
    }

    public async Task<List<IncidentResponseDto>> GetAllIncidentsAsync()
    {
        var response = await _incidentApi.GetIncidentsAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(response.Error?.Content ?? "Failed to load all incidents");
        }
        return response.Content ?? new List<IncidentResponseDto>();
    }

    public async Task<List<IncidentResponseDto>> GetAssignedIncidentsAsync()
    {
        var response = await _incidentApi.GetIncidentsAssignedToMeAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(response.Error?.Content ?? "Failed to load assigned incidents");
        }
        return response.Content ?? new List<IncidentResponseDto>();
    }

    public async Task<IncidentResponseDto> GetIncidentByIdAsync(Guid id)
    {
        var response = await _incidentApi.GetIncidentByIdAsync(id);
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            throw new Exception(response.Error?.Content ?? "Failed to load incident details");
        }
        return response.Content;
    }

    public async Task<IncidentResponseDto> UpdateIncidentAsync(Guid id, UpdateIncidentDto updateDto)
    {
        var response = await _incidentApi.UpdateIncidentAsync(id, updateDto);
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            throw new Exception(response.Error?.Content ?? "Failed to update incident");
        }

        // After updating, fetch the latest version of the incident to get the full IncidentResponseDto
        return await GetIncidentByIdAsync(id);
    }
}