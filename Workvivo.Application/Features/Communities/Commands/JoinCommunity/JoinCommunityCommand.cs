using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Events;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Communities.Commands.JoinCommunity;

/// <summary>
/// Joins a public community, or asks to join one that needs approval.
///
/// One command for both, because from the member's side it is one action and the
/// difference is a property of the community, not of the request. Returning the
/// resulting status is what lets the client say "joined" or "requested" correctly.
/// </summary>
public sealed record JoinCommunityCommand(Guid CommunityId) : ICommand<int>;

public sealed class JoinCommunityCommandHandler : IRequestHandler<JoinCommunityCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CommunityAuthorization _authorization;
    private readonly IAudienceResolver _audience;
    private readonly IDateTimeProvider _clock;

    public JoinCommunityCommandHandler(
        IUnitOfWork unitOfWork,
        CommunityAuthorization authorization,
        IAudienceResolver audience,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _clock = clock;
        _audience = audience;
    }

    public async Task<int> Handle(JoinCommunityCommand request, CancellationToken cancellationToken)
    {
        var access = await _authorization.ResolveAsync(request.CommunityId, cancellationToken);
        var community = access.Community;

        // A Private community cannot be joined by asking - membership is by invitation.
        // Answering "not found" rather than "you need an invitation" keeps the
        // community's existence unconfirmed to somebody guessing ids.
        if (community.Privacy == CommunityPrivacy.Private && !access.IsMember)
        {
            throw new NotFoundException(nameof(Community), request.CommunityId);
        }

        if (!community.Is_Active || community.Is_Deleted)
        {
            throw new BusinessRuleException("This community is closed.", "community.inactive");
        }

        if (access.Membership?.Membership_Status == MembershipStatus.Banned)
        {
            // Deliberately explicit. A banned person silently failing to join would
            // simply try again, and support would have no idea why it did not work.
            throw new ForbiddenException("You cannot join this community.");
        }

        if (access.Membership?.Membership_Status is MembershipStatus.Approved or MembershipStatus.Pending)
        {
            // Idempotent: pressing Join twice is not an error.
            return (int)access.Membership.Membership_Status;
        }

        var now = _clock.UtcNow;
        var approved = !community.RequiresApprovalToJoin;
        var status = approved ? MembershipStatus.Approved : MembershipStatus.Pending;

        if (access.Membership is { } existing)
        {
            // Somebody who left, or was rejected, rejoining. The row is reused so the
            // unique index holds and the history of the original request is not lost
            // to a second row.
            existing.Membership_Status = status;
            existing.Requested_At = now;
            existing.Joined_At = approved ? now : null;
            existing.Member_Role = CommunityMemberRole.Member;
            existing.Is_Deleted = false;
        }
        else
        {
            await _unitOfWork.Repository<CommunityMember, Guid>().AddAsync(new CommunityMember
            {
                Id = Guid.NewGuid(),
                Community_Id = community.Id,
                Employee_Id = access.EmployeeId,
                Member_Role = CommunityMemberRole.Member,
                Membership_Status = status,
                Requested_At = now,
                Joined_At = approved ? now : null,
                Is_Deleted = false,
            });
        }

        if (approved)
        {
            // In the database, not in memory: several people join a popular community
            // at once and a read-modify-write would lose all but one of them.
            await _unitOfWork.Repository<Community, Guid>().ExecuteUpdateAsync(
                candidate => candidate.Id == community.Id,
                setters => setters.SetProperty(
                    candidate => candidate.Members_Count,
                    candidate => candidate.Members_Count + 1),
                cancellationToken);
        }
        else
        {
            community.RecordJoinRequested(access.EmployeeId, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (approved)
        {
            // The community is now part of this person's audience key set, and that set
            // is cached for ten minutes. Without invalidating it, somebody who has just
            // joined sees an empty community feed and concludes the join failed.
            await _audience.InvalidateAsync(access.EmployeeId, cancellationToken);
        }

        return (int)status;
    }
}
