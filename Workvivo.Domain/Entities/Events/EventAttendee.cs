using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Events;

/// <summary>Someone's RSVP.</summary>
public class EventAttendee : BaseCommonEntity<Guid>
{
    public Guid Event_Id { get; set; }
    public Guid Employee_Id { get; set; }

    public EventResponse Response { get; set; }
    public DateTime Responded_At { get; set; }

    /// <summary>Set on arrival, for events that take attendance.</summary>
    public DateTime? Checked_In_At { get; set; }

    public string? Note { get; set; }

    public virtual Event? Event { get; set; }
    public virtual Employee? Employee { get; set; }
}
