using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Features.Surveys.Commands.SubmitSurveyResponse;
using Workvivo.Application.Features.Surveys.Dtos;
using Workvivo.Application.Features.Surveys.Queries.GetSurvey;
using Workvivo.Application.Features.Surveys.Queries.GetSurveyResults;
using Workvivo.Application.Features.Surveys.Queries.GetSurveys;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// Surveys.
///
/// There is no endpoint that returns individual responses, and there is not going to
/// be one. Results are aggregated in the query; an anonymous survey whose raw rows a
/// manager can read is not anonymous, and once people work that out the answers stop
/// being worth collecting.
/// </summary>
public class SurveysController : ApiControllersBase
{
    private readonly ISender _sender;

    public SurveysController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.Surveys.List)]
    public async Task<IActionResult> List(
        [FromQuery] bool includeClosed,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<SurveySummaryDto>>.Ok(
            await _sender.Send(new GetSurveysQuery(includeClosed), cancellationToken)));

    [HttpGet]
    [Route(RouteClass.Surveys.Detail)]
    public async Task<IActionResult> Detail(Guid surveyId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<SurveyDetailDto>.Ok(
            await _sender.Send(new GetSurveyQuery(surveyId), cancellationToken)));

    [HttpPost]
    [Route(RouteClass.Surveys.Respond)]
    public async Task<IActionResult> Respond(
        Guid surveyId,
        [FromBody] SurveyResponseRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<Guid>.Ok(await _sender.Send(
            new SubmitSurveyResponseCommand(surveyId, request.Answers ?? []), cancellationToken)));

    [HttpGet]
    [Route(RouteClass.Surveys.Results)]
    public async Task<IActionResult> Results(Guid surveyId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<SurveyResultsDto>.Ok(
            await _sender.Send(new GetSurveyResultsQuery(surveyId), cancellationToken)));
}

public sealed record SurveyResponseRequest(IReadOnlyList<SurveyAnswerInput>? Answers);
