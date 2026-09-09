using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>One person's reaction to a comment. At most one per person per comment.</summary>
public class CommentReaction : BaseCommonEntity<Guid>
{
    public Guid Comment_Id { get; set; }
    public Guid Employee_Id { get; set; }
    public ReactionType Reaction_Type { get; set; }
    public DateTime Reacted_At { get; set; }

    public virtual Comment? Comment { get; set; }
    public virtual Employee? Employee { get; set; }
}
