namespace Workvivo.Application.Features.Polls.Dtos;

public sealed class PollOptionDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public int SortOrder { get; init; }

    /// <summary>
    /// Null until the caller is allowed to see results.
    ///
    /// Absent rather than zero: a client cannot render "0 votes" by mistake, and the
    /// tally genuinely is not being disclosed rather than being disclosed as nothing.
    /// </summary>
    public int? VotesCount { get; init; }

    public bool ChosenByMe { get; init; }
}

public sealed class PollDto
{
    public Guid Id { get; init; }
    public Guid? PostId { get; init; }
    public string Question { get; init; } = string.Empty;
    public bool IsMultipleChoice { get; init; }
    public bool IsAnonymous { get; init; }
    public int Status { get; init; }
    public DateTime? ExpiryDate { get; init; }

    /// <summary>Null until results are visible, for the same reason as the per-option tally.</summary>
    public int? TotalVotes { get; init; }

    public bool HasVoted { get; init; }
    public bool CanVote { get; init; }
    public bool ResultsVisible { get; init; }
    public IReadOnlyList<PollOptionDto> Options { get; init; } = [];
}
