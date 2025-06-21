using API.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Classes;
using Shared.Models.Dtos;

namespace API.Services;

public class NotificationService : INotificationService
{
    private readonly IncidentDbContext _context;
    private readonly IMapper _mapper;

    public NotificationService(IncidentDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Include(n => n.User)
            .Include(n => n.Incident)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
    }

    public async Task<IEnumerable<NotificationDto>> GetUnreadNotificationsForUserAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Include(n => n.User)
            .Include(n => n.Incident)
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
    }

    public async Task<NotificationDto?> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .Include(n => n.User)
            .Include(n => n.Incident)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return null;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return _mapper.Map<NotificationDto>(notification);
    }

    public async Task<NotificationDto?> MarkAsUnreadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .Include(n => n.User)
            .Include(n => n.Incident)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return null;

        notification.IsRead = false;
        notification.ReadAt = null;

        await _context.SaveChangesAsync();

        return _mapper.Map<NotificationDto>(notification);
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (!notifications.Any())
            return false;

        var now = DateTime.UtcNow;
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CreateIncidentUpdateNotificationAsync(Guid incidentId, string message, Guid? excludeUserId = null)
    {
        var incident = await _context.Incidents
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTo)
            .FirstOrDefaultAsync(i => i.Id == incidentId);

        if (incident == null)
            return;

        var notificationRecipients = new List<Guid>();

        // Add reporter if exists and not excluded
        if (incident.ReportedById.HasValue && (!excludeUserId.HasValue || incident.ReportedById.Value != excludeUserId.Value))
            notificationRecipients.Add(incident.ReportedById.Value);

        // Add assignee if exists and not excluded
        if (incident.AssignedToId.HasValue && (!excludeUserId.HasValue || incident.AssignedToId.Value != excludeUserId.Value))
            notificationRecipients.Add(incident.AssignedToId.Value);

        // Create notifications for all recipients
        var notifications = notificationRecipients.Select(userId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IncidentId = incidentId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.Notifications.AddRangeAsync(notifications);
        await _context.SaveChangesAsync();
    }
} 