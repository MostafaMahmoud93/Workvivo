namespace Workvivo.Application.Common.Paging;

/// <summary>
/// Offset-paged result. Use for admin grids, where a user genuinely jumps to page 37
/// and expects a total count.
///
/// Not for the feed or comment threads - OFFSET makes SQL Server read and discard
/// every skipped row, so deep pages get linearly slower. Those use
/// <see cref="CursorPagedResult{T}"/>.
/// </summary>
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    public static PagedResult<T> Empty(int pageNumber, int pageSize) =>
        new([], pageNumber, pageSize, 0);

    /// <summary>Projects the page to another item type while preserving the paging metadata.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new([.. Items.Select(selector)], PageNumber, PageSize, TotalCount);
}
