using MediatR;
using Microsoft.Extensions.Logging;
using Workvivo.Application.Common.Events;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Behaviors;

/// <summary>
/// Publishes the domain events a command raised, once its transaction has committed.
///
/// Sits immediately outside <see cref="TransactionBehavior{TRequest,TResponse}"/>, and
/// the order is the whole point. Dispatching inside the transaction means a later
/// rollback leaves the side effect behind - the post is gone but four hundred people
/// were told about it, and an email cannot be recalled. Dispatching here, after the
/// commit returns, means an event is only ever published for a write that actually
/// happened.
///
/// The trade in the other direction is real and worth stating: a crash between the
/// commit and the dispatch loses the notification silently. Nobody is told about a post
/// that exists. That is the lesser failure - a missing notification is an annoyance, a
/// notification for a post that never existed is a bug people report - and the fix when
/// it matters is a transactional outbox, which stores the events in the same commit and
/// has a worker drain them. The seam for that is <c>DrainDomainEvents</c>: it would
/// write rows instead of returning a list, and nothing else here would change.
/// </summary>
public sealed class DomainEventDispatchBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventDispatchBehavior<TRequest, TResponse>> _logger;

    public DomainEventDispatchBehavior(
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<DomainEventDispatchBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // A throw from the handler or the commit skips everything below, which is
        // exactly right: a failed command has no events to announce.
        var response = await next();

        var events = _unitOfWork.DrainDomainEvents();

        foreach (var domainEvent in events)
        {
            try
            {
                // CancellationToken.None on purpose. The write has committed; if the
                // client has walked away mid-request, the notification for a post that
                // now exists still has to be sent.
                await _publisher.Publish(DomainEventNotification.For(domainEvent), CancellationToken.None);
            }
            catch (Exception ex)
            {
                // A subscriber that fails must not turn a successful command into a 500.
                // The post was created; the caller should be told so. What went wrong
                // downstream is an operational problem, and it is logged as one.
                _logger.LogError(
                    ex,
                    "Handling {DomainEvent} raised by {RequestName} failed after the transaction committed",
                    domainEvent.GetType().Name,
                    typeof(TRequest).Name);
            }
        }

        return response;
    }
}
