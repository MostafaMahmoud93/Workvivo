namespace Workvivo.Application.Common.Paging;

/// <summary>
/// Keyset ("seek") paged result for high-cardinality timelines: the feed, comment
/// threads, notifications, audit logs.
///
/// There is no total count on purpose. Counting matching rows in a table with
/// millions of posts costs more than fetching the page, and nobody scrolling a feed
/// needs to know they are looking at item 40,218 of 3,100,447.
/// </summary>
public sealed class CursorPagedResult<T>
{
    public CursorPagedResult(IReadOnlyList<T> items, string? nextCursor, bool hasMore)
    {
        Items = items;
        NextCursor = nextCursor;
        HasMore = hasMore;
    }

    public IReadOnlyList<T> Items { get; }

    /// <summary>Opaque token to pass back as <c>cursor</c> for the following page.</summary>
    public string? NextCursor { get; }

    public bool HasMore { get; }

    public static CursorPagedResult<T> Empty() => new([], null, false);
}
