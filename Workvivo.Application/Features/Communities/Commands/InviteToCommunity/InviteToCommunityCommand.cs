using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Application.Features.Communities.Commands.InviteToCommunity;

/// <summary>Invites people to a community. The only way into a Private one.</summary>
public sealed record InviteToCommunityCommand(
    Guid CommunityId,
    IReadOnlyList<Guid> EmployeeIds,
    string? Message) : ICommand<int>;

public sealed class InviteToCommunityCommandValidator : AbstractValidator<InviteToCommunityCommand>
{
    public InviteToCommunityCommandValidator()
    {
        RuleFor(x => x.CommunityId).NotEmpty();
        RuleFor(x => x.EmployeeIds).NotEmpty().Must(ids => ids.Count <= 100)
            .WithMessage("Invite at most 100 people at a time.");
        RuleFor(x => x.Message).MaximumLength(2000);
    }
}

public sealed class InviteToCommunityCommandHandler : IRequestHandler<InviteToCommunityCommand, int>
{
    /// <summary>
    /// How long an invitation stands.
    ///
    /// Invitations lapse because without an expiry a Private community accumulates a
    /// permanent list of standing invitations from people who have since left, and
    /// each of those is a way in.
    /// </summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private readonly IUnitOfWork _unitOfWork;
    private readonly CommunityAuthorization _authorization;
    private readonly IDateTimeProvider _clock;

    public InviteToCommunityCommandHandler(
        IUnitOfWork unitOfWork,
        CommunityAuthorization authorization,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _clock = clock;
    }

    public async Task<int> Handle(InviteToCommunityCommand request, CancellationToken cancellationToken)
    {
        var access = await _authorization.RequireModeratableAsync(request.CommunityId, cancellationToken);
        var now = _clock.UtcNow;

        var requested = request.EmployeeIds.Distinct().ToArray();

        // Only real, active people. An id list arrives from a client and is not to be
        // trusted: without this an invitation row could name anything at all.
        var valid = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => requested.Contains(employee.Id) && employee.Is_Active)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);

        // Anybody already in, pending, or banned is skipped. Re-inviting a banned
        // person would quietly undo the ban when they accepted.
        var existingMembers = await _unitOfWork.Repository<CommunityMember, Guid>()
            .GetAllQ()
            .Where(member =>
                member.Community_Id == request.CommunityId
                && valid.Contains(member.Employee_Id)
                && (member.Membership_Status == MembershipStatus.Approved
                    || member.Membership_Status == MembershipStatus.Pending
                    || member.Membership_Status == MembershipStatus.Banned))
            .Select(member => member.Employee_Id)
            .ToListAsync(cancellationToken);

        var invitations = _unitOfWork.Repository<CommunityInvitation, Guid>();

        var alreadyInvited = await invitations
            .GetAllQ()
            .Where(invitation =>
                invitation.Community_Id == request.CommunityId
                && invitation.Status == InvitationStatus.Pending
                && valid.Contains(invitation.Invited_Employee_Id))
            .Select(invitation => invitation.Invited_Employee_Id)
            .ToListAsync(cancellationToken);

        var targets = valid
            .Except(existingMembers)
            .Except(alreadyInvited)
            .Where(employeeId => employeeId != access.EmployeeId)
            .ToArray();

        foreach (var employeeId in targets)
        {
            await invitations.AddAsync(new CommunityInvitation
            {
                Id = Guid.NewGuid(),
                Community_Id = request.CommunityId,
                Invited_Employee_Id = employeeId,
                Invited_By_Employee_Id = access.EmployeeId,
                Status = InvitationStatus.Pending,
                Message = request.Message?.Trim(),
                Expires_At = now.Add(Lifetime),
                Is_Deleted = false,
            });

            access.Community.RecordInvitationSent(employeeId, access.EmployeeId, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return targets.Length;
    }
}
