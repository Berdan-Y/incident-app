using Shared.Models.Dtos;

namespace API.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(Guid userId);
    Task<IEnumerable<NotificationDto>> GetUnreadNotificationsForUserAsync(Guid userId);
    Task<NotificationDto?> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<NotificationDto?> MarkAsUnreadAsync(Guid notificationId, Guid userId);
    Task<bool> MarkAllAsReadAsync(Guid userId);
    Task CreateIncidentUpdateNotificationAsync(Guid incidentId, string message, Guid? excludeUserId = null);
}