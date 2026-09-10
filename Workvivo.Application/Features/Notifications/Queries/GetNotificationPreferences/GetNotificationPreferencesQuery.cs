using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Notifications.Dtos;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.RealTime;

namespace Workvivo.Application.Features.Notifications.Queries.GetNotificationPreferences;

/// <summary>
/// Every notification type and what the caller has chosen for it.
///
/// Defaults are filled in for types with no stored row, so the screen is complete on
/// first visit rather than empty.
/// </summary>
public sealed record GetNotificationPreferencesQuery : IQuery<IReadOnlyList<NotificationPreferenceDto>>;

public sealed class GetNotificationPreferencesQueryHandler
    : IRequestHandler<GetNotificationPreferencesQuery, IReadOnlyList<NotificationPreferenceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PostAuthorization _authorization;

    public GetNotificationPreferencesQueryHandler(IUnitOfWork unitOfWork, PostAuthorization authorization)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _authorization.RequireEmployeeIdAsync(cancellationToken);

        var stored = await _unitOfWork.Repository<NotificationPreference, Guid>()
            .GetAllQ()
            .Where(preference => preference.Employee_Id == employeeId)
            .ToDictionaryAsync(preference => preference.Notification_Type, cancellationToken);

        return
        [
            .. Enum.GetValues<NotificationType>()
                .Select(type =>
                {
                    var saved = stored.GetValueOrDefault(type);
                    var effective = saved ?? NotificationDefaults.For(employeeId, type);

                    return new NotificationPreferenceDto
                    {
                        Type = (int)type,
                        InApp = effective.In_App_Enabled,
                        Email = effective.Email_Enabled,
                        EmailFrequency = (int)effective.Email_Frequency,
                        IsExplicit = saved is not null,
                    };
                }),
        ];
    }
}
