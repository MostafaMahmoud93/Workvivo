namespace Workvivo.Application.Common.Paging;

/// <summary>
/// Query-string binding for offset paging, sorting and free-text filtering.
///
/// Page size is clamped rather than rejected: an unbounded page size is a
/// denial-of-service vector, and a 400 only teaches the caller which number to try
/// next. Clamping serves them something sane instead.
/// </summary>
public class PageRequest
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    /// <summary>Property to sort by. Handlers must resolve it against an allow-list.</summary>
    public string? SortBy { get; set; }

    public bool SortAscending { get; set; } = true;

    /// <summary>Free-text filter. Always parameterised - never concatenated into SQL.</summary>
    public string? Search { get; set; }

    public int Skip => (PageNumber - 1) * PageSize;
}
