using MediatR;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Common.Events;

/// <summary>
/// Carries a domain event into MediatR.
///
/// The Domain layer must not reference a dispatch library, so <see cref="IDomainEvent"/>
/// is a bare marker with no MediatR interface on it. This wrapper is the adapter: the
/// event stays framework-free, and subscribers are written as ordinary
/// <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>.
/// </summary>
public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;

/// <summary>
/// Wraps an <see cref="IDomainEvent"/> whose concrete type is only known at runtime.
///
/// The dispatcher has a list of <c>IDomainEvent</c>, and
/// <c>DomainEventNotification&lt;TEvent&gt;</c> has to be closed over the real type or
/// no handler matches. Reflection is confined to this one place.
/// </summary>
public static class DomainEventNotification
{
    public static INotification For(IDomainEvent domainEvent)
    {
        var wrapper = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());

        return (INotification)Activator.CreateInstance(wrapper, domainEvent)!;
    }
}
