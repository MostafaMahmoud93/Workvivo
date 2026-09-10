using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Application.Features.Notifications.Jobs;
using Workvivo.Application.Tests.Support;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.RealTime;
using Xunit;

namespace Workvivo.Application.Tests.Features.Notifications;

/// <summary>
/// The dispatcher decides whether a person actually hears about something. Every rule
/// it applies is one that would otherwise be re-implemented, slightly differently, in
/// each subscriber - and the divergence shows up as one feature that keeps emailing
/// people who switched email off.
/// </summary>
public class NotificationDispatcherTests
{
    private static readonly Guid ActorEmployee = Guid.NewGuid();
    private static readonly Guid ActorUser = Guid.NewGuid();
    private static readonly Guid RecipientEmployee = Guid.NewGuid();
    private static readonly Guid RecipientUser = Guid.NewGuid();

    private static Employee Person(Guid employeeId, Guid userId, bool active = true) => new()
    {
        Id = employeeId,
        User_Id = userId,
        Display_Name = "Test Person",
        Email = $"{employeeId:N}@example.com",
        Preferred_Language = "en",
        Is_Active = active,
    };

    private static NotificationSendRequest Request(params Guid[] recipients) => new()
    {
        Type = NotificationType.Mention,
        RecipientEmployeeIds = recipients,
        ActorEmployeeId = ActorEmployee,
        EntityType = NotificationEntityType.Post,
        EntityId = Guid.NewGuid(),
        Copy = NotificationCopy.MentionedYouInAPost("Layla", "hello"),
        RedirectUrl = "/feed/x",
    };

    private static (NotificationDispatcher Dispatcher, FakeUnitOfWork Work, IBackgroundJobScheduler Jobs, IRealtimeNotifier Realtime)
        Build(IEnumerable<Employee> employees, IEnumerable<NotificationPreference>? preferences = null)
    {
        var work = new FakeUnitOfWork()
            .With<Employee, Guid>(employees)
            .With<NotificationPreference, Guid>(preferences ?? [])
            .With<Notification, Guid>([])
            .With<NotificationUser, Guid>([]);

        var jobs = Substitute.For<IBackgroundJobScheduler>();
        var realtime = Substitute.For<IRealtimeNotifier>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc));

        var dispatcher = new NotificationDispatcher(
            work.Object, realtime, jobs, clock, NullLogger<NotificationDispatcher>.Instance);

        return (dispatcher, work, jobs, realtime);
    }

    [Fact]
    public async Task The_actor_is_never_notified_about_their_own_action()
    {
        // Mentioning yourself in your own post is common and the notification would be
        // absurd. Filtered here rather than in each subscriber.
        var (dispatcher, work, _, _) = Build([Person(ActorEmployee, ActorUser)]);

        var delivered = await dispatcher.DispatchAsync(Request(ActorEmployee));

        delivered.ShouldBe(0);
        work.AddedOf<Notification>().ShouldBeEmpty();
    }

    [Fact]
    public async Task A_recipient_is_told_once_however_many_times_they_are_named()
    {
        var (dispatcher, work, _, _) = Build([Person(RecipientEmployee, RecipientUser)]);

        var delivered = await dispatcher.DispatchAsync(
            Request(RecipientEmployee, RecipientEmployee, RecipientEmployee));

        delivered.ShouldBe(1);
        work.AddedOf<Notification>().ShouldHaveSingleItem()
            .NotificationUsers.ShouldHaveSingleItem()
            .Reciever_Id.ShouldBe(RecipientUser);
    }

    [Fact]
    public async Task Somebody_who_has_left_is_not_notified()
    {
        // An inactive employee accumulating notifications is both pointless and a way
        // for a departed account to keep receiving company content by email.
        var (dispatcher, work, _, _) = Build([Person(RecipientEmployee, RecipientUser, active: false)]);

        (await dispatcher.DispatchAsync(Request(RecipientEmployee))).ShouldBe(0);
        work.AddedOf<Notification>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Turning_a_type_off_in_the_app_stops_the_row_being_written()
    {
        var preference = new NotificationPreference
        {
            Id = Guid.NewGuid(),
            Employee_Id = RecipientEmployee,
            Notification_Type = NotificationType.Mention,
            In_App_Enabled = false,
            Email_Enabled = false,
        };

        var (dispatcher, work, _, _) = Build([Person(RecipientEmployee, RecipientUser)], [preference]);

        (await dispatcher.DispatchAsync(Request(RecipientEmployee))).ShouldBe(0);
        work.AddedOf<Notification>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Immediate_email_is_queued_rather_than_sent_inline()
    {
        // Mention defaults to immediate email, so no stored preference is needed - which
        // is the point: the defaults are what nearly everybody is on.
        var (dispatcher, _, jobs, _) = Build([Person(RecipientEmployee, RecipientUser)]);

        await dispatcher.DispatchAsync(Request(RecipientEmployee));

        jobs.Received(1).Enqueue(
            Arg.Any<System.Linq.Expressions.Expression<Func<INotificationEmailJob, Task>>>());
    }

    [Fact]
    public async Task A_digest_subscriber_gets_the_row_but_no_immediate_email()
    {
        // The in-app row is what the digest job later reads, so it has to be written
        // even though nothing is sent now.
        var preference = new NotificationPreference
        {
            Id = Guid.NewGuid(),
            Employee_Id = RecipientEmployee,
            Notification_Type = NotificationType.Mention,
            In_App_Enabled = true,
            Email_Enabled = true,
            Email_Frequency = NotificationDigestFrequency.Daily,
        };

        var (dispatcher, work, jobs, _) = Build([Person(RecipientEmployee, RecipientUser)], [preference]);

        (await dispatcher.DispatchAsync(Request(RecipientEmployee))).ShouldBe(1);

        work.AddedOf<Notification>().ShouldHaveSingleItem();
        jobs.DidNotReceive().Enqueue(
            Arg.Any<System.Linq.Expressions.Expression<Func<INotificationEmailJob, Task>>>());
    }

    [Fact]
    public async Task The_notification_records_who_did_it_and_what_it_is_about()
    {
        var request = Request(RecipientEmployee);

        var (dispatcher, work, _, _) = Build(
            [Person(RecipientEmployee, RecipientUser), Person(ActorEmployee, ActorUser)]);

        await dispatcher.DispatchAsync(request);

        var notification = work.AddedOf<Notification>().ShouldHaveSingleItem();

        notification.Actor_Employee_Id.ShouldBe(ActorEmployee);

        // The creator navigation, which is nullable precisely so a job with nobody
        // signed in can still write a notification.
        notification.Creator_User_Id.ShouldBe(ActorUser);
        notification.Entity_Type.ShouldBe(NotificationEntityType.Post);
        notification.Entity_Id.ShouldBe(request.EntityId);
        notification.Notification_Type.ShouldBe((int)NotificationType.Mention);
    }

    [Fact]
    public async Task A_failed_push_does_not_fail_the_dispatch()
    {
        // The row is already committed by then. A dead backplane must not turn into an
        // exception on the request that caused the notification.
        var (dispatcher, work, _, realtime) = Build([Person(RecipientEmployee, RecipientUser)]);

        realtime
            .SendToUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("hub is down"));

        (await dispatcher.DispatchAsync(Request(RecipientEmployee))).ShouldBe(1);
        work.AddedOf<Notification>().ShouldHaveSingleItem();
    }
}
