using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Feed;

/// <summary>
/// Records that someone has seen a post. One row per person per post.
///
/// Exists for announcement reach - "how many staff have actually read the safety
/// notice" is a question HR asks - which needs distinct viewers, not a hit counter.
/// The denormalised Views_Count on the post is the cheap read; this is the evidence.
/// </summary>
public class PostView : BaseCommonEntity<Guid>
{
    public Guid Post_Id { get; set; }
    public Guid Employee_Id { get; set; }
    public DateTime First_Viewed_At { get; set; }
    public DateTime Last_Viewed_At { get; set; }
    public int View_Count { get; set; } = 1;

    public virtual Post? Post { get; set; }
    public virtual Employee? Employee { get; set; }
}
