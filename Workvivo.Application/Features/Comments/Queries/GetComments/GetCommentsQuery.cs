using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Comments.Dtos;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Application.Features.Posts.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Comments.Queries.GetComments;

/// <summary>
/// A post's comment thread.
///
/// Top-level comments are keyset-paged; each one's replies come back with it. That
/// works because nesting is capped at two levels, so a top-level comment's whole
/// subtree is bounded - on an unlimited tree this would be a request for the entire
/// thread.
/// </summary>
public sealed class GetCommentsQuery : CursorRequest, IQuery<CursorPagedResult<CommentDto>>
{
    public Guid PostId { get; set; }
}

public sealed class GetCommentsQueryHandler
    : IRequestHandler<GetCommentsQuery, CursorPagedResult<CommentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PostAuthorization _authorization;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUser _currentUser;

    public GetCommentsQueryHandler(
        IUnitOfWork unitOfWork,
        PostAuthorization authorization,
        IPermissionService permissions,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<CursorPagedResult<CommentDto>> Handle(
        GetCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var viewerId = await _authorization.RequireEmployeeIdAsync(cancellationToken);

        var canModerate = await _permissions.HasPermissionAsync(
            _currentUser.UserId!.Value, PermissionKeys.PostModerate, cancellationToken);

        var comments = _unitOfWork.Repository<Comment, Guid>().GetAllQ();

        var topLevel = comments.Where(comment =>
            comment.Post_Id == request.PostId && comment.Parent_Comment_Id == null);

        // Oldest first: a conversation reads in the order it happened, so the cursor
        // moves forward through time rather than backward as the feed's does.
        if (Cursor.TryDecode(request.Cursor, out var cursorDate, out var cursorId))
        {
            topLevel = topLevel.Where(comment =>
                comment.Create_Date > cursorDate
                || (comment.Create_Date == cursorDate && comment.Id.CompareTo(cursorId) > 0));
        }

        var page = await topLevel
            .OrderBy(comment => comment.Create_Date)
            .ThenBy(comment => comment.Id)
            .Select(Projection)
            .ToCursorPagedResultAsync(
                request.PageSize,
                comment => (comment.CreatedDate, comment.Id),
                cancellationToken);

        if (page.Items.Count == 0)
        {
            return page;
        }

        // Replies for the whole page, batched by level.
        //
        // Two queries rather than one because the tree is two levels deep below the
        // top: fetching only the children of the page's comments returns depth 1 and
        // silently drops every depth-2 reply - a reply to a reply simply never appears,
        // which reads as lost data rather than as a paging boundary.
        //
        // Two is the whole depth, so this does not generalise into a walk: the cap on
        // nesting is what keeps a thread's subtree bounded and this query finite.
        var topLevelIds = page.Items.Select(item => item.Id).ToArray();

        var firstLevel = await comments
            .Where(comment => comment.Parent_Comment_Id != null
                && topLevelIds.Contains(comment.Parent_Comment_Id!.Value))
            .OrderBy(comment => comment.Create_Date)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        var firstLevelIds = firstLevel.Select(reply => reply.Id).ToArray();

        var secondLevel = firstLevelIds.Length == 0
            ? []
            : await comments
                .Where(comment => comment.Parent_Comment_Id != null
                    && firstLevelIds.Contains(comment.Parent_Comment_Id!.Value))
                .OrderBy(comment => comment.Create_Date)
                .Select(Projection)
                .ToListAsync(cancellationToken);

        var byParent = firstLevel.Concat(secondLevel).ToLookup(reply => reply.ParentCommentId!.Value);

        // Flattened depth-first, so a reply always follows the comment it answers and
        // the client can render the thread by indenting on Depth alone.
        var items = new List<CommentDto>(page.Items.Count + firstLevel.Count + secondLevel.Count);

        foreach (var top in page.Items)
        {
            items.Add(Decorate(top, viewerId, canModerate));

            foreach (var reply in byParent[top.Id])
            {
                items.Add(Decorate(reply, viewerId, canModerate));

                foreach (var nested in byParent[reply.Id])
                {
                    items.Add(Decorate(nested, viewerId, canModerate));
                }
            }
        }

        return new CursorPagedResult<CommentDto>(items, page.NextCursor, page.HasMore);
    }

    /// <summary>
    /// Shared projection, held as an expression tree rather than a method.
    ///
    /// This distinction is not cosmetic. Written as <c>Select(c =&gt; Project(c))</c>, EF
    /// cannot translate the call, so it materialises Comment entities and runs the
    /// projection in memory - and with lazy-loading proxies enabled, reading
    /// <c>comment.Author</c> then fires a query per row while the reader for the outer
    /// query is still open. That fails outright with "There is already an open
    /// DataReader associated with this Connection".
    ///
    /// As an Expression, EF composes it into the SQL and the author is a join.
    /// </summary>
    private static readonly Expression<Func<Comment, CommentDto>> Projection = comment => new CommentDto
    {
        Id = comment.Id,
        PostId = comment.Post_Id,
        ParentCommentId = comment.Parent_Comment_Id,
        Depth = comment.Depth,
        ContentHtml = comment.Content_Html,
        CreatedDate = comment.Create_Date,
        IsEdited = comment.Is_Edited,
        RepliesCount = comment.Replies_Count,
        ReactionsCount = comment.Reactions_Count,
        Author = new AuthorDto
        {
            Id = comment.Author!.Id,
            DisplayName = comment.Author.Display_Name,
            JobTitle = comment.Author.JobTitle == null ? null : comment.Author.JobTitle.Name_En,
            ProfilePictureFileId = comment.Author.Profile_Picture_File_Id,
        },
    };

    /// <summary>
    /// Adds the per-viewer flags after materialisation - they depend on who is asking,
    /// so they cannot be part of a translated projection.
    /// </summary>
    private static CommentDto Decorate(CommentDto comment, Guid viewerId, bool canModerate)
    {
        var isAuthor = comment.Author.Id == viewerId;

        return new CommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            Depth = comment.Depth,
            ContentHtml = comment.ContentHtml,
            CreatedDate = comment.CreatedDate,
            IsEdited = comment.IsEdited,
            RepliesCount = comment.RepliesCount,
            ReactionsCount = comment.ReactionsCount,
            Author = comment.Author,
            Mentions = comment.Mentions,
            CanReply = comment.Depth < Comment.MaxDepth,
            CanEdit = isAuthor,
            CanDelete = isAuthor || canModerate,
        };
    }
}
