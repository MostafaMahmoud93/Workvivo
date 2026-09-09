using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>
/// A feed entry - an ordinary post, an official announcement, an article, a poll
/// wrapper, a recognition, or a system message.
///
/// One table for all of them rather than a table per type: the feed is a single
/// ordered timeline, and splitting the types would turn every page of it into a
/// union that no index can serve cheaply.
/// </summary>
public class Post : AuditableEntity<Guid>
{
    public Guid Author_Employee_Id { get; set; }

    /// <summary>Null for a company-wide post; set when the post lives in a community.</summary>
    public Guid? Community_Id { get; set; }

    public PostType Post_Type { get; set; } = PostType.Normal;
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public PostVisibility Visibility { get; set; } = PostVisibility.Organization;

    public string? Title_Ar { get; set; }
    public string? Title_En { get; set; }

    /// <summary>
    /// Sanitised rich content. What the author wrote, with everything executable
    /// stripped on the way in - never on the way out. Sanitising at render time leaves
    /// live payloads in the database, and it only takes one consumer that forgets - an
    /// export, a digest email, a mobile client - for them to fire.
    /// </summary>
    public string? Content_Html { get; set; }

    /// <summary>
    /// Plain-text rendering of the same content, for search indexing, previews and
    /// plain-text email. Derived on write so no reader has to strip tags itself.
    /// </summary>
    public string? Content_Text { get; set; }

    public bool Is_Pinned { get; set; }
    public bool Is_Featured { get; set; }

    /// <summary>
    /// Marks a post as speaking for the company rather than for its author. Requires
    /// a distinct permission, and the client badges it.
    /// </summary>
    public bool Is_Official { get; set; }

    public bool Comments_Enabled { get; set; } = true;

    /// <summary>
    /// When the post became visible. Null until then.
    ///
    /// Not the same as Create_Date: a post drafted in March and published in May sorts
    /// by May. The feed's keyset cursor rides on this column plus the id.
    /// </summary>
    public DateTime? Published_Date { get; set; }

    /// <summary>Set when Status is Scheduled; a recurring job publishes it at this time.</summary>
    public DateTime? Scheduled_Publish_Date { get; set; }

    public DateTime? Archived_Date { get; set; }

    /// <summary>
    /// Denormalised engagement counters.
    ///
    /// A feed page shows twenty posts; counting reactions and comments per post would
    /// be forty aggregate queries against the two largest tables in the system. These
    /// are maintained by atomic increments in the same transaction as the write, and
    /// reconciled nightly in case anything drifts.
    /// </summary>
    public int Reactions_Count { get; set; }
    public int Comments_Count { get; set; }
    public int Views_Count { get; set; }

    /// <summary>Concurrency token - moderators and authors do edit the same post at once.</summary>
    public byte[]? RowVersion { get; set; }

    public virtual Employee? Author { get; set; }
    public virtual Community? Community { get; set; }
    public virtual ICollection<PostAttachment> Attachments { get; set; } = [];
    public virtual ICollection<PostAudience> Audiences { get; set; } = [];
    public virtual ICollection<PostReaction> Reactions { get; set; } = [];
    public virtual ICollection<PostMention> Mentions { get; set; } = [];
    public virtual ICollection<Comment> Comments { get; set; } = [];

    [NotMapped]
    public string? Title => LocalizedText.Pick(Title_Ar, Title_En);

    /// <summary>True when the post is published and its publish time has passed.</summary>
    public bool IsVisibleAt(DateTime utcNow) =>
        !Is_Deleted
        && Status == PostStatus.Published
        && Published_Date is not null
        && Published_Date <= utcNow;
}
