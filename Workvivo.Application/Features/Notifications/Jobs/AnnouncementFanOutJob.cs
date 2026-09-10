using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.RealTime;

namespace Workvivo.Application.Features.Notifications.Jobs;

/// <summary>
/// Tells everyone an official announcement reaches.
///
/// Not done inline, and not through <see cref="INotificationDispatcher"/>. The
/// dispatcher is built for a handful of named recipients; this addresses whoever the
/// audience rules select, which for a company-wide announcement is the entire
/// workforce. It therefore does three things differently:
///
/// one notification row and many recipient rows, so the text is stored once; recipients
/// resolved a page at a time, so a hundred thousand ids are never in memory; and
/// recipient rows inserted in batches, so one save does not build a hundred-thousand-row
/// transaction that blocks the table it is writing to.
/// </summary>
public sealed class AnnouncementFanOutJob : IAnnouncementFanOutJob
{
    /// <summary>Recipients resolved and written per round.</summary>
    private const int BatchSize = 500;

    /// <summary>
    /// Safety stop. Without it a bug in the paging - a cursor that fails to advance -
    /// is an infinite loop that fills a table rather than a job that fails.
    /// </summary>
    private const int MaxBatches = 1_000;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAudienceRecipientQuery _recipients;
    private readonly IRealtimeNotifier _realtime;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AnnouncementFanOutJob> _logger;

    public AnnouncementFanOutJob(
        IUnitOfWork unitOfWork,
        IAudienceRecipientQuery recipients,
        IRealtimeNotifier realtime,
        IDateTimeProvider clock,
        ILogger<AnnouncementFanOutJob> logger)
    {
        _unitOfWork = unitOfWork;
        _recipients = recipients;
        _realtime = realtime;
        _clock = clock;
        _logger = logger;
    }

    public async Task FanOutAsync(Guid postId)
    {
        var post = await _unitOfWork.Repository<Post, Guid>()
            .GetAllQ()
            .Where(candidate => candidate.Id == postId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Author_Employee_Id,
                candidate.Title_Ar,
                candidate.Title_En,
                candidate.Content_Text,
                candidate.Status,
                AuthorUserId = candidate.Author == null ? (Guid?)null : candidate.Author.User_Id,
            })
            .FirstOrDefaultAsync();

        if (post is null || post.Status != PostStatus.Published)
        {
            // Unpublished or deleted between the event and the job running. Sending
            // anyway would announce something nobody can open.
            _logger.LogDebug("Post {PostId} is no longer publishable; announcement not sent", postId);
            return;
        }

        var copy = NotificationCopy.Announcement(post.Title_Ar, post.Title_En, post.Content_Text);
        var now = _clock.UtcNow;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Header_Ar = copy.HeaderAr,
            Header_En = copy.HeaderEn,
            Content_Ar = copy.ContentAr,
            Content_En = copy.ContentEn,
            RedirectUrl = NotificationLinks.Post(postId),
            Notification_Type = (int)NotificationType.Announcement,
            Notification_Status = "OPEN",
            Actor_Employee_Id = post.Author_Employee_Id,
            Creator_User_Id = post.AuthorUserId,
            Entity_Type = NotificationEntityType.Post,
            Entity_Id = postId,
            Is_Deleted = false,
        };

        await _unitOfWork.Repository<Notification, Guid>().AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        // Who has switched announcement notifications off. Loaded once: it is a small
        // set - almost nobody opts out of announcements - and querying preferences per
        // batch would be a round trip for every five hundred people.
        var optedOut = await _unitOfWork.Repository<NotificationPreference, Guid>()
            .GetAllQ()
            .Where(preference =>
                preference.Notification_Type == NotificationType.Announcement
                && !preference.In_App_Enabled)
            .Select(preference => preference.Employee_Id)
            .ToHashSetAsync();

        Guid? cursor = null;
        var delivered = 0;

        for (var round = 0; round < MaxBatches; round++)
        {
            var batch = await _recipients.GetPostRecipientsAsync(postId, cursor, BatchSize);

            if (batch.Count == 0)
            {
                break;
            }

            cursor = batch[^1];

            var eligible = batch
                .Where(employeeId => employeeId != post.Author_Employee_Id && !optedOut.Contains(employeeId))
                .ToArray();

            if (eligible.Length == 0)
            {
                continue;
            }

            var userIds = await _unitOfWork.Repository<Employee, Guid>()
                .GetAllQ()
                .Where(employee => eligible.Contains(employee.Id))
                .Select(employee => employee.User_Id)
                .ToListAsync();

            var deliveries = _unitOfWork.Repository<NotificationUser, Guid>();

            foreach (var userId in userIds)
            {
                await deliveries.AddAsync(new NotificationUser
                {
                    Id = Guid.NewGuid(),
                    Notification_Id = notification.Id,
                    Reciever_Id = userId,
                    IS_Seen = false,
                    Create_Date = now,
                    Is_Deleted = false,
                });
            }

            await _unitOfWork.SaveChangesAsync();

            // The tracker is cleared between batches. Without it every row written so
            // far stays tracked, and the save gets slower with each round until the
            // last batch of a large announcement costs more than the first thousand.
            _unitOfWork.ClearTracker();

            await _realtime.SendToUsersAsync(userIds, RealtimeEvents.UnreadCountChanged, new { });

            delivered += userIds.Count;
        }

        _logger.LogInformation(
            "Announcement {PostId} delivered to {Count} recipient(s)",
            postId,
            delivered);
    }
}
