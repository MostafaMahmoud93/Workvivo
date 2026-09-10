using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Communities.Commands.ReviewMembership;

/// <summary>What a moderator can decide about one person's membership.</summary>
public enum MembershipDecision
{
    Approve,
    Reject,
    Ban,
    Remove,
}

/// <summary>
/// A moderator's decision on somebody's membership.
///
/// Approve, reject, ban and remove are one command because they share the same
/// authorisation, the same row and the same counter maintenance. Four handlers would
/// be four places to forget that removing an approved member decrements the count and
/// rejecting a pending one does not.
/// </summary>
public sealed record ReviewMembershipCommand(
    Guid CommunityId,
    Guid EmployeeId,
    MembershipDecision Decision) : ICommand;

public sealed class ReviewMembershipCommandValidator : AbstractValidator<ReviewMembershipCommand>
{
    public ReviewMembershipCommandValidator()
    {
        RuleFor(x => x.CommunityId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Decision).Must(Enum.IsDefined).WithMessage("Unknown decision.");
    }
}

public sealed class ReviewMembershipCommandHandler : IRequestHandler<ReviewMembershipCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CommunityAuthorization _authorization;
    private readonly IAudienceResolver _audience;
    private readonly IDateTimeProvider _clock;

    public ReviewMembershipCommandHandler(
        IUnitOfWork unitOfWork,
        CommunityAuthorization authorization,
        IAudienceResolver audience,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _audience = audience;
        _clock = clock;
    }

    public async Task Handle(ReviewMembershipCommand request, CancellationToken cancellationToken)
    {
        var access = await _authorization.RequireModeratableAsync(request.CommunityId, cancellationToken);

        var membership = await _unitOfWork.Repository<CommunityMember, Guid>()
            .GetAllQ()
            .FirstOrDefaultAsync(
                member => member.Community_Id == request.CommunityId
                    && member.Employee_Id == request.EmployeeId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(CommunityMember), request.EmployeeId);

        if (membership.Member_Role == CommunityMemberRole.Owner)
        {
            // Without this, a moderator can ban the owner and leave the community with
            // nobody able to change its settings.
            throw new BusinessRuleException(
                "The owner's membership cannot be changed here.", "community.owner-protected");
        }

        var wasActive = membership.IsActive;
        var now = _clock.UtcNow;

        membership.Membership_Status = request.Decision switch
        {
            MembershipDecision.Approve => MembershipStatus.Approved,
            MembershipDecision.Reject => MembershipStatus.Rejected,
            MembershipDecision.Ban => MembershipStatus.Banned,
            MembershipDecision.Remove => MembershipStatus.Left,
            _ => throw new BusinessRuleException("Unknown decision.", "community.unknown-decision"),
        };

        membership.Reviewed_By_Employee_Id = access.EmployeeId;
        membership.Reviewed_At = now;

        var isActive = membership.Membership_Status == MembershipStatus.Approved;

        membership.Joined_At = isActive ? membership.Joined_At ?? now : null;

        if (isActive)
        {
            membership.Member_Role = membership.Member_Role == CommunityMemberRole.Owner
                ? CommunityMemberRole.Owner
                : membership.Member_Role;
        }

        // The counter moves only when membership actually crossed the line. Approving
        // somebody already approved, or rejecting a request that was never counted,
        // must not touch it - that is precisely how these counters drift.
        var delta = (isActive ? 1 : 0) - (wasActive ? 1 : 0);

        if (delta != 0)
        {
            await _unitOfWork.Repository<Community, Guid>().ExecuteUpdateAsync(
                candidate => candidate.Id == request.CommunityId,
                setters => setters.SetProperty(
                    candidate => candidate.Members_Count,
                    candidate => candidate.Members_Count + delta),
                cancellationToken);
        }

        if (isActive && !wasActive)
        {
            access.Community.RecordMembershipApproved(request.EmployeeId, access.EmployeeId, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Either direction changes what this person can see, and the key set is cached.
        if (delta != 0)
        {
            await _audience.InvalidateAsync(request.EmployeeId, cancellationToken);
        }
    }
}
