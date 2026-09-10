using Shouldly;
using Workvivo.Application.Features.Polls.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.Polls;
using Xunit;

namespace Workvivo.Application.Tests.Features.Polls;

/// <summary>
/// When a poll's running tally becomes visible is a behavioural decision, not a
/// cosmetic one: showing results before somebody votes changes how they vote. The
/// rule is applied in three places, so it lives in one.
/// </summary>
public class PollProjectionTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Poll Poll(
        bool showResultsEarly = false,
        PollStatus status = PollStatus.Open,
        DateTime? expiry = null) => new()
    {
        Id = Guid.NewGuid(),
        Question_En = "Where should lunch be?",
        Status = status,
        Show_Results_Before_Voting = showResultsEarly,
        Expiry_Date = expiry,
        Total_Votes = 12,
    };

    private static PollOption[] Options(Guid pollId) =>
    [
        new() { Id = Guid.NewGuid(), Poll_Id = pollId, Text_En = "A", Sort_Order = 0, Votes_Count = 7 },
        new() { Id = Guid.NewGuid(), Poll_Id = pollId, Text_En = "B", Sort_Order = 1, Votes_Count = 5 },
    ];

    [Fact]
    public void Results_are_hidden_from_somebody_who_has_not_voted()
    {
        var poll = Poll();
        var options = Options(poll.Id);

        var dto = PollProjection.For(poll, options, [], Now);

        dto.ResultsVisible.ShouldBeFalse();
        dto.CanVote.ShouldBeTrue();

        // Absent rather than zero. A client cannot render "0 votes" by mistake, and
        // the tally genuinely is not being disclosed rather than disclosed as nothing.
        dto.TotalVotes.ShouldBeNull();
        dto.Options.ShouldAllBe(option => option.VotesCount == null);
    }

    [Fact]
    public void Voting_earns_sight_of_the_results()
    {
        var poll = Poll();
        var options = Options(poll.Id);

        var dto = PollProjection.For(poll, options, [options[0].Id], Now);

        dto.ResultsVisible.ShouldBeTrue();
        dto.HasVoted.ShouldBeTrue();
        dto.CanVote.ShouldBeFalse();
        dto.TotalVotes.ShouldBe(12);
        dto.Options[0].VotesCount.ShouldBe(7);
    }

    [Fact]
    public void A_poll_can_choose_to_show_results_before_voting()
    {
        var poll = Poll(showResultsEarly: true);

        var dto = PollProjection.For(poll, Options(poll.Id), [], Now);

        dto.ResultsVisible.ShouldBeTrue();
        dto.CanVote.ShouldBeTrue();
    }

    [Fact]
    public void A_closed_poll_shows_its_results_to_everybody()
    {
        // Nothing is left to influence once voting is over, and hiding the outcome
        // from the people who did not vote serves no purpose.
        var poll = Poll(status: PollStatus.Closed);

        var dto = PollProjection.For(poll, Options(poll.Id), [], Now);

        dto.ResultsVisible.ShouldBeTrue();
        dto.CanVote.ShouldBeFalse();
    }

    [Fact]
    public void An_expired_poll_cannot_be_voted_in()
    {
        var poll = Poll(expiry: Now.AddMinutes(-1));

        var dto = PollProjection.For(poll, Options(poll.Id), [], Now);

        dto.CanVote.ShouldBeFalse();
        dto.ResultsVisible.ShouldBeTrue();
    }

    [Fact]
    public void A_poll_that_has_not_started_cannot_be_voted_in()
    {
        var poll = Poll();
        poll.Start_Date = Now.AddHours(1);

        PollProjection.For(poll, Options(poll.Id), [], Now).CanVote.ShouldBeFalse();
    }

    [Fact]
    public void The_voters_own_choice_is_shown_back_to_them_even_when_anonymous()
    {
        // Read from their own vote rows. Hiding it would mean somebody could not see
        // what they picked, which is not what anonymity is protecting.
        var poll = Poll();
        poll.Is_Anonymous = true;
        var options = Options(poll.Id);

        var dto = PollProjection.For(poll, options, [options[1].Id], Now);

        dto.Options[0].ChosenByMe.ShouldBeFalse();
        dto.Options[1].ChosenByMe.ShouldBeTrue();
    }

    [Fact]
    public void Options_come_back_in_their_declared_order()
    {
        var poll = Poll();

        var shuffled = new PollOption[]
        {
            new() { Id = Guid.NewGuid(), Poll_Id = poll.Id, Text_En = "second", Sort_Order = 1 },
            new() { Id = Guid.NewGuid(), Poll_Id = poll.Id, Text_En = "first", Sort_Order = 0 },
        };

        var dto = PollProjection.For(poll, shuffled, [], Now);

        dto.Options[0].Text.ShouldBe("first");
        dto.Options[1].Text.ShouldBe("second");
    }
}
