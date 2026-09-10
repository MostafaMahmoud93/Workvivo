using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.RealTime;

namespace Workvivo.Application.Features.Notifications.Jobs;

/// <summary>
/// Sends the batched email that people who did not want one message per event chose
/// instead.
///
/// Reads from the notification rows already written, so a digest can only ever contain
/// things the recipient was genuinely notified about, and the two channels cannot
/// disagree.
///
/// The window is derived from the frequency rather than stored per employee. Storing a
/// "last digest sent" timestamp per person is more precise and much more fragile - a
/// missed run silently skips a day for everybody, and a replayed one sends twice.
/// </summary>
public sealed class NotificationDigestJob : INotificationDigestJob
{
    /// <summary>Most items quoted in one digest. Beyond this it stops being readable.</summary>
    private const int MaxItemsPerDigest = 20;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _sender;
    private readonly NotificationEmailRenderer _renderer;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<NotificationDigestJob> _logger;

    public NotificationDigestJob(
        IUnitOfWork unitOfWork,
        IEmailSender sender,
        NotificationEmailRenderer renderer,
        IDateTimeProvider clock,
        ILogger<NotificationDigestJob> logger)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
        _renderer = renderer;
        _clock = clock;
        _logger = logger;
    }

    public async Task SendAsync(int frequency)
    {
        if (!Enum.IsDefined(typeof(NotificationDigestFrequency), frequency))
        {
            _logger.LogWarning("Digest job called with an unknown frequency {Frequency}", frequency);
            return;
        }

        var digest = (NotificationDigestFrequency)frequency;

        var window = digest switch
        {
            NotificationDigestFrequency.Hourly => TimeSpan.FromHours(1),
            NotificationDigestFrequency.Daily => TimeSpan.FromDays(1),
            NotificationDigestFrequency.Weekly => TimeSpan.FromDays(7),
            _ => TimeSpan.Zero,
        };

        if (window == TimeSpan.Zero)
        {
            // Immediate is sent by the dispatcher and Never is not sent at all. Both
            // reaching here would mean the recurring jobs were registered wrongly.
            return;
        }

        var since = _clock.UtcNow - window;

        // Who asked for this cadence, on which types. One query, because the
        // alternative is a query per employee.
        var subscriptions = await _unitOfWork.Repository<NotificationPreference, Guid>()
            .GetAllQ()
            .Where(preference =>
                preference.Email_Enabled
                && preference.Email_Frequency == digest)
            .Select(preference => new { preference.Employee_Id, preference.Notification_Type })
            .ToListAsync();

        if (subscriptions.Count == 0)
        {
            return;
        }

        var byEmployee = subscriptions
            .GroupBy(subscription => subscription.Employee_Id)
            .ToDictionary(group => group.Key, group => group.Select(s => (int)s.Notification_Type).ToHashSet());

        var employeeIds = byEmployee.Keys.ToArray();

        var employees = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employeeIds.Contains(employee.Id) && employee.Is_Active)
            .Select(employee => new { employee.Id, employee.User_Id, employee.Email, employee.Preferred_Language })
            .ToListAsync();

        var sent = 0;

        foreach (var employee in employees)
        {
            if (string.IsNullOrWhiteSpace(employee.Email))
            {
                continue;
            }

            var types = byEmployee[employee.Id];

            var items = await _unitOfWork.Repository<NotificationUser, Guid>()
                .GetAllQ()
                .Where(delivery =>
                    delivery.Reciever_Id == employee.User_Id
                    && delivery.Create_Date >= since

                    // Only what they have not already seen in the app. A digest that
                    // repeats what somebody read an hour ago is noise they will
                    // unsubscribe from.
                    && !delivery.IS_Seen
                    && types.Contains(delivery.Notification.Notification_Type))
                .OrderByDescending(delivery => delivery.Create_Date)
                .Take(MaxItemsPerDigest)
                .Select(delivery => new
                {
                    delivery.Notification.Header_Ar,
                    delivery.Notification.Header_En,
                    delivery.Notification.Content_Ar,
                    delivery.Notification.Content_En,
                    delivery.Notification.RedirectUrl,
                })
                .ToListAsync();

            if (items.Count == 0)
            {
                continue;
            }

            var arabic = string.Equals(employee.Preferred_Language, "ar", StringComparison.OrdinalIgnoreCase);

            var subject = arabic
                ? $"لديك {items.Count} إشعار جديد في Workvivo"
                : $"You have {items.Count} new notification(s) in Workvivo";

            var message = _renderer.Digest(
                employee.Email,
                subject,
                [.. items.Select(item => (
                    arabic ? item.Header_Ar : item.Header_En,
                    arabic ? item.Content_Ar : item.Content_En,
                    item.RedirectUrl))],
                null);

            if (await _sender.SendAsync(message))
            {
                sent++;
            }
        }

        _logger.LogInformation("{Digest} digest sent to {Count} employee(s)", digest, sent);
    }
}
