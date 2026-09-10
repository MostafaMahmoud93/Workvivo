using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Events;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Application.Features.Notifications.Jobs;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Events;

namespace Workvivo.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Handles a post becoming visible.
///
/// Two very different jobs, which is why the split is made here rather than inside the
/// dispatcher. Mentions address a handful of named people and are sent inline. An
/// official announcement addresses whoever the audience rules select - potentially
/// everybody - and is handed to a background job that pages through them, because
/// materialising a hundred thousand employee ids inside a web request is how a feature
/// like this takes the site down on its first company-wide announcement.
/// </summary>
public sealed class PostPublishedNotificationHandler
    : INotificationHandler<DomainEventNotification<PostPublishedDomainEvent>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IBackgroundJobScheduler _jobs;

    public PostPublishedNotificationHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        IBackgroundJobScheduler jobs)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _jobs = jobs;
    }

    public async Task Handle(
        DomainEventNotification<PostPublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var raised = notification.DomainEvent;

        if (raised.MentionedEmployeeIds.Count > 0)
        {
            var context = await LoadAsync(raised, cancellationToken);

            if (context is not null)
            {
                await _dispatcher.DispatchAsync(
                    new NotificationSendRequest
                    {
                        Type = NotificationType.Mention,
                        RecipientEmployeeIds = raised.MentionedEmployeeIds,
                        ActorEmployeeId = raised.AuthorEmployeeId,
                        EntityType = NotificationEntityType.Post,
                        EntityId = raised.PostId,
                        Copy = NotificationCopy.MentionedYouInAPost(context.ActorName, context.Excerpt),
                        RedirectUrl = NotificationLinks.Post(raised.PostId),
                    },
                    cancellationToken);
            }
        }

        if (raised.IsOfficial)
        {
            _jobs.Enqueue<IAnnouncementFanOutJob>(job => job.FanOutAsync(raised.PostId));
        }
    }

    private async Task<PostContext?> LoadAsync(
        PostPublishedDomainEvent raised,
        CancellationToken cancellationToken)
    {
        var actorName = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == raised.AuthorEmployeeId)
            .Select(employee => employee.Display_Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (actorName is null)
        {
            return null;
        }

        var excerpt = await _unitOfWork.Repository<Post, Guid>()
            .GetAllQ()
            .Where(post => post.Id == raised.PostId)
            .Select(post => post.Content_Text)
            .FirstOrDefaultAsync(cancellationToken);

        return new PostContext(actorName, excerpt);
    }

    private sealed record PostContext(string ActorName, string? Excerpt);
}
