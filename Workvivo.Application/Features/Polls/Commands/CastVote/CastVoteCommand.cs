using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Polls.Common;
using Workvivo.Application.Features.Polls.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Polls;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Polls.Commands.CastVote;

/// <summary>Casts the caller's vote and returns the poll as they may now see it.</summary>
public sealed record CastVoteCommand(Guid PollId, IReadOnlyList<Guid> OptionIds) : ICommand<PollDto>;

public sealed class CastVoteCommandValidator : AbstractValidator<CastVoteCommand>
{
    public CastVoteCommandValidator()
    {
        RuleFor(x => x.PollId).NotEmpty();
        RuleFor(x => x.OptionIds).NotEmpty().Must(ids => ids.Count <= 10);
    }
}

public sealed class CastVoteCommandHandler : IRequestHandler<CastVoteCommand, PollDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IAnonymityHasher _anonymity;
    private readonly IDateTimeProvider _clock;

    public CastVoteCommandHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IAnonymityHasher anonymity,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _anonymity = anonymity;
        _clock = clock;
    }

    public async Task<PollDto> Handle(CastVoteCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var now = _clock.UtcNow;

        var poll = await _unitOfWork.Repository<Poll, Guid>().FindByIDAsync(request.PollId)
            ?? throw new NotFoundException(nameof(Poll), request.PollId);

        // Being able to name a poll is not being invited to it.
        //
        // The listing query filters by the caller's audience keys, but a poll id is
        // guessable and this is the write path - without the same check here, anybody
        // could vote in a poll targeted at another department and skew a result they
        // were never meant to see. Answered as "not found" rather than "forbidden",
        // because the question's wording is often the sensitive part.
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);

        var invited = await _unitOfWork.Repository<PollAudience, Guid>()
            .GetAllQ()
            .AnyAsync(
                audience => audience.Poll_Id == poll.Id && keys.Contains(audience.Audience_Key),
                cancellationToken);

        if (!invited)
        {
            throw new NotFoundException(nameof(Poll), request.PollId);
        }

        if (!poll.IsOpenAt(now))
        {
            throw new BusinessRuleException("This poll is not open.", "poll.closed");
        }

        if (!poll.Is_Multiple_Choice && request.OptionIds.Count > 1)
        {
            throw new BusinessRuleException(
                "This poll allows one answer.", "poll.single-choice");
        }

        var options = await _unitOfWork.Repository<PollOption, Guid>()
            .GetAllQ()
            .Where(option => option.Poll_Id == poll.Id)
            .ToListAsync(cancellationToken);

        var chosen = request.OptionIds.Distinct().ToArray();

        // Every option has to belong to this poll. Without the check, a caller can
        // vote for an option from a different poll and corrupt both tallies.
        if (chosen.Any(id => options.TrueForAll(option => option.Id != id)))
        {
            throw new BusinessRuleException(
                "One of those answers does not belong to this poll.", "poll.unknown-option");
        }

        // On an anonymous poll the voter is identified only by a keyed hash, so the
        // row cannot be traced back to them - but one person still cannot vote twice.
        var voterHash = poll.Is_Anonymous ? _anonymity.Hash(poll.Id, employeeId) : null;

        var votes = _unitOfWork.Repository<PollVote, Guid>();

        var alreadyVoted = poll.Is_Anonymous
            ? await votes.GetAllQ().AnyAsync(
                vote => vote.Poll_Id == poll.Id && vote.Voter_Hash == voterHash, cancellationToken)
            : await votes.GetAllQ().AnyAsync(
                vote => vote.Poll_Id == poll.Id && vote.Employee_Id == employeeId, cancellationToken);

        if (alreadyVoted)
        {
            throw new BusinessRuleException("You have already voted.", "poll.already-voted");
        }

        foreach (var optionId in chosen)
        {
            await votes.AddAsync(new PollVote
            {
                Id = Guid.NewGuid(),
                Poll_Id = poll.Id,
                Option_Id = optionId,

                // Exactly one of these is set. An anonymous vote that also carried the
                // employee id would not be anonymous at all, however it was labelled.
                Employee_Id = poll.Is_Anonymous ? null : employeeId,
                Voter_Hash = voterHash,
                Voted_At = now,
                Is_Deleted = false,
            });

            // Atomic, per option: several people voting at once must not lose tallies
            // to a read-modify-write.
            await _unitOfWork.Repository<PollOption, Guid>().ExecuteUpdateAsync(
                candidate => candidate.Id == optionId,
                setters => setters.SetProperty(
                    candidate => candidate.Votes_Count,
                    candidate => candidate.Votes_Count + 1),
                cancellationToken);
        }

        // One per voter, not one per selected option - otherwise a multiple-choice
        // poll reports more voters than there are people.
        await _unitOfWork.Repository<Poll, Guid>().ExecuteUpdateAsync(
            candidate => candidate.Id == poll.Id,
            setters => setters.SetProperty(
                candidate => candidate.Total_Votes,
                candidate => candidate.Total_Votes + 1),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-read so the returned tallies include this vote and everybody else's,
        // rather than the values loaded before the update.
        var refreshed = await _unitOfWork.Repository<PollOption, Guid>()
            .GetAllQ()
            .Where(option => option.Poll_Id == poll.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        poll.Total_Votes += 1;

        return PollProjection.For(poll, refreshed, chosen, now);
    }
}
