using FluentValidation;
using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.RealTime;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Notifications.Commands.MarkNotificationsRead;

/// <summary>
/// Marks the caller's own notifications read.
///
/// <paramref name="Ids" /> are delivery ids. Empty means "all of mine", which is the
/// "mark all as read" button.
/// </summary>
public sealed record MarkNotificationsReadCommand(IReadOnlyList<Guid> Ids) : ICommand<int>;

public sealed class MarkNotificationsReadCommandValidator : AbstractValidator<MarkNotificationsReadCommand>
{
    /// <summary>
    /// A ceiling on the batch. The client marks a page at a time; a request carrying
    /// fifty thousand ids is either a bug or somebody probing, and either way it should
    /// not become an IN clause the database has to parse.
    /// </summary>
    public MarkNotificationsReadCommandValidator() =>
        RuleFor(x => x.Ids).Must(ids => ids.Count <= 200)
            .WithMessage("Mark at most 200 notifications at a time.");
}

public sealed class MarkNotificationsReadCommandHandler
    : IRequestHandler<MarkNotificationsReadCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkNotificationsReadCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<int> Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();
        var now = _clock.UtcNow;
        var ids = request.Ids;

        // The recipient predicate is not optional and is not derived from the request.
        //
        // Filtering on the ids alone would let anyone mark anyone else's notifications
        // read by guessing - a small harm on its own, but it is the same shape as every
        // insecure direct object reference, and the ownership clause is what makes it
        // impossible rather than unlikely.
        // Two predicates rather than one with `ids.Count == 0 ||` folded into it. That
        // form reads well but leaves the emptiness test inside the expression tree,
        // where the provider may translate it into SQL instead of deciding it here -
        // and an IN clause over an empty list is a query that matches nothing.
        System.Linq.Expressions.Expression<Func<NotificationUser, bool>> predicate =
            ids.Count == 0
                ? delivery => delivery.Reciever_Id == userId && !delivery.IS_Seen
                : delivery => delivery.Reciever_Id == userId
                    && !delivery.IS_Seen
                    && ids.Contains(delivery.Id);

        var affected = await _unitOfWork.Repository<NotificationUser, Guid>().ExecuteUpdateAsync(
            predicate,
            setters => setters
                .SetProperty(delivery => delivery.IS_Seen, true)
                .SetProperty(delivery => delivery.Seen_Date, now),
            cancellationToken);

        return affected;
    }
}
