namespace Workvivo.Application.Features.Notifications.Dtos;

/// <summary>
/// One row in the bell.
///
/// <see cref="Id"/> is the recipient's own copy, not the shared notification - it is
/// what "mark read" addresses, and it is scoped to one person, so a caller cannot mark
/// somebody else's notification read by guessing an id.
/// </summary>
public sealed class NotificationDto
{
    public Guid Id { get; init; }

    public Guid NotificationId { get; init; }

    public int Type { get; init; }

    /// <summary>Already resolved to the reader's language by the query.</summary>
    public string Header { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string? RedirectUrl { get; init; }

    public int EntityType { get; init; }

    public Guid? EntityId { get; init; }

    public Guid? ActorEmployeeId { get; init; }

    public string? ActorDisplayName { get; init; }

    public Guid? ActorProfilePictureFileId { get; init; }

    public bool IsSeen { get; init; }

    public DateTime CreatedDate { get; init; }
}

/// <summary>What the bell shows without opening it.</summary>
public sealed class UnreadCountDto
{
    public int Unread { get; init; }
}

/// <summary>
/// One row of the preferences screen: a notification type and what it does on each
/// channel.
///
/// Every type is returned whether or not the employee has a stored row, with the
/// defaults filled in. A screen that only listed saved rows would start empty and look
/// broken.
/// </summary>
public sealed class NotificationPreferenceDto
{
    public int Type { get; init; }

    public bool InApp { get; init; }

    public bool Email { get; init; }

    public int EmailFrequency { get; init; }

    /// <summary>False when this row is the default rather than a saved choice.</summary>
    public bool IsExplicit { get; init; }
}

/// <summary>
/// What the client is pushed when something arrives.
///
/// The whole notification, not a "go and fetch it" ping. A ping would have every
/// connected client hit the API the moment an announcement fans out - a hundred
/// thousand of them, at once, which is a denial of service the product performs on
/// itself.
/// </summary>
public sealed class RealtimeNotificationDto
{
    public required NotificationDto Notification { get; init; }

    public int Unread { get; init; }
}
