using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Application.Features.Notifications.Dtos;
using Workvivo.Application.Features.Notifications.Jobs;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.RealTime;

namespace Workvivo.Application.Features.Notifications.Common;

/// <summary>
/// The one place a notification is actually created.
///
/// Runs after the transaction that caused it has committed - see
/// <c>DomainEventDispatchBehavior</c> - so its own writes are a separate, small
/// transaction of their own.
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _realtime;
    private readonly IBackgroundJobScheduler _jobs;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IUnitOfWork unitOfWork,
        IRealtimeNotifier realtime,
        IBackgroundJobScheduler jobs,
        IDateTimeProvider clock,
        ILogger<NotificationDispatcher> logger)
    {
        _unitOfWork = unitOfWork;
        _realtime = realtime;
        _jobs = jobs;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> DispatchAsync(
        NotificationSendRequest request,
        CancellationToken cancellationToken = default)
    {
        // Deduplicated, and the actor removed. Somebody who mentions themselves in
        // their own post should not be told about it, and a person mentioned twice in
        // one comment should be told once.
        var recipientIds = request.RecipientEmployeeIds
            .Where(id => id != Guid.Empty && id != request.ActorEmployeeId)
            .Distinct()
            .ToArray();

        if (recipientIds.Length == 0)
        {
            return 0;
        }

        var now = _clock.UtcNow;
        var recipients = await LoadRecipientsAsync(recipientIds, cancellationToken);

        if (recipients.Count == 0)
        {
            return 0;
        }

        var preferences = await LoadPreferencesAsync(recipientIds, request.Type, cancellationToken);

        var actorUserId = await ResolveActorUserIdAsync(request.ActorEmployeeId, cancellationToken);
        var notification = BuildNotification(request, actorUserId);

        var inApp = new List<Recipient>();
        var immediateEmail = new List<Recipient>();

        foreach (var recipient in recipients)
        {
            var preference = preferences.GetValueOrDefault(recipient.EmployeeId)
                ?? NotificationDefaults.For(recipient.EmployeeId, request.Type);

            if (preference.Allows(NotificationChannel.InApp))
            {
                inApp.Add(recipient);
            }

            // Batched frequencies are not sent here. The digest job picks those up from
            // the rows written below, which is also why somebody on a daily digest still
            // needs the in-app row: it is the digest's source of truth.
            if (preference.Allows(NotificationChannel.Email)
                && preference.Email_Frequency == NotificationDigestFrequency.Immediate
                && !string.IsNullOrWhiteSpace(recipient.Email))
            {
                immediateEmail.Add(recipient);
            }
        }

        if (inApp.Count == 0 && immediateEmail.Count == 0)
        {
            return 0;
        }

        // The notification row exists even when nobody wants it in-app, because the
        // email job reads its text from there.
        await _unitOfWork.Repository<Notification, Guid>().AddAsync(notification);

        foreach (var recipient in inApp)
        {
            notification.NotificationUsers.Add(new NotificationUser
            {
                Id = Guid.NewGuid(),
                Notification_Id = notification.Id,
                Reciever_Id = recipient.UserId,
                IS_Seen = false,
                Create_Date = now,
                Is_Deleted = false,
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PushAsync(notification, inApp, now, cancellationToken);

        foreach (var recipient in immediateEmail)
        {
            // Queued rather than sent here. An SMTP conversation takes hundreds of
            // milliseconds and fails in ways that have nothing to do with the request
            // that triggered it.
            _jobs.Enqueue<INotificationEmailJob>(job => job.SendAsync(notification.Id, recipient.UserId));
        }

        _logger.LogInformation(
            "Notification {NotificationId} of type {NotificationType} delivered in-app to {InAppCount} and queued for email to {EmailCount}",
            notification.Id,
            request.Type,
            inApp.Count,
            immediateEmail.Count);

        return inApp.Count;
    }

    /// <summary>
    /// People who can still receive things.
    ///
    /// Inactive employees are dropped here rather than at the call site: somebody who
    /// has left should not accumulate notifications, and every subscriber would
    /// otherwise have to remember the same filter.
    /// </summary>
    private async Task<IReadOnlyList<Recipient>> LoadRecipientsAsync(
        Guid[] employeeIds,
        CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employeeIds.Contains(employee.Id) && employee.Is_Active)
            .Select(employee => new Recipient(
                employee.Id,
                employee.User_Id,
                employee.Email,
                employee.Preferred_Language))
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<Guid, NotificationPreference>> LoadPreferencesAsync(
        Guid[] employeeIds,
        NotificationType type,
        CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<NotificationPreference, Guid>()
            .GetAllQ()
            .Where(preference =>
                employeeIds.Contains(preference.Employee_Id)
                && preference.Notification_Type == type)
            .ToDictionaryAsync(preference => preference.Employee_Id, cancellationToken);

    /// <summary>
    /// The actor's login, for the notification's creator navigation.
    ///
    /// Null is a valid answer - a birthday or a digest has no human behind it - which is
    /// why the relationship is optional. The alternative, inventing a system account to
    /// satisfy a foreign key, produces audit rows that name a user who does not exist.
    /// </summary>
    private async Task<Guid?> ResolveActorUserIdAsync(Guid? actorEmployeeId, CancellationToken cancellationToken)
    {
        if (actorEmployeeId is not { } employeeId)
        {
            return null;
        }

        return await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => (Guid?)employee.User_Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Notification BuildNotification(NotificationSendRequest request, Guid? actorUserId) => new()
    {
        Id = Guid.NewGuid(),
        Header_Ar = request.Copy.HeaderAr,
        Header_En = request.Copy.HeaderEn,
        Content_Ar = request.Copy.ContentAr,
        Content_En = request.Copy.ContentEn,
        RedirectUrl = request.RedirectUrl,
        Notification_Type = (int)request.Type,
        Notification_Status = "OPEN",
        Actor_Employee_Id = request.ActorEmployeeId,
        Entity_Type = request.EntityType,
        Entity_Id = request.EntityId,
        Creator_User_Id = actorUserId,

        // Create_Date and Created_By are stamped by the DbContext on save. Left alone
        // here so the audit trail says who really wrote the row.
        Is_Deleted = false,
    };

    /// <summary>
    /// Pushes the new notification to whoever is connected.
    ///
    /// Failures are swallowed deliberately. The row is already committed, so the person
    /// will see it the next time the client asks; a dead Redis backplane must not turn
    /// into an exception on the request that caused the notification.
    /// </summary>
    private async Task PushAsync(
        Notification notification,
        IReadOnlyList<Recipient> recipients,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (recipients.Count == 0)
        {
            return;
        }

        try
        {
            // Per-recipient rather than one broadcast, because the payload is not the
            // same for everybody: the text is picked in their language and the unread
            // count is theirs.
            foreach (var recipient in recipients)
            {
                var arabic = string.Equals(recipient.PreferredLanguage, "ar", StringComparison.OrdinalIgnoreCase);

                var payload = new RealtimeNotificationDto
                {
                    Notification = new NotificationDto
                    {
                        NotificationId = notification.Id,
                        Type = notification.Notification_Type,
                        Header = arabic ? notification.Header_Ar : notification.Header_En,
                        Content = arabic ? notification.Content_Ar : notification.Content_En,
                        RedirectUrl = notification.RedirectUrl,
                        EntityType = (int)notification.Entity_Type,
                        EntityId = notification.Entity_Id,
                        ActorEmployeeId = notification.Actor_Employee_Id,
                        IsSeen = false,
                        CreatedDate = now,
                    },
                };

                await _realtime.SendToUserAsync(
                    recipient.UserId, RealtimeEvents.NotificationReceived, payload, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Real-time delivery of notification {NotificationId} failed; the stored copy is unaffected",
                notification.Id);
        }
    }

    private readonly record struct Recipient(
        Guid EmployeeId,
        Guid UserId,
        string? Email,
        string? PreferredLanguage);
}

/// <summary>
/// Names of the messages pushed over the hub.
///
/// Constants because the string appears twice - once here and once in the Angular
/// client - and a typo in either produces silence rather than an error.
/// </summary>
public static class RealtimeEvents
{
    public const string NotificationReceived = "notification";

    public const string UnreadCountChanged = "unreadCount";
}
