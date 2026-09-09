namespace Workvivo.Application.Common.Paging;

/// <summary>Query-string binding for keyset paging.</summary>
public class CursorRequest
{
    public const int MaxPageSize = 50;
    public const int DefaultPageSize = 20;

    private int _pageSize = DefaultPageSize;

    /// <summary>Token from the previous page's <c>nextCursor</c>. Null for the first page.</summary>
    public string? Cursor { get; set; }

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
}
