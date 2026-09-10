using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.RealTime;

namespace Workvivo.Application.Features.Notifications.Commands.UpdateNotificationPreferences;

/// <summary>One row of the preferences screen, as the client sends it back.</summary>
public sealed record NotificationPreferenceInput(
    NotificationType Type,
    bool InApp,
    bool Email,
    NotificationDigestFrequency EmailFrequency);

/// <summary>
/// Saves the caller's own notification preferences.
///
/// Always the caller's. There is no employee id in the request, so an administrator
/// cannot silently switch somebody else's notifications off and nobody can switch
/// their colleague's on.
/// </summary>
public sealed record UpdateNotificationPreferencesCommand(
    IReadOnlyList<NotificationPreferenceInput> Preferences) : ICommand<int>;

public sealed class UpdateNotificationPreferencesCommandValidator
    : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.Preferences).NotEmpty();

        RuleForEach(x => x.Preferences).ChildRules(preference =>
        {
            // Enum.IsDefined rather than a range check: an out-of-range int cast to an
            // enum is not a validation error in C#, it is a value that silently means
            // nothing and then falls through every switch in the system.
            preference.RuleFor(p => p.Type).Must(Enum.IsDefined)
                .WithMessage("Unknown notification type.");

            preference.RuleFor(p => p.EmailFrequency).Must(Enum.IsDefined)
                .WithMessage("Unknown digest frequency.");
        });
    }
}

public sealed class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PostAuthorization _authorization;

    public UpdateNotificationPreferencesCommandHandler(
        IUnitOfWork unitOfWork,
        PostAuthorization authorization)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
    }

    public async Task<int> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _authorization.RequireEmployeeIdAsync(cancellationToken);
        var repository = _unitOfWork.Repository<NotificationPreference, Guid>();

        var incoming = request.Preferences
            .GroupBy(preference => preference.Type)
            .ToDictionary(group => group.Key, group => group.Last());

        var types = incoming.Keys.ToArray();

        var existing = await repository
            .GetAllQ()
            .Where(preference =>
                preference.Employee_Id == employeeId
                && types.Contains(preference.Notification_Type))
            .ToListAsync(cancellationToken);

        var byType = existing.ToDictionary(preference => preference.Notification_Type);

        foreach (var (type, input) in incoming)
        {
            if (byType.TryGetValue(type, out var row))
            {
                row.In_App_Enabled = input.InApp;
                row.Email_Enabled = input.Email;
                row.Email_Frequency = input.EmailFrequency;
                continue;
            }

            // A row is written the first time somebody changes something, which is why
            // the table stays proportional to the people who care rather than to
            // headcount times notification types.
            await repository.AddAsync(new NotificationPreference
            {
                Id = Guid.NewGuid(),
                Employee_Id = employeeId,
                Notification_Type = type,
                In_App_Enabled = input.InApp,
                Email_Enabled = input.Email,
                Email_Frequency = input.EmailFrequency,
                Push_Enabled = false,
                Is_Deleted = false,
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return incoming.Count;
    }
}
