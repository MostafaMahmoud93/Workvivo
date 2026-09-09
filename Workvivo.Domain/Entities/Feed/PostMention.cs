using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>
/// A colleague named in a post.
///
/// Stored as rows rather than parsed out of the HTML on demand: the notification
/// fan-out, the "mentions of me" view and the profile links all need this, and
/// re-parsing markup for each is both slow and fragile once the text is edited.
/// </summary>
public class PostMention : BaseCommonEntity<Guid>
{
    public Guid Post_Id { get; set; }
    public Guid Mentioned_Employee_Id { get; set; }

    /// <summary>
    /// Where the mention sits in the plain-text content, so the client can render it
    /// as a link without re-scanning. Nullable because an edit can move it and the
    /// offset is a convenience, not the source of truth.
    /// </summary>
    public int? Text_Offset { get; set; }
    public int? Text_Length { get; set; }

    public DateTime Mentioned_At { get; set; }

    public virtual Post? Post { get; set; }
    public virtual Employee? MentionedEmployee { get; set; }
}
