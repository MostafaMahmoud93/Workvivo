using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Communities.Commands.SetMemberRole;

/// <summary>
/// Promotes a member to moderator, demotes one, or hands over ownership.
///
/// Ownership transfer is here rather than in its own command because it is the same
/// row and the same check, and because the two must not be able to disagree about
/// what happens to the outgoing owner.
/// </summary>
public sealed record SetMemberRoleCommand(
    Guid CommunityId,
    Guid EmployeeId,
    CommunityMemberRole Role) : ICommand;

public sealed class SetMemberRoleCommandValidator : AbstractValidator<SetMemberRoleCommand>
{
    public SetMemberRoleCommandValidator()
    {
        RuleFor(x => x.CommunityId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Role).Must(Enum.IsDefined).WithMessage("Unknown role.");
    }
}

public sealed class SetMemberRoleCommandHandler : IRequestHandler<SetMemberRoleCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CommunityAuthorization _authorization;

    public SetMemberRoleCommandHandler(IUnitOfWork unitOfWork, CommunityAuthorization authorization)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
    }

    public async Task Handle(SetMemberRoleCommand request, CancellationToken cancellationToken)
    {
        // Only the owner promotes people, not every moderator. A moderator who could
        // appoint moderators can appoint themselves an accomplice, and the owner loses
        // control of their own community.
        var access = await _authorization.RequireManageableAsync(request.CommunityId, cancellationToken);

        var members = _unitOfWork.Repository<CommunityMember, Guid>();

        var target = await members.GetAllQ()
            .FirstOrDefaultAsync(
                member => member.Community_Id == request.CommunityId
                    && member.Employee_Id == request.EmployeeId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(CommunityMember), request.EmployeeId);

        if (!target.IsActive)
        {
            throw new BusinessRuleException(
                "Only an approved member can hold a role.", "community.member-not-approved");
        }

        if (request.Role == CommunityMemberRole.Owner)
        {
            await TransferOwnershipAsync(access, target, cancellationToken);
        }
        else
        {
            if (target.Member_Role == CommunityMemberRole.Owner)
            {
                throw new BusinessRuleException(
                    "Transfer ownership to somebody else instead of demoting the owner.",
                    "community.owner-cannot-be-demoted");
            }

            target.Member_Role = request.Role;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Hands the community to somebody else.
    ///
    /// The outgoing owner becomes a moderator rather than a plain member: they built
    /// the group, and silently stripping them of every power is a surprise nobody
    /// asked for. They can be demoted afterwards by the new owner.
    /// </summary>
    private static async Task TransferOwnershipAsync(
        CommunityAccess access,
        CommunityMember target,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var community = access.Community;

        if (target.Employee_Id == community.Owner_Employee_Id)
        {
            return;
        }

        if (access.Membership is { Member_Role: CommunityMemberRole.Owner } outgoing)
        {
            outgoing.Member_Role = CommunityMemberRole.Moderator;
        }

        target.Member_Role = CommunityMemberRole.Owner;

        // The column and the row are both updated. They are deliberately duplicated -
        // see Community.Owner_Employee_Id - and letting them disagree would make
        // "who owns this" depend on which one you happened to read.
        community.Owner_Employee_Id = target.Employee_Id;
    }
}
