using Refit;
using Shared.Models.Dtos;

namespace Shared.Api;

public interface INotificationApi
{
    [Get("/api/notification")]
    Task<IApiResponse<List<NotificationDto>>> GetNotificationsAsync();

    [Get("/api/notification/unread")]
    Task<IApiResponse<List<NotificationDto>>> GetUnreadNotificationsAsync();

    [Patch("/api/notification/{id}/read")]
    Task<IApiResponse<NotificationDto>> MarkAsReadAsync(Guid id);

    [Patch("/api/notification/{id}/unread")]
    Task<IApiResponse<NotificationDto>> MarkAsUnreadAsync(Guid id);

    [Patch("/api/notification/read-all")]
    Task<IApiResponse<bool>> MarkAllAsReadAsync();
}