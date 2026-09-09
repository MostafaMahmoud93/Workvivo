namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>
/// How public a recognition is. Not everyone wants praise on the company feed, and
/// some recognitions are deliberately between two people.
/// </summary>
public enum RecognitionVisibility
{
    /// <summary>Appears on the company feed as a post.</summary>
    Public = 0,

    /// <summary>Visible to the recipient's department only.</summary>
    Department = 1,

    /// <summary>Visible to the sender, the recipient and their managers.</summary>
    Private = 2,
}

/// <summary>Window a leaderboard snapshot covers.</summary>
public enum LeaderboardPeriod
{
    Month = 0,
    Quarter = 1,
    Year = 2,
    AllTime = 3,
}
