namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Something that happened, stated in domain terms - a post was published, an
/// employee was mentioned.
///
/// A plain marker, with no MediatR reference: Domain must not take a dependency on a
/// dispatch library. The Application layer adapts these into MediatR notifications
/// when it dispatches them.
///
/// Concrete events are defined by the phase that raises them, not up front - an event
/// nobody publishes is a guess about a feature that has not been built.
/// </summary>
public interface IDomainEvent
{
    /// <summary>When the event occurred, in UTC.</summary>
    DateTime OccurredOnUtc { get; }
}
