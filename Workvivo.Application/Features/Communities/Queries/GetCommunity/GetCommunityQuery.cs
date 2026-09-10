using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Application.Features.Communities.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Communities.Queries.GetCommunity;

/// <summary>
/// One community, with what the caller may do to it.
///
/// The permissions travel with the response rather than being re-derived by the
/// client. A client that decides for itself whether to show a Moderate button will
/// eventually disagree with the server, and the disagreement is always discovered as
/// a confusing error at the point of use.
/// </summary>
public sealed record GetCommunityQuery(Guid CommunityId) : IQuery<CommunityDetailDto>;

public sealed class GetCommunityQueryHandler : IRequestHandler<GetCommunityQuery, CommunityDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CommunityAuthorization _authorization;

    public GetCommunityQueryHandler(IUnitOfWork unitOfWork, CommunityAuthorization authorization)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
    }

    public async Task<CommunityDetailDto> Handle(
        GetCommunityQuery request,
        CancellationToken cancellationToken)
    {
        var access = await _authorization.ResolveAsync(request.CommunityId, cancellationToken);

        // Discoverable but not readable is a real state - a Restricted community shows
        // its name and description so somebody can decide to ask to join. Only a
        // Private one is hidden outright.
        if (!access.CanDiscover)
        {
            throw new NotFoundException(nameof(Community), request.CommunityId);
        }

        var community = access.Community;

        var ownerName = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == community.Owner_Employee_Id)
            .Select(employee => employee.Display_Name)
            .FirstOrDefaultAsync(cancellationToken);

        var pending = access.CanModerate
            ? await _unitOfWork.Repository<CommunityMember, Guid>()
                .GetAllQ()
                .CountAsync(
                    member => member.Community_Id == community.Id
                        && member.Membership_Status == MembershipStatus.Pending,
                    cancellationToken)
            : 0;

        return new CommunityDetailDto
        {
            Id = community.Id,
            Slug = community.Slug,
            Name = LocalizedText.Pick(community.Name_Ar, community.Name_En) ?? string.Empty,
            Description = LocalizedText.Pick(community.Description_Ar, community.Description_En),
            Privacy = (int)community.Privacy,
            LogoFileId = community.Logo_File_Id,
            CoverFileId = community.Cover_File_Id,
            MembersCount = community.Members_Count,
            PostsCount = community.Posts_Count,
            IsFeatured = community.Is_Featured,
            IsActive = community.Is_Active,
            OwnerEmployeeId = community.Owner_Employee_Id,
            OwnerDisplayName = ownerName,
            MyMembershipStatus = access.Membership is null ? null : (int)access.Membership.Membership_Status,
            MyRole = access.Membership is null ? null : (int)access.Membership.Member_Role,
            CanJoin = access.CanRequestToJoin,
            CanRead = access.CanRead,
            CanPost = access.CanPost,
            CanModerate = access.CanModerate,
            CanManage = access.CanManage,
            PendingRequestsCount = pending,
        };
    }
}
