namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Distributed-cache facade. Backed by in-process memory by default and by Redis when
/// one is configured; application code cannot tell the difference, which is the point -
/// scaling out to several API replicas must not require touching a handler.
///
/// Caching is always an optimisation here. Every caller must still be correct when a
/// value is absent, so a cache outage degrades latency, never correctness.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the cached value, or invokes <paramref name="factory"/>, caches the
    /// result and returns it. A null result is not cached - caching "nothing" turns a
    /// transient miss into a sticky one.
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops every key under a prefix, for invalidating a whole family at once - all
    /// of a user's permission entries, say. Prefix scans are expensive on a large
    /// Redis; keep prefixes narrow and call this on writes, not on reads.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
