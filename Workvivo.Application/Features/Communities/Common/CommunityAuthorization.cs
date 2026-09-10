using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Communities.Common;

/// <summary>
/// What the caller may do with one community.
///
/// Communities are the first thing in the product where visibility is not a single
/// permission. A Private community must be invisible to non-members, a Restricted one
/// must be listed but not readable, and a moderator's powers come from a membership
/// row rather than from a role. Deciding that in each handler would produce four
/// slightly different answers.
/// </summary>
public sealed class CommunityAuthorization
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissions;
    private readonly CurrentEmployee _currentEmployee;

    public CommunityAuthorization(
        IUnitOfWork unitOfWork,
        IPermissionService permissions,
        CurrentEmployee currentEmployee)
    {
        _unitOfWork = unitOfWork;
        _permissions = permissions;
        _currentEmployee = currentEmployee;
    }

    /// <summary>
    /// Loads a community together with what the caller may do to it.
    ///
    /// A community the caller may not even see comes back as
    /// <see cref="NotFoundException"/> rather than a refusal. Telling somebody "this
    /// exists but is private" confirms the existence of a group and often its purpose,
    /// which for a private community is most of what is being protected.
    /// </summary>
    public async Task<CommunityAccess> RequireReadableAsync(
        Guid communityId,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAsync(communityId, cancellationToken);

        return access.CanRead ? access : throw new NotFoundException(nameof(Community), communityId);
    }

    /// <summary>Loads a community the caller may moderate, or fails.</summary>
    public async Task<CommunityAccess> RequireModeratableAsync(
        Guid communityId,
        CancellationToken cancellationToken)
    {
        var access = await RequireReadableAsync(communityId, cancellationToken);

        return access.CanModerate
            ? access
            : throw new ForbiddenException("You do not moderate this community.");
    }

    /// <summary>Loads a community whose settings the caller may change, or fails.</summary>
    public async Task<CommunityAccess> RequireManageableAsync(
        Guid communityId,
        CancellationToken cancellationToken)
    {
        var access = await RequireReadableAsync(communityId, cancellationToken);

        return access.CanManage
            ? access
            : throw new ForbiddenException("Only the owner can change this community.");
    }

    public async Task<CommunityAccess> ResolveAsync(Guid communityId, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);

        var community = await _unitOfWork.Repository<Community, Guid>().FindByIDAsync(communityId)
            ?? throw new NotFoundException(nameof(Community), communityId);

        var membership = await _unitOfWork.Repository<CommunityMember, Guid>()
            .GetAllQ()
            .FirstOrDefaultAsync(
                member => member.Community_Id == communityId && member.Employee_Id == employeeId,
                cancellationToken);

        // The platform-wide permission. Held by administrators and community managers,
        // it is what lets somebody moderate a group they are not in - and it is checked
        // once here rather than in each handler.
        var isPlatformManager = await _permissions.HasPermissionAsync(
            _currentEmployee.UserId, PermissionKeys.CommunityManage, cancellationToken);

        return new CommunityAccess(community, membership, employeeId, isPlatformManager);
    }
}

/// <summary>
/// One community, the caller's membership of it, and the conclusions drawn from both.
/// </summary>
public sealed class CommunityAccess
{
    public CommunityAccess(
        Community community,
        CommunityMember? membership,
        Guid employeeId,
        bool isPlatformManager)
    {
        Community = community;
        Membership = membership;
        EmployeeId = employeeId;
        IsPlatformManager = isPlatformManager;
    }

    public Community Community { get; }

    public CommunityMember? Membership { get; }

    public Guid EmployeeId { get; }

    public bool IsPlatformManager { get; }

    public bool IsMember => Membership?.IsActive == true;

    /// <summary>
    /// Whether the caller may see the community's content.
    ///
    /// A banned member is explicitly not a member, which matters: banning someone from
    /// a Public community does not hide the content, but it does stop them posting -
    /// so the two questions stay separate.
    /// </summary>
    public bool CanRead =>
        !Community.Is_Deleted
        && (Community.AllowsPublicReading || IsMember || IsPlatformManager);

    /// <summary>Whether the caller may see that the community exists at all.</summary>
    public bool CanDiscover =>
        !Community.Is_Deleted
        && (Community.Privacy != CommunityPrivacy.Private || IsMember || IsPlatformManager);

    public bool CanModerate => Membership?.CanModerate == true || IsPlatformManager;

    public bool CanManage =>
        (IsMember && Membership!.Member_Role == CommunityMemberRole.Owner) || IsPlatformManager;

    /// <summary>Whether the caller may post into the community. Membership, not readability.</summary>
    public bool CanPost => IsMember;

    /// <summary>Whether a join request from the caller would make sense right now.</summary>
    public bool CanRequestToJoin =>
        !Community.Is_Deleted
        && Community.Is_Active
        && Membership?.Membership_Status is not (MembershipStatus.Approved or MembershipStatus.Pending or MembershipStatus.Banned);
}
