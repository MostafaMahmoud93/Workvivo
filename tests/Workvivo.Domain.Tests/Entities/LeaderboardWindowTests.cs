using Shouldly;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.Recognition;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// Two things have to agree about what a leaderboard period covers: the job that
/// writes the snapshots and the query that reads them. If they disagree by a day, the
/// board silently shows the previous period and nobody can explain why their
/// recognition is missing - which is why the window is domain code with tests rather
/// than a date expression written twice.
/// </summary>
public class LeaderboardWindowTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 4)]
    [InlineData(9, 7)]
    [InlineData(12, 10)]
    public void A_quarter_starts_on_the_first_month_of_that_quarter(int month, int expectedMonth)
    {
        var start = LeaderboardWindow.StartOf(LeaderboardPeriod.Quarter, new DateOnly(2026, month, 17));

        start.ShouldBe(new DateOnly(2026, expectedMonth, 1));
    }

    [Fact]
    public void A_month_starts_on_the_first()
    {
        LeaderboardWindow.StartOf(LeaderboardPeriod.Month, new DateOnly(2026, 3, 31))
            .ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public void A_year_starts_in_january()
    {
        LeaderboardWindow.StartOf(LeaderboardPeriod.Year, new DateOnly(2026, 12, 31))
            .ShouldBe(new DateOnly(2026, 1, 1));
    }

    [Theory]
    [InlineData(LeaderboardPeriod.Month)]
    [InlineData(LeaderboardPeriod.Quarter)]
    [InlineData(LeaderboardPeriod.Year)]
    [InlineData(LeaderboardPeriod.AllTime)]
    public void The_end_is_exclusive_and_after_the_start(LeaderboardPeriod period)
    {
        var on = new DateOnly(2026, 5, 20);

        var start = LeaderboardWindow.StartOf(period, on);
        var end = LeaderboardWindow.EndOf(period, on);

        // Exclusive upper bound, so a recognition dated on the last day of a period
        // falls inside it and a recognition dated on the first day of the next does
        // not - the boundary that a `<=` would get wrong for one day every month.
        end.ShouldBeGreaterThan(start);
        end.ShouldBeGreaterThan(on);
    }

    [Fact]
    public void December_rolls_into_the_following_january()
    {
        LeaderboardWindow.EndOf(LeaderboardPeriod.Month, new DateOnly(2026, 12, 5))
            .ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public void The_fourth_quarter_rolls_into_the_following_year()
    {
        LeaderboardWindow.EndOf(LeaderboardPeriod.Quarter, new DateOnly(2026, 11, 5))
            .ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public void All_time_uses_a_real_date_rather_than_the_minimum_value()
    {
        // The value ends up in a unique index key and could be rendered to a user.
        // Year 1 is the kind of sentinel that eventually shows up on a screen.
        var start = LeaderboardWindow.StartOf(LeaderboardPeriod.AllTime, new DateOnly(2026, 5, 20));

        start.ShouldBe(LeaderboardWindow.Epoch);
        start.ShouldBeGreaterThan(DateOnly.MinValue);
    }

    [Fact]
    public void All_time_covers_today()
    {
        var today = new DateOnly(2026, 5, 20);

        LeaderboardWindow.StartOf(LeaderboardPeriod.AllTime, today).ShouldBeLessThan(today);
        LeaderboardWindow.EndOf(LeaderboardPeriod.AllTime, today).ShouldBeGreaterThan(today);
    }
}
