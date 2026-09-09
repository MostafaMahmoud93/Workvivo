using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Caching;

/// <summary>
/// The single <see cref="ICacheService"/> implementation, over IDistributedCache.
///
/// One implementation covers both deployment shapes: the in-memory distributed cache
/// in development and single-node installs, Redis when several API replicas run.
/// Nothing above this line knows which, which is what keeps scaling out from being an
/// application-code change.
///
/// Every cache miss - and every cache failure - falls through to the source of truth.
/// A Redis outage must slow the product down, not break it.
/// </summary>
public sealed class DistributedCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// IDistributedCache cannot enumerate keys, so prefix invalidation needs a local
    /// record of what has been written. Held per process, which is enough because
    /// every replica writes its own entries and each expires on its own schedule.
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> KnownKeys = new();

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            return bytes is null || bytes.Length == 0
                ? default
                : JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A cache read must never fail a request. Log and behave as a miss.
            _logger.LogWarning(ex, "Cache read failed for {CacheKey}; treating as a miss", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? DefaultExpiration,
            };

            await _cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions), options, cancellationToken);
            KnownKeys.TryAdd(key, 0);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache write failed for {CacheKey}", key);
        }
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);

        // Absence is not cached: a null here usually means "not found yet", and
        // caching it turns a transient miss into one that persists for the TTL.
        if (value is not null)
        {
            await SetAsync(key, value, absoluteExpiration, cancellationToken);
        }

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            KnownKeys.TryRemove(key, out _);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Worth an error rather than a warning: a failed invalidation means stale
            // data is being served, which is a correctness problem, not a slow one.
            _logger.LogError(ex, "Cache invalidation failed for {CacheKey}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in KnownKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
        {
            await RemoveAsync(key, cancellationToken);
        }
    }
}
