using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Dtos;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
        return Ok(notifications);
    }

    [HttpGet("unread")]
    [ProducesResponseType(typeof(IEnumerable<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnreadNotifications()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var notifications = await _notificationService.GetUnreadNotificationsForUserAsync(userId);
        return Ok(notifications);
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var notification = await _notificationService.MarkAsReadAsync(id, userId);

        if (notification == null)
            return NotFound();

        return Ok(notification);
    }

    [HttpPatch("{id:guid}/unread")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDto>> MarkAsUnread(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var notification = await _notificationService.MarkAsUnreadAsync(id, userId);

        if (notification == null)
            return NotFound();

        return Ok(notification);
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok();
    }
}