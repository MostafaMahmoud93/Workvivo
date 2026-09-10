namespace Workvivo.Application.Features.Notifications.Jobs;

/// <summary>
/// Sends the email for one notification to one person, out of band.
///
/// Ids only. The job runs in a different process minutes later, so anything richer
/// than a value that can be looked up again would be a stale snapshot at best and an
/// unserialisable object graph at worst.
/// </summary>
public interface INotificationEmailJob
{
    Task SendAsync(Guid notificationId, Guid recipientUserId);
}

/// <summary>Publishes posts whose scheduled time has arrived.</summary>
public interface IScheduledPostPublishingJob
{
    Task PublishDueAsync();
}

/// <summary>Repairs denormalised engagement counters that have drifted from the rows they count.</summary>
public interface ICounterReconciliationJob
{
    Task ReconcileAsync();
}

/// <summary>Sends the batched email people chose instead of one message per event.</summary>
public interface INotificationDigestJob
{
    /// <summary>
    /// <paramref name="frequency"/> is the <see cref="Domain.Abstractions.Enums.NotificationDigestFrequency"/>
    /// value being run. Passed as an int because Hangfire serialises job arguments and
    /// an enum name is a rename away from breaking every queued job.
    /// </summary>
    Task SendAsync(int frequency);
}

/// <summary>Fans an announcement out to everyone its audience rules select.</summary>
public interface IAnnouncementFanOutJob
{
    Task FanOutAsync(Guid postId);
}
