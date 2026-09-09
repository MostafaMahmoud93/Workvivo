using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>
/// A comment on a post, or a reply to one.
///
/// Nesting is capped at two levels below the top comment. Unlimited nesting produces
/// threads nobody can read on a phone and queries whose cost depends on how deep
/// somebody felt like going; the cap is enforced here rather than in a handler so it
/// cannot be bypassed by a second code path.
/// </summary>
public class Comment : AuditableEntity<Guid>
{
    /// <summary>Deepest allowed value of <see cref="Depth"/>. Top-level comments are 0.</summary>
    public const int MaxDepth = 2;

    public Guid Post_Id { get; set; }
    public Guid Author_Employee_Id { get; set; }

    /// <summary>Null for a top-level comment.</summary>
    public Guid? Parent_Comment_Id { get; set; }

    /// <summary>
    /// 0 for a top-level comment, 1 for a reply, 2 for a reply to a reply.
    /// Denormalised so the cap can be checked without walking up the chain.
    /// </summary>
    public int Depth { get; set; }

    public string? Content_Html { get; set; }
    public string? Content_Text { get; set; }

    public int Reactions_Count { get; set; }
    public int Replies_Count { get; set; }

    public bool Is_Edited { get; set; }

    public virtual Post? Post { get; set; }
    public virtual Employee? Author { get; set; }
    public virtual Comment? ParentComment { get; set; }
    public virtual ICollection<Comment> Replies { get; set; } = [];
    public virtual ICollection<CommentReaction> Reactions { get; set; } = [];
    public virtual ICollection<CommentMention> Mentions { get; set; } = [];

    /// <summary>
    /// Depth a reply to this comment would sit at, or null when this comment is
    /// already at the limit and cannot be replied to.
    /// </summary>
    public int? ReplyDepth() => Depth >= MaxDepth ? null : Depth + 1;
}
