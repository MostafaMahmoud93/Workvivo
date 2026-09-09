using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Employees.Commands.FollowEmployee;

/// <summary>Follows or unfollows a colleague.</summary>
public sealed record FollowEmployeeCommand(Guid EmployeeId, bool Follow) : ICommand<bool>;

public sealed class FollowEmployeeCommandHandler : IRequestHandler<FollowEmployeeCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public FollowEmployeeCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<bool> Handle(FollowEmployeeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();
        var employees = _unitOfWork.Repository<Employee, Guid>();

        var follower = await employees.FirstOrDefaultAsync(candidate => candidate.User_Id == userId)
            ?? throw new NotFoundException("You do not have an employee profile.");

        if (follower.Id == request.EmployeeId)
        {
            // Also refused by a check constraint. Caught here so the caller gets a clear
            // message instead of a database error surfacing as a 500.
            throw new BusinessRuleException("You cannot follow yourself.", "employee.follow-self");
        }

        var followee = await employees.FindByIDAsync(request.EmployeeId)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var follows = _unitOfWork.Repository<EmployeeFollower, Guid>();

        var existing = await follows.GetAllQ()
            .FirstOrDefaultAsync(
                follow => follow.Follower_Id == follower.Id && follow.Followee_Id == followee.Id,
                cancellationToken);

        if (request.Follow)
        {
            // Idempotent: a double-tap or a retried request must not add a second row
            // and inflate the counter. The unique index would reject it anyway, but as
            // a 500 rather than a shrug.
            if (existing is not null)
            {
                return true;
            }

            await follows.AddAsync(new EmployeeFollower
            {
                Id = Guid.NewGuid(),
                Follower_Id = follower.Id,
                Followee_Id = followee.Id,
                Followed_At = _clock.UtcNow,
                Is_Deleted = false,
            });

            follower.Following_Count++;
            followee.Followers_Count++;
        }
        else
        {
            if (existing is null)
            {
                return false;
            }

            // Hard delete. An unfollow carries no history worth keeping, and tombstones
            // would make the counters a filtered count over a table that only grows.
            follows.DeleteByEntity(existing);

            // Clamped: a counter that has drifted below zero should stay at zero rather
            // than go negative and render as "-1 followers".
            follower.Following_Count = Math.Max(0, follower.Following_Count - 1);
            followee.Followers_Count = Math.Max(0, followee.Followers_Count - 1);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.Follow;
    }
}
