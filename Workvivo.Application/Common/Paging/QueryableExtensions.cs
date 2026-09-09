using Microsoft.EntityFrameworkCore;

namespace Workvivo.Application.Common.Paging;

/// <summary>
/// Paging that projects.
///
/// The template's repository helpers return <c>List&lt;T&gt;</c> of entities, which
/// materialises whole aggregates - and with lazy-loading proxies switched on, touching
/// a navigation afterwards fires another query per row. These extensions take the
/// projection first, so EF fetches exactly the columns the DTO names and nothing else.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Counts and fetches one page.
    ///
    /// Two round trips, deliberately: a windowed COUNT(*) OVER() in the same query
    /// makes the database compute the total for every row of the page, and it defeats
    /// the covering indexes the paged read is meant to use.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        // Skip is pointless once the count says there is nothing to skip to, and this
        // saves a query on the common "no results" path.
        if (totalCount == 0)
        {
            return PagedResult<T>.Empty(request.PageNumber, request.PageSize);
        }

        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, request.PageNumber, request.PageSize, totalCount);
    }

    /// <summary>
    /// Applies a keyset page and builds the cursor for the next one.
    ///
    /// The caller has already filtered by the cursor and ordered to match it; this only
    /// takes one more row than asked for, so "is there another page" is answered
    /// without a second count.
    /// </summary>
    public static async Task<CursorPagedResult<T>> ToCursorPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageSize,
        Func<T, (DateTime Timestamp, Guid Id)> cursorSelector,
        CancellationToken cancellationToken = default)
    {
        var items = await query.Take(pageSize + 1).ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        if (items.Count == 0)
        {
            return CursorPagedResult<T>.Empty();
        }

        var (timestamp, id) = cursorSelector(items[^1]);

        return new CursorPagedResult<T>(items, hasMore ? Cursor.Encode(timestamp, id) : null, hasMore);
    }

    /// <summary>
    /// Applies a sort chosen from an allow-list.
    ///
    /// Sort fields arrive from the query string. Reflecting on the name, or building
    /// the ORDER BY as a string, turns a user-controlled value into part of a query -
    /// so the caller supplies a dictionary of the columns it is willing to sort by and
    /// anything else falls back to the default.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        PageRequest request,
        IReadOnlyDictionary<string, System.Linq.Expressions.Expression<Func<T, object>>> allowed,
        System.Linq.Expressions.Expression<Func<T, object>> defaultSort)
    {
        if (!string.IsNullOrWhiteSpace(request.SortBy)
            && allowed.TryGetValue(request.SortBy, out var selector))
        {
            return request.SortAscending
                ? query.OrderBy(selector)
                : query.OrderByDescending(selector);
        }

        return request.SortAscending
            ? query.OrderBy(defaultSort)
            : query.OrderByDescending(defaultSort);
    }
}
