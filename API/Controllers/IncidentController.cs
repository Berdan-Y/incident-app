using Shared.Models.Enums;
using Shared.Models.Classes;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Shared.Models.Dtos;
using IncidentCreateDto = Shared.Models.Dtos.IncidentCreateDto;
using IncidentPhotoDto = Shared.Models.Dtos.IncidentPhotoDto;
using IncidentResponseDto = Shared.Models.Dtos.IncidentResponseDto;
using Status = Shared.Models.Enums.Status;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidentController : ControllerBase
{
    private readonly IIncidentService _incidentService;
    private readonly IAuthorizationService _authorizationService;

    public IncidentController(IIncidentService incidentService, IAuthorizationService authorizationService)
    {
        _incidentService = incidentService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    [Authorize(Policy = "CanViewAllIncidents")]
    [ProducesResponseType(typeof(IEnumerable<IncidentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IncidentResponseDto>>> GetAllIncidents(
        [FromQuery] Status? status,
        [FromQuery] Priority? priority,
        [FromQuery] Guid? assignedToId,
        [FromQuery] Guid? reportedById,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] DateTime? updatedFrom,
        [FromQuery] DateTime? updatedTo,
        [FromQuery] string? location,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? distanceInKm,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending = false)
    {
        var filter = new IncidentFilterDto
        {
            Status = status,
            Priority = priority,
            AssignedToId = assignedToId,
            ReportedById = reportedById,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            UpdatedFrom = updatedFrom,
            UpdatedTo = updatedTo,
            Location = location,
            Latitude = latitude,
            Longitude = longitude,
            DistanceInKm = distanceInKm,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var incidents = await _incidentService.GetFilteredIncidentsAsync(filter);
        return Ok(incidents);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(IncidentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponseDto>> GetIncidentById(Guid id)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound();

        // Check if user is authorized to view this incident
        var authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanViewAllIncidents");
        if (!authResult.Succeeded && incident.ReportedById != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException()))
        {
            return Forbid();
        }

        return Ok(incident);
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IncidentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentResponseDto>> CreateIncident([FromForm] IFormCollection form)
    {
        // Get the incident data from form
        var incidentData = form["incident"];

        if (string.IsNullOrEmpty(incidentData))
            return BadRequest("Incident data is required");

        // Parse the incident data as a JSON object
        var incidentDto = JsonSerializer.Deserialize<IncidentCreateDto>(incidentData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (incidentDto == null)
            return BadRequest("Invalid incident data");

        // Validate required fields
        if (string.IsNullOrEmpty(incidentDto.Title))
            return BadRequest("Title is required");
        if (string.IsNullOrEmpty(incidentDto.Description))
            return BadRequest("Description is required");

        // Create the incident with whatever ReportedById was sent in the DTO
        var incident = await _incidentService.CreateIncidentAsync(incidentDto);

        // Handle photos if any
        var photos = form.Files.Where(f => f.Name.StartsWith("photos"));
        Console.WriteLine($"Number of photos received: {photos.Count()}");
        foreach (var photo in photos)
        {
            Console.WriteLine($"Processing photo: {photo.FileName}, ContentType: {photo.ContentType}, Length: {photo.Length}");
            await _incidentService.AddPhotoToIncidentAsync(incident.Id, photo);
        }

        // Reload the incident to include the photos
        incident = await _incidentService.GetIncidentByIdAsync(incident.Id);
        return CreatedAtAction(nameof(GetIncidentById), new { id = incident.Id }, incident);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(IncidentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IncidentResponseDto>> UpdateIncident(Guid id, [FromBody] IncidentCreateDto incidentDto)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound();

        // Check if user is authorized to update this incident
        var authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanUpdateAnyIncident");
        if (!authResult.Succeeded)
        {
            // If not admin, check if user is the reporter and can update their own incidents
            if (incident.ReportedById != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException()))
            {
                return Forbid();
            }
            authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanUpdateOwnIncidents");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }
        }

        try
        {
            var updatedIncident = await _incidentService.UpdateIncidentAsync(id, incidentDto);
            return Ok(updatedIncident);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(IncidentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IncidentResponseDto>> PatchIncident(Guid id, [FromBody] IncidentPatchDto patchDto)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound();

        // Check if user is authorized to update this incident
        var authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanUpdateAnyIncident");
        if (!authResult.Succeeded)
        {
            // If not admin, check if user is the reporter and can update their own incidents
            if (incident.ReportedById != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException()))
            {
                return Forbid();
            }
            authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanUpdateOwnIncidents");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            // Members can only update certain fields
            if (User.IsInRole(Role.Member))
            {
                // Members can only update title, description, and address
                if (patchDto.Status.HasValue || patchDto.Priority.HasValue ||
                    patchDto.AssignedToId.HasValue || patchDto.Latitude.HasValue ||
                    patchDto.Longitude.HasValue || patchDto.ZipCode != null)
                {
                    return Forbid();
                }
            }
        }

        try
        {
            var updatedIncident = await _incidentService.PatchIncidentAsync(id, patchDto);
            return Ok(updatedIncident);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIncident(Guid id)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound();

        // Check if user is authorized to delete this incident
        var authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanDeleteAnyIncident");
        if (!authResult.Succeeded)
        {
            // If not admin, check if user is the reporter and can delete their own incidents
            if (incident.ReportedById != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException()))
            {
                return Forbid();
            }
            authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanDeleteOwnIncidents");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }
        }

        await _incidentService.DeleteIncidentAsync(id);
        return NoContent();
    }

    [HttpGet("my-incidents")]
    [Authorize(Policy = "CanViewOwnIncidents")]
    [ProducesResponseType(typeof(IEnumerable<IncidentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IncidentResponseDto>>> GetMyIncidents()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            Console.WriteLine($"GetMyIncidents called for user ID: {userId}");

            var incidents = await _incidentService.GetIncidentsByUserAsync(userId);

            // Ensure incidents is not null
            incidents ??= Enumerable.Empty<IncidentResponseDto>();

            var incidentCount = incidents.Count();
            Console.WriteLine($"Found {incidentCount} incidents for user {userId}");

            // Log each incident individually without using Aggregate or Select
            foreach (var incident in incidents)
            {
                Console.WriteLine($"Incident available: {incident.Id} - {incident.Title}");
            }

            return Ok(incidents);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetMyIncidents: {ex}");
            throw;
        }
    }

    [HttpGet("assigned-to-me")]
    [Authorize(Policy = "CanViewAllIncidents")]
    [ProducesResponseType(typeof(IEnumerable<IncidentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IncidentResponseDto>>> GetIncidentsAssignedToMe()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var incidents = await _incidentService.GetIncidentsAssignedToUserAsync(userId);
        return Ok(incidents);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "CanUpdateIncidentStatus")]
    [ProducesResponseType(typeof(IncidentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponseDto>> UpdateIncidentStatus(Guid id, [FromBody] Status status)
    {
        try
        {
            var incident = await _incidentService.UpdateIncidentStatusAsync(id, status);
            return Ok(incident);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch("{id:guid}/priority")]
    [Authorize(Policy = "CanUpdateIncidentPriority")]
    [ProducesResponseType(typeof(IncidentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponseDto>> UpdateIncidentPriority(Guid id, [FromBody] Priority priority)
    {
        try
        {
            var incident = await _incidentService.UpdateIncidentPriorityAsync(id, priority);
            return Ok(incident);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Policy = "CanUpdateIncidentStatus")]
    [ProducesResponseType(typeof(IncidentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponseDto>> AssignIncident(Guid id, [FromBody] Guid assigneeId)
    {
        try
        {
            var incident = await _incidentService.AssignIncidentAsync(id, assigneeId);
            if (incident == null)
                return NotFound();

            return Ok(incident);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/photos")]
    [Authorize]
    [ProducesResponseType(typeof(IncidentPhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IncidentPhotoDto>> UploadPhoto(Guid id, IFormFile photo)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound();

        // Check if user is authorized to update this incident
        var authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanUpdateAnyIncident");
        if (!authResult.Succeeded)
        {
            // If not admin, check if user is the reporter and can update their own incidents
            if (incident.ReportedById != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException()))
            {
                return Forbid();
            }
            authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanUpdateOwnIncidents");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }
        }

        try
        {
            var photoDto = await _incidentService.AddPhotoToIncidentAsync(id, photo);
            return CreatedAtAction(nameof(GetPhoto), new { id = photoDto.Id }, photoDto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("photos/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(IncidentPhotoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentPhotoDto>> GetPhoto(Guid id)
    {
        var photo = await _incidentService.GetPhotoByIdAsync(id);
        if (photo == null)
            return NotFound();

        return Ok(photo);
    }

    [HttpGet("{id:guid}/photos")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<IncidentPhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<IncidentPhotoDto>>> GetIncidentPhotos(Guid id)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound();

        // Check if user is authorized to view this incident
        var authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanViewAllIncidents");
        if (!authResult.Succeeded && incident.ReportedById != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException()))
        {
            return Forbid();
        }

        var photos = await _incidentService.GetIncidentPhotosAsync(id);
        return Ok(photos);
    }

    [HttpDelete("photos/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePhoto(Guid id)
    {
        var photo = await _incidentService.GetPhotoByIdAsync(id);
        if (photo == null)
            return NotFound();

        var incident = await _incidentService.GetIncidentByIdAsync(photo.IncidentId);
        if (incident == null)
            return NotFound();

        // Check if user is authorized to update this incident
        var authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanUpdateAnyIncident");
        if (!authResult.Succeeded)
        {
            // If not admin, check if user is the reporter and can update their own incidents
            if (incident.ReportedById != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException()))
            {
                return Forbid();
            }
            authResult = await _authorizationService.AuthorizeAsync(User, incident, "CanUpdateOwnIncidents");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }
        }

        await _incidentService.DeletePhotoAsync(id);
        return NoContent();
    }
}