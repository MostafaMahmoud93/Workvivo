using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Comments.Commands.AddComment;

/// <summary>Adds a comment, or a reply when <see cref="ParentCommentId"/> is supplied.</summary>
public sealed record AddCommentCommand(
    Guid PostId,
    Guid? ParentCommentId,
    string ContentHtml,
    IReadOnlyList<Guid> MentionedEmployeeIds) : ICommand<Guid>;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.PostId).NotEmpty();
        RuleFor(x => x.ContentHtml).NotEmpty().MaximumLength(10_000);
        RuleFor(x => x.MentionedEmployeeIds).Must(ids => ids.Count <= 20);
    }
}

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PostAuthorization _authorization;
    private readonly IContentSanitizer _sanitizer;
    private readonly IDateTimeProvider _clock;

    public AddCommentCommandHandler(
        IUnitOfWork unitOfWork,
        PostAuthorization authorization,
        IContentSanitizer sanitizer,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _sanitizer = sanitizer;
        _clock = clock;
    }

    public async Task<Guid> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var authorId = await _authorization.RequireEmployeeIdAsync(cancellationToken);

        var posts = _unitOfWork.Repository<Post, Guid>();

        var post = await posts.FindByIDAsync(request.PostId)
            ?? throw new NotFoundException(nameof(Post), request.PostId);

        if (!post.Comments_Enabled)
        {
            throw new BusinessRuleException(
                "Comments are turned off for this post.", "comment.disabled");
        }

        var (depth, parentAuthorId) = await ResolveParentAsync(request, cancellationToken);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Post_Id = request.PostId,
            Author_Employee_Id = authorId,
            Parent_Comment_Id = request.ParentCommentId,
            Depth = depth,
            Content_Html = _sanitizer.Sanitize(request.ContentHtml),
            Content_Text = _sanitizer.ToPlainText(request.ContentHtml),
            Is_Deleted = false,
        };

        await _unitOfWork.Repository<Comment, Guid>().AddAsync(comment);

        await AddMentionsAsync(comment, request.MentionedEmployeeIds, cancellationToken);

        // Atomic increments, for the same reason as reactions: two people commenting at
        // once would otherwise both read the same count and write the same value back.
        await posts.ExecuteUpdateAsync(
            candidate => candidate.Id == request.PostId,
            setters => setters.SetProperty(
                candidate => candidate.Comments_Count,
                candidate => candidate.Comments_Count + 1),
            cancellationToken);

        if (request.ParentCommentId is { } parentId)
        {
            await _unitOfWork.Repository<Comment, Guid>().ExecuteUpdateAsync(
                candidate => candidate.Id == parentId,
                setters => setters.SetProperty(
                    candidate => candidate.Replies_Count,
                    candidate => candidate.Replies_Count + 1),
                cancellationToken);
        }

        // Raised once the mentions are attached, because the event carries them, and
        // before the save, because the pipeline drains the events after the commit.
        comment.RecordAdded(post.Author_Employee_Id, parentAuthorId, _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }

    /// <summary>
    /// Works out how deep this comment sits and who it is answering, refusing to go
    /// past the cap.
    ///
    /// The depth is taken from the parent rather than trusted from the request - a
    /// caller supplying its own depth could nest without limit, and the cap exists to
    /// keep threads readable on a phone and reply queries bounded.
    ///
    /// The parent's author comes back from the same query rather than a second one:
    /// the notification needs it, and it is one column further along a row already
    /// being read.
    /// </summary>
    private async Task<(int Depth, Guid? ParentAuthorId)> ResolveParentAsync(
        AddCommentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ParentCommentId is not { } parentId)
        {
            return (0, null);
        }

        var parent = await _unitOfWork.Repository<Comment, Guid>()
            .GetAllQ()
            .Where(comment => comment.Id == parentId && comment.Post_Id == request.PostId)
            .Select(comment => new { comment.Depth, comment.Author_Employee_Id })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), parentId);

        // Domain rule, asked of the domain. The database enforces the same bound with a
        // check constraint, so neither a new code path nor a direct insert can exceed it.
        var replyDepth = new Comment { Depth = parent.Depth }.ReplyDepth();

        if (replyDepth is null)
        {
            throw new BusinessRuleException(
                "This conversation cannot be nested any deeper. Reply to the comment above instead.",
                "comment.max-depth");
        }

        return (replyDepth.Value, parent.Author_Employee_Id);
    }

    private async Task AddMentionsAsync(
        Comment comment,
        IReadOnlyList<Guid> mentionedEmployeeIds,
        CancellationToken cancellationToken)
    {
        if (mentionedEmployeeIds.Count == 0)
        {
            return;
        }

        var distinct = mentionedEmployeeIds.Distinct().ToArray();

        var valid = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => distinct.Contains(employee.Id) && employee.Is_Active)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);

        foreach (var employeeId in valid)
        {
            comment.Mentions.Add(new CommentMention
            {
                Id = Guid.NewGuid(),
                Comment_Id = comment.Id,
                Mentioned_Employee_Id = employeeId,
                Mentioned_At = _clock.UtcNow,
                Is_Deleted = false,
            });
        }
    }
}
