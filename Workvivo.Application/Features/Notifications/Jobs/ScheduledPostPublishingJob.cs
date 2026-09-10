using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Application.Common.Events;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;

namespace Workvivo.Application.Features.Notifications.Jobs;

/// <summary>
/// Publishes posts whose scheduled time has arrived.
///
/// The column and the status have existed since the feed was built; this is what
/// finally makes them mean something. Runs every minute, which is as precise as a
/// scheduled post needs to be and cheap because the index makes "nothing due" a seek
/// that returns no rows.
/// </summary>
public sealed class ScheduledPostPublishingJob : IScheduledPostPublishingJob
{
    /// <summary>
    /// How many are published per run. A bound rather than "all of them": if the job
    /// has been down for a day, publishing ten thousand posts in one transaction is a
    /// worse outcome than catching up over ten runs.
    /// </summary>
    private const int BatchSize = 200;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisherAdapter _publisher;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ScheduledPostPublishingJob> _logger;

    public ScheduledPostPublishingJob(
        IUnitOfWork unitOfWork,
        IPublisherAdapter publisher,
        IDateTimeProvider clock,
        ILogger<ScheduledPostPublishingJob> logger)
    {
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task PublishDueAsync()
    {
        var now = _clock.UtcNow;

        var due = await _unitOfWork.Repository<Post, Guid>()
            .GetAllQ()
            .Where(post =>
                post.Status == PostStatus.Scheduled
                && post.Scheduled_Publish_Date != null
                && post.Scheduled_Publish_Date <= now)
            .OrderBy(post => post.Scheduled_Publish_Date)
            .Take(BatchSize)

            // The mentions come with the post, because Publish raises the event that
            // notifies them and reaching for the navigation afterwards would lazy-load
            // one query per post.
            .Include(post => post.Mentions)
            .ToListAsync();

        if (due.Count == 0)
        {
            return;
        }

        foreach (var post in due)
        {
            // The same domain method the composer and the moderator's publish button
            // use, so a scheduled post is published on identical terms.
            post.Publish(now);
        }

        await _unitOfWork.SaveChangesAsync();

        // Dispatched after the save, for the same reason the pipeline does it after the
        // commit: a notification for a post that failed to publish cannot be recalled.
        await _publisher.PublishDomainEventsAsync(_unitOfWork.DrainDomainEvents());

        _logger.LogInformation("Published {Count} scheduled post(s)", due.Count);
    }
}
