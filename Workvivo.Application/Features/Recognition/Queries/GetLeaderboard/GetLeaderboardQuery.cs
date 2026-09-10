using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Recognition.Dtos;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Recognition;

namespace Workvivo.Application.Features.Recognition.Queries.GetLeaderboard;

/// <summary>
/// The recognition leaderboard for one period.
///
/// Read from precomputed snapshots, never aggregated on request. Ranking every
/// employee by points over a date range is a scan of the recognition table plus a
/// sort - and this sits on the home page, so it would be the most expensive query in
/// the product, run most often, to produce an answer that changes hourly at most.
/// </summary>
public sealed class GetLeaderboardQuery : IQuery<LeaderboardDto>
{
    public LeaderboardPeriod Period { get; set; } = LeaderboardPeriod.Month;

    /// <summary>Scope. Null is company-wide.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>How many places to return. The rest is noise on a home page.</summary>
    public int Top { get; set; } = 10;
}

public sealed class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, LeaderboardDto>
{
    private const int MaxTop = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IDateTimeProvider _clock;

    public GetLeaderboardQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _clock = clock;
    }

    public async Task<LeaderboardDto> Handle(
        GetLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var periodStart = LeaderboardWindow.StartOf(request.Period, today);
        var top = Math.Clamp(request.Top, 1, MaxTop);

        var snapshots = _unitOfWork.Repository<RecognitionLeaderboardSnapshot, Guid>()
            .GetAllQ()
            .Where(snapshot =>
                snapshot.Period == request.Period
                && snapshot.Period_Start == periodStart
                && snapshot.Department_Id == request.DepartmentId);

        var entries = await snapshots
            .OrderBy(snapshot => snapshot.Rank)
            .Take(top)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        // The caller's own row, fetched separately because they are usually not in the
        // visible top and "where am I" is the second thing anyone looks for.
        var me = entries.FirstOrDefault(entry => entry.EmployeeId == employeeId)
            ?? await snapshots
                .Where(snapshot => snapshot.Employee_Id == employeeId)
                .Select(Projection)
                .FirstOrDefaultAsync(cancellationToken);

        var generatedAt = await snapshots
            .OrderByDescending(snapshot => snapshot.Generated_At)
            .Select(snapshot => (DateTime?)snapshot.Generated_At)
            .FirstOrDefaultAsync(cancellationToken);

        return new LeaderboardDto
        {
            Period = (int)request.Period,
            PeriodStart = periodStart,
            GeneratedAt = generatedAt,
            Entries = entries,
            Me = me,
        };
    }

    private static readonly System.Linq.Expressions.Expression<
        Func<RecognitionLeaderboardSnapshot, LeaderboardEntryDto>> Projection =
        snapshot => new LeaderboardEntryDto
        {
            Rank = snapshot.Rank,
            EmployeeId = snapshot.Employee_Id,
            DisplayName = snapshot.Employee!.Display_Name,
            JobTitle = snapshot.Employee.JobTitle == null
                ? null
                : snapshot.Employee.JobTitle.Name_En ?? snapshot.Employee.JobTitle.Name_Ar,
            ProfilePictureFileId = snapshot.Employee.Profile_Picture_File_Id,
            Points = snapshot.Points,
            RecognitionCount = snapshot.Recognition_Count,
        };
}
