using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Posts.Commands.CreatePost;

/// <summary>One audience rule on a new post.</summary>
public sealed record AudienceTargetDto(AudienceType AudienceType, Guid? TargetId);

public sealed record CreatePostCommand(
    PostType PostType,
    string? TitleAr,
    string? TitleEn,
    string ContentHtml,
    Guid? CommunityId,
    bool IsOfficial,
    bool CommentsEnabled,
    DateTime? ScheduledPublishDate,
    bool PublishNow,
    IReadOnlyList<AudienceTargetDto> Audiences,
    IReadOnlyList<Guid> MentionedEmployeeIds) : ICommand<Guid>;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.ContentHtml).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.TitleAr).MaximumLength(200);
        RuleFor(x => x.TitleEn).MaximumLength(200);

        // A post with no audience would be published and reach nobody - a silent
        // failure that looks like a bug in the feed rather than a mistake at the
        // composer.
        RuleFor(x => x.Audiences)
            .NotEmpty().WithMessage("Choose who should see this post.")
            .When(x => x.CommunityId is null);

        RuleFor(x => x.ScheduledPublishDate)
            .Must(date => date is null || date > DateTime.UtcNow)
            .WithMessage("A scheduled time must be in the future.");

        RuleFor(x => x.MentionedEmployeeIds).Must(ids => ids.Count <= 50)
            .WithMessage("A post can mention at most 50 people.");
    }
}

public sealed class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PostAuthorization _authorization;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUser _currentUser;
    private readonly IContentSanitizer _sanitizer;
    private readonly IDateTimeProvider _clock;

    public CreatePostCommandHandler(
        IUnitOfWork unitOfWork,
        PostAuthorization authorization,
        IPermissionService permissions,
        ICurrentUser currentUser,
        IContentSanitizer sanitizer,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _permissions = permissions;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var authorId = await _authorization.RequireEmployeeIdAsync(cancellationToken);
        var userId = _currentUser.UserId!.Value;
        var now = _clock.UtcNow;

        await EnsureMayPostAsync(request, userId, authorId, cancellationToken);

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Author_Employee_Id = authorId,
            Community_Id = request.CommunityId,
            Post_Type = request.PostType,
            Title_Ar = request.TitleAr?.Trim(),
            Title_En = request.TitleEn?.Trim(),

            // Sanitised on the way in, not on the way out. Cleaning at render time
            // leaves live payloads in the database, and it takes only one consumer that
            // forgets - an export, a digest email, a mobile client - for them to fire.
            Content_Html = _sanitizer.Sanitize(request.ContentHtml),

            // Derived once so search, previews and plain-text email do not each have to
            // strip tags themselves.
            Content_Text = _sanitizer.ToPlainText(request.ContentHtml),

            Is_Official = request.IsOfficial,
            Comments_Enabled = request.CommentsEnabled,
            Visibility = request.CommunityId is null ? PostVisibility.Organization : PostVisibility.Community,
            Is_Deleted = false,
        };

        if (request.ScheduledPublishDate is { } scheduled)
        {
            post.Status = PostStatus.Scheduled;
            post.Scheduled_Publish_Date = scheduled;
        }
        else
        {
            post.Status = PostStatus.Draft;
        }

        await _unitOfWork.Repository<Post, Guid>().AddAsync(post);

        AddAudiences(post, request);

        // Mentions first, then publish. Publishing raises the event that notifies the
        // people mentioned, so it has to happen once the list is known and validated -
        // otherwise a post published in the same breath as it is written tells nobody.
        var mentioned = await AddMentionsAsync(post, request.MentionedEmployeeIds, now, cancellationToken);

        if (request.PublishNow && request.ScheduledPublishDate is null)
        {
            // The domain decides what publishing means; this handler only decides
            // whether it happens now. The scheduler and the moderator's publish button
            // go through the same method.
            post.Publish(now, mentioned);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return post.Id;
    }

    /// <summary>
    /// Checks the things a plain Post.Create does not cover.
    ///
    /// Marking a post official makes it speak for the company, and announcements carry
    /// more weight than an ordinary post - both need their own permission, or anyone
    /// who can post can impersonate the organisation.
    /// </summary>
    private async Task EnsureMayPostAsync(
        CreatePostCommand request,
        Guid userId,
        Guid authorId,
        CancellationToken cancellationToken)
    {
        if (request.IsOfficial
            && !await _permissions.HasPermissionAsync(userId, PermissionKeys.AnnouncementPublish, cancellationToken))
        {
            throw new ForbiddenException(
                "You cannot mark a post as official.", PermissionKeys.AnnouncementPublish);
        }

        if (request.PostType is PostType.Announcement or PostType.SystemAnnouncement
            && !await _permissions.HasPermissionAsync(userId, PermissionKeys.AnnouncementCreate, cancellationToken))
        {
            throw new ForbiddenException(
                "You cannot create announcements.", PermissionKeys.AnnouncementCreate);
        }

        if (request.CommunityId is { } communityId)
        {
            // Posting into a community requires being in it. Without this, knowing a
            // community id is enough to post into a private group.
            var isMember = await _unitOfWork.Repository<CommunityMember, Guid>()
                .GetAllQ()
                .AnyAsync(
                    member => member.Community_Id == communityId
                        && member.Employee_Id == authorId
                        && member.Membership_Status == MembershipStatus.Approved,
                    cancellationToken);

            if (!isMember)
            {
                throw new ForbiddenException("You are not a member of that community.");
            }
        }
    }

    private static void AddAudiences(Post post, CreatePostCommand request)
    {
        if (request.CommunityId is { } communityId)
        {
            // A community post is addressed to the community, whatever else was asked
            // for. Letting a caller widen it here would leak a private group's content
            // to the whole company.
            post.Audiences.Add(new PostAudience(post.Id, AudienceType.Community, communityId));
            return;
        }

        // Deduplicated by key: the same target twice would multiply the post out of the
        // feed's EXISTS and is rejected by the unique index anyway.
        foreach (var target in request.Audiences.DistinctBy(a => (a.AudienceType, a.TargetId)))
        {
            post.Audiences.Add(new PostAudience(post.Id, target.AudienceType, target.TargetId));
        }
    }

    /// <summary>
    /// Records who was mentioned.
    ///
    /// The client sends ids from the mention picker rather than the server parsing them
    /// back out of the HTML - re-parsing markup to find names is fragile and gets worse
    /// with every edit. The ids are still validated here: a caller can send anything.
    /// </summary>
    /// <returns>The ids that survived validation - the people who will actually be told.</returns>
    private async Task<IReadOnlyList<Guid>> AddMentionsAsync(
        Post post,
        IReadOnlyList<Guid> mentionedEmployeeIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (mentionedEmployeeIds.Count == 0)
        {
            return [];
        }

        var distinct = mentionedEmployeeIds.Distinct().ToArray();

        var valid = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => distinct.Contains(employee.Id) && employee.Is_Active)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);

        foreach (var employeeId in valid)
        {
            post.Mentions.Add(new PostMention
            {
                Id = Guid.NewGuid(),
                Post_Id = post.Id,
                Mentioned_Employee_Id = employeeId,
                Mentioned_At = now,
                Is_Deleted = false,
            });
        }

        return valid;
    }
}
