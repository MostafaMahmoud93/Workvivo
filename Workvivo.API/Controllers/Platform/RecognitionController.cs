using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Recognition.Commands.GiveRecognition;
using Workvivo.Application.Features.Recognition.Dtos;
using Workvivo.Application.Features.Recognition.Queries.GetLeaderboard;
using Workvivo.Application.Features.Recognition.Queries.GetRecognitionTypes;
using Workvivo.Application.Features.Recognition.Queries.GetRecognitionWall;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// Peer recognition.
///
/// Giving recognition is not gated by a permission on purpose. It is the one feature
/// in the product whose entire value depends on everybody being able to use it, and a
/// permission that some roles lack would quietly make recognition a management
/// activity. What stops abuse is the domain: you cannot recognise yourself, and the
/// point value comes from the category rather than the sender.
/// </summary>
public class RecognitionController : ApiControllersBase
{
    private readonly ISender _sender;

    public RecognitionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.RecognitionRoutes.Types)]
    public async Task<IActionResult> Types(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<RecognitionTypeDto>>.Ok(
            await _sender.Send(new GetRecognitionTypesQuery(), cancellationToken)));

    [HttpGet]
    [Route(RouteClass.RecognitionRoutes.Wall)]
    public async Task<IActionResult> Wall(
        [FromQuery] GetRecognitionWallQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<CursorPagedResult<RecognitionDto>>.Ok(
            await _sender.Send(query, cancellationToken)));

    [HttpPost]
    [Route(RouteClass.RecognitionRoutes.Give)]
    public async Task<IActionResult> Give(
        GiveRecognitionCommand command,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<Guid>.Ok(await _sender.Send(command, cancellationToken)));

    [HttpGet]
    [Route(RouteClass.RecognitionRoutes.Leaderboard)]
    public async Task<IActionResult> Leaderboard(
        [FromQuery] GetLeaderboardQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<LeaderboardDto>.Ok(await _sender.Send(query, cancellationToken)));
}
