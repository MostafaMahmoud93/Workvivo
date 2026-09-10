namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>
/// What a notification is about. Drives copy, the deep link, and which preference
/// row decides whether it is delivered on a given channel.
///
/// The existing <c>Notification.Notification_Type</c> column is an int, so these
/// values map onto it directly.
/// </summary>
public enum NotificationType
{
    General = 0,
    PostComment = 1,
    CommentReply = 2,
    Mention = 3,
    Reaction = 4,
    Recognition = 5,
    Announcement = 6,
    EventInvitation = 7,
    EventReminder = 8,
    SurveyInvitation = 9,
    PollInvitation = 10,
    CommunityInvitation = 11,
    CommunityJoinRequest = 12,
    CommunityMembershipApproved = 13,
    ContentModerated = 14,
    NewFollower = 15,
    Birthday = 16,
    WorkAnniversary = 17,
}

/// <summary>Delivery channels a notification preference can switch on or off.</summary>
public enum NotificationChannel
{
    InApp = 0,
    Email = 1,

    /// <summary>Not delivered yet - the preference exists so the column is ready.</summary>
    Push = 2,
}

/// <summary>How often batched email is sent for a notification type.</summary>
public enum NotificationDigestFrequency
{
    Immediate = 0,
    Hourly = 1,
    Daily = 2,
    Weekly = 3,
    Never = 4,
}

/// <summary>
/// What a notification points at.
///
/// Held alongside the id so the client can build the deep link itself and so
/// "everything about this post" can be found without parsing <c>RedirectUrl</c> - which
/// is a display concern and changes whenever the routes do.
/// </summary>
public enum NotificationEntityType
{
    None = 0,
    Post = 1,
    Comment = 2,
    Employee = 3,
    Community = 4,
    Event = 5,
    Survey = 6,
    Poll = 7,
    Recognition = 8,
    Document = 9,
}
