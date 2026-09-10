using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Posts.Commands.UpdatePostState;

/// <summary>What a state change can do to a post.</summary>
public enum PostStateAction
{
    Publish,
    Archive,
    Pin,
    Unpin,
    Feature,
    Unfeature,
    Delete,
}

/// <summary>
/// Moves a post through its lifecycle.
///
/// One command for the lot because they share the same authorisation and the same
/// "load, check, change, save" shape; seven near-identical handlers would be seven
/// places to forget the resource check.
/// </summary>
public sealed record UpdatePostStateCommand(Guid PostId, PostStateAction Action) : ICommand;

public sealed class UpdatePostStateCommandHandler : IRequestHandler<UpdatePostStateCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PostAuthorization _authorization;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public UpdatePostStateCommandHandler(
        IUnitOfWork unitOfWork,
        PostAuthorization authorization,
        IPermissionService permissions,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _permissions = permissions;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(UpdatePostStateCommand request, CancellationToken cancellationToken)
    {
        // Resolves the post and proves the caller may act on it, in one place.
        var post = await _authorization.RequireEditableAsync(request.PostId, cancellationToken);

        switch (request.Action)
        {
            case PostStateAction.Publish:
                if (post.Status == PostStatus.Published)
                {
                    // Idempotent: re-publishing must not move Published_Date and push an
                    // old post back to the top of everyone's feed.
                    return;
                }

                post.Status = PostStatus.Published;
                post.Published_Date ??= _clock.UtcNow;
                post.Scheduled_Publish_Date = null;
                break;

            case PostStateAction.Archive:
                post.Status = PostStatus.Archived;
                post.Archived_Date = _clock.UtcNow;
                break;

            // Pinning and featuring are editorial decisions about the company timeline
            // rather than about your own post, so being the author is not enough.
            case PostStateAction.Pin:
            case PostStateAction.Unpin:
                await RequireModerationAsync(cancellationToken);
                post.Is_Pinned = request.Action == PostStateAction.Pin;
                break;

            case PostStateAction.Feature:
            case PostStateAction.Unfeature:
                await RequireModerationAsync(cancellationToken);
                post.Is_Featured = request.Action == PostStateAction.Feature;
                break;

            case PostStateAction.Delete:
                // Soft: a deleted post still has comments and reactions pointing at it,
                // and moderation needs to be reviewable after the fact.
                post.Is_Deleted = true;
                break;

            default:
                throw new BusinessRuleException("Unsupported action.", "post.unknown-action");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RequireModerationAsync(CancellationToken cancellationToken)
    {
        var canModerate = await _permissions.HasPermissionAsync(
            _currentUser.UserId!.Value, PermissionKeys.PostModerate, cancellationToken);

        if (!canModerate)
        {
            throw new ForbiddenException(
                "Pinning and featuring require moderation rights.", PermissionKeys.PostModerate);
        }
    }
}
