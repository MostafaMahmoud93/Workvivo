using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Documents;

/// <summary>
/// Who may read a document. See <see cref="AudienceEntity{TKey}"/>.
///
/// Here the audience rules are an access-control boundary rather than a relevance
/// filter: a salary band document targeted at HR must be unreadable by everyone else,
/// so the same predicate that hides it from the list also guards the download.
/// </summary>
public class DocumentAudience : AudienceEntity<Guid>
{
    public DocumentAudience()
    {
    }

    public DocumentAudience(Guid documentId, AudienceType audienceType, Guid? targetId)
        : base(audienceType, targetId)
    {
        Document_Id = documentId;
    }

    public Guid Document_Id { get; set; }

    public virtual Document? Document { get; set; }
}
