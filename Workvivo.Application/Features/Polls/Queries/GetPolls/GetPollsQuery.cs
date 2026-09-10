using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Polls.Common;
using Workvivo.Application.Features.Polls.Dtos;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Polls;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Polls.Queries.GetPolls;

/// <summary>
/// The polls addressed to the caller, newest first.
///
/// Audience-filtered by the same key set the feed uses. A poll targeted at one
/// department must not appear - and must not be votable - for anybody else, and
/// reusing the resolver is what stops that rule being re-implemented differently
/// here.
/// </summary>
public sealed record GetPollsQuery(bool IncludeClosed = false) : IQuery<IReadOnlyList<PollDto>>;

public sealed class GetPollsQueryHandler : IRequestHandler<GetPollsQuery, IReadOnlyList<PollDto>>
{
    /// <summary>Polls are few and short-lived; a page of twenty is the whole list in practice.</summary>
    private const int MaxPolls = 20;

    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;
    private readonly IAnonymityHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public GetPollsQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience,
        IAnonymityHasher hasher,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PollDto>> Handle(
        GetPollsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);
        var now = _clock.UtcNow;

        var query = _unitOfWork.Repository<Poll, Guid>()
            .GetAllQ()
            .Where(poll => poll.Status != PollStatus.Draft)

            // One indexed EXISTS against the viewer's key set, exactly as the feed
            // does it - rather than a union of one predicate per targeting dimension.
            .Where(poll => _unitOfWork.Repository<PollAudience, Guid>()
                .GetAllQ()
                .Any(audience => audience.Poll_Id == poll.Id && keys.Contains(audience.Audience_Key)));

        if (!request.IncludeClosed)
        {
            query = query.Where(poll =>
                poll.Status == PollStatus.Open
                && (poll.Start_Date == null || poll.Start_Date <= now)
                && (poll.Expiry_Date == null || poll.Expiry_Date > now));
        }

        var polls = await query
            .OrderByDescending(poll => poll.Create_Date)
            .Take(MaxPolls)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (polls.Count == 0)
        {
            return [];
        }

        var pollIds = polls.ConvertAll(poll => poll.Id);

        var options = await _unitOfWork.Repository<PollOption, Guid>()
            .GetAllQ()
            .Where(option => pollIds.Contains(option.Poll_Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // The caller's own votes, in one query for the whole page.
        //
        // Anonymous polls are matched by hash rather than skipped: the point of the
        // hash is that the voter can still be told they have voted without the row
        // recording who they are.
        var myVotes = await _unitOfWork.Repository<PollVote, Guid>()
            .GetAllQ()
            .Where(vote => pollIds.Contains(vote.Poll_Id) && vote.Employee_Id == employeeId)
            .Select(vote => new PollVoteKey(vote.Poll_Id, vote.Option_Id))
            .ToListAsync(cancellationToken);

        var anonymousVotes = await LoadAnonymousVotesAsync(polls, employeeId, cancellationToken);

        var chosen = myVotes
            .Concat(anonymousVotes)
            .GroupBy(vote => vote.Poll_Id)
            .ToDictionary(group => group.Key, group => group.Select(vote => vote.Option_Id).ToArray());

        return
        [
            .. polls.Select(poll => PollProjection.For(
                poll,
                options.FindAll(option => option.Poll_Id == poll.Id),
                chosen.GetValueOrDefault(poll.Id, []),
                now)),
        ];
    }

    private async Task<List<PollVoteKey>> LoadAnonymousVotesAsync(
        List<Poll> polls,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var anonymous = polls.FindAll(poll => poll.Is_Anonymous);

        if (anonymous.Count == 0)
        {
            return [];
        }

        // One hash per anonymous poll, because the hash is deliberately scoped to the
        // poll - the same person is a different token in each.
        var hashes = anonymous.ConvertAll(poll => _hasher.Hash(poll.Id, employeeId));

        return await _unitOfWork.Repository<PollVote, Guid>()
            .GetAllQ()
            .Where(vote => vote.Voter_Hash != null && hashes.Contains(vote.Voter_Hash))
            .Select(vote => new PollVoteKey(vote.Poll_Id, vote.Option_Id))
            .ToListAsync(cancellationToken);
    }

    private readonly record struct PollVoteKey(Guid Poll_Id, Guid Option_Id);
}
