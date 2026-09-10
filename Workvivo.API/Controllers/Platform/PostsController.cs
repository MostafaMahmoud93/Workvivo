using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Comments.Commands.AddComment;
using Workvivo.Application.Features.Comments.Commands.DeleteComment;
using Workvivo.Application.Features.Comments.Dtos;
using Workvivo.Application.Features.Comments.Queries.GetComments;
using Workvivo.Application.Features.Posts.Commands.CreatePost;
using Workvivo.Application.Features.Posts.Commands.ReactToPost;
using Workvivo.Application.Features.Posts.Commands.UpdatePostState;
using Workvivo.Application.Features.Posts.Dtos;
using Workvivo.Application.Features.Posts.Queries.GetFeed;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.API.Controllers.Platform;

public class PostsController : ApiControllersBase
{
    private readonly ISender _sender;

    public PostsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// The news feed.
    ///
    /// Post.View is the bar. What each person actually sees is decided by the audience
    /// rules inside the query, not by the permission - the permission says whether they
    /// may read a feed at all.
    /// </summary>
    [HttpGet]
    [Route(RouteClass.Posts.Feed)]
    [HasPermission(Permissions.Post.View)]
    public async Task<IActionResult> Feed(
        [FromQuery] GetFeedQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<CursorPagedResult<FeedItemDto>>.Ok(await _sender.Send(query, cancellationToken)));

    [HttpPost]
    [Route(RouteClass.Posts.Create)]
    [HasPermission(Permissions.Post.Create)]
    public async Task<IActionResult> Create(
        CreatePostCommand command,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<Guid>.Ok(await _sender.Send(command, cancellationToken)));

    /// <summary>
    /// Sets, changes or clears the caller's reaction. A null body clears it.
    ///
    /// Post.View rather than Post.Create: reacting is reading behaviour, and requiring
    /// authoring rights to press a button would be wrong.
    /// </summary>
    [HttpPut]
    [Route(RouteClass.Posts.React)]
    [HasPermission(Permissions.Post.View)]
    public async Task<IActionResult> React(
        Guid postId,
        [FromBody] ReactionRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<int>.Ok(
            await _sender.Send(new ReactToPostCommand(postId, request.Reaction), cancellationToken)));

    /// <summary>
    /// Publish, archive, pin, feature or delete.
    ///
    /// The endpoint asks only for Post.View; the handler decides. Pinning needs
    /// moderation rights, editing needs authorship - both are properties of the post
    /// and the caller together, which an endpoint attribute cannot express.
    /// </summary>
    [HttpPost]
    [Route(RouteClass.Posts.State)]
    [HasPermission(Permissions.Post.View)]
    public async Task<IActionResult> ChangeState(
        Guid postId,
        [FromBody] PostStateRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdatePostStateCommand(postId, request.Action), cancellationToken);
        return Ok(ApiResponse.Ok("Post updated."));
    }

    [HttpGet]
    [Route(RouteClass.Comments.List)]
    [HasPermission(Permissions.Post.View)]
    public async Task<IActionResult> Comments(
        Guid postId,
        [FromQuery] GetCommentsQuery query,
        CancellationToken cancellationToken)
    {
        // Taken from the route, not the query string, so the two cannot disagree.
        query.PostId = postId;

        return Ok(ApiResponse<CursorPagedResult<CommentDto>>.Ok(
            await _sender.Send(query, cancellationToken)));
    }

    [HttpPost]
    [Route(RouteClass.Comments.Add)]
    [HasPermission(Permissions.Post.View)]
    public async Task<IActionResult> AddComment(
        Guid postId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<Guid>.Ok(await _sender.Send(
            new AddCommentCommand(
                postId,
                request.ParentCommentId,
                request.ContentHtml,
                request.MentionedEmployeeIds ?? []),
            cancellationToken)));

    [HttpDelete]
    [Route(RouteClass.Comments.Delete)]
    [HasPermission(Permissions.Post.View)]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCommentCommand(commentId), cancellationToken);
        return Ok(ApiResponse.Ok("Comment removed."));
    }
}

/// <summary>Null clears the caller's reaction.</summary>
public sealed record ReactionRequest(ReactionType? Reaction);

public sealed record PostStateRequest(PostStateAction Action);

/// <summary>
/// The post id comes from the route, so it is deliberately absent here - a body that
/// could carry a different one would be two sources of truth for the same thing.
/// </summary>
public sealed record AddCommentRequest(
    Guid? ParentCommentId,
    string ContentHtml,
    IReadOnlyList<Guid>? MentionedEmployeeIds);
