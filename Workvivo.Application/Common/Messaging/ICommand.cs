using MediatR;

namespace Workvivo.Application.Common.Messaging;

/// <summary>
/// A request that changes state. Marker interfaces rather than bare IRequest so the
/// pipeline can tell reads from writes: only commands are wrapped in a transaction,
/// only queries are eligible for caching.
/// </summary>
public interface ICommand : IRequest;

/// <summary>A state-changing request that returns a value - typically a new id.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>A read. Handlers for these must not call SaveChanges.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Opt-in caching for a query. The key must include every input that changes the
/// result - crucially the viewer, for anything audience-filtered, or one employee
/// will be served another's feed.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }

    TimeSpan? CacheDuration => null;
}
