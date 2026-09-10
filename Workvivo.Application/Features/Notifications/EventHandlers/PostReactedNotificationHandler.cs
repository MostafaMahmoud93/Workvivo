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
/// Tells an author that somebody reacted to their post.
///
/// The noisiest notification in the product, and the one whose defaults matter most:
/// in-app yes, email no. See <c>NotificationDefaults</c>.
/// </summary>
public sealed class PostReactedNotificationHandler
    : INotificationHandler<DomainEventNotification<PostReactedDomainEvent>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public PostReactedNotificationHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        DomainEventNotification<PostReactedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var raised = notification.DomainEvent;

        // Cheapest possible exit, before any query: reacting to your own post is the
        // single most common case that produces nothing.
        if (raised.ActorEmployeeId == raised.PostAuthorEmployeeId)
        {
            return;
        }

        var actorName = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == raised.ActorEmployeeId)
            .Select(employee => employee.Display_Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (actorName is null)
        {
            return;
        }

        await _dispatcher.DispatchAsync(
            new NotificationSendRequest
            {
                Type = NotificationType.Reaction,
                RecipientEmployeeIds = [raised.PostAuthorEmployeeId],
                ActorEmployeeId = raised.ActorEmployeeId,
                EntityType = NotificationEntityType.Post,
                EntityId = raised.PostId,
                Copy = NotificationCopy.ReactedToYourPost(actorName, raised.Reaction),
                RedirectUrl = NotificationLinks.Post(raised.PostId),
            },
            cancellationToken);
    }
}
