using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.Application.Features.Posts.Dtos;

/// <summary>One card in the feed.</summary>
public sealed class FeedItemDto
{
    public Guid Id { get; init; }
    public PostType PostType { get; init; }
    public string? Title { get; init; }
    public string? ContentHtml { get; init; }
    public bool IsPinned { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsOfficial { get; init; }
    public bool CommentsEnabled { get; init; }
    public DateTime PublishedDate { get; init; }

    public required AuthorDto Author { get; init; }

    public Guid? CommunityId { get; init; }
    public string? CommunityName { get; init; }

    public int CommentsCount { get; init; }
    public int ReactionsCount { get; init; }

    /// <summary>Counts per reaction type, for the reaction bar. Absent types are zero.</summary>
    public Dictionary<ReactionType, int> Reactions { get; init; } = [];

    /// <summary>The viewer's own reaction, or null. Drives the highlighted button.</summary>
    public ReactionType? MyReaction { get; init; }

    public IReadOnlyList<AttachmentDto> Attachments { get; init; } = [];
    public IReadOnlyList<MentionDto> Mentions { get; init; } = [];

    /// <summary>Whether the viewer may edit or delete this post - decided server-side.</summary>
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
}

public sealed class AuthorDto
{
    public Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public string? JobTitle { get; init; }
    public Guid? ProfilePictureFileId { get; init; }
}

public sealed class AttachmentDto
{
    public Guid Id { get; init; }
    public PostAttachmentType Type { get; init; }
    public Guid? FileId { get; init; }
    public string? LinkUrl { get; init; }
    public string? LinkTitle { get; init; }
    public string? Caption { get; init; }
}

public sealed class MentionDto
{
    public Guid EmployeeId { get; init; }
    public required string DisplayName { get; init; }
}

/// <summary>One reaction total, as the database groups them.</summary>
public sealed record ReactionTally(Guid PostId, ReactionType Type, int Count);
