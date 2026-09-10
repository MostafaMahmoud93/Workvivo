namespace Workvivo.Application.Features.Documents.Dtos;

public sealed class DocumentCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int DocumentCount { get; init; }
}

public sealed class DocumentSummaryDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public long? SizeBytes { get; init; }
    public int VersionNumber { get; init; }
    public int DownloadCount { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateOnly? ReviewDate { get; init; }
    public string OwnerDisplayName { get; init; } = string.Empty;

    /// <summary>
    /// False when the file has not passed its malware scan.
    ///
    /// Surfaced rather than hidden: a document whose download button silently does
    /// nothing produces a support ticket, and "still being checked" is the honest
    /// answer.
    /// </summary>
    public bool IsDownloadable { get; init; }

    public bool CanManage { get; init; }
}
