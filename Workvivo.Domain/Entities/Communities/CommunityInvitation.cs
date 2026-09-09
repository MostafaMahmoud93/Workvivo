using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Communities;

/// <summary>An invitation to join a community.</summary>
public class CommunityInvitation : FullBaseEntity<Guid>
{
    public Guid Community_Id { get; set; }
    public Guid Invited_Employee_Id { get; set; }
    public Guid Invited_By_Employee_Id { get; set; }

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public string? Message { get; set; }

    /// <summary>
    /// Invitations lapse. Without an expiry, a Private community accumulates a
    /// permanent list of standing invitations from people who have since left.
    /// </summary>
    public DateTime Expires_At { get; set; }

    public DateTime? Responded_At { get; set; }

    public virtual Community? Community { get; set; }
    public virtual Employee? InvitedEmployee { get; set; }
    public virtual Employee? InvitedBy { get; set; }

    public bool IsActionableAt(DateTime utcNow) =>
        !Is_Deleted && Status == InvitationStatus.Pending && Expires_At > utcNow;
}
