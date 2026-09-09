using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>
/// One person's reaction to a post. At most one per person per post - changing your
/// mind updates the row rather than adding another.
///
/// Hard-deleted when withdrawn: a retracted "like" is not history worth keeping, and
/// soft-deleting would make the unique constraint that enforces one-per-person
/// unworkable.
/// </summary>
public class PostReaction : BaseCommonEntity<Guid>
{
    public Guid Post_Id { get; set; }
    public Guid Employee_Id { get; set; }
    public ReactionType Reaction_Type { get; set; }
    public DateTime Reacted_At { get; set; }

    public virtual Post? Post { get; set; }
    public virtual Employee? Employee { get; set; }
}
