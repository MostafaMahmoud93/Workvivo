namespace Workvivo.Application.Features.Events.Dtos;

public sealed class EventSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int EventType { get; init; }
    public int Format { get; init; }
    public int Status { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string? TimeZoneId { get; init; }
    public bool IsAllDay { get; init; }
    public string? Address { get; init; }
    public string? LocationName { get; init; }
    public Guid OrganizerEmployeeId { get; init; }
    public string? OrganizerDisplayName { get; init; }
    public int AttendeesCount { get; init; }
    public int? Capacity { get; init; }
    public bool RequiresRsvp { get; init; }

    /// <summary>The caller's RSVP, or null if they have not answered.</summary>
    public int? MyResponse { get; init; }

    /// <summary>False once the event is full, cancelled, or in the past.</summary>
    public bool CanRsvp { get; init; }

    /// <summary>
    /// The join link, present only for people who are actually going.
    ///
    /// An open meeting link is an open meeting: anybody who has it can walk in,
    /// including somebody the audience rules excluded. It is therefore withheld from
    /// the list and released only once a person has said they are attending.
    /// </summary>
    public string? MeetingUrl { get; init; }
}
