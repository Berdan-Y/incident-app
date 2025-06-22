using Shared.Api;
using Shared.Models.Dtos;
using System.Net;
using System.Diagnostics;

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
        try
        {
            var response = await _incidentApi.DeleteIncidentAsync(incident.Id);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = "Failed to delete incident";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to delete incident. Please try again.");
        }
    }

    public async Task<List<IncidentResponseDto>> GetMyIncidentsAsync()
    {
        try
        {
            var response = await _incidentApi.GetMyIncidentsAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = "Failed to load my incidents";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }

            return response.Content ?? new List<IncidentResponseDto>();
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to load incidents. Please try again.");
        }
    }

    public async Task<List<IncidentResponseDto>> GetAllIncidentsAsync()
    {
        try
        {
            var response = await _incidentApi.GetIncidentsAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = "Failed to load all incidents";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }

            return response.Content ?? new List<IncidentResponseDto>();
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to load incidents. Please try again.");
        }
    }

    public async Task<List<IncidentResponseDto>> GetAssignedIncidentsAsync()
    {
        try
        {
            var response = await _incidentApi.GetIncidentsAssignedToMeAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = "Failed to load assigned incidents";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }

            return response.Content ?? new List<IncidentResponseDto>();
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to load incidents. Please try again.");
        }
    }

    public async Task<IncidentResponseDto> GetIncidentByIdAsync(Guid id)
    {
        try
        {
            var response = await _incidentApi.GetIncidentByIdAsync(id);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                string errorContent = "Failed to load incident details";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }

            return response.Content;
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to load incident details. Please try again.");
        }
    }

    public async Task<IncidentResponseDto> UpdateIncidentAsync(Guid id, UpdateIncidentDto updateDto)
    {
        try
        {
            var response = await _incidentApi.UpdateIncidentAsync(id, updateDto);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                string errorContent = "Failed to update incident";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }

            // After updating, fetch the latest version of the incident to get the full IncidentResponseDto
            return await GetIncidentByIdAsync(id);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to update incident. Please try again.");
        }
    }

    public async Task<IncidentResponseDto> UpdateIncidentDetailsAsync(Guid id, UpdateIncidentDetailsDto details)
    {
        try
        {
            var response = await _incidentApi.UpdateIncidentDetailsAsync(id, details);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                string errorContent = "Failed to update incident details";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }

            // After updating, fetch the latest version of the incident to get the full IncidentResponseDto
            return await GetIncidentByIdAsync(id);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to update incident details. Please try again.");
        }
    }

    public async Task<IncidentResponseDto> UpdateIncidentStatusAsync(Guid id, int status)
    {
        try
        {
            var response = await _incidentApi.UpdateIncidentStatusAsync(id, status);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = "Failed to update incident status";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }

            // After updating, fetch the latest version of the incident to get the full IncidentResponseDto
            return await GetIncidentByIdAsync(id);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to update incident status. Please try again.");
        }
    }

    public async Task<IncidentResponseDto> PatchIncidentAsync(Guid id, IncidentPatchDto patchDto)
    {
        try
        {
            var response = await _incidentApi.PatchIncidentAsync(id, patchDto);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = "Failed to update incident";
                try
                {
                    if (response.Error?.Content != null)
                    {
                        errorContent = response.Error.Content;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // If the error content is disposed, just use the default message
                }
                throw new Exception(errorContent);
            }

            // After updating, fetch the latest version of the incident to get the full IncidentResponseDto
            return await GetIncidentByIdAsync(id);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw unauthorized access exceptions
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to update incident. Please try again.");
        }
    }
}