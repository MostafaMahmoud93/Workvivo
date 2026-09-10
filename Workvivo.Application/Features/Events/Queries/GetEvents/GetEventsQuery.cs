using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Events.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Events;

namespace Workvivo.Application.Features.Events.Queries.GetEvents;

/// <summary>
/// The events the caller is invited to, soonest first.
///
/// Audience-filtered with the same key set as the feed, so an event for one office
/// does not appear on another office's calendar.
/// </summary>
public sealed class GetEventsQuery : IQuery<IReadOnlyList<EventSummaryDto>>
{
    /// <summary>Include events that have already finished.</summary>
    public bool IncludePast { get; set; }

    /// <summary>How far ahead to look. Bounded so a calendar cannot ask for a decade.</summary>
    public int Days { get; set; } = 60;
}

public sealed class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, IReadOnlyList<EventSummaryDto>>
{
    private const int MaxDays = 365;
    private const int MaxEvents = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IDateTimeProvider _clock;

    public GetEventsQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _clock = clock;
    }

    public async Task<IReadOnlyList<EventSummaryDto>> Handle(
        GetEventsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);

        var now = _clock.UtcNow;
        var until = now.AddDays(Math.Clamp(request.Days, 1, MaxDays));

        var query = _unitOfWork.Repository<Event, Guid>()
            .GetAllQ()
            .Where(candidate => candidate.Status != EventStatus.Draft)
            .Where(candidate => candidate.Start_At < until)
            .Where(candidate => _unitOfWork.Repository<EventAudience, Guid>()
                .GetAllQ()
                .Any(audience => audience.Event_Id == candidate.Id
                    && keys.Contains(audience.Audience_Key)));

        if (!request.IncludePast)
        {
            // Compared on the end, not the start: an event running right now is still
            // an event you can join, and filtering on the start hides it the moment
            // it begins.
            query = query.Where(candidate => candidate.End_At >= now);
        }

        var events = await query
            .OrderBy(candidate => candidate.Start_At)
            .Take(MaxEvents)
            .Select(candidate => new Row
            {
                Id = candidate.Id,
                TitleAr = candidate.Title_Ar,
                TitleEn = candidate.Title_En,
                DescriptionAr = candidate.Description_Ar,
                DescriptionEn = candidate.Description_En,
                EventType = (int)candidate.Event_Type,
                Format = (int)candidate.Format,
                Status = (int)candidate.Status,
                StartAt = candidate.Start_At,
                EndAt = candidate.End_At,
                TimeZoneId = candidate.TimeZone_Id,
                IsAllDay = candidate.Is_All_Day,
                AddressAr = candidate.Address_Ar,
                AddressEn = candidate.Address_En,
                LocationName = candidate.Location == null
                    ? null
                    : candidate.Location.Name_En ?? candidate.Location.Name_Ar,
                OrganizerEmployeeId = candidate.Organizer_Employee_Id,
                OrganizerDisplayName = candidate.Organizer == null
                    ? null
                    : candidate.Organizer.Display_Name,
                AttendeesCount = candidate.Attendees_Count,
                Capacity = candidate.Capacity,
                RequiresRsvp = candidate.Requires_Rsvp,
                MeetingUrl = candidate.Meeting_Url,
            })
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            return [];
        }

        var eventIds = events.ConvertAll(row => row.Id);

        var myResponses = await _unitOfWork.Repository<EventAttendee, Guid>()
            .GetAllQ()
            .Where(attendee => eventIds.Contains(attendee.Event_Id) && attendee.Employee_Id == employeeId)
            .Select(attendee => new { attendee.Event_Id, attendee.Response })
            .ToDictionaryAsync(
                attendee => attendee.Event_Id, attendee => (int)attendee.Response, cancellationToken);

        return
        [
            .. events.Select(row =>
            {
                var myResponse = myResponses.TryGetValue(row.Id, out var response) ? response : (int?)null;
                var attending = myResponse == (int)EventResponse.Attending;

                var full = row.Capacity is { } capacity && row.AttendeesCount >= capacity;

                return new EventSummaryDto
                {
                    Id = row.Id,
                    Title = LocalizedText.Pick(row.TitleAr, row.TitleEn) ?? string.Empty,
                    Description = LocalizedText.Pick(row.DescriptionAr, row.DescriptionEn),
                    EventType = row.EventType,
                    Format = row.Format,
                    Status = row.Status,
                    StartAt = row.StartAt,
                    EndAt = row.EndAt,
                    TimeZoneId = row.TimeZoneId,
                    IsAllDay = row.IsAllDay,
                    Address = LocalizedText.Pick(row.AddressAr, row.AddressEn),
                    LocationName = row.LocationName,
                    OrganizerEmployeeId = row.OrganizerEmployeeId,
                    OrganizerDisplayName = row.OrganizerDisplayName,
                    AttendeesCount = row.AttendeesCount,
                    Capacity = row.Capacity,
                    RequiresRsvp = row.RequiresRsvp,
                    MyResponse = myResponse,

                    // Somebody already attending can always change their mind, even
                    // when the event is full - otherwise the only way out of a full
                    // event is to stay in it.
                    CanRsvp = row.Status == (int)EventStatus.Published
                        && row.EndAt >= now
                        && (!full || attending),

                    // Released only to people who are going. See the DTO.
                    MeetingUrl = attending ? row.MeetingUrl : null,
                };
            }),
        ];
    }

    private sealed class Row
    {
        public Guid Id { get; init; }
        public string? TitleAr { get; init; }
        public string? TitleEn { get; init; }
        public string? DescriptionAr { get; init; }
        public string? DescriptionEn { get; init; }
        public int EventType { get; init; }
        public int Format { get; init; }
        public int Status { get; init; }
        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }
        public string? TimeZoneId { get; init; }
        public bool IsAllDay { get; init; }
        public string? AddressAr { get; init; }
        public string? AddressEn { get; init; }
        public string? LocationName { get; init; }
        public Guid OrganizerEmployeeId { get; init; }
        public string? OrganizerDisplayName { get; init; }
        public int AttendeesCount { get; init; }
        public int? Capacity { get; init; }
        public bool RequiresRsvp { get; init; }
        public string? MeetingUrl { get; init; }
    }
}
