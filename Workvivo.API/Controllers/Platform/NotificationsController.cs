using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Notifications.Commands.MarkNotificationsRead;
using Workvivo.Application.Features.Notifications.Commands.UpdateNotificationPreferences;
using Workvivo.Application.Features.Notifications.Dtos;
using Workvivo.Application.Features.Notifications.Queries.GetMyNotifications;
using Workvivo.Application.Features.Notifications.Queries.GetNotificationPreferences;
using Workvivo.Application.Features.Notifications.Queries.GetUnreadCount;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// The caller's own notifications.
///
/// No <c>[HasPermission]</c> anywhere in this controller, and that is deliberate rather
/// than an omission. Reading your own notifications is not a privilege an administrator
/// grants - every authenticated employee has it, and gating it behind a permission
/// would mean a misconfigured role silently switches somebody's bell off. Authorisation
/// here is ownership, and it is enforced in the handlers, which take the recipient from
/// the token and never from the request.
/// </summary>
public class NotificationsController : ApiControllersBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.Notifications.List)]
    public async Task<IActionResult> List(
        [FromQuery] GetMyNotificationsQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<CursorPagedResult<NotificationDto>>.Ok(await _sender.Send(query, cancellationToken)));

    [HttpGet]
    [Route(RouteClass.Notifications.UnreadCount)]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken) =>
        Ok(ApiResponse<UnreadCountDto>.Ok(await _sender.Send(new GetUnreadCountQuery(), cancellationToken)));

    /// <summary>
    /// Marks notifications read. An empty or absent list means all of them, which is
    /// the "mark all as read" button.
    /// </summary>
    [HttpPost]
    [Route(RouteClass.Notifications.MarkRead)]
    public async Task<IActionResult> MarkRead(
        [FromBody] MarkReadRequest? request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<int>.Ok(await _sender.Send(
            new MarkNotificationsReadCommand(request?.Ids ?? []), cancellationToken)));

    [HttpGet]
    [Route(RouteClass.Notifications.Preferences)]
    public async Task<IActionResult> Preferences(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<NotificationPreferenceDto>>.Ok(
            await _sender.Send(new GetNotificationPreferencesQuery(), cancellationToken)));

    [HttpPut]
    [Route(RouteClass.Notifications.Preferences)]
    public async Task<IActionResult> SavePreferences(
        [FromBody] UpdateNotificationPreferencesCommand command,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<int>.Ok(await _sender.Send(command, cancellationToken)));
}

/// <summary>Which notifications to mark read. Null or empty means every unread one.</summary>
public sealed record MarkReadRequest(IReadOnlyList<Guid>? Ids);
