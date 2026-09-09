using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Communities;

/// <summary>
/// Somebody's membership of a community, including moderators and the owner.
///
/// Role and status are separate axes: a moderator whose membership is Pending is not
/// yet a moderator of anything, and collapsing the two would lose that.
/// </summary>
public class CommunityMember : FullBaseEntity<Guid>
{
    public Guid Community_Id { get; set; }
    public Guid Employee_Id { get; set; }

    public CommunityMemberRole Member_Role { get; set; } = CommunityMemberRole.Member;
    public MembershipStatus Membership_Status { get; set; } = MembershipStatus.Approved;

    public DateTime Requested_At { get; set; }
    public DateTime? Joined_At { get; set; }

    /// <summary>Who approved or rejected the request. Null for an open join.</summary>
    public Guid? Reviewed_By_Employee_Id { get; set; }
    public DateTime? Reviewed_At { get; set; }

    /// <summary>Mutes this community's notifications without leaving it.</summary>
    public bool Notifications_Enabled { get; set; } = true;

    public virtual Community? Community { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual Employee? ReviewedBy { get; set; }

    /// <summary>True when this membership currently grants access.</summary>
    public bool IsActive => !Is_Deleted && Membership_Status == MembershipStatus.Approved;

    /// <summary>True when this member may moderate the community's content.</summary>
    public bool CanModerate =>
        IsActive && Member_Role is CommunityMemberRole.Moderator or CommunityMemberRole.Owner;
}
