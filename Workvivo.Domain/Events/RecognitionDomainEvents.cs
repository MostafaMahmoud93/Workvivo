using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Domain.Events;

/// <summary>One colleague recognised another.</summary>
public sealed record RecognitionGivenDomainEvent(
    Guid RecognitionId,
    Guid SenderEmployeeId,
    Guid RecipientEmployeeId,
    int Points,
    DateTime OccurredOnUtc) : IDomainEvent;
