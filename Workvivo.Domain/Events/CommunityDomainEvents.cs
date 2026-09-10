using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Domain.Events;

/// <summary>Somebody asked to join a community that requires approval.</summary>
public sealed record CommunityJoinRequestedDomainEvent(
    Guid CommunityId,
    Guid RequesterEmployeeId,
    DateTime OccurredOnUtc) : IDomainEvent;

/// <summary>A pending membership was approved, so the person is now in.</summary>
public sealed record CommunityMembershipApprovedDomainEvent(
    Guid CommunityId,
    Guid MemberEmployeeId,
    Guid ReviewerEmployeeId,
    DateTime OccurredOnUtc) : IDomainEvent;

/// <summary>Somebody was invited to a community.</summary>
public sealed record CommunityInvitationSentDomainEvent(
    Guid CommunityId,
    Guid InvitedEmployeeId,
    Guid InvitedByEmployeeId,
    DateTime OccurredOnUtc) : IDomainEvent;
