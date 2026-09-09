using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Events;

/// <summary>Targeting rule for an event. See <see cref="AudienceEntity{TKey}"/>.</summary>
public class EventAudience : AudienceEntity<Guid>
{
    public EventAudience()
    {
    }

    public EventAudience(Guid eventId, AudienceType audienceType, Guid? targetId)
        : base(audienceType, targetId)
    {
        Event_Id = eventId;
    }

    public Guid Event_Id { get; set; }

    public virtual Event? Event { get; set; }
}
