using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Documents;

/// <summary>
/// A record that somebody downloaded a document version.
///
/// Separate from the general audit log because it is queried differently and grows
/// far faster: "who has read the updated code of conduct" is a routine compliance
/// question, and answering it by scanning a mixed-purpose audit table would not hold
/// up. Append-only.
/// </summary>
public class DocumentDownloadLog : BaseCommonEntity<long>
{
    public Guid Document_Id { get; set; }
    public Guid Version_Id { get; set; }
    public Guid Employee_Id { get; set; }

    public DateTime Downloaded_At { get; set; }
    public string? Ip_Address { get; set; }
    public string? User_Agent { get; set; }

    public virtual Document? Document { get; set; }
    public virtual DocumentVersion? Version { get; set; }
    public virtual Employee? Employee { get; set; }
}
