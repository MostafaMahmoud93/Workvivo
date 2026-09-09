namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Pushes messages to connected clients. Implemented over SignalR, abstracted so the
/// notification pipeline does not depend on the transport and can be unit-tested.
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>Sends to every live connection belonging to one user, across their devices.</summary>
    Task SendToUserAsync<T>(Guid userId, string eventName, T payload, CancellationToken cancellationToken = default);

    Task SendToUsersAsync<T>(IEnumerable<Guid> userIds, string eventName, T payload, CancellationToken cancellationToken = default);

    /// <summary>Sends to a named group - a community, an event's attendees.</summary>
    Task SendToGroupAsync<T>(string groupName, string eventName, T payload, CancellationToken cancellationToken = default);
}
