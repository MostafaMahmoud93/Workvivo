using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Communities;

/// <summary>
/// An interest or working group with its own feed - Engineering, HR, the football
/// club, a CSR committee.
/// </summary>
public class Community : AuditableEntity<Guid>
{
    /// <summary>URL-safe identifier, so a community has a stable shareable link.</summary>
    public string Slug { get; set; } = string.Empty;

    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }

    public Guid? Logo_File_Id { get; set; }
    public Guid? Cover_File_Id { get; set; }

    public CommunityPrivacy Privacy { get; set; } = CommunityPrivacy.Public;

    /// <summary>
    /// The owner, also held as a member row with the Owner role.
    ///
    /// Duplicated on purpose: the column makes "who owns this" a single read and
    /// guarantees a community always has exactly one owner, while the member row keeps
    /// the owner inside the same membership queries as everyone else.
    /// </summary>
    public Guid Owner_Employee_Id { get; set; }

    public int Members_Count { get; set; }
    public int Posts_Count { get; set; }
    public bool Is_Active { get; set; } = true;

    /// <summary>Featured communities are promoted on the discovery page.</summary>
    public bool Is_Featured { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual Employee? Owner { get; set; }
    public virtual FileAsset? Logo { get; set; }
    public virtual FileAsset? Cover { get; set; }
    public virtual ICollection<CommunityMember> Members { get; set; } = [];
    public virtual ICollection<CommunityInvitation> Invitations { get; set; } = [];
    public virtual ICollection<Post> Posts { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);

    [NotMapped]
    public string? Description => LocalizedText.Pick(Description_Ar, Description_En);

    /// <summary>
    /// Whether someone who is not a member may read the community's content.
    /// Public communities are open; the other two are not.
    /// </summary>
    public bool AllowsPublicReading => Privacy == CommunityPrivacy.Public;

    /// <summary>Whether joining needs a moderator to approve it.</summary>
    public bool RequiresApprovalToJoin => Privacy != CommunityPrivacy.Public;
}
