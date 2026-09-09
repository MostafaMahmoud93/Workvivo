using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Documents;

/// <summary>
/// One issued version of a document.
///
/// Superseded versions are kept, not overwritten: when a policy changes, "which
/// version was in force in March, and who had downloaded it" is a question compliance
/// asks, and it cannot be answered from a single mutable row.
/// </summary>
public class DocumentVersion : FullBaseEntity<Guid>
{
    public Guid Document_Id { get; set; }
    public Guid File_Id { get; set; }

    /// <summary>Sequential from 1, unique within the document.</summary>
    public int Version_Number { get; set; }

    public string? Change_Note { get; set; }

    /// <summary>When this version took effect, which may be later than its upload.</summary>
    public DateTime? Effective_From { get; set; }

    public virtual Document? Document { get; set; }
    public virtual FileAsset? File { get; set; }
    public virtual ICollection<DocumentDownloadLog> Downloads { get; set; } = [];
}
