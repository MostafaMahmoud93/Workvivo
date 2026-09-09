using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>
/// One employee following another. Directional - following is not mutual.
///
/// Hard-deleted rather than soft: an unfollow carries no history anyone needs, and
/// keeping tombstones would make the follower count a filtered count on a table that
/// grows without bound.
/// </summary>
public class EmployeeFollower : BaseCommonEntity<Guid>
{
    /// <summary>The employee doing the following.</summary>
    public Guid Follower_Id { get; set; }

    /// <summary>The employee being followed.</summary>
    public Guid Followee_Id { get; set; }

    public DateTime Followed_At { get; set; }

    public virtual Employee? Follower { get; set; }
    public virtual Employee? Followee { get; set; }
}
