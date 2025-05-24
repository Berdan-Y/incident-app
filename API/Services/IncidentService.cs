using API.Dtos;
using API.Models.Classes;
using API.Models.Enums;
using API.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class IncidentService : IIncidentService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;

    public IncidentService(IIncidentRepository incidentRepository, IMapper mapper, IFileService fileService)
    {
        _incidentRepository = incidentRepository;
        _mapper = mapper;
        _fileService = fileService;
    }

    public async Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync()
    {
        var incidents = await _incidentRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<IncidentResponseDto>>(incidents);
    }

    public async Task<IncidentResponseDto?> GetIncidentByIdAsync(Guid id)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        return incident != null ? _mapper.Map<IncidentResponseDto>(incident) : null;
    }

    public async Task<IEnumerable<IncidentResponseDto>> GetIncidentsByUserAsync(Guid userId)
    {
        var incidents = await _incidentRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<IncidentResponseDto>>(incidents);
    }

    public async Task<IEnumerable<IncidentResponseDto>> GetIncidentsAssignedToUserAsync(Guid userId)
    {
        var incidents = await _incidentRepository.GetAssignedToUserAsync(userId);
        return _mapper.Map<IEnumerable<IncidentResponseDto>>(incidents);
    }

    public async Task<IncidentResponseDto> CreateIncidentAsync(IncidentCreateDto incidentDto, Guid? userId = null)
    {
        var incident = _mapper.Map<Incident>(incidentDto);
        incident.ReportedById = incidentDto.ReportedById ?? userId;
        incident.AssignedToId = null;
        incident.CreatedAt = DateTime.UtcNow;
        incident.UpdatedAt = DateTime.UtcNow;

        var createdIncident = await _incidentRepository.CreateAsync(incident);
        return _mapper.Map<IncidentResponseDto>(createdIncident);
    }

    public async Task<IncidentResponseDto?> UpdateIncidentAsync(Guid id, IncidentCreateDto incidentDto)
    {
        var existingIncident = await _incidentRepository.GetByIdAsync(id);
        if (existingIncident == null)
            return null;

        _mapper.Map(incidentDto, existingIncident);
        existingIncident.UpdatedAt = DateTime.UtcNow;

        var updatedIncident = await _incidentRepository.UpdateAsync(existingIncident);
        return _mapper.Map<IncidentResponseDto>(updatedIncident);
    }

    public async Task<IncidentResponseDto?> PatchIncidentAsync(Guid id, IncidentPatchDto patchDto)
    {
        var existingIncident = await _incidentRepository.GetByIdAsync(id);
        if (existingIncident == null)
            return null;

        // Only update properties that are not null in the patch DTO
        if (patchDto.Title != null) existingIncident.Title = patchDto.Title;
        if (patchDto.Description != null) existingIncident.Description = patchDto.Description;
        if (patchDto.Latitude.HasValue) existingIncident.Latitude = patchDto.Latitude.Value;
        if (patchDto.Longitude.HasValue) existingIncident.Longitude = patchDto.Longitude.Value;
        if (patchDto.Address != null) existingIncident.Address = patchDto.Address;
        if (patchDto.ZipCode != null) existingIncident.ZipCode = patchDto.ZipCode;
        if (patchDto.Status.HasValue) existingIncident.Status = patchDto.Status.Value;
        if (patchDto.Priority.HasValue) existingIncident.Priority = patchDto.Priority.Value;
        if (patchDto.AssignedToId.HasValue) existingIncident.AssignedToId = patchDto.AssignedToId.Value;

        existingIncident.UpdatedAt = DateTime.UtcNow;

        var updatedIncident = await _incidentRepository.UpdateAsync(existingIncident);
        return _mapper.Map<IncidentResponseDto>(updatedIncident);
    }

    public async Task<IncidentResponseDto?> UpdateIncidentStatusAsync(Guid id, Status status)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        if (incident == null)
            return null;

        incident.Status = status;
        incident.UpdatedAt = DateTime.UtcNow;

        var updatedIncident = await _incidentRepository.UpdateAsync(incident);
        return _mapper.Map<IncidentResponseDto>(updatedIncident);
    }

    public async Task<IncidentResponseDto?> UpdateIncidentPriorityAsync(Guid id, Priority priority)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        if (incident == null)
            return null;

        incident.Priority = priority;
        incident.UpdatedAt = DateTime.UtcNow;

        var updatedIncident = await _incidentRepository.UpdateAsync(incident);
        return _mapper.Map<IncidentResponseDto>(updatedIncident);
    }

    public async Task<bool> DeleteIncidentAsync(Guid id)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        if (incident == null)
            return false;

        // Delete all associated photos
        var photos = await _incidentRepository.GetIncidentPhotosAsync(id);
        foreach (var photo in photos)
        {
            await _fileService.DeleteFileAsync(photo.FilePath);
        }

        return await _incidentRepository.DeleteAsync(id);
    }

    public async Task<IncidentResponseDto?> AssignIncidentAsync(Guid id, Guid assigneeId)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        if (incident == null)
            return null;

        incident.AssignedToId = assigneeId;
        incident.UpdatedAt = DateTime.UtcNow;

        var updatedIncident = await _incidentRepository.UpdateAsync(incident);
        return _mapper.Map<IncidentResponseDto>(updatedIncident);
    }

    public async Task<IEnumerable<IncidentResponseDto>> GetFilteredIncidentsAsync(IncidentFilterDto filter)
    {
        var incidents = await _incidentRepository.GetFilteredIncidentsAsync(filter);
        return _mapper.Map<IEnumerable<IncidentResponseDto>>(incidents);
    }

    public async Task<IncidentPhotoDto> AddPhotoToIncidentAsync(Guid incidentId, IFormFile photo)
    {
        var incident = await _incidentRepository.GetByIdAsync(incidentId);
        if (incident == null)
            throw new KeyNotFoundException($"Incident with ID {incidentId} not found");

        // Validate file type
        if (!photo.ContentType.StartsWith("image/"))
            throw new ArgumentException("Only image files are allowed");

        // Validate file size (max 10MB)
        if (photo.Length > 10 * 1024 * 1024)
            throw new ArgumentException("File size must be less than 10MB");

        // Check maximum number of photos (max 10 per incident)
        var existingPhotos = await _incidentRepository.GetIncidentPhotosAsync(incidentId);
        if (existingPhotos.Count() >= 10)
            throw new InvalidOperationException("Maximum number of photos (10) reached for this incident");

        var filePath = await _fileService.SaveFileAsync(photo, "uploads/incidents");
        
        var photoEntity = new IncidentPhoto
        {
            FileName = photo.FileName,
            ContentType = photo.ContentType,
            FilePath = filePath,
            IncidentId = incidentId,
            UploadedAt = DateTime.UtcNow
        };

        var savedPhoto = await _incidentRepository.AddPhotoAsync(photoEntity);
        return _mapper.Map<IncidentPhotoDto>(savedPhoto);
    }

    public async Task DeletePhotoAsync(Guid photoId)
    {
        var photo = await _incidentRepository.GetPhotoByIdAsync(photoId);
        if (photo == null)
            throw new KeyNotFoundException($"Photo with ID {photoId} not found");

        await _fileService.DeleteFileAsync(photo.FilePath);
        await _incidentRepository.DeletePhotoAsync(photoId);
    }

    public async Task<IEnumerable<IncidentPhotoDto>> GetIncidentPhotosAsync(Guid incidentId)
    {
        var photos = await _incidentRepository.GetIncidentPhotosAsync(incidentId);
        return _mapper.Map<IEnumerable<IncidentPhotoDto>>(photos);
    }

    public async Task<IncidentPhotoDto?> GetPhotoByIdAsync(Guid photoId)
    {
        var photo = await _incidentRepository.GetPhotoByIdAsync(photoId);
        return photo != null ? _mapper.Map<IncidentPhotoDto>(photo) : null;
    }
}