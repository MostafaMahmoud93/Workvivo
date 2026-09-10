using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Features.Events.Commands.RespondToEvent;
using Workvivo.Application.Features.Events.Commands.SaveEvent;
using Workvivo.Application.Features.Events.Dtos;
using Workvivo.Application.Features.Events.Queries.GetEvents;
using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// Events and RSVPs.
///
/// Which events a person can see - and therefore respond to - is decided by the
/// audience rules in the query and re-checked on the write path, because an event id
/// is guessable and an RSVP is how somebody would obtain the joining link.
/// </summary>
public class EventsController : ApiControllersBase
{
    private readonly ISender _sender;

    public EventsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.Events.List)]
    public async Task<IActionResult> List(
        [FromQuery] GetEventsQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<EventSummaryDto>>.Ok(await _sender.Send(query, cancellationToken)));

    [HttpPost]
    [Route(RouteClass.Events.Save)]
    public async Task<IActionResult> Save(SaveEventCommand command, CancellationToken cancellationToken) =>
        Ok(ApiResponse<Guid>.Ok(await _sender.Send(command, cancellationToken)));

    /// <summary>Returns the new attendee count so the client does not have to guess it.</summary>
    [HttpPost]
    [Route(RouteClass.Events.Rsvp)]
    public async Task<IActionResult> Rsvp(
        Guid eventId,
        [FromBody] RsvpRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<int>.Ok(await _sender.Send(
            new RespondToEventCommand(eventId, request.Response), cancellationToken)));
}

public sealed record RsvpRequest(EventResponse Response);
