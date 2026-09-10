using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Posts.Commands.ReactToPost;

/// <summary>
/// Sets, changes or removes the caller's reaction to a post.
///
/// One command rather than three, because they are one operation from the user's point
/// of view - tapping a reaction they already hold removes it, tapping a different one
/// swaps it - and splitting them would put the "at most one per person" rule in three
/// places.
/// </summary>
public sealed record ReactToPostCommand(Guid PostId, ReactionType? Reaction) : ICommand<int>;

public sealed class ReactToPostCommandHandler : IRequestHandler<ReactToPostCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PostAuthorization _authorization;
    private readonly IDateTimeProvider _clock;

    public ReactToPostCommandHandler(
        IUnitOfWork unitOfWork,
        PostAuthorization authorization,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _clock = clock;
    }

    public async Task<int> Handle(ReactToPostCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _authorization.RequireEmployeeIdAsync(cancellationToken);
        var posts = _unitOfWork.Repository<Post, Guid>();

        var post = await posts.FindByIDAsync(request.PostId)
            ?? throw new NotFoundException(nameof(Post), request.PostId);

        var reactions = _unitOfWork.Repository<PostReaction, Guid>();

        var existing = await reactions.GetAllQ()
            .FirstOrDefaultAsync(
                reaction => reaction.Post_Id == request.PostId && reaction.Employee_Id == employeeId,
                cancellationToken);

        var delta = 0;
        var reacted = false;

        if (request.Reaction is { } reaction)
        {
            if (existing is null)
            {
                await reactions.AddAsync(new PostReaction
                {
                    Id = Guid.NewGuid(),
                    Post_Id = request.PostId,
                    Employee_Id = employeeId,
                    Reaction_Type = reaction,
                    Reacted_At = _clock.UtcNow,
                    Is_Deleted = false,
                });

                delta = 1;
                reacted = true;
            }
            else
            {
                // Changing Love to Celebrate updates the row. The total is unchanged;
                // only the per-type breakdown moves.
                //
                // Still worth telling the author about: from their side somebody
                // reacted, and which reaction it is is the part they see.
                reacted = existing.Reaction_Type != reaction;

                existing.Reaction_Type = reaction;
                existing.Reacted_At = _clock.UtcNow;
            }
        }
        else if (existing is not null)
        {
            // Hard delete. A withdrawn reaction is not history, and a tombstone would
            // break the unique index that enforces one per person.
            reactions.DeleteByEntity(existing);
            delta = -1;
        }

        if (delta != 0)
        {
            // Incremented in the database rather than read-modify-written in memory.
            //
            // Two people reacting at the same moment would both read the same count and
            // write the same value back, losing one. An UPDATE that adds to the current
            // value is atomic and cannot lose either.
            await posts.ExecuteUpdateAsync(
                candidate => candidate.Id == request.PostId,
                setters => setters.SetProperty(
                    candidate => candidate.Reactions_Count,
                    candidate => candidate.Reactions_Count + delta),
                cancellationToken);
        }

        if (reacted)
        {
            // Nothing is raised for a withdrawn reaction. There is no notification
            // worth sending for "a colleague took their like back".
            post.RecordReaction(employeeId, request.Reaction!.Value, _clock.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return post.Reactions_Count + delta;
    }
}
