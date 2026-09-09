using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>
/// One targeting rule for a post. A post with several rows reaches the union of them.
///
/// The shared shape, and the reason the key column exists, are on
/// <see cref="AudienceEntity{TKey}"/>.
/// </summary>
public class PostAudience : AudienceEntity<Guid>
{
    public PostAudience()
    {
    }

    public PostAudience(Guid postId, AudienceType audienceType, Guid? targetId)
        : base(audienceType, targetId)
    {
        Post_Id = postId;
    }

    public Guid Post_Id { get; set; }

    public virtual Post? Post { get; set; }
}
