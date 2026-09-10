using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.Domain.Entities.Recognition;

/// <summary>
/// The date range a leaderboard period covers.
///
/// In the domain rather than in the job, because two things have to agree about it:
/// the job that writes snapshots and the query that reads them. If they disagree by a
/// day, the leaderboard silently shows the previous period and nobody can explain why
/// their recognition is missing.
/// </summary>
public static class LeaderboardWindow
{
    /// <summary>
    /// Earliest date treated as "all time".
    ///
    /// A real date rather than <see cref="DateOnly.MinValue"/>: the value is stored in
    /// a unique index key, and year 1 in a date column is the kind of sentinel that
    /// eventually gets rendered to a user.
    /// </summary>
    public static readonly DateOnly Epoch = new(2000, 1, 1);

    /// <summary>The first day of the period containing <paramref name="on"/>.</summary>
    public static DateOnly StartOf(LeaderboardPeriod period, DateOnly on) => period switch
    {
        LeaderboardPeriod.Month => new DateOnly(on.Year, on.Month, 1),
        LeaderboardPeriod.Quarter => new DateOnly(on.Year, (((on.Month - 1) / 3) * 3) + 1, 1),
        LeaderboardPeriod.Year => new DateOnly(on.Year, 1, 1),
        _ => Epoch,
    };

    /// <summary>The day after the period ends - an exclusive upper bound.</summary>
    public static DateOnly EndOf(LeaderboardPeriod period, DateOnly on)
    {
        var start = StartOf(period, on);

        return period switch
        {
            LeaderboardPeriod.Month => start.AddMonths(1),
            LeaderboardPeriod.Quarter => start.AddMonths(3),
            LeaderboardPeriod.Year => start.AddYears(1),

            // Exclusive, and far enough out that nothing dated today falls outside it.
            _ => new DateOnly(9999, 12, 31),
        };
    }
}
