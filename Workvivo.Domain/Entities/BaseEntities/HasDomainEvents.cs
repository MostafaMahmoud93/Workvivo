using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Domain.Entities.BaseEntities;

/// <summary>
/// An entity that can record things that happened to it.
///
/// Events are collected during a use case and dispatched by the pipeline *after*
/// SaveChanges commits. That ordering is the whole point: raising a notification or
/// sending an email inside the transaction means a later rollback still leaves the
/// side effect behind, and there is no way to take an email back.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

/// <summary>
/// Audited, soft-deletable entity that can also raise domain events.
///
/// Sits on top of <see cref="FullBaseEntity{U}"/> rather than replacing it, so the
/// existing entities are untouched and only aggregates that genuinely raise events
/// pay for the list.
/// </summary>
public abstract class AuditableEntity<TKey> : FullBaseEntity<TKey>, IHasDomainEvents
{
    private List<IDomainEvent>? _domainEvents;

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents ?? (IReadOnlyCollection<IDomainEvent>)Array.Empty<IDomainEvent>();

    public void ClearDomainEvents() => _domainEvents?.Clear();

    protected void Raise(IDomainEvent domainEvent) => (_domainEvents ??= []).Add(domainEvent);
}
