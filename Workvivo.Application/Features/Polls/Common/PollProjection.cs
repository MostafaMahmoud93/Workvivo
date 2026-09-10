using Workvivo.Application.Features.Polls.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Entities.Polls;

namespace Workvivo.Application.Features.Polls.Common;

/// <summary>
/// Turns a poll into what one particular person is allowed to see of it.
///
/// The visibility rule is the whole reason this is shared rather than written at each
/// call site: results are hidden until you have voted, unless the poll says otherwise
/// or it has closed. Getting that wrong in one of three places leaks the running
/// tally and changes how people vote.
/// </summary>
public static class PollProjection
{
    public static PollDto For(
        Poll poll,
        IReadOnlyCollection<PollOption> options,
        IReadOnlyCollection<Guid> myOptionIds,
        DateTime utcNow)
    {
        var hasVoted = myOptionIds.Count > 0;
        var isOpen = poll.IsOpenAt(utcNow);

        // Three ways to earn sight of the results: you voted, the poll says results
        // are open, or voting is over and there is nothing left to influence.
        var resultsVisible = hasVoted || poll.Show_Results_Before_Voting || !isOpen;

        return new PollDto
        {
            Id = poll.Id,
            PostId = poll.Post_Id,
            Question = LocalizedText.Pick(poll.Question_Ar, poll.Question_En) ?? string.Empty,
            IsMultipleChoice = poll.Is_Multiple_Choice,
            IsAnonymous = poll.Is_Anonymous,
            Status = (int)poll.Status,
            ExpiryDate = poll.Expiry_Date,
            TotalVotes = resultsVisible ? poll.Total_Votes : null,
            HasVoted = hasVoted,
            CanVote = isOpen && !hasVoted,
            ResultsVisible = resultsVisible,
            Options =
            [
                .. options
                    .OrderBy(option => option.Sort_Order)
                    .ThenBy(option => option.Id)
                    .Select(option => new PollOptionDto
                    {
                        Id = option.Id,
                        Text = LocalizedText.Pick(option.Text_Ar, option.Text_En) ?? string.Empty,
                        SortOrder = option.Sort_Order,
                        VotesCount = resultsVisible ? option.Votes_Count : null,

                        // Shown even on an anonymous poll: this is the caller's own
                        // choice, read from their own vote rows, and hiding it would
                        // mean they could not see what they picked.
                        ChosenByMe = myOptionIds.Contains(option.Id),
                    }),
            ],
        };
    }
}
