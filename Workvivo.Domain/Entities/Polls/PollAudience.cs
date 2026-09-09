using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Polls;

/// <summary>Targeting rule for a poll. See <see cref="AudienceEntity{TKey}"/>.</summary>
public class PollAudience : AudienceEntity<Guid>
{
    public PollAudience()
    {
    }

    public PollAudience(Guid pollId, AudienceType audienceType, Guid? targetId)
        : base(audienceType, targetId)
    {
        Poll_Id = pollId;
    }

    public Guid Poll_Id { get; set; }

    public virtual Poll? Poll { get; set; }
}
