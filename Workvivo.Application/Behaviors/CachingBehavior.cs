using MediatR;
using Microsoft.Extensions.Logging;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Behaviors;

/// <summary>
/// Serves queries that opt in via <see cref="ICacheableQuery"/> from the cache.
///
/// Opt-in rather than automatic, because the dangerous case is a query whose result
/// depends on who is asking: cache it under a viewer-independent key and one employee
/// is served another's audience-filtered feed. Making each query state its own key
/// forces that decision to be conscious.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheable)
        {
            return await next();
        }

        var cached = await _cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheable.CacheKey);
            return cached;
        }

        var response = await next();

        if (response is not null)
        {
            await _cache.SetAsync(
                cacheable.CacheKey,
                response,
                cacheable.CacheDuration ?? DefaultDuration,
                cancellationToken);
        }

        return response;
    }
}
