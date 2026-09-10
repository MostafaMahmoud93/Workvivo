namespace Workvivo.Application.Features.Notifications.Common;

/// <summary>
/// Turns "this happened, tell these people" into rows, a live push and, where the
/// recipient asked for it, an email.
///
/// One implementation, called from every domain-event subscriber, so the rules that
/// decide whether somebody actually hears about something - do not notify the actor,
/// honour their preferences, skip people who have left - are stated once. Spread across
/// subscribers they diverge, and the divergence shows up as one feature that keeps
/// emailing people who switched email off.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Delivers to the given recipients. Returns how many people were actually
    /// notified in-app, which is zero when everyone was filtered out.
    /// </summary>
    Task<int> DispatchAsync(NotificationSendRequest request, CancellationToken cancellationToken = default);
}
