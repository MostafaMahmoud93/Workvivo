using Shouldly;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.Feed;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// Mirrors the predicate the feed query uses. If the two ever disagree, a post is
/// either withheld from people entitled to see it or shown before it was meant to be.
/// </summary>
public class PostVisibilityTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    private static Post Published(DateTime publishedAt) => new()
    {
        Status = PostStatus.Published,
        Published_Date = publishedAt,
    };

    [Fact]
    public void A_published_post_whose_time_has_passed_is_visible()
    {
        Published(Now.AddHours(-1)).IsVisibleAt(Now).ShouldBeTrue();
    }

    [Fact]
    public void A_post_published_exactly_now_is_visible()
    {
        Published(Now).IsVisibleAt(Now).ShouldBeTrue();
    }

    [Fact]
    public void A_future_dated_post_is_not_visible_yet()
    {
        Published(Now.AddMinutes(1)).IsVisibleAt(Now).ShouldBeFalse();
    }

    [Theory]
    [InlineData(PostStatus.Draft)]
    [InlineData(PostStatus.Scheduled)]
    [InlineData(PostStatus.Archived)]
    public void Only_published_posts_are_visible(PostStatus status)
    {
        var post = Published(Now.AddHours(-1));
        post.Status = status;

        post.IsVisibleAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void A_published_post_with_no_publish_date_is_not_visible()
    {
        // An inconsistent row - published status, no timestamp - has no place in an
        // ordered timeline, so it is withheld rather than sorted arbitrarily.
        new Post { Status = PostStatus.Published, Published_Date = null }
            .IsVisibleAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void A_soft_deleted_post_is_not_visible()
    {
        var post = Published(Now.AddHours(-1));
        post.Is_Deleted = true;

        post.IsVisibleAt(Now).ShouldBeFalse();
    }
}
