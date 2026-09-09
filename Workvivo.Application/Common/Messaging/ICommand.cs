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
/// A command that manages its own persistence and must not be wrapped in the
/// pipeline's transaction.
///
/// Opt-out rather than opt-in, so a new command is transactional unless somebody
/// deliberately says otherwise - the safe default.
///
/// The case this exists for: a handler whose *failure path* has to persist something.
/// Refresh-token replay detection revokes the whole token family and then rejects the
/// request. Inside the ambient transaction, throwing rolls that revocation back - so
/// the replay is refused but the compromised session stays alive, which defeats the
/// entire point of detecting the replay. Marking such a command means its writes commit
/// on their own terms.
///
/// Use sparingly. A handler carrying this marker is responsible for its own atomicity.
/// </summary>
public interface INonTransactionalCommand;

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
