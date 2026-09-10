using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.Application.Features.Notifications.Common;

/// <summary>
/// One notification, addressed to a known set of people.
///
/// Recipients are employee ids because that is what the rest of the platform speaks;
/// the dispatcher translates them to login ids, which is what a notification row and a
/// real-time connection are keyed by.
///
/// For a handful of recipients - a comment, a mention, a reaction. Announcements to a
/// whole audience go through the broadcast job instead, which pages rather than
/// materialising a hundred thousand ids in memory.
/// </summary>
public sealed class NotificationSendRequest
{
    public required NotificationType Type { get; init; }

    public required IReadOnlyList<Guid> RecipientEmployeeIds { get; init; }

    /// <summary>Who caused it. Excluded from the recipients - nobody wants telling about their own click.</summary>
    public Guid? ActorEmployeeId { get; init; }

    public NotificationEntityType EntityType { get; init; } = NotificationEntityType.None;

    public Guid? EntityId { get; init; }

    public required NotificationCopy.Text Copy { get; init; }

    public string? RedirectUrl { get; init; }
}
