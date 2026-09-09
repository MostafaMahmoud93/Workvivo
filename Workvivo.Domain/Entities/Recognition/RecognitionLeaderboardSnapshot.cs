using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Recognition;

/// <summary>
/// A precomputed leaderboard position for one employee over one period.
///
/// Rebuilt by a scheduled job rather than aggregated on request. Ranking every
/// employee by points over a date range is a full scan of the recognition table plus
/// a sort, and the leaderboard is on the home page - it would be the most expensive
/// query in the product, run most often, to produce an answer that changes hourly at
/// most.
/// </summary>
public class RecognitionLeaderboardSnapshot : BaseCommonEntity<Guid>
{
    public Guid Employee_Id { get; set; }

    public LeaderboardPeriod Period { get; set; }

    /// <summary>First day of the period this row covers.</summary>
    public DateOnly Period_Start { get; set; }

    public int Points { get; set; }
    public int Recognition_Count { get; set; }
    public int Rank { get; set; }

    /// <summary>Scope of the ranking - null for company-wide, otherwise a department.</summary>
    public Guid? Department_Id { get; set; }

    public DateTime Generated_At { get; set; }

    public virtual Employee? Employee { get; set; }
    public virtual Department? Department { get; set; }
}
