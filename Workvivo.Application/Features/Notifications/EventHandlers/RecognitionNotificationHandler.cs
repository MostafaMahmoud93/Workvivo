using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Events;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Events;

namespace Workvivo.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Tells somebody they were recognised.
///
/// One of the few notifications that defaults to email as well as in-app. Being
/// thanked by a colleague is exactly the thing a person would be sorry to miss
/// because they were not looking at the tab.
/// </summary>
public sealed class RecognitionNotificationHandler
    : INotificationHandler<DomainEventNotification<RecognitionGivenDomainEvent>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public RecognitionNotificationHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        DomainEventNotification<RecognitionGivenDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var raised = notification.DomainEvent;

        var details = await _unitOfWork
            .Repository<Domain.Entities.Recognition.Recognition, Guid>()
            .GetAllQ()
            .Where(recognition => recognition.Id == raised.RecognitionId)
            .Select(recognition => new
            {
                recognition.Message,
                Category = recognition.RecognitionType!.Name_En ?? recognition.RecognitionType.Name_Ar,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (details is null)
        {
            return;
        }

        var actorName = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == raised.SenderEmployeeId)
            .Select(employee => employee.Display_Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (actorName is null)
        {
            return;
        }

        await _dispatcher.DispatchAsync(
            new NotificationSendRequest
            {
                Type = NotificationType.Recognition,
                RecipientEmployeeIds = [raised.RecipientEmployeeId],
                ActorEmployeeId = raised.SenderEmployeeId,
                EntityType = NotificationEntityType.Recognition,
                EntityId = raised.RecognitionId,
                Copy = NotificationCopy.RecognisedYou(actorName, details.Category, details.Message),
                RedirectUrl = NotificationLinks.Recognition(raised.RecognitionId),
            },
            cancellationToken);
    }
}
