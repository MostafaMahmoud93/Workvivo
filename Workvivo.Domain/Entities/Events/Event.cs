using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Events;

/// <summary>A company, team or community event.</summary>
public class Event : AuditableEntity<Guid>
{
    public string Title_Ar { get; set; } = string.Empty;
    public string? Title_En { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }

    public EventType Event_Type { get; set; } = EventType.Company;
    public EventFormat Format { get; set; } = EventFormat.Physical;
    public EventStatus Status { get; set; } = EventStatus.Draft;

    /// <summary>
    /// Start and end in UTC, with the organiser's zone kept alongside.
    ///
    /// Both are needed: UTC orders and compares correctly across offices, while the
    /// zone preserves what the organiser meant - "09:00 in Dubai" has to stay 09:00
    /// there even when a daylight-saving change moves the UTC instant.
    /// </summary>
    public DateTime Start_At { get; set; }
    public DateTime End_At { get; set; }
    public string? TimeZone_Id { get; set; }
    public bool Is_All_Day { get; set; }

    public Guid? Location_Id { get; set; }
    public string? Address_Ar { get; set; }
    public string? Address_En { get; set; }

    /// <summary>
    /// Join link for an online or hybrid event.
    ///
    /// Treated as sensitive: it is handed out only to people the audience rules admit,
    /// because an open meeting link is an open meeting.
    /// </summary>
    public string? Meeting_Url { get; set; }

    public Guid? Banner_File_Id { get; set; }
    public Guid Organizer_Employee_Id { get; set; }
    public Guid? Community_Id { get; set; }

    /// <summary>Null for unlimited. When set, RSVPs stop being accepted once reached.</summary>
    public int? Capacity { get; set; }

    public int Attendees_Count { get; set; }
    public bool Requires_Rsvp { get; set; } = true;

    /// <summary>Set when reminders have gone out, so a retry cannot send them twice.</summary>
    public DateTime? Reminder_Sent_At { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual Location? Location { get; set; }
    public virtual Employee? Organizer { get; set; }
    public virtual Community? Community { get; set; }
    public virtual FileAsset? Banner { get; set; }
    public virtual ICollection<EventAttendee> Attendees { get; set; } = [];
    public virtual ICollection<EventAudience> Audiences { get; set; } = [];

    [NotMapped]
    public string? Title => LocalizedText.Pick(Title_Ar, Title_En);

    [NotMapped]
    public string? Description => LocalizedText.Pick(Description_Ar, Description_En);

    [NotMapped]
    public string? Address => LocalizedText.Pick(Address_Ar, Address_En);

    public bool HasCapacityRemaining => Capacity is null || Attendees_Count < Capacity;
}
