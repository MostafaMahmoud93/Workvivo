namespace Workvivo.Application.Features.Search.Dtos;

/// <summary>What a search result points at.</summary>
public enum SearchResultKind
{
    Employee = 0,
    Post = 1,
    Community = 2,
    Document = 3,
    Event = 4,
}

public sealed class SearchResultDto
{
    public required SearchResultKind Kind { get; init; }
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }

    /// <summary>A short extract, already plain text.</summary>
    public string? Snippet { get; init; }

    /// <summary>Client-relative link, built by the server so the routes live in one place.</summary>
    public required string Url { get; init; }

    public DateTime? Date { get; init; }
}

public sealed class SearchResultsDto
{
    public string Term { get; init; } = string.Empty;
    public IReadOnlyList<SearchResultDto> Employees { get; init; } = [];
    public IReadOnlyList<SearchResultDto> Posts { get; init; } = [];
    public IReadOnlyList<SearchResultDto> Communities { get; init; } = [];
    public IReadOnlyList<SearchResultDto> Documents { get; init; } = [];
    public IReadOnlyList<SearchResultDto> Events { get; init; } = [];

    public int TotalShown =>
        Employees.Count + Posts.Count + Communities.Count + Documents.Count + Events.Count;
}
