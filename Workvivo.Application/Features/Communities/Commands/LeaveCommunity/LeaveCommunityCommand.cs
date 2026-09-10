using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Communities.Commands.LeaveCommunity;

public sealed record LeaveCommunityCommand(Guid CommunityId) : ICommand;

public sealed class LeaveCommunityCommandHandler : IRequestHandler<LeaveCommunityCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CommunityAuthorization _authorization;
    private readonly IAudienceResolver _audience;

    public LeaveCommunityCommandHandler(
        IUnitOfWork unitOfWork,
        CommunityAuthorization authorization,
        IAudienceResolver audience)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _audience = audience;
    }

    public async Task Handle(LeaveCommunityCommand request, CancellationToken cancellationToken)
    {
        var access = await _authorization.ResolveAsync(request.CommunityId, cancellationToken);

        if (access.Membership is not { } membership || !membership.IsActive)
        {
            // Idempotent. Leaving something you are not in has already happened.
            return;
        }

        if (membership.Member_Role == CommunityMemberRole.Owner)
        {
            // A community with no owner has nobody who can change its settings or
            // close it, and nothing else in the model can repair that.
            throw new BusinessRuleException(
                "Transfer ownership before leaving this community.", "community.owner-cannot-leave");
        }

        // Left, not deleted. The row records that this person was once a member and
        // chose to go, which is what stops a Restricted community treating them as a
        // brand-new request and what keeps the unique index meaningful.
        membership.Membership_Status = MembershipStatus.Left;
        membership.Joined_At = null;

        await _unitOfWork.Repository<Community, Guid>().ExecuteUpdateAsync(
            candidate => candidate.Id == request.CommunityId && candidate.Members_Count > 0,
            setters => setters.SetProperty(
                candidate => candidate.Members_Count,
                candidate => candidate.Members_Count - 1),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audience.InvalidateAsync(access.EmployeeId, cancellationToken);
    }
}
