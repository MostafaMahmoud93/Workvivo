using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Communities.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;

namespace Workvivo.Application.Features.Communities.Queries.GetMyInvitations;

/// <summary>The caller's outstanding community invitations.</summary>
public sealed record GetMyInvitationsQuery : IQuery<IReadOnlyList<CommunityInvitationDto>>;

public sealed class GetMyInvitationsQueryHandler
    : IRequestHandler<GetMyInvitationsQuery, IReadOnlyList<CommunityInvitationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IDateTimeProvider _clock;

    public GetMyInvitationsQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CommunityInvitationDto>> Handle(
        GetMyInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var now = _clock.UtcNow;

        var rows = await _unitOfWork.Repository<CommunityInvitation, Guid>()
            .GetAllQ()
            .Where(invitation =>
                invitation.Invited_Employee_Id == employeeId
                && invitation.Status == InvitationStatus.Pending

                // Expiry is applied in the query rather than by a sweep job. An
                // invitation past its date must stop working immediately, not whenever
                // the next clean-up runs.
                && invitation.Expires_At > now)
            .OrderBy(invitation => invitation.Expires_At)
            .Select(invitation => new
            {
                invitation.Id,
                invitation.Community_Id,
                NameAr = invitation.Community!.Name_Ar,
                NameEn = invitation.Community.Name_En,
                invitation.Message,
                InvitedBy = invitation.InvitedBy!.Display_Name,
                invitation.Expires_At,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new CommunityInvitationDto
            {
                Id = row.Id,
                CommunityId = row.Community_Id,
                CommunityName = LocalizedText.Pick(row.NameAr, row.NameEn) ?? string.Empty,
                Message = row.Message,
                InvitedByDisplayName = row.InvitedBy,
                ExpiresAt = row.Expires_At,
            }),
        ];
    }
}
