using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.Domain.Entities.RealTime;

/// <summary>
/// What a channel does when an employee has never expressed a preference.
///
/// <see cref="NotificationPreference"/> rows exist only for people who changed
/// something, so the defaults are what almost everyone actually gets. Getting them
/// wrong is how a product ends up emailing a hundred thousand people every time
/// somebody likes a post, and how it then gets filtered to junk wholesale.
///
/// The rule: in-app for everything, email only for the things a person would be sorry
/// to miss while they were not looking at the tab.
/// </summary>
public static class NotificationDefaults
{
    /// <summary>Everything is worth an in-app entry; that list is cheap and dismissible.</summary>
    public static bool InAppEnabled(NotificationType type) => true;

    public static bool EmailEnabled(NotificationType type) => type switch
    {
        // Addressed to you personally, or asked something of you.
        NotificationType.Mention => true,
        NotificationType.Announcement => true,
        NotificationType.Recognition => true,
        NotificationType.EventInvitation => true,
        NotificationType.EventReminder => true,
        NotificationType.SurveyInvitation => true,
        NotificationType.CommunityInvitation => true,
        NotificationType.CommunityJoinRequest => true,
        NotificationType.ContentModerated => true,

        // Everything else - reactions, replies, new followers, birthdays - is social
        // noise in an inbox. Visible in the app, silent by mail unless asked for.
        _ => false,
    };

    /// <summary>
    /// How email for a type is batched by default.
    ///
    /// Replies and comments default to a daily digest rather than off, so somebody who
    /// switches email on for them gets one message a day instead of forty.
    /// </summary>
    public static NotificationDigestFrequency EmailFrequency(NotificationType type) => type switch
    {
        NotificationType.PostComment => NotificationDigestFrequency.Daily,
        NotificationType.CommentReply => NotificationDigestFrequency.Daily,
        NotificationType.Reaction => NotificationDigestFrequency.Daily,
        NotificationType.NewFollower => NotificationDigestFrequency.Weekly,
        NotificationType.Birthday => NotificationDigestFrequency.Daily,
        NotificationType.WorkAnniversary => NotificationDigestFrequency.Daily,
        _ => NotificationDigestFrequency.Immediate,
    };

    /// <summary>The preference an employee has until they save one of their own.</summary>
    public static NotificationPreference For(Guid employeeId, NotificationType type) => new()
    {
        Employee_Id = employeeId,
        Notification_Type = type,
        In_App_Enabled = InAppEnabled(type),
        Email_Enabled = EmailEnabled(type),
        Push_Enabled = false,
        Email_Frequency = EmailFrequency(type),
    };
}
