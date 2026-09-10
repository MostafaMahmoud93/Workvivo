using FluentValidation;
using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Events;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Events.Commands.SaveEvent;

public sealed record SaveEventCommand(
    Guid? Id,
    string TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    EventType EventType,
    EventFormat Format,
    DateTime StartAt,
    DateTime EndAt,
    string? TimeZoneId,
    bool IsAllDay,
    Guid? LocationId,
    string? AddressAr,
    string? AddressEn,
    string? MeetingUrl,
    int? Capacity,
    bool RequiresRsvp,
    bool PublishNow,
    IReadOnlyList<Guid> AudienceDepartmentIds,
    IReadOnlyList<Guid> AudienceLocationIds) : ICommand<Guid>;

public sealed class SaveEventCommandValidator : AbstractValidator<SaveEventCommand>
{
    public SaveEventCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TitleEn).MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);

        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt)
            .WithMessage("An event has to end after it starts.");

        RuleFor(x => x.Capacity)
            .Must(capacity => capacity is null or > 0)
            .WithMessage("Capacity has to be at least one, or left empty for unlimited.");

        // An online event with no way to join is the single most common thing to get
        // wrong, and the failure is only discovered by the attendees.
        RuleFor(x => x.MeetingUrl)
            .NotEmpty()
            .When(x => x.Format is EventFormat.Online or EventFormat.Hybrid)
            .WithMessage("An online event needs a joining link.");

        RuleFor(x => x.MeetingUrl)
            .Must(BeAnHttpUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingUrl))
            .WithMessage("The joining link has to be an http or https address.");

        RuleFor(x => x.TimeZoneId)
            .Must(BeAKnownTimeZone)
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZoneId))
            .WithMessage("Unknown time zone.");
    }

    /// <summary>
    /// Rejects anything that is not http or https.
    ///
    /// The link is rendered as an anchor for attendees, so a <c>javascript:</c> value
    /// here would be stored cross-site scripting delivered through a field nobody
    /// thinks of as content.
    /// </summary>
    private static bool BeAnHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool BeAKnownTimeZone(string? value)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(value!);
            return true;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}

public sealed class SaveEventCommandHandler : IRequestHandler<SaveEventCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IPermissionService _permissions;
    private readonly IContentSanitizer _sanitizer;

    public SaveEventCommandHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IPermissionService permissions,
        IContentSanitizer sanitizer)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _permissions = permissions;
        _sanitizer = sanitizer;
    }

    public async Task<Guid> Handle(SaveEventCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);

        if (!await _permissions.HasPermissionAsync(
                _currentEmployee.UserId, PermissionKeys.EventManage, cancellationToken))
        {
            throw new ForbiddenException("You cannot manage events.", PermissionKeys.EventManage);
        }

        var events = _unitOfWork.Repository<Event, Guid>();

        var calendarEvent = request.Id is { } id
            ? await events.FindByIDAsync(id) ?? throw new NotFoundException(nameof(Event), id)
            : new Event { Id = Guid.NewGuid(), Organizer_Employee_Id = employeeId, Is_Deleted = false };

        calendarEvent.Title_Ar = _sanitizer.ToPlainText(request.TitleAr);
        calendarEvent.Title_En = request.TitleEn is null ? null : _sanitizer.ToPlainText(request.TitleEn);
        calendarEvent.Description_Ar = request.DescriptionAr is null
            ? null
            : _sanitizer.ToPlainText(request.DescriptionAr);
        calendarEvent.Description_En = request.DescriptionEn is null
            ? null
            : _sanitizer.ToPlainText(request.DescriptionEn);
        calendarEvent.Event_Type = request.EventType;
        calendarEvent.Format = request.Format;
        calendarEvent.Start_At = request.StartAt;
        calendarEvent.End_At = request.EndAt;
        calendarEvent.TimeZone_Id = request.TimeZoneId;
        calendarEvent.Is_All_Day = request.IsAllDay;
        calendarEvent.Location_Id = request.LocationId;
        calendarEvent.Address_Ar = request.AddressAr;
        calendarEvent.Address_En = request.AddressEn;
        calendarEvent.Meeting_Url = request.MeetingUrl;
        calendarEvent.Capacity = request.Capacity;
        calendarEvent.Requires_Rsvp = request.RequiresRsvp;
        calendarEvent.Status = request.PublishNow ? EventStatus.Published : EventStatus.Draft;

        if (request.Id is null)
        {
            await events.AddAsync(calendarEvent);
        }
        else
        {
            // Rescheduling makes any reminder already sent misleading, so the stamp
            // is cleared and the job is allowed to send one for the new time.
            calendarEvent.Reminder_Sent_At = null;

            var existing = await _unitOfWork.Repository<EventAudience, Guid>()
                .GetAllQ()
                .Where(audience => audience.Event_Id == calendarEvent.Id)
                .ToListAsync(cancellationToken);

            foreach (var audience in existing)
            {
                _unitOfWork.Repository<EventAudience, Guid>().DeleteByEntity(audience);
            }
        }

        AddAudiences(calendarEvent, request);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return calendarEvent.Id;
    }

    private static void AddAudiences(Event calendarEvent, SaveEventCommand request)
    {
        var departments = request.AudienceDepartmentIds.Distinct().ToArray();
        var locations = request.AudienceLocationIds.Distinct().ToArray();

        if (departments.Length == 0 && locations.Length == 0)
        {
            calendarEvent.Audiences.Add(
                new EventAudience(calendarEvent.Id, AudienceType.AllEmployees, null));
            return;
        }

        foreach (var departmentId in departments)
        {
            calendarEvent.Audiences.Add(
                new EventAudience(calendarEvent.Id, AudienceType.Department, departmentId));
        }

        foreach (var locationId in locations)
        {
            calendarEvent.Audiences.Add(
                new EventAudience(calendarEvent.Id, AudienceType.Location, locationId));
        }
    }
}
