using Workvivo.Application.Features.Posts.Dtos;

namespace Workvivo.Application.Features.Comments.Dtos;

public sealed class CommentDto
{
    public Guid Id { get; init; }
    public Guid PostId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public int Depth { get; init; }
    public string? ContentHtml { get; init; }
    public DateTime CreatedDate { get; init; }
    public bool IsEdited { get; init; }
    public int RepliesCount { get; init; }
    public int ReactionsCount { get; init; }

    public required AuthorDto Author { get; init; }

    public IReadOnlyList<MentionDto> Mentions { get; init; } = [];

    /// <summary>Null when this comment is already at the nesting limit.</summary>
    public bool CanReply { get; init; }

    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
}
