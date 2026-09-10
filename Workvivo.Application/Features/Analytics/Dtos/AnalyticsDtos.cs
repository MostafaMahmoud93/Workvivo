namespace Workvivo.Application.Features.Analytics.Dtos;

/// <summary>One headline number.</summary>
public sealed class MetricDto
{
    public required string Key { get; init; }
    public required long Value { get; init; }

    /// <summary>The same measure over the preceding window, so the number has a direction.</summary>
    public long? Previous { get; init; }
}

public sealed class TopPostDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;
    public int Views { get; init; }
    public int Reactions { get; init; }
    public int Comments { get; init; }
    public DateTime? PublishedAt { get; init; }
}

public sealed class ActivityPointDto
{
    public DateOnly Date { get; init; }
    public int Posts { get; init; }
    public int Comments { get; init; }
    public int Reactions { get; init; }
}

public sealed class AnalyticsDashboardDto
{
    public int Days { get; init; }
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<MetricDto> Metrics { get; init; } = [];
    public IReadOnlyList<TopPostDto> TopPosts { get; init; } = [];
    public IReadOnlyList<ActivityPointDto> Activity { get; init; } = [];
}
