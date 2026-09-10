using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;

namespace Workvivo.Application.Features.Posts.Commands.RecordPostViews;

/// <summary>
/// Records that the caller has seen these posts.
///
/// A batch, not one call per post: the client observes a screenful at a time, and a
/// request per post would mean twenty requests per scroll from every employee. This
/// is the highest-volume write in the product, so it is also the one that has to
/// cost the least.
/// </summary>
public sealed record RecordPostViewsCommand(IReadOnlyList<Guid> PostIds) : ICommand<int>;

public sealed class RecordPostViewsCommandValidator : AbstractValidator<RecordPostViewsCommand>
{
    public RecordPostViewsCommandValidator() =>
        RuleFor(x => x.PostIds).NotEmpty().Must(ids => ids.Count <= 50)
            .WithMessage("Report at most 50 views at a time.");
}

public sealed class RecordPostViewsCommandHandler : IRequestHandler<RecordPostViewsCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IDateTimeProvider _clock;

    public RecordPostViewsCommandHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _clock = clock;
    }

    public async Task<int> Handle(RecordPostViewsCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var now = _clock.UtcNow;

        var postIds = request.PostIds.Distinct().ToArray();

        var views = _unitOfWork.Repository<PostView, Guid>();

        var existing = await views.GetAllQ()
            .Where(view => view.Employee_Id == employeeId && postIds.Contains(view.Post_Id))
            .ToListAsync(cancellationToken);

        var seen = existing.ConvertAll(view => view.Post_Id);

        // Only posts that exist *and* that this person is allowed to see.
        //
        // Existence alone is not enough. Reach is the number this table exists to
        // produce - "how many staff have read the safety notice" - and a caller who
        // could report views on posts hidden from them could inflate any post's
        // reach by naming its id. The audience filter is the same one the feed uses.
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);

        var real = await _unitOfWork.Repository<Post, Guid>()
            .GetAllQ()
            .Where(post => postIds.Contains(post.Id))
            .Where(post => _unitOfWork.Repository<PostAudience, Guid>()
                .GetAllQ()
                .Any(audience => audience.Post_Id == post.Id && keys.Contains(audience.Audience_Key)))
            .Select(post => post.Id)
            .ToListAsync(cancellationToken);

        foreach (var view in existing)
        {
            // A repeat view updates the row rather than adding one. The question this
            // table answers is "how many people have seen it", so a second look by
            // the same person must not move that number.
            view.Last_Viewed_At = now;
            view.View_Count += 1;
        }

        var fresh = real.FindAll(postId => !seen.Contains(postId));

        foreach (var postId in fresh)
        {
            await views.AddAsync(new PostView
            {
                Id = Guid.NewGuid(),
                Post_Id = postId,
                Employee_Id = employeeId,
                First_Viewed_At = now,
                Last_Viewed_At = now,
                View_Count = 1,
                Is_Deleted = false,
            });

            // Incremented only for a first view, so the counter means distinct
            // readers - which is what "reach" has to mean for an announcement.
            await _unitOfWork.Repository<Post, Guid>().ExecuteUpdateAsync(
                candidate => candidate.Id == postId,
                setters => setters.SetProperty(
                    candidate => candidate.Views_Count,
                    candidate => candidate.Views_Count + 1),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return fresh.Count;
    }
}
