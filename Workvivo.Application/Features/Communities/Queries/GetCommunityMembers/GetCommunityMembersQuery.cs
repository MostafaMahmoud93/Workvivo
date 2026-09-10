using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Application.Features.Communities.Dtos;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Communities.Queries.GetCommunityMembers;

/// <summary>
/// The community's members, or - for a moderator - its pending join requests.
///
/// One query for both because they are the same rows filtered differently, and
/// splitting them would duplicate the authorisation.
/// </summary>
public sealed class GetCommunityMembersQuery : PageRequest, IQuery<PagedResult<CommunityMemberDto>>
{
    public Guid CommunityId { get; set; }

    /// <summary>When true, the approval queue instead of the roster. Moderators only.</summary>
    public bool PendingOnly { get; set; }
}

public sealed class GetCommunityMembersQueryHandler
    : IRequestHandler<GetCommunityMembersQuery, PagedResult<CommunityMemberDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CommunityAuthorization _authorization;

    public GetCommunityMembersQueryHandler(
        IUnitOfWork unitOfWork,
        CommunityAuthorization authorization)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
    }

    public async Task<PagedResult<CommunityMemberDto>> Handle(
        GetCommunityMembersQuery request,
        CancellationToken cancellationToken)
    {
        var access = await _authorization.RequireReadableAsync(request.CommunityId, cancellationToken);

        if (request.PendingOnly && !access.CanModerate)
        {
            // Who has asked to join is moderator information. In a Restricted
            // community it also reveals interest that the person may not want public.
            throw new ForbiddenException("You do not moderate this community.");
        }

        var status = request.PendingOnly ? MembershipStatus.Pending : MembershipStatus.Approved;

        return await _unitOfWork.Repository<CommunityMember, Guid>()
            .GetAllQ()
            .Where(member =>
                member.Community_Id == request.CommunityId
                && member.Membership_Status == status)

            // Owner, then moderators, then everyone else - a roster where the people
            // to contact are at the top rather than sorted by an accident of joining.
            .OrderByDescending(member => member.Member_Role)
            .ThenBy(member => member.Joined_At)
            .Select(Projection)
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <summary>
    /// An expression, not a method: a method here is evaluated in memory, which
    /// materialises every member row and then lazy-loads the employee for each one.
    /// </summary>
    private static readonly System.Linq.Expressions.Expression<Func<CommunityMember, CommunityMemberDto>> Projection =
        member => new CommunityMemberDto
        {
            EmployeeId = member.Employee_Id,
            DisplayName = member.Employee!.Display_Name,
            JobTitle = member.Employee.JobTitle == null
                ? null
                : member.Employee.JobTitle.Name_En ?? member.Employee.JobTitle.Name_Ar,
            ProfilePictureFileId = member.Employee.Profile_Picture_File_Id,
            Role = (int)member.Member_Role,
            Status = (int)member.Membership_Status,
            JoinedAt = member.Joined_At,
            RequestedAt = member.Requested_At,
        };
}
