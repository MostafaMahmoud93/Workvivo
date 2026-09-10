using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Communities.Commands.RespondToInvitation;

/// <summary>Accepts or declines an invitation addressed to the caller.</summary>
public sealed record RespondToInvitationCommand(Guid InvitationId, bool Accept) : ICommand;

public sealed class RespondToInvitationCommandHandler : IRequestHandler<RespondToInvitationCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IDateTimeProvider _clock;

    public RespondToInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _clock = clock;
    }

    public async Task Handle(RespondToInvitationCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var now = _clock.UtcNow;

        var invitation = await _unitOfWork.Repository<CommunityInvitation, Guid>()
            .GetAllQ()

            // Scoped to the caller in the same predicate as the id. An invitation is a
            // key to a private group; accepting somebody else's by guessing its id
            // would be a way in.
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.InvitationId
                    && candidate.Invited_Employee_Id == employeeId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(CommunityInvitation), request.InvitationId);

        if (!invitation.IsActionableAt(now))
        {
            throw new BusinessRuleException(
                "This invitation is no longer valid.", "community.invitation-expired");
        }

        invitation.Status = request.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.Responded_At = now;

        if (!request.Accept)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var members = _unitOfWork.Repository<CommunityMember, Guid>();

        var membership = await members.GetAllQ()
            .FirstOrDefaultAsync(
                member => member.Community_Id == invitation.Community_Id
                    && member.Employee_Id == employeeId,
                cancellationToken);

        if (membership is { Membership_Status: MembershipStatus.Banned })
        {
            // A stale invitation must not undo a ban imposed after it was sent.
            throw new ForbiddenException("You cannot join this community.");
        }

        var wasActive = membership?.IsActive == true;

        if (membership is null)
        {
            await members.AddAsync(new CommunityMember
            {
                Id = Guid.NewGuid(),
                Community_Id = invitation.Community_Id,
                Employee_Id = employeeId,
                Member_Role = CommunityMemberRole.Member,
                Membership_Status = MembershipStatus.Approved,
                Requested_At = invitation.Create_Date,
                Joined_At = now,
                Is_Deleted = false,
            });
        }
        else
        {
            membership.Membership_Status = MembershipStatus.Approved;
            membership.Joined_At = now;
            membership.Is_Deleted = false;
        }

        if (!wasActive)
        {
            await _unitOfWork.Repository<Community, Guid>().ExecuteUpdateAsync(
                candidate => candidate.Id == invitation.Community_Id,
                setters => setters.SetProperty(
                    candidate => candidate.Members_Count,
                    candidate => candidate.Members_Count + 1),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audience.InvalidateAsync(employeeId, cancellationToken);
    }
}
