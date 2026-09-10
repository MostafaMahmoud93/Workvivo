using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Events;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Events.Commands.RespondToEvent;

/// <summary>Says whether the caller is coming.</summary>
public sealed record RespondToEventCommand(Guid EventId, EventResponse Response) : ICommand<int>;

public sealed class RespondToEventCommandValidator : AbstractValidator<RespondToEventCommand>
{
    public RespondToEventCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.Response).Must(Enum.IsDefined).WithMessage("Unknown response.");
    }
}

public sealed class RespondToEventCommandHandler : IRequestHandler<RespondToEventCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IDateTimeProvider _clock;

    public RespondToEventCommandHandler(
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

    public async Task<int> Handle(RespondToEventCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var now = _clock.UtcNow;

        var calendarEvent = await _unitOfWork.Repository<Event, Guid>().FindByIDAsync(request.EventId)
            ?? throw new NotFoundException(nameof(Event), request.EventId);

        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);

        // Invitation is checked on the write path too, not only when listing. An
        // event id is guessable, and an RSVP is how somebody would get the join link.
        var invited = await _unitOfWork.Repository<EventAudience, Guid>()
            .GetAllQ()
            .AnyAsync(
                audience => audience.Event_Id == calendarEvent.Id
                    && keys.Contains(audience.Audience_Key),
                cancellationToken);

        if (!invited)
        {
            throw new NotFoundException(nameof(Event), request.EventId);
        }

        if (calendarEvent.Status != EventStatus.Published || calendarEvent.End_At < now)
        {
            throw new BusinessRuleException(
                "This event is not accepting responses.", "event.closed");
        }

        var attendees = _unitOfWork.Repository<EventAttendee, Guid>();

        var existing = await attendees.GetAllQ()
            .FirstOrDefaultAsync(
                attendee => attendee.Event_Id == calendarEvent.Id && attendee.Employee_Id == employeeId,
                cancellationToken);

        var wasAttending = existing?.Response == EventResponse.Attending;
        var willAttend = request.Response == EventResponse.Attending;

        // Capacity is checked only for somebody joining. Changing from Attending to
        // Declined on a full event has to work, or the only way out of a full event
        // is to stay in it.
        if (willAttend && !wasAttending && !calendarEvent.HasCapacityRemaining)
        {
            throw new BusinessRuleException("This event is full.", "event.full");
        }

        if (existing is null)
        {
            await attendees.AddAsync(new EventAttendee
            {
                Id = Guid.NewGuid(),
                Event_Id = calendarEvent.Id,
                Employee_Id = employeeId,
                Response = request.Response,
                Responded_At = now,
                Is_Deleted = false,
            });
        }
        else
        {
            existing.Response = request.Response;
            existing.Responded_At = now;
        }

        // Only "Attending" counts towards the headcount, and the counter moves only
        // when the answer actually crossed that line - the same discipline as
        // community membership, and for the same reason.
        var delta = (willAttend ? 1 : 0) - (wasAttending ? 1 : 0);

        if (delta != 0)
        {
            await _unitOfWork.Repository<Event, Guid>().ExecuteUpdateAsync(
                candidate => candidate.Id == calendarEvent.Id
                    && (delta > 0 || candidate.Attendees_Count > 0),
                setters => setters.SetProperty(
                    candidate => candidate.Attendees_Count,
                    candidate => candidate.Attendees_Count + delta),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return calendarEvent.Attendees_Count + delta;
    }
}
