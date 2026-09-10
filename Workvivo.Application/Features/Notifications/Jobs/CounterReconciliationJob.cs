using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;

namespace Workvivo.Application.Features.Notifications.Jobs;

/// <summary>
/// Repairs engagement counters that have drifted from the rows they count.
///
/// The counters on a post are denormalised because a feed page showing twenty posts
/// would otherwise be forty aggregate queries against the two largest tables in the
/// system. They are maintained by atomic increments, which is correct for every path
/// that goes through the application - and wrong for every path that does not: a
/// support query run by hand, a bulk import, a moderation script, a migration.
///
/// This is not a hypothetical. Two comments deleted directly in SQL during development
/// left a post reading eight comments against six rows, which is exactly the shape of
/// drift users report as "the number is wrong" and nobody can reproduce.
///
/// The window exists because a full sweep of a table with millions of posts is not a
/// nightly job. Activity concentrates in recent posts, and anything older that drifts
/// needs a deliberate one-off sweep rather than a scan every night.
/// </summary>
public sealed class CounterReconciliationJob : ICounterReconciliationJob
{
    /// <summary>How far back a nightly run looks.</summary>
    private static readonly TimeSpan Window = TimeSpan.FromDays(30);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CounterReconciliationJob> _logger;

    public CounterReconciliationJob(
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<CounterReconciliationJob> logger)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task ReconcileAsync()
    {
        var since = _clock.UtcNow - Window;
        var posts = _unitOfWork.Repository<Post, Guid>();

        // Set-based, in the database. Loading the posts to fix them in memory would
        // read every row in the window to correct the handful that are wrong.
        //
        // The predicate is what keeps this cheap to write and honest to read: only rows
        // whose stored count disagrees with the real one are touched, so the number of
        // updated rows is the amount of drift there actually was.
        var comments = await posts.ExecuteUpdateAsync(
            post => post.Create_Date >= since
                && post.Comments_Count != post.Comments.Count(comment => !comment.Is_Deleted),
            setters => setters.SetProperty(
                post => post.Comments_Count,
                post => post.Comments.Count(comment => !comment.Is_Deleted)));

        var reactions = await posts.ExecuteUpdateAsync(
            post => post.Create_Date >= since
                && post.Reactions_Count != post.Reactions.Count(reaction => !reaction.Is_Deleted),
            setters => setters.SetProperty(
                post => post.Reactions_Count,
                post => post.Reactions.Count(reaction => !reaction.Is_Deleted)));

        var replies = await _unitOfWork.Repository<Comment, Guid>().ExecuteUpdateAsync(
            comment => comment.Create_Date >= since
                && comment.Replies_Count != comment.Replies.Count(reply => !reply.Is_Deleted),
            setters => setters.SetProperty(
                comment => comment.Replies_Count,
                comment => comment.Replies.Count(reply => !reply.Is_Deleted)));

        if (comments + reactions + replies == 0)
        {
            _logger.LogInformation("Counter reconciliation found no drift");
            return;
        }

        // Warning, not Information. Drift means something wrote to these tables without
        // going through the application, and that is worth somebody knowing about
        // rather than being silently corrected every night forever.
        _logger.LogWarning(
            "Counter reconciliation corrected {Comments} comment count(s), {Reactions} reaction count(s) and {Replies} reply count(s)",
            comments,
            reactions,
            replies);
    }
}
