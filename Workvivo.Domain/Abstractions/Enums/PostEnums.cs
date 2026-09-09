namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>
/// What kind of thing a feed entry is. Drives the card the client renders and, for
/// the official types, the extra permission needed to create one.
/// </summary>
public enum PostType
{
    Normal = 0,
    Announcement = 1,
    Article = 2,
    Celebration = 3,
    Recognition = 4,
    Poll = 5,
    Event = 6,
    SystemAnnouncement = 7,
}

/// <summary>
/// Lifecycle of a post. Only <see cref="Published"/> is visible in the feed; the
/// feed query filters on this before anything else.
/// </summary>
public enum PostStatus
{
    Draft = 0,
    Scheduled = 1,
    Published = 2,
    Archived = 3,
}

/// <summary>
/// How widely a published post travels, on top of its audience rows.
///
/// Audience targeting says *who may see it*; visibility says *where it appears*. A
/// community post targeted at everyone still belongs in that community's feed, not
/// on the company timeline.
/// </summary>
public enum PostVisibility
{
    Organization = 0,
    Community = 1,
    Followers = 2,
    Private = 3,
}

/// <summary>
/// Reactions, shared by posts and comments.
///
/// Values are pinned because they are persisted; reordering the list would silently
/// rewrite the meaning of every existing row.
/// </summary>
public enum ReactionType
{
    Like = 0,
    Love = 1,
    Celebrate = 2,
    Support = 3,
    Insightful = 4,
}

public enum PostAttachmentType
{
    Image = 0,
    Video = 1,
    Document = 2,
    Link = 3,
}
