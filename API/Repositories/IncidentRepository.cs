using API.Data;
using Shared.Models.Dtos;
using Shared.Models.Classes;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly IncidentDbContext _context;

    public IncidentRepository(IncidentDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Incident>> GetAllAsync()
    {
        return await _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Incident?> GetByIdAsync(Guid id)
    {
        return await _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .Include(i => i.Photos)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Incident>> GetByReporterIdAsync(Guid reporterId)
    {
        return await _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .Where(i => i.ReportedById == reporterId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Incident> CreateAsync(Incident incident)
    {
        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        // Reload the incident with related entities
        return await _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .FirstOrDefaultAsync(i => i.Id == incident.Id) ?? incident;
    }

    public async Task<Incident> UpdateAsync(Incident incident)
    {
        _context.Entry(incident).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        // Reload the incident with related entities
        return await _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .FirstOrDefaultAsync(i => i.Id == incident.Id) ?? incident;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var incident = await _context.Incidents.FindAsync(id);
        if (incident == null)
            return false;

        _context.Incidents.Remove(incident);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Incident>> GetByUserIdAsync(Guid userId)
    {
        Console.WriteLine($"GetByUserIdAsync called for user {userId}");

        // First check if the user exists
        var user = await _context.Users.FindAsync(userId);
        Console.WriteLine($"User found: {user != null}, Email: {user?.Email}");

        // Get all incidents and log them
        var allIncidents = await _context.Incidents.ToListAsync();
        Console.WriteLine($"Total incidents in database: {allIncidents.Count}");
        foreach (var incident in allIncidents)
        {
            Console.WriteLine($"Incident {incident.Id}: Title={incident.Title}, ReportedById={incident.ReportedById}");
        }

        // Now get incidents for this user
        var incidents = await _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .Where(i => i.ReportedById == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        Console.WriteLine($"Found {incidents.Count()} incidents for user {userId}");
        foreach (var incident in incidents)
        {
            Console.WriteLine($"Found incident: {incident.Id}, Title: {incident.Title}, ReportedById: {incident.ReportedById}");
        }

        return incidents;
    }

    public async Task<IEnumerable<Incident>> GetAssignedToUserAsync(Guid userId)
    {
        return await _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .Where(i => i.AssignedToId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Incident>> GetFilteredIncidentsAsync(IncidentFilterDto filter)
    {
        var query = _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .Include(i => i.Photos)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.Title))
            query = query.Where(i => i.Title.Contains(filter.Title));

        if (!string.IsNullOrEmpty(filter.Description))
            query = query.Where(i => i.Description.Contains(filter.Description));

        if (filter.Status.HasValue)
            query = query.Where(i => i.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(i => i.Priority == filter.Priority.Value);

        if (filter.AssignedToId.HasValue)
            query = query.Where(i => i.AssignedToId == filter.AssignedToId.Value);

        if (filter.ReportedById.HasValue)
            query = query.Where(i => i.ReportedById == filter.ReportedById.Value);

        if (filter.CreatedFrom.HasValue)
            query = query.Where(i => i.CreatedAt >= filter.CreatedFrom.Value);

        if (filter.CreatedTo.HasValue)
            query = query.Where(i => i.CreatedAt <= filter.CreatedTo.Value);

        if (filter.UpdatedFrom.HasValue)
            query = query.Where(i => i.UpdatedAt >= filter.UpdatedFrom.Value);

        if (filter.UpdatedTo.HasValue)
            query = query.Where(i => i.UpdatedAt <= filter.UpdatedTo.Value);

        if (!string.IsNullOrEmpty(filter.Location))
            query = query.Where(i => i.Address.Contains(filter.Location) || i.ZipCode.Contains(filter.Location));

        // Apply distance filter if coordinates and distance are provided
        if (filter.Latitude.HasValue && filter.Longitude.HasValue && filter.DistanceInKm.HasValue)
        {
            // Using the Haversine formula to calculate distance
            var distanceInKm = filter.DistanceInKm.Value;
            var lat = filter.Latitude.Value;
            var lon = filter.Longitude.Value;

            query = query.Where(i =>
                (6371 * Math.Acos(
                    Math.Cos(lat * Math.PI / 180) * Math.Cos(i.Latitude * Math.PI / 180) *
                    Math.Cos((lon - i.Longitude) * Math.PI / 180) +
                    Math.Sin(lat * Math.PI / 180) * Math.Sin(i.Latitude * Math.PI / 180)
                )) <= distanceInKm
            );
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(filter.SortBy))
        {
            query = filter.SortBy.ToLower() switch
            {
                "title" => filter.SortDescending
                    ? query.OrderByDescending(i => i.Title)
                    : query.OrderBy(i => i.Title),
                "description" => filter.SortDescending
                    ? query.OrderByDescending(i => i.Description)
                    : query.OrderBy(i => i.Description),
                "status" => filter.SortDescending
                    ? query.OrderByDescending(i => i.Status)
                    : query.OrderBy(i => i.Status),
                "priority" => filter.SortDescending
                    ? query.OrderByDescending(i => i.Priority)
                    : query.OrderBy(i => i.Priority),
                "reporter" => filter.SortDescending
                    ? query.OrderByDescending(i => i.CreatedBy.FirstName + " " + i.CreatedBy.LastName)
                    : query.OrderBy(i => i.CreatedBy.FirstName + " " + i.CreatedBy.LastName),
                "assignee" => filter.SortDescending
                    ? query.OrderByDescending(i => i.AssignedTo.FirstName + " " + i.AssignedTo.LastName)
                    : query.OrderBy(i => i.AssignedTo.FirstName + " " + i.AssignedTo.LastName),
                "createdat" => filter.SortDescending
                    ? query.OrderByDescending(i => i.CreatedAt)
                    : query.OrderBy(i => i.CreatedAt),
                "updatedat" => filter.SortDescending
                    ? query.OrderByDescending(i => i.UpdatedAt)
                    : query.OrderBy(i => i.UpdatedAt),
                "location" when filter.Latitude.HasValue && filter.Longitude.HasValue =>
                    filter.SortDescending
                        ? query.OrderByDescending(i =>
                            (6371 * Math.Acos(
                                Math.Cos(filter.Latitude.Value * Math.PI / 180) * Math.Cos(i.Latitude * Math.PI / 180) *
                                Math.Cos((filter.Longitude.Value - i.Longitude) * Math.PI / 180) +
                                Math.Sin(filter.Latitude.Value * Math.PI / 180) * Math.Sin(i.Latitude * Math.PI / 180)
                            )))
                        : query.OrderBy(i =>
                            (6371 * Math.Acos(
                                Math.Cos(filter.Latitude.Value * Math.PI / 180) * Math.Cos(i.Latitude * Math.PI / 180) *
                                Math.Cos((filter.Longitude.Value - i.Longitude) * Math.PI / 180) +
                                Math.Sin(filter.Latitude.Value * Math.PI / 180) * Math.Sin(i.Latitude * Math.PI / 180)
                            ))),
                _ => filter.SortDescending
                    ? query.OrderByDescending(i => i.CreatedAt)
                    : query.OrderBy(i => i.CreatedAt)
            };
        }
        else
        {
            // Default sorting by CreatedAt
            query = filter.SortDescending
                ? query.OrderByDescending(i => i.CreatedAt)
                : query.OrderBy(i => i.CreatedAt);
        }

        return await query.ToListAsync();
    }

    public async Task<IncidentPhoto> AddPhotoAsync(IncidentPhoto photo)
    {
        _context.IncidentPhotos.Add(photo);
        await _context.SaveChangesAsync();
        return photo;
    }

    public async Task<IncidentPhoto?> GetPhotoByIdAsync(Guid photoId)
    {
        return await _context.IncidentPhotos.FindAsync(photoId);
    }

    public async Task DeletePhotoAsync(Guid photoId)
    {
        var photo = await _context.IncidentPhotos.FindAsync(photoId);
        if (photo != null)
        {
            _context.IncidentPhotos.Remove(photo);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<IncidentPhoto>> GetIncidentPhotosAsync(Guid incidentId)
    {
        return await _context.IncidentPhotos
            .Where(p => p.IncidentId == incidentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Incident>> GetIncidentsByUserAsync(Guid userId, bool includeCreated = true, bool includeAssigned = true)
    {
        var query = _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .Include(i => i.Photos)
            .AsQueryable();

        if (includeCreated && includeAssigned)
        {
            query = query.Where(i => i.ReportedById == userId || i.AssignedToId == userId);
        }
        else if (includeCreated)
        {
            query = query.Where(i => i.ReportedById == userId);
        }
        else if (includeAssigned)
        {
            query = query.Where(i => i.AssignedToId == userId);
        }

        return await query.ToListAsync();
    }

    public async Task<bool> UserExistsAsync(Guid userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }
}