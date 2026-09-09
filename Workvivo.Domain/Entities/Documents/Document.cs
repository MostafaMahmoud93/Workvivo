using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Documents;

/// <summary>
/// A company document. The stable thing people link to and search for; the file
/// behind it changes with each version.
///
/// That indirection is the point: a policy's URL, its audience rules and its download
/// history all have to survive the document being reissued.
/// </summary>
public class Document : AuditableEntity<Guid>
{
    public Guid Category_Id { get; set; }

    public string Title_Ar { get; set; } = string.Empty;
    public string? Title_En { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }

    /// <summary>The version served on download. Null only between creation and first upload.</summary>
    public Guid? Current_Version_Id { get; set; }

    public Guid Owner_Employee_Id { get; set; }

    public bool Is_Published { get; set; }
    public DateTime? Published_At { get; set; }

    /// <summary>
    /// When the content should next be reviewed. Policies go stale, and a document
    /// centre without this quietly fills with guidance nobody has checked in years.
    /// </summary>
    public DateOnly? Review_Date { get; set; }

    public int Download_Count { get; set; }
    public int Version_Count { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual DocumentCategory? Category { get; set; }
    public virtual Employee? Owner { get; set; }
    public virtual DocumentVersion? CurrentVersion { get; set; }
    public virtual ICollection<DocumentVersion> Versions { get; set; } = [];
    public virtual ICollection<DocumentAudience> Audiences { get; set; } = [];
    public virtual ICollection<DocumentDownloadLog> Downloads { get; set; } = [];

    [NotMapped]
    public string? Title => LocalizedText.Pick(Title_Ar, Title_En);

    [NotMapped]
    public string? Description => LocalizedText.Pick(Description_Ar, Description_En);
}
