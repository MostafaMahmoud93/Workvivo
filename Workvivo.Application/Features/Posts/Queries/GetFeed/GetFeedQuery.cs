using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Posts.Dtos;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Posts.Queries.GetFeed;

/// <summary>
/// The news feed.
///
/// Keyset-paged, not offset-paged: <c>OFFSET 40000</c> makes SQL Server read and discard
/// forty thousand rows, so a deep scroll gets linearly slower. A cursor on
/// <c>(Published_Date, Id)</c> costs the same at any depth.
/// </summary>
public sealed class GetFeedQuery : CursorRequest, IQuery<CursorPagedResult<FeedItemDto>>
{
    /// <summary>Restricts the feed to one community. Null for the company timeline.</summary>
    public Guid? CommunityId { get; set; }

    /// <summary>Restricts to one post type - the announcements tab, for instance.</summary>
    public PostType? PostType { get; set; }
}

public sealed class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, CursorPagedResult<FeedItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAudienceResolver _audience;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissions;
    private readonly IDateTimeProvider _clock;

    public GetFeedQueryHandler(
        IUnitOfWork unitOfWork,
        IAudienceResolver audience,
        ICurrentUser currentUser,
        IPermissionService permissions,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _audience = audience;
        _currentUser = currentUser;
        _permissions = permissions;
        _clock = clock;
    }

    public async Task<CursorPagedResult<FeedItemDto>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();

        var viewer = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.User_Id == userId)
            .Select(employee => new { employee.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (viewer is null)
        {
            // No employee record means no audience, and therefore no feed. Returning
            // empty beats throwing: a service account hitting the endpoint is odd, not
            // an error worth a stack trace.
            return CursorPagedResult<FeedItemDto>.Empty();
        }

        var keys = await _audience.ResolveKeysAsync(viewer.Id, cancellationToken);
        var now = _clock.UtcNow;

        var posts = _unitOfWork.Repository<Post, Guid>().GetAllQ();
        var audiences = _unitOfWork.Repository<PostAudience, Guid>().GetAllQ();

        var query = posts.Where(post =>
            post.Status == PostStatus.Published
            && post.Published_Date != null
            && post.Published_Date <= now);

        query = request.CommunityId is { } communityId
            ? query.Where(post => post.Community_Id == communityId)
            // The company timeline excludes community posts: those belong in their own
            // community's feed, or the timeline is dominated by whichever group posts most.
            : query.Where(post => post.Community_Id == null);

        if (request.PostType is { } postType)
        {
            query = query.Where(post => post.Post_Type == postType);
        }

        // The audience filter.
        //
        // One EXISTS against a covering index on Audience_Key, rather than a UNION of
        // one predicate per targeting dimension. EXISTS rather than a join because a
        // post matching several of the viewer's keys must still appear once.
        query = query.Where(post =>
            audiences.Any(audience => audience.Post_Id == post.Id && keys.Contains(audience.Audience_Key)));

        // The keyset cursor. A malformed cursor decodes as "no cursor" and yields page
        // one rather than an error - see Cursor.TryDecode.
        if (Cursor.TryDecode(request.Cursor, out var cursorDate, out var cursorId))
        {
            // Compared as a tuple, not as two independent predicates: posts published in
            // the same tick are separated by the id, so none is skipped or repeated at a
            // page boundary.
            query = query.Where(post =>
                post.Published_Date < cursorDate
                || (post.Published_Date == cursorDate && post.Id.CompareTo(cursorId) < 0));
        }

        var page = await query
            .OrderByDescending(post => post.Published_Date)
            .ThenByDescending(post => post.Id)
            .Select(post => new FeedItemDto
            {
                Id = post.Id,
                PostType = post.Post_Type,
                Title = post.Title_En ?? post.Title_Ar,
                ContentHtml = post.Content_Html,
                IsPinned = post.Is_Pinned,
                IsFeatured = post.Is_Featured,
                IsOfficial = post.Is_Official,
                CommentsEnabled = post.Comments_Enabled,
                PublishedDate = post.Published_Date!.Value,

                // Joined in the same statement rather than fetched per row.
                Author = new AuthorDto
                {
                    Id = post.Author!.Id,
                    DisplayName = post.Author.Display_Name,
                    JobTitle = post.Author.JobTitle == null ? null : post.Author.JobTitle.Name_En ?? post.Author.JobTitle.Name_Ar,
                    ProfilePictureFileId = post.Author.Profile_Picture_File_Id,
                },

                CommunityId = post.Community_Id,
                CommunityName = post.Community == null ? null : post.Community.Name_En ?? post.Community.Name_Ar,

                // Denormalised, so a page of twenty posts is not forty aggregate queries
                // against the two largest tables in the system.
                CommentsCount = post.Comments_Count,
                ReactionsCount = post.Reactions_Count,
            })
            .ToCursorPagedResultAsync(
                request.PageSize,
                item => (item.PublishedDate, item.Id),
                cancellationToken);

        if (page.Items.Count == 0)
        {
            return page;
        }

        return await EnrichAsync(page, viewer.Id, cancellationToken);
    }

    /// <summary>
    /// Fills in the parts that cannot come from the post row.
    ///
    /// Three batched queries for the whole page, each keyed on the ids already fetched -
    /// not three per post. This is the difference between a feed page costing four
    /// queries and costing sixty.
    /// </summary>
    private async Task<CursorPagedResult<FeedItemDto>> EnrichAsync(
        CursorPagedResult<FeedItemDto> page,
        Guid viewerEmployeeId,
        CancellationToken cancellationToken)
    {
        var postIds = page.Items.Select(item => item.Id).ToArray();

        var tallies = await _unitOfWork.Repository<PostReaction, Guid>()
            .GetAllQ()
            .Where(reaction => postIds.Contains(reaction.Post_Id))
            .GroupBy(reaction => new { reaction.Post_Id, reaction.Reaction_Type })
            .Select(group => new ReactionTally(group.Key.Post_Id, group.Key.Reaction_Type, group.Count()))
            .ToListAsync(cancellationToken);

        var myReactions = await _unitOfWork.Repository<PostReaction, Guid>()
            .GetAllQ()
            .Where(reaction => postIds.Contains(reaction.Post_Id) && reaction.Employee_Id == viewerEmployeeId)
            .Select(reaction => new { reaction.Post_Id, reaction.Reaction_Type })
            .ToDictionaryAsync(reaction => reaction.Post_Id, reaction => reaction.Reaction_Type, cancellationToken);

        var attachments = await _unitOfWork.Repository<PostAttachment, Guid>()
            .GetAllQ()
            .Where(attachment => postIds.Contains(attachment.Post_Id))
            .OrderBy(attachment => attachment.Sort_Order)
            .Select(attachment => new
            {
                attachment.Post_Id,
                Dto = new AttachmentDto
                {
                    Id = attachment.Id,
                    Type = attachment.Attachment_Type,
                    FileId = attachment.File_Id,
                    LinkUrl = attachment.Link_Url,
                    LinkTitle = attachment.Link_Title,
                    Caption = attachment.Caption,
                },
            })
            .ToListAsync(cancellationToken);

        var mentions = await _unitOfWork.Repository<PostMention, Guid>()
            .GetAllQ()
            .Where(mention => postIds.Contains(mention.Post_Id) && mention.MentionedEmployee != null)
            .Select(mention => new
            {
                mention.Post_Id,
                Dto = new MentionDto
                {
                    EmployeeId = mention.Mentioned_Employee_Id,
                    DisplayName = mention.MentionedEmployee!.Display_Name,
                },
            })
            .ToListAsync(cancellationToken);

        var canModerate = await _permissions.HasPermissionAsync(
            _currentUser.UserId!.Value, PermissionKeys.PostModerate, cancellationToken);

        var tallyLookup = tallies.ToLookup(tally => tally.PostId);
        var attachmentLookup = attachments.ToLookup(item => item.Post_Id, item => item.Dto);
        var mentionLookup = mentions.ToLookup(item => item.Post_Id, item => item.Dto);

        var enriched = page.Items.Select(item =>
        {
            // An author may edit their own post; a moderator may act on any. Decided
            // here rather than in the client so the button and the endpoint agree.
            var isAuthor = item.Author.Id == viewerEmployeeId;

            return new FeedItemDto
            {
                Id = item.Id,
                PostType = item.PostType,
                Title = item.Title,
                ContentHtml = item.ContentHtml,
                IsPinned = item.IsPinned,
                IsFeatured = item.IsFeatured,
                IsOfficial = item.IsOfficial,
                CommentsEnabled = item.CommentsEnabled,
                PublishedDate = item.PublishedDate,
                Author = item.Author,
                CommunityId = item.CommunityId,
                CommunityName = item.CommunityName,
                CommentsCount = item.CommentsCount,
                ReactionsCount = item.ReactionsCount,
                Reactions = tallyLookup[item.Id].ToDictionary(tally => tally.Type, tally => tally.Count),
                MyReaction = myReactions.TryGetValue(item.Id, out var mine) ? mine : null,
                Attachments = [.. attachmentLookup[item.Id]],
                Mentions = [.. mentionLookup[item.Id]],
                CanEdit = isAuthor || canModerate,
                CanDelete = isAuthor || canModerate,
            };
        }).ToList();

        return new CursorPagedResult<FeedItemDto>(enriched, page.NextCursor, page.HasMore);
    }
}
