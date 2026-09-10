using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Events;

namespace Workvivo.Application.Features.Events.Jobs;

/// <summary>Reminds attendees shortly before an event starts.</summary>
public interface IEventReminderJob
{
    Task SendDueAsync();
}

/// <summary>
/// Sends the "starting soon" notification.
///
/// Only to people who said they are coming: a reminder for an event somebody
/// declined is noise, and reminding the whole invited audience again would make the
/// feature something people switch off.
/// </summary>
public sealed class EventReminderJob : IEventReminderJob
{
    /// <summary>How far ahead a reminder goes out.</summary>
    private static readonly TimeSpan Lead = TimeSpan.FromHours(24);

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<EventReminderJob> _logger;

    public EventReminderJob(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        IDateTimeProvider clock,
        ILogger<EventReminderJob> logger)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _clock = clock;
        _logger = logger;
    }

    public async Task SendDueAsync()
    {
        var now = _clock.UtcNow;
        var horizon = now.Add(Lead);

        var due = await _unitOfWork.Repository<Event, Guid>()
            .GetAllQ()
            .Where(candidate =>
                candidate.Status == EventStatus.Published
                && candidate.Start_At > now
                && candidate.Start_At <= horizon

                // The stamp is what makes this idempotent. Without it a retry, or two
                // workers picking up the same minute, reminds everybody twice.
                && candidate.Reminder_Sent_At == null)
            .Take(50)
            .ToListAsync();

        if (due.Count == 0)
        {
            return;
        }

        var sent = 0;

        foreach (var calendarEvent in due)
        {
            var attendees = await _unitOfWork.Repository<EventAttendee, Guid>()
                .GetAllQ()
                .Where(attendee =>
                    attendee.Event_Id == calendarEvent.Id
                    && attendee.Response == EventResponse.Attending)
                .Select(attendee => attendee.Employee_Id)
                .ToListAsync();

            // Stamped whether or not anybody was attending, so an event with no
            // takers is not re-examined every minute until it starts.
            calendarEvent.Reminder_Sent_At = now;

            if (attendees.Count == 0)
            {
                continue;
            }

            var title = LocalizedText.Pick(calendarEvent.Title_Ar, calendarEvent.Title_En)
                ?? string.Empty;

            await _dispatcher.DispatchAsync(new NotificationSendRequest
            {
                Type = NotificationType.EventReminder,
                RecipientEmployeeIds = attendees,

                // No actor: nobody performed this, a clock did. The notification's
                // creator relationship is nullable precisely for this case.
                ActorEmployeeId = null,
                EntityType = NotificationEntityType.Event,
                EntityId = calendarEvent.Id,
                Copy = NotificationCopy.EventStartingSoon(title, calendarEvent.Start_At),
                RedirectUrl = NotificationLinks.Event(calendarEvent.Id),
            });

            sent++;
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Sent reminders for {Count} event(s)", sent);
    }
}
