using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Realtime;

/// <summary>
/// Pushes over SignalR.
///
/// Sends are best-effort by design. A notification's durable copy is the database row;
/// this is the part that makes it appear without a refresh. If the hub is unreachable
/// or the recipient has no connection open, nothing is lost - they see it next time the
/// client asks - so a failure here is logged, never thrown.
///
/// With Redis configured the backplane makes <c>SendToUser</c> reach that user's
/// connections on every instance. Without it, sends only reach clients connected to
/// this process, which is correct for one instance and quietly wrong for several - see
/// the Redis note in appsettings.
/// </summary>
public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<SignalRRealtimeNotifier> _logger;

    public SignalRRealtimeNotifier(
        IHubContext<NotificationHub> hub,
        ILogger<SignalRRealtimeNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task SendToUserAsync<T>(
        Guid userId,
        string eventName,
        T payload,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            () => _hub.Clients.User(userId.ToString()).SendAsync(eventName, payload, cancellationToken),
            eventName);

    public Task SendToUsersAsync<T>(
        IEnumerable<Guid> userIds,
        string eventName,
        T payload,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Select(id => id.ToString()).ToList();

        return ids.Count == 0
            ? Task.CompletedTask
            : SendAsync(
                () => _hub.Clients.Users(ids).SendAsync(eventName, payload, cancellationToken),
                eventName);
    }

    public Task SendToGroupAsync<T>(
        string groupName,
        string eventName,
        T payload,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            () => _hub.Clients.Group(groupName).SendAsync(eventName, payload, cancellationToken),
            eventName);

    private async Task SendAsync(Func<Task> send, string eventName)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Real-time send of {EventName} failed", eventName);
        }
    }
}

/// <summary>
/// Used when SignalR is not running - the integration test host, and any deployment
/// that has switched real-time off.
///
/// Silent rather than logged: unlike a missing job scheduler, a missing push has no
/// consequence beyond the client refreshing a moment later, and warning on every
/// notification would drown the log.
/// </summary>
public sealed class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task SendToUserAsync<T>(Guid userId, string eventName, T payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendToUsersAsync<T>(IEnumerable<Guid> userIds, string eventName, T payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendToGroupAsync<T>(string groupName, string eventName, T payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
