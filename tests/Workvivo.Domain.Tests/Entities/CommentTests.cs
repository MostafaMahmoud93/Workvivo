using Shouldly;
using Workvivo.Domain.Entities.Feed;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

public class CommentTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    public void A_comment_below_the_cap_can_be_replied_to(int depth, int expectedReplyDepth)
    {
        new Comment { Depth = depth }.ReplyDepth().ShouldBe(expectedReplyDepth);
    }

    [Fact]
    public void A_comment_at_the_cap_cannot_be_replied_to()
    {
        // Unlimited nesting produces threads that are unreadable on a phone and reply
        // chains whose query cost depends on how deep somebody felt like going.
        new Comment { Depth = Comment.MaxDepth }.ReplyDepth().ShouldBeNull();
    }

    [Fact]
    public void A_comment_somehow_past_the_cap_still_refuses_replies()
    {
        // Defensive: if a bad import or an older row lands beyond the limit, the cap
        // must still hold rather than letting the thread run away.
        new Comment { Depth = Comment.MaxDepth + 5 }.ReplyDepth().ShouldBeNull();
    }

    [Fact]
    public void The_cap_is_two_levels_of_reply()
    {
        Comment.MaxDepth.ShouldBe(2);
    }
}
