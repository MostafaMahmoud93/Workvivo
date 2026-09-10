namespace Workvivo.Application.Features.Recognition.Dtos;

/// <summary>A recognition category, as offered in the picker.</summary>
public sealed class RecognitionTypeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BadgeIcon { get; init; }
    public string? BadgeColor { get; init; }
    public int DefaultPoints { get; init; }
}

/// <summary>One act of recognition, as shown on the wall or a profile.</summary>
public sealed class RecognitionDto
{
    public Guid Id { get; init; }
    public Guid SenderEmployeeId { get; init; }
    public string SenderDisplayName { get; init; } = string.Empty;
    public Guid RecipientEmployeeId { get; init; }
    public string RecipientDisplayName { get; init; } = string.Empty;
    public Guid RecognitionTypeId { get; init; }
    public string RecognitionTypeName { get; init; } = string.Empty;
    public string? BadgeIcon { get; init; }
    public string? BadgeColor { get; init; }
    public string Message { get; init; } = string.Empty;
    public int Points { get; init; }
    public int Visibility { get; init; }
    public DateTime RecognisedOn { get; init; }
}

/// <summary>One row of the leaderboard.</summary>
public sealed class LeaderboardEntryDto
{
    public int Rank { get; init; }
    public Guid EmployeeId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? JobTitle { get; init; }
    public Guid? ProfilePictureFileId { get; init; }
    public int Points { get; init; }
    public int RecognitionCount { get; init; }
}

/// <summary>The leaderboard plus the metadata a reader needs to trust it.</summary>
public sealed class LeaderboardDto
{
    public int Period { get; init; }
    public DateOnly PeriodStart { get; init; }

    /// <summary>
    /// When the snapshot was built. Shown to the reader: a leaderboard that is a few
    /// hours stale is fine, a leaderboard that looks live and is not is misleading.
    /// </summary>
    public DateTime? GeneratedAt { get; init; }

    public IReadOnlyList<LeaderboardEntryDto> Entries { get; init; } = [];

    /// <summary>The caller's own position, even when it falls outside the visible top.</summary>
    public LeaderboardEntryDto? Me { get; init; }
}
