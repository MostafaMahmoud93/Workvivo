using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Events;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Events;

namespace Workvivo.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Tells the people a new comment concerns.
///
/// Three audiences overlap here, and the overlap is the interesting part. Somebody can
/// be the post's author, the parent comment's author and mentioned in the reply, all at
/// once - and they should be told once, by the most specific of the three. A person who
/// gets "Sara commented on your post" and "Sara replied to your comment" and "Sara
/// mentioned you" for one comment learns to ignore the bell.
/// </summary>
public sealed class CommentAddedNotificationHandler
    : INotificationHandler<DomainEventNotification<CommentAddedDomainEvent>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public CommentAddedNotificationHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        DomainEventNotification<CommentAddedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var raised = notification.DomainEvent;

        var context = await LoadAsync(raised, cancellationToken);

        if (context is null)
        {
            return;
        }

        // Claimed in order of specificity. Each notification removes its recipients
        // from the pool, so the lower-priority ones cannot re-address the same person.
        var alreadyTold = new HashSet<Guid> { raised.AuthorEmployeeId };

        var mentioned = raised.MentionedEmployeeIds.Where(alreadyTold.Add).ToArray();

        if (mentioned.Length > 0)
        {
            await _dispatcher.DispatchAsync(
                new NotificationSendRequest
                {
                    Type = NotificationType.Mention,
                    RecipientEmployeeIds = mentioned,
                    ActorEmployeeId = raised.AuthorEmployeeId,
                    EntityType = NotificationEntityType.Comment,
                    EntityId = raised.CommentId,
                    Copy = NotificationCopy.MentionedYouInAComment(context.ActorName, context.Excerpt),
                    RedirectUrl = NotificationLinks.Comment(raised.PostId, raised.CommentId),
                },
                cancellationToken);
        }

        if (raised.ParentAuthorEmployeeId is { } parentAuthor && alreadyTold.Add(parentAuthor))
        {
            await _dispatcher.DispatchAsync(
                new NotificationSendRequest
                {
                    Type = NotificationType.CommentReply,
                    RecipientEmployeeIds = [parentAuthor],
                    ActorEmployeeId = raised.AuthorEmployeeId,
                    EntityType = NotificationEntityType.Comment,
                    EntityId = raised.CommentId,
                    Copy = NotificationCopy.RepliedToYourComment(context.ActorName, context.Excerpt),
                    RedirectUrl = NotificationLinks.Comment(raised.PostId, raised.CommentId),
                },
                cancellationToken);
        }

        if (alreadyTold.Add(raised.PostAuthorEmployeeId))
        {
            await _dispatcher.DispatchAsync(
                new NotificationSendRequest
                {
                    Type = NotificationType.PostComment,
                    RecipientEmployeeIds = [raised.PostAuthorEmployeeId],
                    ActorEmployeeId = raised.AuthorEmployeeId,
                    EntityType = NotificationEntityType.Comment,
                    EntityId = raised.CommentId,
                    Copy = NotificationCopy.CommentedOnYourPost(context.ActorName, context.Excerpt),
                    RedirectUrl = NotificationLinks.Comment(raised.PostId, raised.CommentId),
                },
                cancellationToken);
        }
    }

    /// <summary>
    /// The actor's name and the comment's text, in one round trip each.
    ///
    /// <c>Content_Text</c>, not <c>Content_Html</c>. The excerpt ends up in an email
    /// body and in a push payload, and putting markup through either is how a
    /// sanitised post becomes an unsanitised notification.
    /// </summary>
    private async Task<CommentContext?> LoadAsync(
        CommentAddedDomainEvent raised,
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

        var excerpt = await _unitOfWork.Repository<Comment, Guid>()
            .GetAllQ()
            .Where(comment => comment.Id == raised.CommentId)
            .Select(comment => comment.Content_Text)
            .FirstOrDefaultAsync(cancellationToken);

        return new CommentContext(actorName, excerpt);
    }

    private sealed record CommentContext(string ActorName, string? Excerpt);
}
