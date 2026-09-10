using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Analytics.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Analytics.Queries.GetDashboard;

/// <summary>
/// The engagement dashboard.
///
/// Every figure here is aggregate. There is no per-employee activity report, and
/// that is a deliberate limit rather than an unfinished feature: a tool that tells a
/// manager who has not posted this week changes what an internal network is for, and
/// people stop using it honestly.
/// </summary>
public sealed class GetAnalyticsDashboardQuery : IQuery<AnalyticsDashboardDto>
{
    public int Days { get; set; } = 30;
}

public sealed class GetAnalyticsDashboardQueryHandler
    : IRequestHandler<GetAnalyticsDashboardQuery, AnalyticsDashboardDto>
{
    private const int MaxDays = 365;
    private const int TopPostCount = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IPermissionService _permissions;
    private readonly IDateTimeProvider _clock;

    public GetAnalyticsDashboardQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IPermissionService permissions,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _permissions = permissions;
        _clock = clock;
    }

    public async Task<AnalyticsDashboardDto> Handle(
        GetAnalyticsDashboardQuery request,
        CancellationToken cancellationToken)
    {
        await _currentEmployee.RequireIdAsync(cancellationToken);

        if (!await _permissions.HasPermissionAsync(
                _currentEmployee.UserId, PermissionKeys.AnalyticsView, cancellationToken))
        {
            throw new ForbiddenException("You cannot view analytics.", PermissionKeys.AnalyticsView);
        }

        var days = Math.Clamp(request.Days, 1, MaxDays);
        var now = _clock.UtcNow;
        var from = now.AddDays(-days);

        // The window immediately before this one, so each headline number carries a
        // direction. A figure with nothing to compare it to is a number, not a
        // measure.
        var previousFrom = from.AddDays(-days);

        var posts = _unitOfWork.Repository<Post, Guid>().GetAllQ();
        var comments = _unitOfWork.Repository<Comment, Guid>().GetAllQ();
        var reactions = _unitOfWork.Repository<PostReaction, Guid>().GetAllQ();
        var views = _unitOfWork.Repository<PostView, Guid>().GetAllQ();

        var metrics = new List<MetricDto>
        {
            new()
            {
                Key = "employees",
                Value = await _unitOfWork.Repository<Employee, Guid>()
                    .GetAllQ()
                    .CountAsync(employee => employee.Is_Active, cancellationToken),
            },
            new()
            {
                Key = "posts",
                Value = await posts.CountAsync(
                    post => post.Status == PostStatus.Published && post.Published_Date >= from,
                    cancellationToken),
                Previous = await posts.CountAsync(
                    post => post.Status == PostStatus.Published
                        && post.Published_Date >= previousFrom
                        && post.Published_Date < from,
                    cancellationToken),
            },
            new()
            {
                Key = "comments",
                Value = await comments.CountAsync(
                    comment => comment.Create_Date >= from, cancellationToken),
                Previous = await comments.CountAsync(
                    comment => comment.Create_Date >= previousFrom && comment.Create_Date < from,
                    cancellationToken),
            },
            new()
            {
                Key = "reactions",
                Value = await reactions.CountAsync(
                    reaction => reaction.Reacted_At >= from, cancellationToken),
                Previous = await reactions.CountAsync(
                    reaction => reaction.Reacted_At >= previousFrom && reaction.Reacted_At < from,
                    cancellationToken),
            },
            new()
            {
                // Distinct people who read something, which is the closest honest
                // measure of adoption this data supports. Not "logins" - a login
                // says somebody opened a tab.
                Key = "readers",
                Value = await views
                    .Where(view => view.Last_Viewed_At >= from)
                    .Select(view => view.Employee_Id)
                    .Distinct()
                    .CountAsync(cancellationToken),
                Previous = await views
                    .Where(view => view.Last_Viewed_At >= previousFrom && view.Last_Viewed_At < from)
                    .Select(view => view.Employee_Id)
                    .Distinct()
                    .CountAsync(cancellationToken),
            },
        };

        var topPosts = await posts
            .Where(post => post.Status == PostStatus.Published && post.Published_Date >= from)

            // Ranked by engagement rather than views: a post everybody scrolled past
            // is not the post of the month.
            .OrderByDescending(post => post.Reactions_Count + post.Comments_Count)
            .ThenByDescending(post => post.Views_Count)
            .Take(TopPostCount)
            .Select(post => new
            {
                post.Id,
                post.Title_Ar,
                post.Title_En,
                post.Content_Text,
                post.Views_Count,
                post.Reactions_Count,
                post.Comments_Count,
                post.Published_Date,
                Author = post.Author == null ? string.Empty : post.Author.Display_Name,
            })
            .ToListAsync(cancellationToken);

        var activity = await BuildActivityAsync(from, cancellationToken);

        return new AnalyticsDashboardDto
        {
            Days = days,
            GeneratedAt = now,
            Metrics = metrics,
            TopPosts =
            [
                .. topPosts.Select(post => new TopPostDto
                {
                    Id = post.Id,
                    Title = LocalizedText.Pick(post.Title_Ar, post.Title_En)
                        ?? Trim(post.Content_Text, 60),
                    AuthorDisplayName = post.Author,
                    Views = post.Views_Count,
                    Reactions = post.Reactions_Count,
                    Comments = post.Comments_Count,
                    PublishedAt = post.Published_Date,
                }),
            ],
            Activity = activity,
        };
    }

    /// <summary>
    /// Daily counts across the window.
    ///
    /// Three grouped queries rather than one per day. A thirty-day chart built by
    /// looping would be ninety round trips to draw one line.
    /// </summary>
    private async Task<IReadOnlyList<ActivityPointDto>> BuildActivityAsync(
        DateTime from,
        CancellationToken cancellationToken)
    {
        var postsByDay = await _unitOfWork.Repository<Post, Guid>()
            .GetAllQ()
            .Where(post => post.Status == PostStatus.Published && post.Published_Date >= from)
            .GroupBy(post => post.Published_Date!.Value.Date)
            .Select(group => new { Day = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var commentsByDay = await _unitOfWork.Repository<Comment, Guid>()
            .GetAllQ()
            .Where(comment => comment.Create_Date >= from)
            .GroupBy(comment => comment.Create_Date.Date)
            .Select(group => new { Day = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var reactionsByDay = await _unitOfWork.Repository<PostReaction, Guid>()
            .GetAllQ()
            .Where(reaction => reaction.Reacted_At >= from)
            .GroupBy(reaction => reaction.Reacted_At.Date)
            .Select(group => new { Day = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var days = postsByDay.Select(row => row.Day)
            .Union(commentsByDay.Select(row => row.Day))
            .Union(reactionsByDay.Select(row => row.Day))
            .OrderBy(day => day)
            .ToList();

        return
        [
            .. days.Select(day => new ActivityPointDto
            {
                Date = DateOnly.FromDateTime(day),
                Posts = postsByDay.Find(row => row.Day == day)?.Count ?? 0,
                Comments = commentsByDay.Find(row => row.Day == day)?.Count ?? 0,
                Reactions = reactionsByDay.Find(row => row.Day == day)?.Count ?? 0,
            }),
        ];
    }

    private static string Trim(string? text, int length)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var value = text.Trim();

        return value.Length <= length ? value : string.Concat(value.AsSpan(0, length).TrimEnd(), "\u2026");
    }
}
