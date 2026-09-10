using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.Recognition;

namespace Workvivo.Application.Features.Recognition.Jobs;

/// <summary>Rebuilds the precomputed leaderboards.</summary>
public interface ILeaderboardSnapshotJob
{
    Task RebuildAsync();
}

/// <summary>
/// Aggregates recognition into leaderboard snapshots.
///
/// Runs hourly. The alternative - ranking on demand - is a scan and a sort of the
/// recognition table on every home-page load, to answer a question whose answer
/// barely moves.
///
/// Only the current period is rebuilt. Past periods are finished: recomputing them
/// every hour forever would make the job's cost grow with the age of the system, and
/// a closed month's leaderboard should not change anyway.
/// </summary>
public sealed class LeaderboardSnapshotJob : ILeaderboardSnapshotJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<LeaderboardSnapshotJob> _logger;

    public LeaderboardSnapshotJob(
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<LeaderboardSnapshotJob> logger)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task RebuildAsync()
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var written = 0;

        foreach (var period in new[]
        {
            LeaderboardPeriod.Month,
            LeaderboardPeriod.Quarter,
            LeaderboardPeriod.Year,
            LeaderboardPeriod.AllTime,
        })
        {
            written += await RebuildPeriodAsync(period, today);
        }

        _logger.LogInformation("Rebuilt {Count} leaderboard snapshot rows", written);
    }

    private async Task<int> RebuildPeriodAsync(LeaderboardPeriod period, DateOnly today)
    {
        var start = LeaderboardWindow.StartOf(period, today);
        var end = LeaderboardWindow.EndOf(period, today);

        var from = start.ToDateTime(TimeOnly.MinValue);
        var until = end.ToDateTime(TimeOnly.MinValue);

        // Private recognition is excluded from the ranking.
        //
        // A leaderboard is public by definition, so counting private recognition would
        // publish through the ranking exactly what the sender chose to keep between
        // themselves and the recipient - the total would move and the reason would be
        // invisible.
        var totals = await _unitOfWork.Repository<Domain.Entities.Recognition.Recognition, Guid>()
            .GetAllQ()
            .Where(recognition =>
                recognition.Recognised_On >= from
                && recognition.Recognised_On < until
                && recognition.Visibility != RecognitionVisibility.Private)
            .GroupBy(recognition => recognition.Recipient_Employee_Id)
            .Select(group => new
            {
                EmployeeId = group.Key,
                Points = group.Sum(recognition => recognition.Points),
                Count = group.Count(),
            })
            .ToListAsync();

        var snapshots = _unitOfWork.Repository<RecognitionLeaderboardSnapshot, Guid>();

        // Cleared and rewritten rather than merged. The set of ranked people changes
        // between runs, and a merge leaves last hour's leavers sitting in the table
        // with a stale rank.
        await snapshots.GetAllQ()
            .Where(snapshot => snapshot.Period == period && snapshot.Period_Start == start)
            .ExecuteDeleteAsync();

        if (totals.Count == 0)
        {
            await _unitOfWork.SaveChangesAsync();
            return 0;
        }

        var departments = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => totals.Select(total => total.EmployeeId).Contains(employee.Id))
            .Select(employee => new { employee.Id, employee.Department_Id })
            .ToDictionaryAsync(employee => employee.Id, employee => employee.Department_Id);

        var generatedAt = _clock.UtcNow;
        var written = 0;

        // Company-wide, then one ranking per department. Both are stored so the
        // department view is a lookup rather than a re-rank at read time.
        written += await WriteRankingAsync(
            snapshots, totals.Select(t => (t.EmployeeId, t.Points, t.Count)),
            period, start, null, generatedAt);

        var byDepartment = totals
            .Where(total => departments.GetValueOrDefault(total.EmployeeId) is not null)
            .GroupBy(total => departments[total.EmployeeId]!.Value);

        foreach (var group in byDepartment)
        {
            written += await WriteRankingAsync(
                snapshots, group.Select(t => (t.EmployeeId, t.Points, t.Count)),
                period, start, group.Key, generatedAt);
        }

        await _unitOfWork.SaveChangesAsync();

        return written;
    }

    /// <summary>
    /// Assigns ranks and writes the rows.
    ///
    /// Ties share a rank and the next one skips, which is how people expect a
    /// leaderboard to read - two in second place means nobody is third.
    /// </summary>
    private static async Task<int> WriteRankingAsync(
        IBaseRepository<RecognitionLeaderboardSnapshot, Guid> snapshots,
        IEnumerable<(Guid EmployeeId, int Points, int Count)> totals,
        LeaderboardPeriod period,
        DateOnly periodStart,
        Guid? departmentId,
        DateTime generatedAt)
    {
        var ordered = totals
            .OrderByDescending(total => total.Points)
            .ThenByDescending(total => total.Count)
            .ThenBy(total => total.EmployeeId)
            .ToList();

        var rank = 0;
        var placed = 0;
        var previousPoints = int.MinValue;

        foreach (var total in ordered)
        {
            placed++;

            if (total.Points != previousPoints)
            {
                rank = placed;
                previousPoints = total.Points;
            }

            await snapshots.AddAsync(new RecognitionLeaderboardSnapshot
            {
                Id = Guid.NewGuid(),
                Employee_Id = total.EmployeeId,
                Period = period,
                Period_Start = periodStart,
                Department_Id = departmentId,
                Points = total.Points,
                Recognition_Count = total.Count,
                Rank = rank,
                Generated_At = generatedAt,
                Is_Deleted = false,
            });
        }

        return ordered.Count;
    }
}
