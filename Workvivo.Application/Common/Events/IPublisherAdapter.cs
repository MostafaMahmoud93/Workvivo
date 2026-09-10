using MediatR;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Common.Events;

/// <summary>
/// Publishes domain events from somewhere that is not the MediatR pipeline.
///
/// The pipeline behaviour handles the request path. A background job has no request,
/// so it drains and publishes for itself - and this exists so it can do that without
/// each job repeating the wrapping and the swallow-and-log.
/// </summary>
public interface IPublisherAdapter
{
    Task PublishDomainEventsAsync(IReadOnlyList<IDomainEvent> domainEvents);
}

/// <inheritdoc />
public sealed class PublisherAdapter : IPublisherAdapter
{
    private readonly IPublisher _publisher;
    private readonly Microsoft.Extensions.Logging.ILogger<PublisherAdapter> _logger;

    public PublisherAdapter(IPublisher publisher, Microsoft.Extensions.Logging.ILogger<PublisherAdapter> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishDomainEventsAsync(IReadOnlyList<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await _publisher.Publish(DomainEventNotification.For(domainEvent), CancellationToken.None);
            }
            catch (Exception ex)
            {
                // One subscriber failing must not abandon the rest of the batch - in a
                // scheduled-publish run that would mean the posts after the failure are
                // published but announced to nobody.
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(
                    _logger, ex, "Handling {DomainEvent} failed", domainEvent.GetType().Name);
            }
        }
    }
}
