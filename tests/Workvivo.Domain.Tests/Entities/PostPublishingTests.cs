using Shouldly;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Events;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// Publishing is the moment a post reaches people, and three separate code paths reach
/// it - the composer, a moderator's publish button, and the scheduler. These pin the
/// rules they all have to share.
/// </summary>
public class PostPublishingTests
{
    private static Post Draft() => new()
    {
        Id = Guid.NewGuid(),
        Author_Employee_Id = Guid.NewGuid(),
        Status = PostStatus.Draft,
    };

    [Fact]
    public void Publishing_a_draft_makes_it_visible_and_announces_it()
    {
        var post = Draft();
        var now = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

        post.Publish(now).ShouldBeTrue();

        post.Status.ShouldBe(PostStatus.Published);
        post.Published_Date.ShouldBe(now);
        post.DomainEvents.Count.ShouldBe(1);
        post.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PostPublishedDomainEvent>();
    }

    [Fact]
    public void Publishing_twice_changes_nothing_and_tells_nobody_again()
    {
        // The failure this prevents is unpleasant and plausible: a double-click on
        // "publish", or a retried job, moving a week-old post back to the top of every
        // employee's feed and notifying the whole audience a second time.
        var post = Draft();
        var first = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

        post.Publish(first);
        post.ClearDomainEvents();

        post.Publish(first.AddDays(7)).ShouldBeFalse();

        post.Published_Date.ShouldBe(first);
        post.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Publishing_a_scheduled_post_clears_the_schedule()
    {
        // Left set, the scheduler would pick the post up again on its next run.
        var post = Draft();
        post.Status = PostStatus.Scheduled;
        post.Scheduled_Publish_Date = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

        post.Publish(post.Scheduled_Publish_Date.Value);

        post.Scheduled_Publish_Date.ShouldBeNull();
    }

    [Fact]
    public void The_publish_event_carries_the_mentions_it_was_given()
    {
        var post = Draft();
        var mentioned = new[] { Guid.NewGuid(), Guid.NewGuid() };

        post.Publish(DateTime.UtcNow, mentioned);

        var raised = post.DomainEvents.OfType<PostPublishedDomainEvent>().ShouldHaveSingleItem();
        raised.MentionedEmployeeIds.ShouldBe(mentioned);
    }

    [Fact]
    public void The_publish_event_falls_back_to_the_stored_mentions()
    {
        // What the scheduler relies on: it loads the post with its mention rows and has
        // no separate list to hand over.
        var post = Draft();
        var mentionedEmployee = Guid.NewGuid();

        post.Mentions.Add(new PostMention
        {
            Id = Guid.NewGuid(),
            Post_Id = post.Id,
            Mentioned_Employee_Id = mentionedEmployee,
        });

        post.Publish(DateTime.UtcNow);

        var raised = post.DomainEvents.OfType<PostPublishedDomainEvent>().ShouldHaveSingleItem();
        raised.MentionedEmployeeIds.ShouldBe([mentionedEmployee]);
    }

    [Fact]
    public void A_reaction_is_recorded_with_the_author_who_should_hear_about_it()
    {
        var post = Draft();
        var actor = Guid.NewGuid();

        post.RecordReaction(actor, ReactionType.Celebrate, DateTime.UtcNow);

        var raised = post.DomainEvents.OfType<PostReactedDomainEvent>().ShouldHaveSingleItem();
        raised.ActorEmployeeId.ShouldBe(actor);
        raised.PostAuthorEmployeeId.ShouldBe(post.Author_Employee_Id);
        raised.Reaction.ShouldBe(ReactionType.Celebrate);
    }

    [Fact]
    public void Draining_events_is_what_clears_them()
    {
        // The pipeline drains after the commit. If clearing did not happen, a handler
        // that saves twice would publish the same event twice - two notifications for
        // one comment.
        var post = Draft();
        post.Publish(DateTime.UtcNow);

        post.DomainEvents.ShouldNotBeEmpty();
        post.ClearDomainEvents();
        post.DomainEvents.ShouldBeEmpty();
    }
}
