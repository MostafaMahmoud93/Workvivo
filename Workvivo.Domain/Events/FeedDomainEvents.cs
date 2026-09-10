using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Domain.Events;

/// <summary>
/// A post became visible to its audience.
///
/// Carries the mention list because the notification for "you were mentioned" has to
/// be sent whether the post was published immediately or by the scheduler hours later,
/// and the mention rows are the only place that list survives.
/// </summary>
public sealed record PostPublishedDomainEvent(
    Guid PostId,
    Guid AuthorEmployeeId,
    Guid? CommunityId,
    bool IsOfficial,
    IReadOnlyList<Guid> MentionedEmployeeIds,
    DateTime OccurredOnUtc) : IDomainEvent;

/// <summary>
/// Somebody commented on a post, or replied to a comment.
///
/// The post author and the parent comment's author are both carried on the event
/// rather than looked up by the subscriber. They are already loaded where the event is
/// raised, and a subscriber that re-queried them would turn one comment into three
/// extra round trips - on the write path, inside a transaction.
/// </summary>
public sealed record CommentAddedDomainEvent(
    Guid CommentId,
    Guid PostId,
    Guid AuthorEmployeeId,
    Guid PostAuthorEmployeeId,
    Guid? ParentCommentId,
    Guid? ParentAuthorEmployeeId,
    IReadOnlyList<Guid> MentionedEmployeeIds,
    DateTime OccurredOnUtc) : IDomainEvent;

/// <summary>
/// Somebody set or changed their reaction on a post.
///
/// Not raised when a reaction is cleared: there is no notification worth sending for
/// "a colleague took their like back", and sending one would be unkind as well as noisy.
/// </summary>
public sealed record PostReactedDomainEvent(
    Guid PostId,
    Guid PostAuthorEmployeeId,
    Guid ActorEmployeeId,
    ReactionType Reaction,
    DateTime OccurredOnUtc) : IDomainEvent;
