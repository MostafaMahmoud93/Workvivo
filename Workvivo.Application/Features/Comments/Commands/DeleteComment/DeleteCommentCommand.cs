using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Comments.Commands.DeleteComment;

public sealed record DeleteCommentCommand(Guid CommentId) : ICommand;

public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PostAuthorization _authorization;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUser _currentUser;

    public DeleteCommentCommandHandler(
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

    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _authorization.RequireEmployeeIdAsync(cancellationToken);
        var comments = _unitOfWork.Repository<Comment, Guid>();

        var comment = await comments.FindByIDAsync(request.CommentId)
            ?? throw new NotFoundException(nameof(Comment), request.CommentId);

        if (comment.Author_Employee_Id != employeeId)
        {
            var canModerate = await _permissions.HasPermissionAsync(
                _currentUser.UserId!.Value, PermissionKeys.PostModerate, cancellationToken);

            // Not found rather than forbidden: telling somebody a comment id exists but
            // is out of reach is how an id space gets probed.
            if (!canModerate)
            {
                throw new NotFoundException(nameof(Comment), request.CommentId);
            }
        }

        comment.Is_Deleted = true;

        // Replies go with the parent. Leaving them would show answers to a question
        // nobody can see, which reads as a bug and can be worse than the removed
        // comment - moderation usually removes a thread, not a single line of it.
        var replyCount = await comments.ExecuteUpdateAsync(
            candidate => candidate.Parent_Comment_Id == request.CommentId && !candidate.Is_Deleted,
            setters => setters.SetProperty(candidate => candidate.Is_Deleted, true),
            cancellationToken);

        var removed = replyCount + 1;

        await _unitOfWork.Repository<Post, Guid>().ExecuteUpdateAsync(
            candidate => candidate.Id == comment.Post_Id,
            setters => setters.SetProperty(
                candidate => candidate.Comments_Count,
                // Clamped, so a drifted counter cannot go negative and render as "-1".
                candidate => candidate.Comments_Count - removed < 0 ? 0 : candidate.Comments_Count - removed),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
