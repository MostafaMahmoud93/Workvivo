using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Features.Polls.Commands.CastVote;
using Workvivo.Application.Features.Polls.Commands.SavePoll;
using Workvivo.Application.Features.Polls.Dtos;
using Workvivo.Application.Features.Polls.Queries.GetPolls;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// Polls.
///
/// No permission attribute on reading or voting: which polls a person sees is decided
/// by the audience rules inside the query, exactly as the feed works, and a permission
/// here would be a second, coarser answer to the same question. Creating one needs
/// Poll.Manage, checked in the handler.
/// </summary>
public class PollsController : ApiControllersBase
{
    private readonly ISender _sender;

    public PollsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.Polls.List)]
    public async Task<IActionResult> List(
        [FromQuery] bool includeClosed,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<PollDto>>.Ok(
            await _sender.Send(new GetPollsQuery(includeClosed), cancellationToken)));

    [HttpPost]
    [Route(RouteClass.Polls.Save)]
    public async Task<IActionResult> Save(SavePollCommand command, CancellationToken cancellationToken) =>
        Ok(ApiResponse<Guid>.Ok(await _sender.Send(command, cancellationToken)));

    /// <summary>Casts a vote and returns the poll as the caller may now see it.</summary>
    [HttpPost]
    [Route(RouteClass.Polls.Vote)]
    public async Task<IActionResult> Vote(
        Guid pollId,
        [FromBody] VoteRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<PollDto>.Ok(await _sender.Send(
            new CastVoteCommand(pollId, request.OptionIds ?? []), cancellationToken)));
}

public sealed record VoteRequest(IReadOnlyList<Guid>? OptionIds);
