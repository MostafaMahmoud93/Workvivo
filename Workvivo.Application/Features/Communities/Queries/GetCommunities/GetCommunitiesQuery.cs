using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Communities.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;

namespace Workvivo.Application.Features.Communities.Queries.GetCommunities;

/// <summary>Which slice of the directory to show.</summary>
public enum CommunityScope
{
    /// <summary>Everything the caller is allowed to see.</summary>
    Discover = 0,

    /// <summary>Only the ones they belong to.</summary>
    Mine = 1,
}

/// <summary>
/// The community directory.
///
/// Offset-paged rather than keyset, unlike the feed. Communities number in the
/// hundreds, people genuinely sort and jump around them, and a total count is
/// meaningful here in a way it is not on an endless timeline.
/// </summary>
public sealed class GetCommunitiesQuery : PageRequest, IQuery<PagedResult<CommunitySummaryDto>>
{
    public string? Search { get; set; }

    public CommunityScope Scope { get; set; } = CommunityScope.Discover;
}

public sealed class GetCommunitiesQueryHandler
    : IRequestHandler<GetCommunitiesQuery, PagedResult<CommunitySummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;

    public GetCommunitiesQueryHandler(IUnitOfWork unitOfWork, CurrentEmployee currentEmployee)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
    }

    public async Task<PagedResult<CommunitySummaryDto>> Handle(
        GetCommunitiesQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);

        // The caller's memberships, fetched once. Doing this as a correlated subquery
        // per row would work and would also be a join against the largest table in the
        // community schema for every page of the directory.
        var memberships = await _unitOfWork.Repository<CommunityMember, Guid>()
            .GetAllQ()
            .Where(member => member.Employee_Id == employeeId)
            .Select(member => new { member.Community_Id, member.Membership_Status, member.Member_Role })
            .ToListAsync(cancellationToken);

        var byCommunity = memberships.ToDictionary(member => member.Community_Id);

        var joinedIds = memberships
            .Where(member => member.Membership_Status == MembershipStatus.Approved)
            .Select(member => member.Community_Id)
            .ToArray();

        var query = _unitOfWork.Repository<Community, Guid>()
            .GetAllQ()
            .Where(community => community.Is_Active);

        // A Private community is invisible to anyone outside it. Not merely
        // unreadable - absent, so its existence and name are not disclosed by a
        // directory listing.
        query = query.Where(community =>
            community.Privacy != CommunityPrivacy.Private || joinedIds.Contains(community.Id));

        if (request.Scope == CommunityScope.Mine)
        {
            query = query.Where(community => joinedIds.Contains(community.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            query = query.Where(community =>
                community.Name_Ar.Contains(term)
                || (community.Name_En != null && community.Name_En.Contains(term)));
        }

        var page = await query
            .OrderByDescending(community => community.Is_Featured)
            .ThenByDescending(community => community.Members_Count)
            .ThenBy(community => community.Id)
            .Select(community => new Row
            {
                Id = community.Id,
                Slug = community.Slug,
                NameAr = community.Name_Ar,
                NameEn = community.Name_En,
                DescriptionAr = community.Description_Ar,
                DescriptionEn = community.Description_En,
                Privacy = (int)community.Privacy,
                LogoFileId = community.Logo_File_Id,
                MembersCount = community.Members_Count,
                PostsCount = community.Posts_Count,
                IsFeatured = community.Is_Featured,
            })
            .ToPagedResultAsync(request, cancellationToken);

        return page.Map(row =>
        {
            var membership = byCommunity.GetValueOrDefault(row.Id);

            return new CommunitySummaryDto
            {
                Id = row.Id,
                Slug = row.Slug,
                Name = LocalizedText.Pick(row.NameAr, row.NameEn) ?? string.Empty,
                Description = LocalizedText.Pick(row.DescriptionAr, row.DescriptionEn),
                Privacy = row.Privacy,
                LogoFileId = row.LogoFileId,
                MembersCount = row.MembersCount,
                PostsCount = row.PostsCount,
                IsFeatured = row.IsFeatured,
                MyMembershipStatus = membership is null ? null : (int)membership.Membership_Status,
                MyRole = membership is null ? null : (int)membership.Member_Role,

                // Banned is deliberately not offered a Join button. Pending is not
                // either - a second request would just be noise in the queue.
                CanJoin = membership?.Membership_Status
                    is not (MembershipStatus.Approved or MembershipStatus.Pending or MembershipStatus.Banned),
            };
        });
    }

    private sealed class Row
    {
        public Guid Id { get; init; }
        public string Slug { get; init; } = string.Empty;
        public string? NameAr { get; init; }
        public string? NameEn { get; init; }
        public string? DescriptionAr { get; init; }
        public string? DescriptionEn { get; init; }
        public int Privacy { get; init; }
        public Guid? LogoFileId { get; init; }
        public int MembersCount { get; init; }
        public int PostsCount { get; init; }
        public bool IsFeatured { get; init; }
    }
}
