using NSubstitute;
using Shouldly;
using Workvivo.Application.Common.Events;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Application.Features.Notifications.EventHandlers;
using Workvivo.Application.Tests.Support;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Events;
using Xunit;

namespace Workvivo.Application.Tests.Features.Notifications;

/// <summary>
/// One comment can concern the same person three ways at once: they wrote the post,
/// they wrote the comment being replied to, and they were mentioned in the reply.
///
/// Telling them three times is how people learn to ignore the bell, so the handler
/// claims recipients in order of specificity and each notification removes its
/// recipients from the pool. These pin that.
/// </summary>
public class CommentNotificationRoutingTests
{
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid PostAuthor = Guid.NewGuid();
    private static readonly Guid ParentAuthor = Guid.NewGuid();
    private static readonly Guid CommentId = Guid.NewGuid();
    private static readonly Guid PostId = Guid.NewGuid();

    private static (CommentAddedNotificationHandler Handler, List<NotificationSendRequest> Sent) Build()
    {
        var work = new FakeUnitOfWork()
            .With<Employee, Guid>([new Employee { Id = Author, Display_Name = "Sara", Is_Active = true }])
            .With<Comment, Guid>([new Comment { Id = CommentId, Post_Id = PostId, Content_Text = "well spotted" }]);

        var sent = new List<NotificationSendRequest>();
        var dispatcher = Substitute.For<INotificationDispatcher>();

        dispatcher
            .DispatchAsync(Arg.Any<NotificationSendRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent.Add(call.Arg<NotificationSendRequest>());
                return Task.FromResult(1);
            });

        return (new CommentAddedNotificationHandler(work.Object, dispatcher), sent);
    }

    private static Task HandleAsync(
        CommentAddedNotificationHandler handler,
        Guid postAuthor,
        Guid? parentAuthor,
        params Guid[] mentioned) =>
        handler.Handle(
            new DomainEventNotification<CommentAddedDomainEvent>(new CommentAddedDomainEvent(
                CommentId,
                PostId,
                Author,
                postAuthor,
                parentAuthor is null ? null : Guid.NewGuid(),
                parentAuthor,
                mentioned,
                DateTime.UtcNow)),
            CancellationToken.None);

    [Fact]
    public async Task A_plain_comment_tells_the_post_author()
    {
        var (handler, sent) = Build();

        await HandleAsync(handler, PostAuthor, parentAuthor: null);

        var request = sent.ShouldHaveSingleItem();
        request.Type.ShouldBe(NotificationType.PostComment);
        request.RecipientEmployeeIds.ShouldBe([PostAuthor]);
    }

    [Fact]
    public async Task A_reply_tells_both_the_parent_author_and_the_post_author()
    {
        var (handler, sent) = Build();

        await HandleAsync(handler, PostAuthor, ParentAuthor);

        sent.Count.ShouldBe(2);
        sent.ShouldContain(request => request.Type == NotificationType.CommentReply);
        sent.ShouldContain(request => request.Type == NotificationType.PostComment);
    }

    [Fact]
    public async Task Somebody_who_is_both_mentioned_and_the_post_author_is_told_once()
    {
        // The overlap that matters. Mention wins because it is the most specific thing
        // that happened to them.
        var (handler, sent) = Build();

        await HandleAsync(handler, PostAuthor, parentAuthor: null, mentioned: PostAuthor);

        var request = sent.ShouldHaveSingleItem();
        request.Type.ShouldBe(NotificationType.Mention);
        request.RecipientEmployeeIds.ShouldBe([PostAuthor]);
    }

    [Fact]
    public async Task Somebody_who_is_all_three_at_once_is_told_once()
    {
        var (handler, sent) = Build();

        await HandleAsync(handler, PostAuthor, parentAuthor: PostAuthor, mentioned: PostAuthor);

        var request = sent.ShouldHaveSingleItem();
        request.Type.ShouldBe(NotificationType.Mention);
    }

    [Fact]
    public async Task Commenting_on_your_own_post_tells_nobody()
    {
        var (handler, sent) = Build();

        await HandleAsync(handler, postAuthor: Author, parentAuthor: null);

        sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Mentioning_yourself_in_your_own_comment_tells_nobody()
    {
        var (handler, sent) = Build();

        await HandleAsync(handler, postAuthor: Author, parentAuthor: null, mentioned: Author);

        sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Every_notification_links_to_the_comment_rather_than_the_post()
    {
        // Landing on a post with four hundred comments and being left to find the one
        // that was about you is the difference between a useful notification and a
        // frustrating one.
        var (handler, sent) = Build();

        await HandleAsync(handler, PostAuthor, ParentAuthor, mentioned: Guid.NewGuid());

        sent.Count.ShouldBe(3);
        sent.ShouldAllBe(request => request.RedirectUrl!.Contains(CommentId.ToString("D")));
    }
}
